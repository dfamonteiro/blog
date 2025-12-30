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

Gone are the days where language designers only needed to concern themselves with language features. Modern programming languages are nowadays expected to support their users across all phases of the sofware development lifecycle[^1]: from writing code (IDE support, formatters, linters, package managers) to then later deploying and monitoring the deployed code in production (out of the box support for metrics, profiling, tracing, logging, etc).

[^1]: I'm using the term "sofware development lifecycle" very loosely here. I don't mean Agile or Waterfall or anything like that. I mean writing and deploying code from the perspective of a single developer.

As the software industry transitioned towards a more cloud-centric execution environment, programming languages have had to focus on treating virtualized linux environments as a first class citizen, or risk getting left behind. C# was no exception, and for this language to maintain its popularity, Microsoft has had to perform a [complete overhaul](https://devblogs.microsoft.com/dotnet/announcing-net-core-1-0/) of the .NET runtime to make it cloud-ready.
