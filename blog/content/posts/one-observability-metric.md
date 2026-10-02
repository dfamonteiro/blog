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

To answer this problem, we need a source of performance data: server logs, traces, anything. Thankfully, the product I work on has open telemetry support and therefore we have reams of observability data stored in clickhouse, so it's really a matter of selecting the right query. But which one?

## Weeding out the obvious candidates

### Sort by p95

"sort by p95" doesn't work because you could end up with background services that only end up being executed once a day and is not critical

### Sort by service execution count

"sort by service execution count" doesn't work because this because, at least in our case, basic entity loads will top the list.

We have two ends of the spectrum and we need something in the middle.

## The solution: sort by Total Execution Time

What I ended up suggesting to my colleagues was to multiply the service execution count by the average service execution time, and sort by descending order. (represents the time the host spends executing this service in total, hence the name)

TODO: some SQL (with a comment explaining how this can be simplified to a sum)

## Steve Jobs got there first

TODO: Boot optimization story (and I believe)
