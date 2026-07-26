using System.Text.Json;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;

namespace dotnet_trace_final_fix;

class Program
{
    const string BASE_PATH = "C:\\Users\\Daniel\\Desktop\\github\\blog\\dotnet-trace-final-fix\\";

    static void Main(string[] args)
    {
        // --- PHASE 2: RESOLVE SYMBOLS (TWO-PASS PARSE) ---
        // This creates a .etlx file alongside your .nettrace file. 
        // It reads the Rundown at the end of the file and maps it to the events.
        string etlxPath = TraceLog.CreateFromEventPipeDataFile(BASE_PATH + "dotnet-dsrouter_20240212_135920.nettrace");


        // --- PHASE 3: EXTRACT THE DICTIONARY ---
        // threadId -> { timestamp -> [callstacks] }
        var threadMap = new Dictionary<int, Dictionary<double, List<string>>>();

        using (var traceLog = new TraceLog(etlxPath))
        {
            var eventSource = traceLog.Events.GetSource();

            eventSource.Dynamic.All += (TraceEvent eventData) =>
            {
                var callStack = eventData.CallStack();
                if (callStack == null) return;

                int threadId = eventData.ThreadID;
                double timestamp = eventData.TimeStampRelativeMSec;

                if (!threadMap.ContainsKey(threadId))
                {
                    threadMap[threadId] = new Dictionary<double, List<string>>();
                }

                var frames = new List<string>();
                var currentFrame = callStack;

                // Walk the stack from top to bottom
                while (currentFrame != null)
                {
                    string methodName = currentFrame.CodeAddress.Method?.FullMethodName ?? "Native/Unresolved";
                    frames.Add(methodName);

                    currentFrame = currentFrame.Caller;
                }

                threadMap[threadId][timestamp] = frames;
            };

            // Process the entire ETLX file synchronously
            eventSource.Process();
        }

        Console.WriteLine("Extraction complete. Saving to JSON...");

        // --- PHASE 4: DUMP TO JSON ---
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        string jsonOutput = JsonSerializer.Serialize(threadMap, jsonOptions);

        File.WriteAllText(BASE_PATH + "callstacks.json", jsonOutput);
        Console.WriteLine("Done! Check callstacks.json");
    }
}
