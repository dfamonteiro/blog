+++ 
draft = true
date = 2026-10-04T19:36:49+01:00
title = "One observability metric to rule them all"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["programming", "observability"]
categories = []
externalLink = ""
series = []
+++

I was talking with some colleagues regarding their [load-testing](/posts/production-load-generator/) efforts, when an interesting question came up during the conversation:

> How do we determine which services deserve the most optimization engineering effort?[^1]

[^1]: In many cases, just by having a good understanding of the business problems the software is solving you know which API endpoints are performance-sensitive. For the sake of this blog post, let's assume we are devoid of any and all business intuition.

To answer this problem, we need a source of performance data: server logs, traces, etc - thankfully, the product I work on has OpenTelemetry support and therefore we have reams of observability data stored in ClickHouse, so it's really a matter of crafting the right query. But which one?

## Why not use p95 latency

Your first thought might be to sort by [p95/p99](https://en.wikipedia.org/wiki/Latency_(engineering)#Tail_latency) and be done with it. This will not work for my particular circumstances because our p95 rankings are polluted by services that are executed perhaps once per week and take ~20s to complete. Should these rarely executed services be my top optimization targets? Probably not.

The number of times a service is executed **must be taken into account**.

## The solution: sort by Total Execution Time

What I ended up suggesting to my colleagues was to multiply the service execution count by the average service execution time, and sort by descending order:

```sql
SELECT TOP 20
    ServiceName,
    SUM(DATEDIFF(MILLISECOND, ServiceStartTime, ServiceEndTime)) AS TotalExecutionTime
    -- Note: AVG(x) * COUNT(x) can be simplified down to SUM(x)
FROM [dbo].[T_ServiceHistory]
WHERE ServiceEndTime >= DATEADD(DAY, -7, GETDATE()) -- Avoid a full table scan
GROUP BY ServiceName
ORDER BY TotalExecutionTime DESC;
```

This query will return a ranking of the services executed by your system ordered by how much cumulative time the system has spent executing them. In essence, it shows you **where the system is spending its time**.

Now, all that is left to do is to pick a service from this ranking and start optimizing!

## Steve Jobs got there first

You will not be surprised to hear that this metric is not a new idea at all: Datadog has [Total Time Spent](https://docs.datadoghq.com/tracing/services/resource_page/#avg-time-per-request), Splunk has [Total Response Time](https://help.splunk.com/en/splunk-observability-cloud/monitor-application-performance/monitor-database-query-performance), and all the other observability vendors probably have something similar. But going even further back, I reckon that Steve Jobs has beaten all of us to this idea, as I recall reading this anecdotal tale from his [biography](https://www.goodreads.com/book/show/11084145-steve-jobs):

> One day Jobs came into the cubicle of Larry Kenyon, an engineer
> who was working on the Macintosh operating system, and complained
> that it was taking too long to boot up. Kenyon started to explain, but
> Jobs cut him off. "If it could save a person's life, would you find a way
> to shave ten seconds off the boot time?" he asked. Kenyon allowed that
> he probably could.
>
> Jobs went to a whiteboard and showed that if there
> were five million people using the Mac, and it took ten seconds extra to
> turn it on everyday, that added up to three hundred million or so hours
> per year that people would save, which was the equivalent of at least
> one hundred lifetimes saved per year.

I can only conclude that Jobs would have made an excellent SRE in another life.
