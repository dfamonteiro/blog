+++ 
draft = true
date = 2026-08-10T23:45:05+01:00
title = "Introducing the Production Load Generator: test the performance of your MES by simulating the entire factory"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Programming"]
categories = []
externalLink = ""
series = []
+++

Do you happen to have a spare factory laying around?

Probably not, which is a problem for us at [Critical Manufacturing](https://www.criticalmanufacturing.com/): how do we ensure that our MES will work as expected at the factory, _before actually deploying_ our MES in that said factory? This is not a problem that is solved by standard functional testing - functional tests help by validating that features work as expected; they tell us nothing about how a given feature behaves under the load of a factory producing at full capacity.

This is an especially pertinent problem in the electronics industry: a very nasty mix of high production volumes combined with onerous traceability and quality tracking requirements will bring your MES to its knees if you are not careful! It is therefore critical for projects in this industry to understand how their MES customization behaves under very high loads, because that's the harsh reality in which their MES system will operate, day in and day out.

There is only one issue: how do you simulate realistic factory conditions, without running the MES against an real factory?

## Introducing the Production Load Generator

The Production Load Generator project (or more informally known as "PLG") is a tool that stress-tests an MES system **by simulating the factory that MES system is being built for** - hence the name: this is a **Load Generator** that replicates the **Production Load** of a factory.

This simulation-first approach also means that this tool represents a significant leap forward in [Critical Manufacturing](https://www.criticalmanufacturing.com/)'s internal factory simulation capabilities. In matter of fact, you can think of the PLG as a factory simulator disguised as a load generator!

<!-- TODO -->
The design of the PLG was informed primarily by understanding who the end user is: our internal MES customization teams. 

### Written in Async C#

MES customization teams with concerns about 

The PLG is a load generator designed specifically for [Critical Manufacturing](https://www.criticalmanufacturing.com/)'s needs blabla not end to end because user logic is very bespoke C# async, we worry about concurrency, users only need to focus figuring out the business logic behind the scenarios - usually IoT.

## So early results

40 simulated lines

Testimonies TODO

## Final thoughs

The project I'm most proud of
