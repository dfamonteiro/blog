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
using System.Text.Json.Nodes;

// This code is textbook definition of throwaway code. Proceed with caution

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

    // 1. Read and parse the existing file
    string existingJson = File.ReadAllText(chromiumTraceFile);
    var root = JsonNode.Parse(existingJson)?.AsObject();
    
    if (root == null || !root.ContainsKey("traceEvents"))
    {
        // Fallback: If file is a raw array or malformed, handle accordingly
        throw new InvalidDataException("The existing file is not a valid Chromium trace object.");
    }

    JsonArray eventsArray = root["traceEvents"]!.AsArray();

    // 2. Convert and Append SqlTraces
    foreach (var trace in sqlTraces)
    {
        if (!trace.Start.HasValue || !trace.End.HasValue) continue;

        string asyncId = $"0x{trace.ObjectId:X}";
        
        // Add Begin event
        eventsArray.Add(new {
            name = "SQL Query",
            cat = "sql",
            ph = "b",
            id = asyncId,
            ts = (long)(trace.Start.Value * 1000),
            pid = 1,
            tid = 1,
            args = new { sql = trace.SqlText }
        });

        // Add End event
        eventsArray.Add(new {
            name = "SQL Query",
            cat = "sql",
            ph = "e",
            id = asyncId,
            ts = (long)(trace.End.Value * 1000),
            pid = 1,
            tid = 1
        });
    }

    // 3. Write the merged data back
    using var writer = new Utf8JsonWriter(File.Create(chromiumTraceFileWithSql), new JsonWriterOptions { Indented = true });
    root.WriteTo(writer);
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