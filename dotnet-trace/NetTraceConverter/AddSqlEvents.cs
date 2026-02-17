#:package System.CommandLine@2.0.2
#:package Microsoft.Diagnostics.Tracing.TraceEvent@3.1.29

using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Diagnostics.Tracing.Etlx;

Option<FileInfo> nettrace = new("--nettrace-file")
{
    Description = "The path to the .NET trace file (.nettrace)."
};
Option<FileInfo> chromiumTraceFile = new("--chromium-trace-file")
{
    Description = "The path to the .chromium.json trace file to be modified."
};
Option<FileInfo> output = new("--output")
{
    Description = "The path where the new Chromium trace file with SQL events will be written."
};

var rootCommand = new RootCommand("A tool to add SQL events from .NET traces to Chromium trace files.");

rootCommand.Options.Add(nettrace);
rootCommand.Options.Add(chromiumTraceFile);
rootCommand.Options.Add(output);

ParseResult parseResult = rootCommand.Parse(args);

bool allOptionsAreSet = parseResult.GetValue(nettrace) is FileInfo && 
    parseResult.GetValue(chromiumTraceFile) is FileInfo && 
    parseResult.GetValue(output) is FileInfo;

if (parseResult.Errors.Count == 0 && allOptionsAreSet)
{
    if (!AddSqlEventsToChromiumTraceFile(
            parseResult.GetValue(nettrace)!.FullName,
            parseResult.GetValue(chromiumTraceFile)!.FullName,
            parseResult.GetValue(output)!.FullName
        ))
    {
        return 1; // Indicate failure
    }
}
return 0;


/// <summary>
/// Adds SQL events parsed from a .NET trace file to an existing Chromium trace file.
/// </summary>
/// <param name="nettraceFile">The path to the .NET trace file (.nettrace).</param>
/// <param name="chromiumTraceFile">The path to the existing Chromium trace file (.json).</param>
/// <param name="chromiumTraceFileWithSql">The path where the new Chromium trace file with SQL events will be written.</param>
bool AddSqlEventsToChromiumTraceFile(string nettraceFile, string chromiumTraceFile, string chromiumTraceFileWithSql)
{
    if (!File.Exists(nettraceFile))
    {
        Console.Error.WriteLine($"Error: Nettrace file not found at '{nettraceFile}'");
        return false;
    }

    if (!File.Exists(chromiumTraceFile))
    {
        Console.Error.WriteLine($"Error: Chromium trace file not found at '{chromiumTraceFile}'");
        return false;
    }

    List<SqlTrace>? sqlTraces = ParseEvents(nettraceFile);
    if (sqlTraces == null)
    {
        // Error message already printed by ParseEvents
        return false;
    }

    // 1. Read and parse the existing file
    string existingJson;
    try
    {
        existingJson = File.ReadAllText(chromiumTraceFile);
    }
    catch (IOException ex)
    {
        Console.Error.WriteLine($"Error reading chromium trace file '{chromiumTraceFile}': {ex.Message}");
        return false;
    }

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
        eventsArray.Add( (JsonNode)new JsonObject
        {
            ["name"] = "SQL Query",
            ["cat"] = "sql",
            ["ph"] = "b",
            ["id"] = asyncId,
            ["ts"] = (long)(trace.Start.Value * 1000),
            ["pid"] = 1,
            ["tid"] = 1,
            ["args"] = new JsonObject { ["sql"] = trace.SqlText }
        });

        // Add End event
        eventsArray.Add( (JsonNode)new JsonObject
        {
            ["name"] = "SQL Query",
            ["cat"] = "sql",
            ["ph"] = "e",
            ["id"] = asyncId,
            ["ts"] = (long)(trace.End.Value * 1000),
            ["pid"] = 1,
            ["tid"] = 1
        });
    }

    // 3. Write the merged data back
    using var writer = new Utf8JsonWriter(File.Create(chromiumTraceFileWithSql), new JsonWriterOptions { Indented = true });
    root.WriteTo(writer);
    return true;
}

/// <summary>
/// Parses SQL events from a .NET trace file.
/// </summary>
/// <param name="nettraceFile">The path to the .NET trace file (.nettrace).</param>
/// <returns>A list of <see cref="SqlTrace"/> objects representing the SQL events, or null if the file is not found or an error occurs.</returns>
List<SqlTrace>? ParseEvents(string nettraceFile)
{
    if (!File.Exists(nettraceFile))
    {
        Console.Error.WriteLine($"Error: Nettrace file not found at '{nettraceFile}'");
        return null;
    }

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

/// <summary>
/// Prints SQL events from a .NET trace file to the console.
/// </summary>
/// <param name="fileName">The path to the .NET trace file (.nettrace).</param>
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

/// <summary>
/// Represents a SQL trace event, capturing its start and end times, object ID, and the SQL text.
/// </summary>
struct SqlTrace
{
    /// <summary>
    /// Gets or sets the start timestamp of the SQL trace in milliseconds.
    /// </summary>
    public double? Start;

    /// <summary>
    /// Gets or sets the end timestamp of the SQL trace in milliseconds.
    /// </summary>
    public double? End;

    /// <summary>
    /// Gets or sets the object ID associated with the SQL trace.
    /// </summary>
    public int ObjectId;

    /// <summary>
    /// Gets or sets the SQL command text.
    /// </summary>
    public string? SqlText;
};