+++ 
draft = true
date = 2025-12-30T11:20:18Z
title = "Diagnosing performance issues in .NET applications with dotnet-trace and Perfetto"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Programming", "Tracing"]
categories = []
externalLink = ""
series = []
+++

Modern programming languages are more than just ways of expressing computer instructions: they bring with them entire ecosystems of package managers, formatters, linters, profilers, etc that evolve alongside the language they support. Python has `pip`/`uv`, JavaScript has `NPM`, `ESList` and `TypeScript`, Go has utilities such as `gofmt` and `pprof` directly baked in the CLI, and the list goes on endlessly.

Gone are the days where language designers only needed to concern themselves with language features. Modern programming languages are nowadays expected to support their users across all phases of the sofware development lifecycle[^1]: from writing code (IDE support, formatters, linters, package managers) to then later deploying and monitoring the deployed code in production (out-of-the-box support for metrics, profiling, tracing, logging, etc).

[^1]: I'm using the term "sofware development lifecycle" very loosely here. I don't mean Agile or Waterfall or anything like that. I mean writing, deploying and diagnosing code from the perspective of a single developer.

### The origin of dotnet-trace

As the software industry transitioned towards more cloud-centric environments, programming languages have had to focus on treating virtualized Linux environments as first-class citizens or risk getting left behind. C# was no exception and for this language to maintain its popularity, Microsoft had to perform a [complete overhaul](https://devblogs.microsoft.com/dotnet/announcing-net-core-1-0/) of the .NET runtime to make it cloud-ready.

As part of this initiative to make .NET platform-agnostic, a supporting cast of CLI diagnostic tools was developed from scratch with the goal of uniformizing the developer experience of diagnosing C# applications across operating systems. One of these tools is `dotnet-trace`: a tool that can be hooked into a running .NET application to collect tracing data which can then be analysed with trace viewers.

### Harnessing the power of dotnet-trace & Perfetto

In an effort to popularize the usage of performance diagnostics tools during development at my workplace, I've been [working out the kinks](../dotnet-trace-100-limit) of using `dotnet-trace` to analyse the performance of our multithreaded [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) application. I have also been testing various trace viewers to analyse `dotnet-trace` files and in my opinion there is no discussion be had: `dotnet-trace` and [Perfetto](https://perfetto.dev/)[^2] are a match made in heaven, especially when you have to analyse complex trace files with multiple threads.

[^2]: Perfetto is a trace viewer developed by Google.

In this blog post I will show how you can use a combination of `dotnet-trace` & [Perfetto](https://perfetto.dev/) to collect traces from a running application and then perform post-processing on those traces, so that the focus is always on the parts of the call stack that matter to you.

## Collecting traces with dotnet-trace

### Installing dotnet-trace

If you don't have this tool already installed there are two ways of installing it:

1. `dotnet tool install --global dotnet-trace`
2. [Download the executable from the official page](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)

Please note that the `dotnet-trace` tool needs to be on the same execution environment as the target .NET application.[^3]

[^3]: I know there are ways to collect traces from outside of the docker container, but let's not complicate things.

If you're running your .NET application in a locked-down docker container, it might not be possible to run `dotnet tool install`, so your only option will be to do option 2: download the linux executable and move it to the container's filesystem by either:

- Running something like `docker cp dotnet-trace custom_host:/opt/app/dotnet-trace`[^4], or...
- Placing the executable in a folder that your .NET container can reach[^5]

[^4]: The name of my container is `custom_host` - replace it with your container's name whenever you see it.
[^5]: I personally know that `custom_host` will copy everything under `LocalEnvironment\BusinessTier`, so I just throw my `dotnet-trace` executable in there and it works like a charm!

### Running dotnet-trace

## Analysing traces with Perfetto

## Next steps
