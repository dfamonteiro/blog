// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Stacks;
using Microsoft.Diagnostics.Tracing.Stacks.Formats;

namespace Microsoft.Diagnostics.Tools.Trace
{
    internal enum TraceFileFormat { NetTrace = 1, Speedscope, Chromium };

    internal static class TraceFileFormatConverter
    {
        private static readonly IReadOnlyDictionary<TraceFileFormat, string> TraceFileFormatExtensions = new Dictionary<TraceFileFormat, string>() {
            { TraceFileFormat.NetTrace,     "nettrace" },
            { TraceFileFormat.Speedscope,   "speedscope.json" },
            { TraceFileFormat.Chromium,     "chromium.json" }
        };

        internal static string GetConvertedFilename(string fileToConvert, string outputfile, TraceFileFormat format)
        {
            if (string.IsNullOrWhiteSpace(outputfile))
            {
                outputfile = fileToConvert;
            }

            return Path.ChangeExtension(outputfile, TraceFileFormatExtensions[format]);
        }

        internal static void ConvertToFormat(TextWriter stdOut, TextWriter stdError, TraceFileFormat format, string fileToConvert, string outputFilename)
        {
            switch (format)
            {
                case TraceFileFormat.NetTrace:
                    break;
                case TraceFileFormat.Speedscope:
                case TraceFileFormat.Chromium:
                    stdOut.WriteLine($"Processing trace data file '{fileToConvert}' to create a new {format} file '{outputFilename}'.");
                    try
                    {
                        Convert(format, fileToConvert, outputFilename);
                    }
                    // TODO: On a broken/truncated trace, the exception we get from TraceEvent is a plain System.Exception type because it gets caught and rethrown inside TraceEvent.
                    // We should probably modify TraceEvent to throw a better exception.
                    catch (Exception ex)
                    {
                        if (ex.ToString().Contains("Read past end of stream."))
                        {
                            stdOut.WriteLine("Detected a potentially broken trace. Continuing with best-efforts to convert the trace, but resulting speedscope file may contain broken stacks as a result.");
                            Convert(format, fileToConvert, outputFilename, continueOnError: true);
                        }
                        else
                        {
                            stdError.WriteLine(ex.ToString());
                        }
                    }
                    break;
                default:
                    // Validation happened way before this, so we shoud never reach this...
                    throw new Exception($"Invalid TraceFileFormat \"{format}\"");
            }
            stdOut.WriteLine("Conversion complete");
        }

        private static void Convert(TraceFileFormat format, string fileToConvert, string outputFilename, bool continueOnError = false)
        {
            string etlxFilePath = TraceLog.CreateFromEventPipeDataFile(fileToConvert, null, new TraceLogOptions() { ContinueOnError = continueOnError });
            
            // Retrieve the call stacks from the file
            Dictionary<int, List<CallstackSample>> callStacks = GetCallstacks(etlxFilePath);

            // Fix the callstacks
            FixCallStacks(callStacks);

            if (File.Exists(etlxFilePath))
            {
                File.Delete(etlxFilePath);
            }

            switch (format)
            {
                case TraceFileFormat.Speedscope:
                    SpeedscopeWriter.Convert(outputFilename, callStacks);
                    break;
                case TraceFileFormat.Chromium:
                    ChromiumWriter.Convert(outputFilename, callStacks);
                    break;
                default:
                    // we should never get here
                    throw new Exception($"Invalid TraceFileFormat \"{format}\"");
            }
        }

        /// <summary>
        /// Retrieves the call stack samples from the given etlx file.
        /// </summary>
        private static Dictionary<int, List<CallstackSample>> GetCallstacks(string etlxFilePath)
        {
            // threadId -> List of (timestamp, frames)
            var result = new Dictionary<int, List<CallstackSample>>();

            using (TraceLog eventLog = new(etlxFilePath))
            {
                var eventSource = eventLog.Events.GetSource();

                // Subscribe to all trace events
                eventSource.Dynamic.All += (TraceEvent eventData) =>
                {
                    var callStack = eventData.CallStack();
                    if (callStack == null) return;

                    int threadId = eventData.ThreadID;
                    double timestamp = eventData.TimeStampRelativeMSec;

                    if (!result.TryGetValue(threadId, out var samples))
                    {
                        samples = new List<CallstackSample>();
                        result[threadId] = samples;
                    }

                    var frames = new List<string>();
                    var currentFrame = callStack;

                    while (currentFrame != null)
                    {
                        string methodName = currentFrame.CodeAddress.Method?.FullMethodName ?? "Native/Unresolved";

                        frames.Add(methodName);
                        currentFrame = currentFrame.Caller;
                    }
                    frames.Reverse(); // Make sure the "root frames" appear first

                    samples.Add(new CallstackSample(timestamp, frames));
                };

                // Process the trace log
                eventSource.Process();
            }

            return result;
        }

        /// <summary>
        /// Fixes the call stacks truncated by the EventPipe's 100 stack frame limit.
        /// </summary>
        public static void FixCallStacks(Dictionary<int, List<CallstackSample>> threadMap)
        {
            foreach ((int threadId, var samples) in threadMap)
            {
                for (int sampleIndex = 1; sampleIndex < samples.Count; sampleIndex++)
                {
                    var previous = samples[sampleIndex - 1];
                    var current = samples[sampleIndex];

                    if (current.StackTrace.Count < 100)
                    {
                        // We aren't exceeding the stack frame limit here,
                        // therefore we don't need to fix anything.
                        continue;
                    }

                    if (previous.StackTrace[0] != current.StackTrace[0])
                    {
                        // Get list of stack traces from `previous` that matches the `current` base trace
                        var candidates = new List<int>();
                        for (int i = 0; i < previous.StackTrace.Count; i++)
                        {
                            if (previous.StackTrace[i] == current.StackTrace[0])
                            {
                                candidates.Add(i);
                            }
                        }

                        if (candidates.Count == 0)
                        {
                            // If there's no matching stack frame from `previous`, delete this sample.
                            samples.RemoveAt(sampleIndex);
                            sampleIndex--;
                            continue;
                        }

                        // Select the best candidate match from the list of candidates.
                        // The best candidate match is the one with the most call stack overlap
                        // between `previous` and `current`
                        (int Index, int Overlap) bestCandidate = (-1, -1);
                        foreach (int candidateIndex in candidates)
                        {
                            int overlap = 0;
                            while (previous.StackTrace[candidateIndex + overlap] == current.StackTrace[overlap])
                            {
                                // For as long as the stack frames keep matching, keep increasing the overlap
                                overlap++;

                                if (candidateIndex + overlap == previous.StackTrace.Count || overlap == current.StackTrace.Count)
                                {
                                    // We have an index out of bounds, so we have to stop
                                    break;
                                }
                            }

                            if (overlap > bestCandidate.Overlap)
                            {
                                bestCandidate = (candidateIndex, overlap);
                            }
                        }
                        
                        // Insert the missing stack frames
                        for (int prevIndex = 0; prevIndex < bestCandidate.Index; prevIndex++)
                        {
                            current.StackTrace.Insert(prevIndex, previous.StackTrace[prevIndex]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Represents an internal intermediate representation of a recorded stack trace sample 
        /// before it is mapped to the final format.
        /// </summary>
        /// <param name="TimestampMs">The relative timestamp of the sample in milliseconds.</param>
        /// <param name="StackTrace">The ordered list of method names, from root to leaf.</param>
        public record CallstackSample(double TimestampMs, List<string> StackTrace);
    }
}