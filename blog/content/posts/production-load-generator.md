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

Probably not, which is a problem for us at [Critical Manufacturing](https://www.criticalmanufacturing.com/): how do we ensure that our MES will work as expected at the factory, _before actually deploying_ our MES in said factory? This is not a problem solved by standard functional testing - functional tests do help by validating that features work as expected, but they they tell us nothing about how those features will behave under the load of a factory producing at full capacity.

This is an especially pertinent problem in the electronics industry: a very nasty mix of high production volumes combined with onerous traceability and quality tracking requirements will bring your MES to its knees if you are not careful! It is therefore critical for projects in this industry to understand how their MES customization behaves under very high loads, because that's the harsh reality in which their MES system will operate, day in and day out.

There is only one issue: how do you simulate realistic factory conditions, without running the MES against an real factory?

## Introducing the Production Load Generator

The Production Load Generator project (more informally known as "PLG") is a tool that stress-tests an MES system by simulating the factory that the MES system is being built for, hence the name: it's a **Load Generator** that replicates the **Production Load** of a factory.

This simulation-first approach also means that this tool is a very good factory simulator in its own right, and it therefore represents a significant leap forward in [Critical Manufacturing](https://www.criticalmanufacturing.com/)'s internal factory simulation capabilities.

The Production Load Generator has been developed with one core tenet: **ease of use**. The reason for this is that teams only adopt new tools if their benefits far outweigh their learning curve[^1]. Therefore, every decision I made during the development of this project was done with the goal to make the usage of the PLG as pleasant as it can possibly be.

[^1]: This obviously assuming that you get to choose whether or not you want to use a given tool. Many times this isn't the case and you get a mandate from above to use xyz tool.

Now that you are more aware of the philosophy and motivations behind the Production Load Generator, let's move on to more technical matters.

## So... how does it work?

## Excellent documentation is the bare minimum

## Early results

40 simulated lines

Testimonies TODO

## Final thoughs

The project I'm most proud of
