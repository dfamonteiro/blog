using System.Text.Encodings.Web;
using System.Text.Json;
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
    }
}

// Named record/class ensures clean JSON serialization instead of ValueTuple formatting
public record SampleData(double TimestampMs, List<string> StackTrace);