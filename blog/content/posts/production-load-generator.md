+++ 
draft = true
date = 2026-08-10T23:45:05+01:00
title = "Introducing the Production Load Generator: test your MES by simulating the entire factory"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Programming"]
categories = []
externalLink = ""
series = []
+++

Do you happen to have a spare factory laying around? Probably not, I assume.

Sadly, we also don't have a spare factory at [Critical Manufacturing](https://www.criticalmanufacturing.com/), which poses some issues for us: how do we ensure that our MES[^1] will work as expected at the factory, _before actually deploying_ our MES in said factory? This is not a problem solved by standard functional testing - functional tests do help by validating that features work as expected, but what will happen when the factory is producing at full capacity, putting the MES under maximum stress?

[^1]: A _Manufacturing Execution System_ is a software system responsible for the bookkeeping of a factory's production. It is generally used in highly sophisticated industries, such as the semiconductor industry and the medical devices industry, where a high level of material tracking and control is required.

This is an especially pertinent problem in the electronics industry: a very nasty mix of high production volumes combined with onerous traceability and quality tracking requirements will bring your MES to its knees if you are not careful! It is therefore critical for projects in this industry to understand how their MES customizations behave under very high loads, because that's the harsh reality in which the system will operate, day in and day out.

<figure>
    <img src="/images/production-load-generator/qualitel.png" alt="A screenshot of a broken trace.">
    <figcaption>A factory with (at least) 3 SMT lines. You are reading this blog post with a screen powered by electronic circuit boards - those boards came from a manufacturing line of this kind.<br>(image source: <a href="https://www.qualitel.com/what-is-smt-manufacturing/">Qualitel</a>)</figcaption>
</figure>

So, how do you prove that your MES system will handle the expected production rate of a factory, without actually running the MES in production? The solution: **test the performance of your MES system against a simulated factory**.

Only one problem though... how do you simulate a factory?

## Introducing the Production Load Generator

The Production Load Generator project (more informally known as "PLG") is a tool that stress-tests an MES system **by simulating the factory that the MES system is being built for**, hence the name: it's a **Load Generator** that replicates the **Production Load** of a factory in our MES.

The goal of the Production Load Generator is simple: **make it as easy as possible for MES customization teams to simulate their customer's factories**, so that performance issues that previously would only show up in production now appear far earlier in the project's lifecycle. The easier the PLG is to use, the more likely teams are to adopt this tool, so a lot of care and attention was put into the PLG's overall developer experience.

The Production Load Generator is equal parts a load generator and a factory simulator, which makes it very useful for other purposes within Critical Manufacturing, such as showcasing features of the MES that can only be assessed properly when the MES is running around the clock (for example, our reports and dashboards).

Now that you get the broad strokes of what the Production Load Generator is supposed to be, let's see how it works in practice.

## So... how does it work?

## Excellent documentation is the bare minimum

## Early results

40 simulated lines

Testimonies TODO

## Final thoughs

The project I'm most proud of
