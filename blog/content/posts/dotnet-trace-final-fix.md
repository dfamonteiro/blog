+++ 
draft = true
date = 2026-07-26T22:00:11+01:00
title = "Fixing dotnet-trace's 100 stack frame limit once and for all"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Programming", "Tracing"]
categories = []
externalLink = ""
series = []
+++

I've had enough of workarounds to `dotnet-trace`'s fundamental limitation of 100 stack frames. Just to recap from my [previous misadventures](dotnet-trace-100-limit.md), if you attach `dotnet-trace` to your application and your application has, for example, a call stack 120 calls deep, the "root" 20 stack frames get cut from the call stack that `dotnet-trace` receives, and you end up with completely unusable traces like these:

<figure>
    <img src="/images/dotnet-trace-final-fix/colleague-trace.png" alt="A screenshot of a broken trace.">
    <figcaption>A screenshot of a broken trace.</figcaption>
</figure>

This isn't any random trace: it comes from me guiding a work colleague through [using `dotnet-trace`](./using-dotnet-trace-with-perfetto.md), and the grand result is this abominable rectangle that looks more like a [spectogram](https://en.wikipedia.org/wiki/Spectrogram) than an actual trace visualization.

I was a bit disheartened after this experience: I can write all the guides in the world about [how to use `dotnet-trace`](./using-dotnet-trace-with-perfetto.md), but if a person's first experience with using `dotnet-trace` results in _this_, then it will all be for naught.

## We have to fix this at the source

You can't fix this in post. Trust me, [I tried](dotnet-trace-100-limit.md) and the conclusion I reached was that asking the user to do any post-processing step will just discourage them from using this tool. This leaves us with only one final option: fixing `dotnet-trace` itself.

Trust me, this is easier than it sounds: the one method you need to modify is this one under [`src/Tools/dotnet-trace/TraceFileFormatConverter.cs`](https://github.com/dotnet/diagnostics/blob/main/src/Tools/dotnet-trace/TraceFileFormatConverter.cs):

```csharp
private static void Convert(TraceFileFormat format, string fileToConvert, string outputFilename, bool continueOnError = false)
{
    string etlxFilePath = TraceLog.CreateFromEventPipeDataFile(fileToConvert, null, new TraceLogOptions() { ContinueOnError = continueOnError });
    using (SymbolReader symbolReader = new(TextWriter.Null) { SymbolPath = SymbolPath.MicrosoftSymbolServerPath })
    using (TraceLog eventLog = new(etlxFilePath))
    {
        MutableTraceEventStackSource stackSource = new(eventLog)
        {
            OnlyManagedCodeStacks = true // EventPipe currently only has managed code stacks.
        };

        SampleProfilerThreadTimeComputer computer = new(eventLog, symbolReader)
        {
            IncludeEventSourceEvents = false // SpeedScope handles only CPU samples, events are not supported
        };
        computer.GenerateThreadTimeStacks(stackSource);

        switch (format)
        {
            case TraceFileFormat.Speedscope:
                SpeedScopeStackSourceWriter.WriteStackViewAsJson(stackSource, outputFilename);
                break;
            case TraceFileFormat.Chromium:
                ChromiumStackSourceWriter.WriteStackViewAsJson(stackSource, outputFilename, compress: false);
                break;
            default:
                // we should never get here
                throw new DiagnosticToolException($"Invalid TraceFileFormat \"{format}\"");
        }
    }

    if (File.Exists(etlxFilePath))
    {
        File.Delete(etlxFilePath);
    }
}
```

`dotnet-trace` implements the conversion to the [speedscope](https://www.speedscope.app/) and [chromium](https://perfetto.dev/) formats by essencially [delegating that responsibility to the `perfview` project](https://github.com/microsoft/perfview/blob/f3ec1b38a6d7535e4f878510e5041f9da7d0fdb6/src/TraceEvent/Stacks/ChromiumStackSourceWriter.cs#L12). We will replace the contents of this method and implement this conversion ourselves.

This will be done in classic [ETL](https://en.wikipedia.org/wiki/Extract,_transform,_load) fashion: **Extract**, **Transform** and **Load**.

### Extract

In this initial step we extract all the relevant data into a mapping of threads to lists of trace samples. This will be the main data structure we will be operating on.

```csharp
private static void Convert(TraceFileFormat format, string fileToConvert, string outputFilename, bool continueOnError = false)
{
    string etlxFilePath = TraceLog.CreateFromEventPipeDataFile(fileToConvert, null, new TraceLogOptions() { ContinueOnError = continueOnError });
            
    // Retrieve the call stacks from the file
    // threadId -> List of (timestamp, frames)
    Dictionary<int, List<CallstackSample>> callStacks = GetCallstacks(etlxFilePath);

    if (File.Exists(etlxFilePath))
    {
        File.Delete(etlxFilePath);
    }
}

/// <summary
/// Represents a singular call stack from a thread, sampled at a given TimestampMs.
/// </summary>
public record CallstackSample(double TimestampMs, List<string> StackTrace);
```

### Transform

### Load
