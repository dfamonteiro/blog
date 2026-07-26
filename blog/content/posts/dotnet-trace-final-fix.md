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

You can't fix this "in post". Trust me, [I've tried](dotnet-trace-100-limit.md) and the conclusion I reached was that asking the user to do any post-processing step will just discourage them from using this tool. This leaves us with only one final option: fixing `dotnet-trace` itself.
