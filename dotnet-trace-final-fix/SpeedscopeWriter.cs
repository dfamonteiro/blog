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
/// Provides functionality to export sampled stack trace data to the Speedscope JSON format.
/// </summary>
public static class SpeedscopeWriter
{
    /// <summary>
    /// Converts a dictionary of sampled call stacks into a Speedscope-compatible JSON file.
    /// </summary>
    /// <param name="path">The file path where the resulting Speedscope JSON document will be written.</param>
    /// <param name="threadMap">A dictionary mapping thread IDs to chronologically ordered lists of stack samples.</param>
    internal static void Convert(string path, Dictionary<int, List<CallstackSample>> threadMap)
    {
        // 1. Build a shared frame registry to intern method names (string -> frameIndex)
        var sharedFrames = new List<SpeedscopeFrame>();
        var frameIndexMap = new Dictionary<string, int>();

        int GetFrameIndex(string frameName)
        {
            if (!frameIndexMap.TryGetValue(frameName, out int index))
            {
                index = sharedFrames.Count;
                sharedFrames.Add(new SpeedscopeFrame { Name = frameName });
                frameIndexMap[frameName] = index;
            }
            return index;
        }

        var profiles = new List<SpeedscopeProfile>();

        // 2. Convert each thread into a Speedscope sampled profile
        foreach (var (threadId, samples) in threadMap)
        {
            if (samples.Count == 0) continue;

            // Ensure samples are sorted chronologically
            var sortedSamples = samples.OrderBy(s => s.TimestampMs).ToList();

            var sampleStackIndices = new List<List<int>>();
            var weights = new List<double>();

            for (int i = 0; i < sortedSamples.Count; i++)
            {
                var current = sortedSamples[i];

                // Convert stack frame strings into index arrays based on the shared frames
                // Note: Speedscope expects stacks root-first (index 0 = root, last index = leaf)
                var frameIndices = current.StackTrace
                    .Select(frameName => GetFrameIndex(frameName))
                    .ToList();

                sampleStackIndices.Add(frameIndices);

                // Calculate weight (duration in milliseconds) until the next sample
                double weight = 1.0; // Fallback default
                if (i < sortedSamples.Count - 1)
                {
                    weight = sortedSamples[i + 1].TimestampMs - current.TimestampMs;
                }
                
                // Safety check: ensure positive weights, as Speedscope renderer requires > 0
                weights.Add(weight > 0 ? weight : 0.001);
            }

            double startValue = sortedSamples.First().TimestampMs;
            double endValue = sortedSamples.Last().TimestampMs;

            profiles.Add(new SpeedscopeProfile
            {
                Type = "sampled",
                Name = $"Thread {threadId}",
                Unit = "milliseconds",
                StartValue = startValue,
                EndValue = endValue,
                Samples = sampleStackIndices,
                Weights = weights
            });
        }

        // 3. Assemble the top-level Speedscope document
        var speedscopeDoc = new SpeedscopeDocument
        {
            Schema = "https://www.speedscope.app/file-format-schema.json",
            Shared = new SpeedscopeShared { Frames = sharedFrames },
            Profiles = profiles,
            ActiveProfileIndex = 0,
            Exporter = "dotnet-trace-final-fix"
        };

        // 4. Serialize to disk with relaxed escaping
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // Prevent escaping < and > in generic types
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        string json = JsonSerializer.Serialize(speedscopeDoc, jsonOptions);
        File.WriteAllText(path, json);
    }

    #region Speedscope JSON Models

    /// <summary>
    /// Represents the root object of a Speedscope JSON document.
    /// </summary>
    public class SpeedscopeDocument
    {
        /// <summary>
        /// The URI of the Speedscope JSON schema. 
        /// Required by the Speedscope viewer to validate the file structure.
        /// </summary>
        [JsonPropertyName("$schema")]
        public string Schema { get; set; } = string.Empty;

        /// <summary>
        /// Contains data shared across all profiles in the document, such as the deduplicated string pool for frame names.
        /// </summary>
        public SpeedscopeShared Shared { get; set; } = new();

        /// <summary>
        /// A list of profile timelines, typically representing individual threads.
        /// </summary>
        public List<SpeedscopeProfile> Profiles { get; set; } = new();

        /// <summary>
        /// The index of the profile in the <see cref="Profiles"/> list that should be displayed first when opening the file.
        /// </summary>
        public int ActiveProfileIndex { get; set; }

        /// <summary>
        /// The name of the tool or process that generated this Speedscope document.
        /// </summary>
        public string Exporter { get; set; } = string.Empty;
    }

    /// <summary>
    /// Contains data shared across all profiles to minimize JSON file size by avoiding string duplication.
    /// </summary>
    public class SpeedscopeShared
    {
        /// <summary>
        /// A deduplicated list of all stack frames referenced across all profiles.
        /// Profiles reference these frames by their integer index in this array.
        /// </summary>
        public List<SpeedscopeFrame> Frames { get; set; } = new();
    }

    /// <summary>
    /// Represents a single function or method in a stack trace.
    /// </summary>
    public class SpeedscopeFrame
    {
        /// <summary>
        /// The name of the method, function, or frame (e.g., "System.String.Concat").
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a single timeline of execution, such as a specific thread's call stacks over time.
    /// </summary>
    public class SpeedscopeProfile
    {
        /// <summary>
        /// The type of profile. For sampling profilers mapping periodic snapshots, this must be "sampled".
        /// </summary>
        public string Type { get; set; } = "sampled";

        /// <summary>
        /// The display name of the profile (e.g., "Thread 1234").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The unit of measurement for the timeline and weights. 
        /// Supported values are "milliseconds", "microseconds", "nanoseconds", "hz", or "bytes".
        /// </summary>
        public string Unit { get; set; } = "milliseconds";

        /// <summary>
        /// The starting timestamp or offset for this profile timeline.
        /// </summary>
        public double StartValue { get; set; }

        /// <summary>
        /// The ending timestamp or offset for this profile timeline.
        /// </summary>
        public double EndValue { get; set; }

        /// <summary>
        /// A chronological list of sampled stack traces. 
        /// Each inner list contains integers that index into the <see cref="SpeedscopeShared.Frames"/> array.
        /// The integer sequences must be ordered root-first (index 0 is the root/entrypoint, the last item is the leaf).
        /// </summary>
        public List<List<int>> Samples { get; set; } = new();

        /// <summary>
        /// The duration or weight of each sample in the <see cref="Samples"/> list.
        /// The count of this list must exactly match the count of the Samples list.
        /// </summary>
        public List<double> Weights { get; set; } = new();
    }

    #endregion
}