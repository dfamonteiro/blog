+++ 
draft = true
date = 2026-04-04T00:59:14+01:00
title = "Crafting a Zero-Capacity Multi-Producer-Multi-Consumer queue in async C#"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Programming"]
categories = []
externalLink = ""
series = []
+++

In recent times I have been putting some thought into designing a load generator that simulates queued-based manufacturing processes - think car [assembly lines](https://en.wikipedia.org/wiki/Assembly_line) or [SMT lines](https://en.wikipedia.org/wiki/Surface-mount_technology). A key detail about these types of setups is that they're susceptible to traffic jams: if a station at the end of the line breaks, the entire line is bottlenecked until the problem is fixed.

The intrinsically linear nature of these manufacturing setups creates dependencies between the materials in the line: panel #345436 can only enter the next machine if panel #345437 has finished processing, which in turn needs panel #345438 to clear the conveyor belt... you get the point.

Simulating these lines with a [stateful load generator approach](../stateful-load-generators) will not work because it handles each panel independently - one would need to layer an obscene amount of synchronization code[^1] on top to make the panels behave in a way that somewhat resembles a real-world SMT line. It's just not feasible.

[^1]: Think mutexes, semaphores, etc.

We need a new approach.

## The case for a machine-centered load generator

Instead of having a load generator that is focused on the panels, we should have a load generator that is focused on the machines that handle the panels. Each resource would then be handled by its own dedicated thread which would be tasked with simulating the machine's behaviours: receiving new panels, "processing" them, and sending them to the next machine.[^2] With this approach, each machine can independently enforce its own constraints by refusing to perform any given operation if its internal conditions are not met.

[^2]: You can think of this as a manufacturing simulation-oriented [Actor Model](https://en.wikipedia.org/wiki/Actor_model).

This is a good idea in principle... as long as we figure out how we are going to pass materials. In other words, how should a _handover_ of a panel from resource A to resource B occur?

## The handover problem statement

Given two machines A and B running on independent threads, if the following conditions are met:

1. Machine A wishes to send a material by calling `send()`.
2. Machine B wishes to receive a material by calling `receive()`.

The panel should then be transferred to Machine B, and both `send()` and `receive()` should return `true` when the transfer is completed.

The functions `send()` and `receive()` should also be [Atomic](https://en.wikipedia.org/wiki/Atomicity_(database_systems)), [Thread-safe](https://en.wikipedia.org/wiki/Thread_safety) and able to support timeouts.

### Some reflections

Our handover challenge is fundamentally a synchronization problem involving two independent threads - we should take inspiration from the field of concurrent programming and define a wishlist of properties that our conceptual data structure will need to have:

- Is a zero-capacity queue (i.e. the sender blocks until the receiver is ready to receive).
- Supports multiple producers and multiple consumers (i.e. thread-safe).
- Supports timeouts.
- Is written in async C# (because, well... I need it to be written in async C#).

## The solution: an [order book](https://en.wikipedia.org/wiki/Order_book) protected by a mutex

What does it mean when Machine A calls `send()`? Does it mean that there's a 100% guarantee that he panel will be sent? No. It means that Machine A **_is interested_** in sending the panel, and if there's matching interest from the other side, a trade will happen... hold on, is this a stock market? Our `send()` calls are the equivalent of sell orders and our `receive()` calls represent buy orders! Whenever there's an available `receive()` order and an available `send()` order, they are matched, removed from the "order book" and the panel is transferred.


