using System.CommandLine;
using System.CommandLine.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Stacks;
using Microsoft.Diagnostics.Tracing.Stacks.Formats;
using Microsoft.Diagnostics.Tracing.Parsers;
using System.Text.Json;

// This is the textbook definition of throwaway code. Proceed with caution

string fileName = @"C:\Users\Daniel\Desktop\github\blog\dotnet-trace\NetTraceConverter\dotnet_20260121_100330";
// PrintSqlEvents(fileName);
AddSqlEventsToChromiumTraceFile($"{fileName}.nettrace", $"{fileName}.chromium.json", $"{fileName}_with_sql.chromium.json");

// Option<FileInfo> fileOption = new("--file")
// {
//     Description = "The file to read and display on the console"
// };

// RootCommand rootCommand = new("Sample app for System.CommandLine");
// rootCommand.Options.Add(fileOption);

// ParseResult parseResult = rootCommand.Parse(args);
// if (parseResult.Errors.Count == 0 && parseResult.GetValue(fileOption) is FileInfo parsedFile)
// {
    
// }

void AddSqlEventsToChromiumTraceFile(string nettraceFile, string chromiumTraceFile, string chromiumTraceFileWithSql)
{
    List<SqlTrace> sqlTraces = ParseEvents(nettraceFile);
    var traceEvents = new List<object>();

    foreach (var trace in sqlTraces)
    {
        // Skip incomplete traces
        if (!trace.Start.HasValue || !trace.End.HasValue) continue;

        // Chromium expects timestamps in microseconds (us)
        // Assuming your input is in milliseconds, we multiply by 1000
        long startUs = (long)(trace.Start.Value * 1000);
        long endUs = (long)(trace.End.Value * 1000);
        
        // Use the ObjectId as the correlation ID for the async slice
        string asyncId = $"0x{trace.ObjectId:X}";

        // 1. Create the 'Begin' Event (ph: "b")
        traceEvents.Add(new
        {
            name = "SQL Query",
            cat = "sql",
            ph = "b",
            id = asyncId,
            ts = startUs,
            pid = 1, // Process ID (arbitrary)
            tid = 1, // Thread ID (arbitrary)
            args = new { sql = trace.SqlText }
        });

        // 2. Create the 'End' Event (ph: "e")
        traceEvents.Add(new
        {
            name = "SQL Query",
            cat = "sql",
            ph = "e",
            id = asyncId,
            ts = endUs,
            pid = 1,
            tid = 1
        });
    }

    // Write the JSON array to the file
    // Chromium can wrap these in a "traceEvents" object or just use a raw array
    string jsonString = JsonSerializer.Serialize(new { traceEvents = traceEvents });
    File.WriteAllText(chromiumTraceFile, jsonString);
}

List<SqlTrace> ParseEvents(string nettraceFile)
{
    // Maps object IDs to traces
    Dictionary<int, SqlTrace> sqlTraces = new();

    string etlxFilePath = TraceLog.CreateFromEventPipeDataFile(nettraceFile, null, new TraceLogOptions() { ContinueOnError = false });
    using (TraceLog eventLog = new(etlxFilePath))
    {
        foreach (var traceEvent in eventLog.Events.Where(e => e.ProviderName == "Microsoft.Data.SqlClient.EventSource"))
        {
            // Parse the event
            string eventName = traceEvent.EventName;
            double timestamp = traceEvent.TimeStampRelativeMSec;
            int? objectId = null;
            string? sqlText = null;

            for (int i = 0; i < traceEvent.PayloadNames.Length; i++)
            {
                var name = traceEvent.PayloadNames[i];
                var value = traceEvent.PayloadValue(i);

                if (name == "sqlBatch" || name == "commandText")
                {
                    sqlText = value as string;
                }
                else if (name == "objectId")
                {
                    objectId = (int)value;
                }
            }

            if (objectId == null)
            {
                throw new Exception("objectId is null");
            }

            if (!sqlTraces.ContainsKey((int)objectId))
            {
                SqlTrace trace = new SqlTrace()
                {
                    Start = (eventName == "BeginExecute/Start") ? timestamp : null,
                    End   = (eventName == "EndExecute/Stop")    ? timestamp : null,
                    ObjectId = (int)objectId,
                    SqlText = sqlText,
                };
                sqlTraces[trace.ObjectId] = trace;
            }
            // The trace already exists
            else if (eventName == "BeginExecute/Start")
            {
                SqlTrace trace = sqlTraces[(int)objectId];

                // Sanity check
                if (trace.Start != null)
                {
                    throw new Exception("Start is already set!");
                }

                trace.Start = timestamp;
                trace.SqlText = sqlText;
                sqlTraces[(int)objectId] = trace;
            }
            else if (eventName == "EndExecute/Stop")
            {
                SqlTrace trace = sqlTraces[(int)objectId];

                // Sanity check
                if (trace.End != null)
                {
                    throw new Exception("End is already set!");
                }

                trace.End = timestamp;
                sqlTraces[(int)objectId] = trace;
            }
        }
    }

    if (File.Exists(etlxFilePath))
    {
        File.Delete(etlxFilePath);
    }

    return sqlTraces
        .Values
        .Where(trace => trace.Start != null)
        .Where(trace => trace.End   != null)
        .ToList();
}

void PrintSqlEvents(string fileName)
{
    string etlxFilePath = TraceLog.CreateFromEventPipeDataFile(fileName, null, new TraceLogOptions() { ContinueOnError = false });
    using (TraceLog eventLog = new(etlxFilePath))
    {
        foreach (var traceEvent in eventLog.Events.Where(e => e.ProviderName == "Microsoft.Data.SqlClient.EventSource"))
        {
            // 1. Basic Event Info
            Console.WriteLine($"[{traceEvent.TimeStampRelativeMSec:N2}ms] Event: {traceEvent.EventName}");

            // 2. Iterate through the payload fields (where the 'real' data lives)
            for (int i = 0; i < traceEvent.PayloadNames.Length; i++)
            {
                var name = traceEvent.PayloadNames[i];
                var value = traceEvent.PayloadValue(i);

                // Filter out noise or specifically highlight SQL commands
                if (name == "sqlBatch" || name == "commandText")
                {
                    Console.WriteLine($"   >>> SQL: {value}");
                }
                else
                {
                    Console.WriteLine($"   {name}: {value}");
                }
            }
            Console.WriteLine(new string('-', 30));
        }
    }

    if (File.Exists(etlxFilePath))
    {
        File.Delete(etlxFilePath);
    }
}

struct SqlTrace
{
    public double? Start;
    public double? End;

    public int ObjectId;
    public string? SqlText;
};