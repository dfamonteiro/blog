// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Microsoft.Diagnostics.Tools.Trace.TraceFileFormatConverter;

namespace Microsoft.Diagnostics.Tools.Trace;

/// <summary>
/// Provides functionality to export sampled stack trace data to the Chromium Trace Event format,
/// viewable in chrome://tracing or ui.perfetto.dev.
/// </summary>
public static class ChromiumWriter
{
    /// <summary>
    /// Converts a dictionary of sampled call stacks into a Chromium Trace Event JSON file.
    /// </summary>
    /// <param name="path">The file path where the resulting JSON document will be written.</param>
    /// <param name="threadMap">A dictionary mapping thread IDs to chronologically ordered lists of stack samples.</param>
    internal static void Convert(string path, Dictionary<int, List<CallstackSample>> threadMap)
    {
        List<ChromiumTraceEvent> events = new();
        int fakePid = 1; // Group everything under a single logical process

        foreach ((int threadId, List<CallstackSample> samples) in threadMap)
        {
            if (samples.Count == 0)
            {
                continue;
            }

            // 1. Emit a Metadata event to label the thread properly in the viewer
            events.Add(new ChromiumTraceEvent
            {
                Name = "thread_name",
                Phase = "M", // Metadata phase
                ProcessId = fakePid,
                ThreadId = threadId,
                Args = new Dictionary<string, object> { { "name", "Thread" } }
            });

            List<CallstackSample> sortedSamples = samples.OrderBy(s => s.TimestampMs).ToList();

            // Tracks frames currently "active" in the timeline: (MethodName, StartTimestampMs)
            List<(string Name, double StartTimeMs)> activeStack = new();

            // 2. Synthesize B/E flamegraph slices by diffing consecutive stack samples
            for (int i = 0; i < sortedSamples.Count; i++)
            {
                CallstackSample currentSample = sortedSamples[i];
                List<string> currentStack = currentSample.StackTrace;
                double currentTsMs = currentSample.TimestampMs;

                // Find the index where the current stack diverges from the active tracked stack
                int matchCount = 0;
                while (matchCount < activeStack.Count &&
                       matchCount < currentStack.Count &&
                       activeStack[matchCount].Name == currentStack[matchCount])
                {
                    matchCount++;
                }

                // Pop frames that are no longer active and emit them as 'Complete' events
                for (int j = activeStack.Count - 1; j >= matchCount; j--)
                {
                    (string Name, double StartTimeMs) frame = activeStack[j];
                    double durationMs = currentTsMs - frame.StartTimeMs;

                    events.Add(new ChromiumTraceEvent
                    {
                        Name = frame.Name,
                        Category = "dotnet",
                        Phase = "X", // Complete event (combines Begin and End)
                        TimestampUs = frame.StartTimeMs * 1000.0, // Chromium expects microseconds
                        DurationUs = Math.Max(durationMs * 1000.0, 1.0), // Ensure at least 1us duration to render
                        ProcessId = fakePid,
                        ThreadId = threadId
                    });

                    activeStack.RemoveAt(j);
                }

                // Push new frames onto the active stack
                for (int j = matchCount; j < currentStack.Count; j++)
                {
                    activeStack.Add((currentStack[j], currentTsMs));
                }
            }

            // 3. Flush any remaining active frames after the final sample
            if (activeStack.Count > 0)
            {
                // Assign a 1ms fallback duration for the final snapshot so it doesn't have 0 width
                double finalTsMs = sortedSamples.Last().TimestampMs + 1.0;

                for (int j = activeStack.Count - 1; j >= 0; j--)
                {
                    (string Name, double StartTimeMs) frame = activeStack[j];
                    double durationMs = finalTsMs - frame.StartTimeMs;

                    events.Add(new ChromiumTraceEvent
                    {
                        Name = frame.Name,
                        Category = "dotnet",
                        Phase = "X",
                        TimestampUs = frame.StartTimeMs * 1000.0,
                        DurationUs = Math.Max(durationMs * 1000.0, 1.0),
                        ProcessId = fakePid,
                        ThreadId = threadId
                    });
                }
            }
        }

        ChromiumTraceDocument document = new() { TraceEvents = events };

        // 4. Serialize to disk
        JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        string json = JsonSerializer.Serialize(document, jsonOptions);
        File.WriteAllText(path, json);
    }

    #region Chromium JSON Models

    /// <summary>
    /// Represents the root object of a Chromium Trace Event JSON document.
    /// </summary>
    public class ChromiumTraceDocument
    {
        [JsonPropertyName("traceEvents")]
        public List<ChromiumTraceEvent> TraceEvents { get; set; } = new();
    }

    /// <summary>
    /// Represents a single trace event in the Chromium format.
    /// </summary>
    public class ChromiumTraceEvent
    {
        /// <summary>
        /// The name of the event, as displayed in the trace viewer.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The event category. Helps with filtering in the viewer.
        /// </summary>
        [JsonPropertyName("cat")]
        public string? Category { get; set; }

        /// <summary>
        /// The event phase. Common values: "X" (Complete), "B" (Begin), "E" (End), "M" (Metadata).
        /// </summary>
        [JsonPropertyName("ph")]
        public string Phase { get; set; } = string.Empty;

        /// <summary>
        /// Tracing clock timestamp of the event in microseconds.
        /// </summary>
        [JsonPropertyName("ts")]
        public double TimestampUs { get; set; }

        /// <summary>
        /// The duration of the event in microseconds (Required for 'X' Complete events).
        /// </summary>
        [JsonPropertyName("dur")]
        public double? DurationUs { get; set; }

        /// <summary>
        /// The process ID that generated the event.
        /// </summary>
        [JsonPropertyName("pid")]
        public int ProcessId { get; set; }

        /// <summary>
        /// The thread ID that generated the event.
        /// </summary>
        [JsonPropertyName("tid")]
        public int ThreadId { get; set; }

        /// <summary>
        /// Optional arguments associated with the event (used for Metadata events like thread naming).
        /// </summary>
        [JsonPropertyName("args")]
        public Dictionary<string, object>? Args { get; set; }
    }

    #endregion
}
