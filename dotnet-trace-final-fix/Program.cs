using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;

namespace dotnet_trace_final_fix;

class Program
{
    const string BASE_PATH = @"C:\Users\Daniel\Desktop\github\blog\dotnet-trace-final-fix\";

    static void Main(string[] args)
    {
        string nettraceFile = Path.Combine(BASE_PATH, "dotnet-dsrouter_20240212_135920.nettrace");
        
        // --- PHASE 1: CONVERT NETTRACE TO ETLX ---
        string etlxPath = TraceLog.CreateFromEventPipeDataFile(nettraceFile);

        // threadId -> List of (timestamp, frames)
        var threadMap = new Dictionary<int, List<SampleData>>();

        // --- PHASE 2: PARSE CALL STACKS ---
        using (var traceLog = new TraceLog(etlxPath))
        {
            var eventSource = traceLog.Events.GetSource();

            // Subscribe to all trace events
            eventSource.Dynamic.All += (TraceEvent eventData) =>
            {
                var callStack = eventData.CallStack();
                if (callStack == null) return;

                int threadId = eventData.ThreadID;
                double timestamp = eventData.TimeStampRelativeMSec;

                if (!threadMap.TryGetValue(threadId, out var samples))
                {
                    samples = new List<SampleData>();
                    threadMap[threadId] = samples;
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

                samples.Add(new SampleData(timestamp, frames));
            };

            // Process the trace log
            eventSource.Process();
        }

        Console.WriteLine("Extraction complete. Saving to JSON...");

        // --- PHASE 3: DUMP TO JSON ---
        var jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Allows < and > without escaping to \u003C and \u003E
        };

        string jsonOutput = JsonSerializer.Serialize(threadMap, jsonOptions);

        File.WriteAllText(Path.Combine(BASE_PATH, "callstacks.json"), jsonOutput);

        Console.WriteLine("Writing speedscope file...");
        ConvertToSpeedscope(Path.Combine(BASE_PATH, "test.speedscope.json"), threadMap);
    }

    public static void ConvertToSpeedscope(string path, Dictionary<int, List<SampleData>> threadMap)
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

                // Calculate weight (duration in milliseconds) until next sample
                double weight = 1.0; // Fallback default
                if (i < sortedSamples.Count - 1)
                {
                    weight = sortedSamples[i + 1].TimestampMs - current.TimestampMs;
                }
                
                // Safety check: ensure positive weights
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
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        string json = JsonSerializer.Serialize(speedscopeDoc, jsonOptions);
        File.WriteAllText(path, json);
    }
}

// Named record/class ensures clean JSON serialization instead of ValueTuple formatting
public record SampleData(double TimestampMs, List<string> StackTrace);

#region Speedscope JSON Models

public class SpeedscopeDocument
{
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = string.Empty;

    public SpeedscopeShared Shared { get; set; } = new();

    public List<SpeedscopeProfile> Profiles { get; set; } = new();

    public int ActiveProfileIndex { get; set; }

    public string Exporter { get; set; } = string.Empty;
}

public class SpeedscopeShared
{
    public List<SpeedscopeFrame> Frames { get; set; } = new();
}

public class SpeedscopeFrame
{
    public string Name { get; set; } = string.Empty;
}

public class SpeedscopeProfile
{
    public string Type { get; set; } = "sampled";

    public string Name { get; set; } = string.Empty;

    public string Unit { get; set; } = "milliseconds";

    public double StartValue { get; set; }

    public double EndValue { get; set; }

    public List<List<int>> Samples { get; set; } = new();

    public List<double> Weights { get; set; } = new();
}

#endregion