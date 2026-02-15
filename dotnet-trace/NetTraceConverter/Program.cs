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

string fileName = @"C:\Users\Daniel\Desktop\github\blog\dotnet-trace\NetTraceConverter\dotnet_20260121_100330.nettrace";
PrintSqlEvents(fileName);

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