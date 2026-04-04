+++ 
draft = true
date = 2026-04-04T00:59:14+01:00
title = "Solving the puzzle of the perfect handover"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Programming"]
categories = []
externalLink = ""
series = []
+++

In recent times I have been putting some thought into designing a load generator that simulates queued-based manufacturing processes - think car [assembly lines](https://en.wikipedia.org/wiki/Assembly_line) or [SMT lines](https://en.wikipedia.org/wiki/Surface-mount_technology). A key detail about these types of setups is that they're susceptible to traffic jams: if a station at the end of the line breaks, the entire line is bottlenecked until the problem is fixed.

The intrinsically linear nature of these manufacturing setups creates dependencies between the materials in the line: panel #345436 can only enter the next machine if panel #345437 has finished processing, which in turn needs panel #345438 to clear the conveyor belt... you get the point. Simulating these lines with a [stateful load generator approach](../stateful-load-generators) will not work because it handles each panel independently - one would need to layer an obscene amount of synchronization code[^1] on top to make the panels behave in a way that somewhat resembles a real-world SMT line. Even if it could be done (and that's a big if), the resulting code would completely subvert the way that stateful load generators should be used!

[^1]: Think mutexes, semaphores, etc.

We need a new approach.

## The case for a machine-centered load generator

Instead of having a load generator that is focused on the panels, we should have a load generator that is focused on the machines that handle the panels. Each resource would then be handled by its own dedicated thread which would be tasked with simulating the machine's behaviours: receiving new panels, "processing" them, and sending them to the next machine. With this approach, each machine can independently enforce its own constraints by refusing to perform any given operation if its internal conditions are not met.

Nothing in this idea is that revolutionary: if anything, one could say that this is nothing more than a manufacturing simulation-oriented [Actor Model](https://en.wikipedia.org/wiki/Actor_model)! The devil lies in the details however: how can a machine send a panel to the next step in the line?
