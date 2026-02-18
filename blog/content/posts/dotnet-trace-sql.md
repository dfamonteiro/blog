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

If your .NET application uses something like `Microsoft​.Data​.SqlClient` to talk to the database, then you are in luck because this library is [capable of emitting tracing events related to your SQL queries](https://learn.microsoft.com/en-us/sql/connect/ado-net/enable-eventsource-tracing?view=sql-server-ver17#use-dotnet-trace-to-collect-traces).

Collecting these SQL traces is simply a matter of making sure that the `Microsoft​.Data​.SqlClient​.EventSource` provider is also included when running `dotnet-trace collect`:

```txt
dotnet-trace collect -p 135 --providers Microsoft.Data.SqlClient.EventSource:1:5,Microsoft-Windows-DotNETRuntime:0x100003801D:4,Microsoft-DotNETCore-SampleProfiler:0xF00000000000:4
```

Analysing the resulting `.nettrace` file with [PerfView](https://github.com/microsoft/perfview) indeed confirms that the we've captured the SQL Queries:

<figure>
    <img src="/images/dotnet-trace-sql/perfview.png" alt="PerfView visualization of a Microsoft​.Data​.SqlClient event">
    <figcaption>PerfView visualization of a <code>Microsoft​.Data​.SqlClient</code> event</figcaption>
</figure>

## The Bad

We know that our SQL queries are present in the .nettrace file. Can we convert this file into a chromium trace file, so that we can use [Perfetto](https://perfetto.dev/) to analyse it?

Sadly, the answer is no: for some reason, you lose the SQL queries when converting .nettrace to .chromium.json files.

## The Ugly

But it gets worse! Not only do you **_not_** get the SQL queries in your trace files, but you also are greeted with these incredibly annoying "Activity BeginExecute" slices that do absolutely nothing other than ruining your traces.

<figure>
    <img src="/images/dotnet-trace-sql/bad.png" alt="Visualization of a broken trace">
    <figcaption>These "Activity BeginExecute" slices are the bane of my existence</figcaption>
</figure>

## The Fix

I can live with these ugly "Activity BeginExecute" slices[^1], but we have to find a way to propagate the SQL query data to our chromium traces.

[^1]: You can use [Perfetto to filter these slices if necessary](../using-dotnet-trace-with-perfetto/#introducing-perfettosql).

After some digging, I discovered that `dotnet-trace` delegates the nuances of converting formats to the [`Microsoft​.Diagnostics​.Tracing​.TraceEvent`](https://www.nuget.org/packages/Microsoft.Diagnostics.Tracing.TraceEvent) dependency. Using this package seems to be the recommended way of dealing with `.nettrace` files, so I decided to not reinvent the wheel and took advantage of this package to create a script that adds the missing SQL events to my trace files.

I will spare you the implementation details, but if you are interested in running this on your side, you can [download the code here](https://github.com/dfamonteiro/blog/blob/main/dotnet-trace/NetTraceConverter/AddSqlEvents.cs).

Usage example:

```txt
dotnet run AddSqlEvents.cs --nettrace-file .\dotnet_20260121_100330.nettrace --chromium-trace-file .\dotnet_20260121_100330.chromium.json --output test.chromium.json
```

When you open your newly generated trace file, you will now have your precious SQL queries available to be analysed!

<figure>
    <img src="/images/dotnet-trace-sql/sql.png" alt="Visualization of a broken trace">
    <figcaption>The <code>Material​​.Save()</code> function call has a matching "SQL Query" span, which contains information regarding which SQL was executed: in this case, is was the <code>[CoreDataModel].[P_GeneratedMaterialUpdate1200]</code> procedure.</figcaption>
</figure>

## The End

So it's possible to embed SQL query data in your `dotnet-trace`-generated traces, but given the ordeal one has to go through to make this information available in [Perfetto](https://perfetto.dev/), I doubt many people will take advantage of this. Even for me, the person that wrote a script to fix this problem, this is way too much work!

`dotnet-trace` deserves plaudits for its immense usefulness, but there is clearly some room for improvement here.
