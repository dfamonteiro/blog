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

Thankfully, this is easier than it may appear at first glance: the one method you need to modify is this one under [`src/Tools/dotnet-trace/TraceFileFormatConverter.cs`](https://github.com/dotnet/diagnostics/blob/main/src/Tools/dotnet-trace/TraceFileFormatConverter.cs):

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

Now it's time to put our surgeon gloves on and start manipulating our call stacks. If a potentially truncated call stack is detected, the following is done:

1. Compare the base stack frame against all the stack frames of the previous sample. The idea is that while call `abc()` might be the root frame of our truncated call stack, it might in reality be frame #10 and the actual first 10 traces were suppressed. The best way to check this is to compare `abc()` against the stack frames of the previous sample, which is assumed to be correct, meaning that `abc()` will appear "lower" in the stack.
2. Compare the matches and select the one that better aligns with the previous call stack - the candidate with the most overlap wins.
3. Insert the missing stack frames - the call stack should be correct now.

I recognize it might be difficult to understand the algorithm just from this synopsis, so I cooked up a visualization just for you:

<figure>
    <video controls autoplay loop muted width="100%">
        <source src="/images/dotnet-trace-final-fix/Scene-1.mp4" type="video/mp4">
        Your browser does not support the video tag.
    </video>
    <figcaption>Visualization of the <code>FixCallStacks</code> algorithm.</figcaption>
</figure>

And here's the corresponding code:

```csharp
/// <summary>
/// Fixes the call stacks truncated by the EventPipe's 100 stack frame limit.
/// </summary>
public static void FixCallStacks(Dictionary<int, List<CallstackSample>> threadMap)
{
    foreach ((int threadId, var samples) in threadMap)
    {
        for (int sampleIndex = 1; sampleIndex < samples.Count; sampleIndex++)
        {
            var previous = samples[sampleIndex - 1];
            var current = samples[sampleIndex];

            if (current.StackTrace.Count < 100)
            {
                // We aren't exceeding the stack frame limit here,
                // therefore we don't need to fix anything.
                continue;
            }

            if (previous.StackTrace[0] != current.StackTrace[0])
            {
                // Get list of stack traces from `previous` that matches the `current` base trace
                var candidates = new List<int>();
                for (int i = 0; i < previous.StackTrace.Count; i++)
                {
                    if (previous.StackTrace[i] == current.StackTrace[0])
                    {
                        candidates.Add(i);
                    }
                }

                if (candidates.Count == 0)
                {
                    // If there's no matching stack frame from `previous`,
                    // there's nothing we can do
                    continue;
                }

                // Select the best candidate match from the list of candidates.
                // The best candidate match is the one with the most call stack overlap
                // between `previous` and `current`
                (int Index, int Overlap) bestCandidate = (-1, -1);
                foreach (int candidateIndex in candidates)
                {
                    int overlap = 0;
                    while (previous.StackTrace[candidateIndex + overlap] == current.StackTrace[overlap])
                    {
                        // For as long as the stack frames keep matching, keep increasing the overlap
                        overlap++;

                        if (candidateIndex + overlap == previous.StackTrace.Count || overlap == current.StackTrace.Count)
                        {
                            // We have an index out of bounds, so we have to stop
                            break;
                        }
                    }

                    if (overlap > bestCandidate.Overlap)
                    {
                        bestCandidate = (candidateIndex, overlap);
                    }
                }
                
                // Insert the missing stack frames
                for (int prevIndex = 0; prevIndex < bestCandidate.Index; prevIndex++)
                {
                    current.StackTrace.Insert(prevIndex, previous.StackTrace[prevIndex]);
                }
            }
        }
    }
}
```

### Load

The final step is convert our `callStacks` data structure into our preferred trace format. The code behind this serialization is not that interesting, so I decided to outsource the implementation of `SpeedscopeWriter` and `ChromiumWriter` to Gemini.

This is the final state of the `Convert` method:

```csharp
private static void Convert(TraceFileFormat format, string fileToConvert, string outputFilename, bool continueOnError = false)
{
    string etlxFilePath = TraceLog.CreateFromEventPipeDataFile(fileToConvert, null, new TraceLogOptions() { ContinueOnError = continueOnError });
    
    // Retrieve the call stacks from the file
    Dictionary<int, List<CallstackSample>> callStacks = GetCallstacks(etlxFilePath);

    // Fix the callstacks
    FixCallStacks(callStacks);

    if (File.Exists(etlxFilePath))
    {
        File.Delete(etlxFilePath);
    }

    switch (format)
    {
        case TraceFileFormat.Speedscope:
            SpeedscopeWriter.Convert(outputFilename, callStacks);
            break;
        case TraceFileFormat.Chromium:
            ChromiumWriter.Convert(outputFilename, callStacks);
            break;
        default:
            // we should never get here
            throw new Exception($"Invalid TraceFileFormat \"{format}\"");
    }
}
```

## So... does it work?

After all this work, have we done it? Let's start with a simple test: a broken trace from [this github issue](https://github.com/dotnet/diagnostics/issues/4490#issuecomment-1939428734), which happens to be one of the first mentions of this limitation that I discovered:

<div class="juxtapose" data-startingposition="80%" data-showlabels="true">
    <img src="/images/dotnet-trace-final-fix/github-diff-a.png" data-label="Before" alt="before" />
    <img src="/images/dotnet-trace-final-fix/github-diff-b.png" data-label="After"  alt="after" />
</div>

So far so good! But this trace is child's play compared to some traces I've collected at [Critical Manufacturing](https://www.criticalmanufacturing.com/), where the host of the system can compile C# code on-demand **_while serving a request_**[^1]! This is a call stack that can easily go 200-300 frames deep, and is definitely the ultimate challenge for the adjustments we made to `dotnet-trace` in this blog post.

[^1]: There are very good extensibility-related reasons for doing this, I will leave this link [here](https://help.criticalmanufacturing.com/userguide/administration/dee_actions/) for more info.

## So... does it _really_ work?

Let's run our tweaked `Convert` method against a trace from a [Critical Manufacturing](https://www.criticalmanufacturing.com/) MES host and let's see what happens:

<style>
  /* 1. Make the thin vertical center line black */
  .black-slider .jx-controller {
      background-color: #000000 !important;
  }

  /* 2. Make the handle container background transparent/black */
  .black-slider .jx-control {
      background-color: #000000 !important;
  }

  /* 3. Turn the left and right arrow triangles black */
  .black-slider .jx-arrow.jx-left {
      border-right-color: #000000 !important;
  }
  .black-slider .jx-arrow.jx-right {
      border-left-color: #000000 !important;
  }
</style>

<div class="juxtapose black-slider" data-startingposition="50%" data-showlabels="true">
    <img src="/images/dotnet-trace-final-fix/cm-1-a.png" data-label="Before" alt="before" />
    <img src="/images/dotnet-trace-final-fix/cm-1-b.png" data-label="After" alt="after" />
</div>

The quality of the trace has improved significantly, but we're not there yet. If we zoom in, we can still find disruptions:

<figure>
    <img src="/images/dotnet-trace-final-fix/disruptions.png" alt="A screenshot of a broken trace.">
    <figcaption>A screenshot of a break in our trace.</figcaption>
</figure>

This is happening because the `ForceCompleteMemberByLocation` can not be found in the previous trace, and therefore we hit this code path in `FixCallStacks`:

```csharp
if (candidates.Count == 0) // <==== No matches! ====
{
    // If there's no matching stack frame from `previous`,
    // there's nothing we can do
    continue; 
}
```

I honestly thought this `if (candidates.Count == 0)` edge case would never be triggered. I mean, who on earth goes 100 function calls deep within a single millisecond?! The Rosylin compiler apparently.

I have an idea on how to fix this, but it's not pretty.

### The observability gods demand a blood sacrifice

I'm going to do something truly sacrilegious... I'm going to just delete the samples that can't be rescued:

```csharp
if (candidates.Count == 0)
{
    // If there's no matching stack frame from `previous`, delete this sample.
    samples.RemoveAt(sampleIndex);
    sampleIndex--;
    continue;
}
```

Yes, I know, pure heresy. But you can't argue against results:

<div class="juxtapose" data-startingposition="50%" data-showlabels="true">
    <img src="/images/dotnet-trace-final-fix/cm-2-a.png" data-label="Before" alt="before" />
    <img src="/images/dotnet-trace-final-fix/cm-2-b.png" data-label="After"  alt="after" />
</div>

And what is the price I had to pay for perfect traces? 69 samples out of 2194244, or 0.003%. I'll take that deal any day of the week.

## Putting everything together

The only task remaining is going through the bureaucracy of forking [dotnet/diagnostics](https://github.com/dotnet/diagnostics), introducing our changes, and compiling our very own customized `dotnet-trace`.

In order to be able to distinguish between the canonical `dotnet-trace` and my own version, I renamed my version of this tool to `daniel-trace`... I couldn't come up with a better name, sorry.

!["Command line image of daniel-trace being executed"](/images/dotnet-trace-final-fix/daniel-trace.png)

### Give it a go!

You can install `daniel-trace` by downloading the relevant executable:

- Windows: [x64](https://github.com/dfamonteiro/daniel-trace/releases/download/daniel-trace-1/daniel-trace.exe)
- Linux: [x64](https://github.com/dfamonteiro/daniel-trace/releases/download/daniel-trace-1/daniel-trace)

If you are running another architecture (Arm, etc.) it should be easy enough to compile the project yourself - here's the link to my [fork](https://github.com/dfamonteiro/daniel-trace) of the [dotnet/diagnostics](https://github.com/dotnet/diagnostics) repository.

### Usage

`daniel-trace` is a drop-in replacement of `dotnet-trace` - just change the name and you should be good to go:

```txt
PS C:\Users\Daniel\Desktop\github\blog\dotnet-trace-final-fix> .\daniel-trace.exe convert .\dotnet_20260727_184408.nettrace --format Chromium          
Processing trace data file 'C:\Users\Daniel\Desktop\github\blog\dotnet-trace-final-fix\dotnet_20260727_184408.nettrace' to create a new Chromium file 'C:\Users\Daniel\Desktop\github\blog\dotnet-trace-final-fix\dotnet_20260727_184408.chromium.json'.
69 samples out of 2195227 could not be recovered and have been deleted (0.003%)
    Thread 269 (7): 51.514s-51.524s
    Thread 313 (1): 12.130s
    Thread 330 (45): 17.697s-17.705s, 72.990s-72.999s, 74.921s-74.932s, 75.620s-75.661s, 76.705s-76.712s, 79.055s-79.057s
    Thread 363 (16): 58.786s, 60.525s-60.546s, 60.992s-60.999s
Conversion complete
PS C:\Users\Daniel\Desktop\github\blog\dotnet-trace-final-fix>
```

Have fun analysing traces!

<!-- Juxtapose CSS -->
<link rel="stylesheet" href="https://cdn.knightlab.com/libs/juxtapose/latest/css/juxtapose.css">

<!-- Juxtapose JS -->
<script src="https://cdn.knightlab.com/libs/juxtapose/latest/js/juxtapose.min.js"></script>
