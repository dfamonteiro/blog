+++ 
draft = true
date = 2026-02-16T02:12:26Z
title = "Embedding SQL query information in dotnet-trace-generated trace files: the good, the bad and the ugly"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Programming", "Tracing"]
categories = []
externalLink = ""
series = []
+++

A couple of weeks after I published my [guide on getting the most out of `dotnet-trace`](../using-dotnet-trace-with-perfetto/), I started having a nagging thought on the back of my head: could we do even better? And by "better" I mean collecting information on which SQL queries are being executed by our .NET application.

So, is it possible? Yes! Is it easy and convenient? ...Not really.

## The Good

If your .NET application uses something like `Microsoft​.Data​.SqlClient` to talk to the database, then you are in luck because this library is [capable of emitting tracing events related to your SQL queries](https://learn.microsoft.com/en-us/sql/connect/ado-net/enable-eventsource-tracing?view=sql-server-ver17#use-dotnet-trace-to-collect-traces)!

Collecting these SQL traces is simply a matter of making sure that the `Microsoft​.Data​.SqlClient​.EventSource` provider is also included when running `dotnet-trace collect`:

```txt
dotnet-trace collect -p 135 --providers Microsoft.Data.SqlClient.EventSource:1:5,Microsoft-Windows-DotNETRuntime:0x100003801D:4,Microsoft-DotNETCore-SampleProfiler:0xF00000000000:4
```

Analysing the resulting .nettrace file with [PerfView](https://github.com/microsoft/perfview) indeed confirms that the we've captured the SQL Queries:

<figure>
    <img src="/images/dotnet-trace-sql/perfview.png" alt="Thread 450">
    <figcaption>PerfView visualization of a <code>Microsoft​.Data​.SqlClient</code> event</figcaption>
</figure>

## The Bad

The SQL queries aren't embedded in the trace file

## The Ugly

... But some awful-looking slices are included.

## The fix

You can get there with some trace file manipulation

## The End

It's disappointing that this SQL info isn't included out of the box, but at least we can take fate into our own hands and insert this information ourselves.
