// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        internal static void ConvertToFormat(TextWriter stdOut, TextWriter stdError, TraceFileFormat format, string fileToConvert, string outputFilename, string firstSpan, string spanFilter)
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
                        Convert(stdOut, format, fileToConvert, outputFilename, false, firstSpan, spanFilter);
                    }
                    // TODO: On a broken/truncated trace, the exception we get from TraceEvent is a plain System.Exception type because it gets caught and rethrown inside TraceEvent.
                    // We should probably modify TraceEvent to throw a better exception.
                    catch (Exception ex)
                    {
                        if (ex.ToString().Contains("Read past end of stream."))
                        {
                            stdOut.WriteLine("Detected a potentially broken trace. Continuing with best-efforts to convert the trace, but resulting speedscope file may contain broken stacks as a result.");
                            Convert(stdOut, format, fileToConvert, outputFilename, true, firstSpan, spanFilter);
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

        private static void Convert(TextWriter stdOut, TraceFileFormat format, string fileToConvert, string outputFilename, bool continueOnError, string firstSpan, string spanFilter)
        {
            string etlxFilePath = TraceLog.CreateFromEventPipeDataFile(fileToConvert, null, new TraceLogOptions() { ContinueOnError = continueOnError });

            // Retrieve the call stacks from the file
            Dictionary<int, List<CallstackSample>> callStacks = GetCallstacks(etlxFilePath);

            if (File.Exists(etlxFilePath))
            {
                File.Delete(etlxFilePath);
            }

            // Fix the callstacks
            FixCallStacks(callStacks, stdOut);

            FilterByFirstSpan(callStacks, firstSpan);

            FilterBySpan(callStacks, spanFilter);

            RemoveEmptyThreads(callStacks);

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
        /// Removes threads containing only empty call stacks
        /// </summary>
        private static void RemoveEmptyThreads(Dictionary<int, List<CallstackSample>> callStacks)
        {
            List<int> threads = callStacks.Keys.ToList();
            foreach (int thread in threads)
            {
                if (callStacks[thread].All(callStack => callStack.StackTrace.Count == 0))
                {
                    callStacks.Remove(thread);
                }
            }
        }

        /// <summary>
        /// For every call stack sample, only keep stack frames that match spanFilter. Supports wildcards(*).
        /// </summary>
        private static void FilterBySpan(Dictionary<int, List<CallstackSample>> callStacks, string spanFilter)
        {
            if (spanFilter == null)
            {
                return;
            }

            string regexPattern = "^" + Regex.Escape(spanFilter).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            Regex regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            foreach ((int threadId, List<CallstackSample> samples) in callStacks)
            {
                foreach (CallstackSample sample in samples)
                {
                    sample.StackTrace.RemoveAll(frame => !regex.IsMatch(frame));
                }
            }
        }

        /// <summary>
        /// For every call stack sample, remove stack frames until firstSpan is found. Supports wildcards(*).
        /// </summary>
        private static void FilterByFirstSpan(Dictionary<int, List<CallstackSample>> callStacks, string firstSpan)
        {
            if (firstSpan == null)
            {
                return;
            }

            string regexPattern = "^" + Regex.Escape(firstSpan).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            Regex regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            foreach ((int threadId, List<CallstackSample> samples) in callStacks)
            {
                foreach (CallstackSample sample in samples)
                {
                    List<string> stackTrace = sample.StackTrace;
                    int matchIndex = stackTrace.FindIndex(frame => regex.IsMatch(frame));

                    if (matchIndex > 0)
                    {
                        // If there's a match, clear everything before the match
                        stackTrace.RemoveRange(0, matchIndex);
                    }
                    else if (matchIndex == -1)
                    {
                        // If no match found, clear the whole stack trace
                        stackTrace.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the call stack samples from the given etlx file.
        /// </summary>
        private static Dictionary<int, List<CallstackSample>> GetCallstacks(string etlxFilePath)
        {
            // threadId -> List of (timestamp, frames)
            Dictionary<int, List<CallstackSample>> result = new();

            using (TraceLog eventLog = new(etlxFilePath))
            {
                TraceLogEventSource eventSource = eventLog.Events.GetSource();

                // Subscribe to all trace events
                eventSource.Dynamic.All += (TraceEvent eventData) =>
                {
                    TraceCallStack callStack = eventData.CallStack();
                    if (callStack == null)
                    {
                        return;
                    }

                    int threadId = eventData.ThreadID;
                    double timestamp = eventData.TimeStampRelativeMSec;

                    if (!result.TryGetValue(threadId, out List<CallstackSample>? samples))
                    {
                        samples = new List<CallstackSample>();
                        result[threadId] = samples;
                    }

                    List<string> frames = new();
                    TraceCallStack currentFrame = callStack;

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
        public static void FixCallStacks(Dictionary<int, List<CallstackSample>> threadMap, TextWriter stdOut)
        {
            int sampleCount = threadMap.Values.Select(samples => samples.Count).Sum();
            List<(int ThreadId, int SampleIndex, double SampleTimestamp)> deletedSamples = new();

            foreach ((int threadId, List<CallstackSample> samples) in threadMap)
            {
                for (int sampleIndex = 1; sampleIndex < samples.Count; sampleIndex++)
                {
                    CallstackSample previous = samples[sampleIndex - 1];
                    CallstackSample current = samples[sampleIndex];

                    if (current.StackTrace.Count < 100)
                    {
                        // We aren't exceeding the stack frame limit here,
                        // therefore we don't need to fix anything.
                        continue;
                    }

                    if (previous.StackTrace[0] != current.StackTrace[0])
                    {
                        // Get list of stack traces from `previous` that matches the `current` base trace
                        List<int> candidates = new();
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
                            deletedSamples.Add((threadId, sampleIndex, current.TimestampMs));
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

            PrintDeletedSampleInfo(sampleCount, deletedSamples, stdOut);
        }

        /// <summary>
        /// Writes a diagnostic summary of deleted call stack samples to the specified output stream.
        /// </summary>
        public static void PrintDeletedSampleInfo(
            int totalSampleCount,
            List<(int ThreadId, int SampleIndex, double SampleTimestamp)> deletedSamples,
            TextWriter stdOut
        )
        {
            int deletedCount = deletedSamples.Count;
            double percentage = totalSampleCount > 0 ? (double)deletedCount / totalSampleCount * 100.0 : 0.0;

            if (deletedCount == 0)
            {
                return;
            }

            // 1. Print summary line
            stdOut.WriteLine($"{deletedCount} samples out of {totalSampleCount} could not be recovered and have been deleted ({percentage:F3}%)");

            // 2. Group deleted samples by thread
            IOrderedEnumerable<IGrouping<int, (int ThreadId, int SampleIndex, double SampleTimestamp)>> groupedByThread = deletedSamples
                .GroupBy(s => s.ThreadId)
                .OrderBy(g => g.Key);

            foreach (IGrouping<int, (int ThreadId, int SampleIndex, double SampleTimestamp)> threadGroup in groupedByThread)
            {
                int threadId = threadGroup.Key;
                List<(int ThreadId, int SampleIndex, double SampleTimestamp)> samples = threadGroup.ToList();
                int count = samples.Count;

                List<string> ranges = new();
                List<(int ThreadId, int SampleIndex, double SampleTimestamp)> currentRun = new();

                // 3. Cluster contiguous deleted samples into ranges
                foreach ((int ThreadId, int SampleIndex, double SampleTimestamp) sample in samples)
                {
                    if (currentRun.Count == 0)
                    {
                        currentRun.Add(sample);
                    }
                    else
                    {
                        (int ThreadId, int SampleIndex, double SampleTimestamp) prev = currentRun[^1];

                        // Samples are contiguous if deleted at the same index
                        bool isContiguous = sample.SampleIndex == prev.SampleIndex;

                        if (isContiguous)
                        {
                            currentRun.Add(sample);
                        }
                        else
                        {
                            ranges.Add(FormatRun(currentRun));
                            currentRun.Clear();
                            currentRun.Add(sample);
                        }
                    }
                }

                if (currentRun.Count > 0)
                {
                    ranges.Add(FormatRun(currentRun));
                }

                // 4. Output thread summary line
                string formattedRanges = string.Join(", ", ranges);
                stdOut.WriteLine($"    Thread {threadId} ({count}): {formattedRanges}");
            }
        }

        /// <summary>
        /// Formats a single run of deleted samples into either a single timestamp ("12.034s")
        /// or a range ("7.678s-7.682s").
        /// </summary>
        private static string FormatRun(List<(int ThreadId, int SampleIndex, double SampleTimestamp)> run)
        {
            double startSec = run[0].SampleTimestamp / 1000.0;
            string startStr = $"{startSec:F3}s";

            if (run.Count == 1)
            {
                return startStr;
            }

            double endSec = run[^1].SampleTimestamp / 1000.0;
            string endStr = $"{endSec:F3}s";

            // If start and end round to the exact same millisecond string, show as a single timestamp
            return startStr == endStr ? startStr : $"{startStr}-{endStr}";
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
