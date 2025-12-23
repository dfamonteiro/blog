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

[Critical Manufacturing](https://www.criticalmanufacturing.com/) is an example of such a company: our MES[^1] runs on [OpenShift](https://www.redhat.com/en/technologies/cloud-computing/openshift) (an enterprise Kubernetes offering) and the host of the MES, in many ways the heart of this system, is a [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) application running in a Kubernetes pod. So, the question lends itself: can `dotnet-trace` be used to collect tracing data in a live MES host? The answer is yes... with an asterisk.

[^1]: A _Manufacturing Execution System_ is a software system responsible for the bookkeeping of a factory's production. It is generally used in highly sophisticated industries, such as the semiconductor industry and the medical devices industry, where a high level of material tracking and control is required.

## The asterisk

Collecting the tracing data is relatively straightforward: you simply copy the `dotnet-trace` executable to the pod with the .NET application, collect the tracing data, and then copy the trace file to your computer. All that remains to do now is figuring out why service xyz took 5 seconds to execute by analysing the traces with your favourite trace analysis tool. `dotnet-trace` supports 3 different trace formats, but I'd recommend that you use the Chromium format alongside the [Perfetto](https://perfetto.dev) trace viewer to analyse your .NET traces.

I've done the the steps in the paragraph above several times, and in almost every single trace file I found something like this:

<figure>
    <img src="/images/dotnet-trace-100-limit/dotnet-trace-fragmentation.png" alt="A screenshot of a broken trace.">
    <figcaption>A screenshot of a broken trace.</figcaption>
</figure>

These two discontinuities indicated by the arrows cause what was meant to be a singular span to be broken into 3 separate spans. What are these spikes doing here? When we zoom in on one of these discontinuities, things get even weirder:

<figure>
    <img src="/images/dotnet-trace-100-limit/dotnet-trace-fragmentation-zoom.png" alt="A screenshot of one of the zoomed-in spikes.">
    <figcaption>A screenshot of one of the zoomed-in spikes.</figcaption>
</figure>

The call stack of the spike is nearly equivalent to what comes before and after, but for some reason the first two base function calls are missing, which pushes the spike two frames up. Looking at the tail of the call stack of these spikes will give us the final piece of the puzzle:

<figure>
    <img src="/images/dotnet-trace-100-limit/dotnet-trace-fragmentation-tail.png" alt="The two spikes are highlighted by the arrows.">
    <figcaption>The two spikes are highlighted by the arrows.</figcaption>
</figure>

The two spikes have the exact same height... that can't be a coincidence. Let's check the height of these spikes: **it's exactly 100 stack frames tall**. What's going on here?!

### The .NET EventPipe's 100 stack frame limit

- technical reasons
- can we recover? notice how no data is lost!

## The Chromium trace format: a primer

## Performing surgery on the trace file

- detecting spikes
- adding the missing stack frames
- stitching the functions spans
