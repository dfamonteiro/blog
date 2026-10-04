+++ 
draft = true
date = 2026-09-28T23:37:07+01:00
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

[^1]: In a lot of cases, just by having a good understanding of the business problems the software is solving, you know which API endpoints are performance-sensitive. For the sake of this blog post, let's assume we are disavowed of any and all business intuition.

To answer this problem, we need a source of performance data: server logs, traces, etc - thankfully, the product I work on has open telemetry support and therefore we have reams of observability data stored in clickhouse, so it's really a matter of crafting the right query. But which one?

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

The beauty of this metric is that it shows you where your system is spending its time.

## Steve Jobs got there first

TODO: Boot optimization story (and I believe)
