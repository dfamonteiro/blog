+++ 
draft = true
date = 2025-12-22T22:51:27Z
title = "Working around dotnet-trace's 100 stack frame limit"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Programming"]
categories = []
externalLink = ""
series = []
+++

The [dotnet-trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace) command line tool is a rather neat piece of technology: by taking advantage of the .NET runtime's [EventPipe](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe) component, it is able to collect tracing data in a way that is both consistent and agnostic to the underlying operating system.

It gets even better though: you can start and stop collecting traces **_without having to restart the target application_**! This makes it a particularly useful tool for analysing performance issues in long-running containerized .NET applications, a common need in companies that run their workloads in Kubernetes clusters.

[Critical Manufacturing](https://www.criticalmanufacturing.com/) is an example of such a company: our MES[^1] runs on [OpenShift](https://www.redhat.com/en/technologies/cloud-computing/openshift) (an enterprise Kubernetes offering) and the host of the MES, in many ways the heart of this system, is an [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) application running in a Kubernetes pod. So, the question lends itself: can `dotnet-trace` be used to collect tracing data in a live MES host? The answer is yes... with an asterisk.

[^1]: A _Manufacturing Execution System_ is a software system responsible for the bookkeeping of a factory's production. It is generally used in highly sophisticated industries, such as the semiconductor industry and the medical devices industry, where a high level of material tracking and control is required.

## The asterisk

Collecting the tracing data is relatively straightforward: you simply copy the `dotnet-trace` executable to the pod with the .NET application, collect the tracing data, and then copy the trace file to your computer. All that remains to do now is figuring out why service xyz took 5 seconds to execute by analysing the traces with your favourite trace analysis tool. `dotnet-trace` supports 3 different trace formats, but I'd recommend that you use the Chromium format alongside the [Perfetto](https://perfetto.dev) trace viewer to analyse your .NET traces.

I've done the the steps in the paragraph above several times, and in almost every single trace file I found something like this:

<figure>
    <img src="/images/dotnet-trace-100-limit/fragmentation.png" alt="A screenshot of a broken trace.">
    <figcaption>A screenshot of a broken trace.</figcaption>
</figure>

These two discontinuities indicated by the arrows cause what was meant to be a singular span to be broken into 3 separate spans. What are these spikes doing here? When we zoom in on one of these discontinuities, things get even weirder:

<figure>
    <img src="/images/dotnet-trace-100-limit/fragmentation-zoom.png" alt="A screenshot of one of the zoomed-in spikes.">
    <figcaption>A screenshot of one of the zoomed-in spikes.</figcaption>
</figure>

The call stack of the spike is nearly equivalent to what comes before and after, but for some reason the first two base function calls are missing, which pushes the spike two frames up. Looking at the tail of the call stack of these spikes will give us the final piece of the puzzle:

<figure>
    <img src="/images/dotnet-trace-100-limit/fragmentation-tail.png" alt="The two spikes are highlighted by the arrows.">
    <figcaption>The two spikes are highlighted by the arrows.</figcaption>
</figure>

The two spikes have the exact same height... that can't be a coincidence. Let's check the height of these spikes: **it's exactly 100 stack frames tall**. What's going on here?!

### The .NET EventPipe's 100 stack frame limit

It turns out that I'm not the only person with this issue. After some digging up, I found this [github issue](https://github.com/dotnet/diagnostics/issues/4490) detailing the exact same problem with `dotnet-trace`, and after some back and forth they managed to figure out where the problem lies:

> Hi @JaneySprings, thank you for providing that sample. The timeframes where it seemed like base events were being omitted had deeper call stacks to the neighbors where the base events were not omitted. From offline discussion with @noahfalk, he had a suspicion that **the max stack depth of 100 led to base events being trimmed in favor of newer events**.
>
> After bumping that stack depth to 1000, from the speedscopes I've obtained, it looks like the max stack depth of 100 is the cause for base events being omitted.
>
> <span>- </span> <span><a href="https://github.com/mdh1418">@mdh1418</a></span>, <span><a href="https://github.com/dotnet/diagnostics/issues/4490#issuecomment-1939428734">12-Feb-2024</a></span>

So it looks like the EventPipe component has a hardcoded limit of 100 stack frames, and if this limit is exceeded the base stack frames are truncated: this is the cause of the misalignments we saw in our trace viewer. A quick look at the [EventPipe's source code](https://github.com/dotnet/runtime/blob/379d100b3cc18394064a276d7610e88a2aa09b6f/src/native/eventpipe/ep-types-forward.h#L70) confirms the existence of this limit:

```h
#define EP_ACTIVITY_ID_SIZE EP_GUID_SIZE
#define EP_MAX_STACK_DEPTH 100 // <---------- BINGO!!

/* EventPipe Enums. */
typedef enum {
    EP_BUFFER_STATE_WRITABLE = 0,
    EP_BUFFER_STATE_READ_ONLY = 1
} EventPipeBufferState;
// Author's note: this code snippet has been edited for improved readability
```

So, where does this leaves us? We can't fix this issue by changing the `EP_MAX_STACK_DEPTH` as that would require rebuilding and running a custom version of the .NET runtime. But even if we could do it, why would we want to? By manipulating the trace data, we can essentially fix this in post: as long as we can detect the spikes and add the missing base stack frames, we can make these discontinuities disappear. All it takes is a little surgery on the trace file.

## Performing surgery on the trace file

### The Chromium trace format: a primer

- detecting spikes
- adding the missing stack frames
- stitching the functions spans
