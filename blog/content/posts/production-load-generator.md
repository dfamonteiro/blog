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

Probably not, and that's a problem for us at Critical Manufacturing: how do we ensure that our MES will work as expected at the factory, _before actually deploying_ our MES in that said factory? This is not a problem that is solved by standard functional testing - functional tests only validate that features work; they tell us nothing about how a given feature behaves under load.

This is an especially pertinent problem in the electronics industry: a very nasty mix of high production volumes combined with onerous traceability and quality tracking requirements will bring your MES to its knees if you are not careful! It is therefore critical for projects in this industry to understand how their MES customization behaves under very high loads, because that's the reality in which their MES will operate.

There is only one issue: how do you simulate realistic factory conditions, without running the MES against an real factory?

## Introducing the Production Load Generator

The Production Load Generator project (or PLG) is a tool that stress-tests an MES system by simulating the factory that MES system is being built for. The fundamental idea that underpins this project is that by accurately simulating a factory's production processes, we also replicate the performance issues this factory will encounter.

This was the problem I was tasked with solving

The PLG represents a technological leap forward in Critical Manufacturing's internal factory simulation capabilities.
bla bla stateful load generator bla bla line load generator

## So early results

40 simulated lines

Testimonies TODO

## Final thoughs

The project I'm most proud of
