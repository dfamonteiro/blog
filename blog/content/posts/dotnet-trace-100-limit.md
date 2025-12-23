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

## The .NET EventPipe's 100 stack frame limit

Collecting the tracing data is relatively straightforward: you simply copy the `dotnet-trace` executable to the pod with the .NET application, collect the tracing data, and then copy the file to your computer. All that remains to do now is figuring out why service xyz took 5 seconds to execute by analysing the traces with your favourite trace analysis tool.

TODO

- i like chromium traces.
- but when looking at the traces, i noticed something odd: a function split in two.
- zooming in, you notice there a spike in your traces
- stratification
- technical reasons
- can we recover? notice how no data is lost!

## The Chromium trace format: a primer

## Performing surgery on the trace file

- detecting spikes
- adding the missing stack frames
- stitching the functions spans
