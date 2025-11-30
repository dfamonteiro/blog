+++ 
draft = true
date = 2025-11-02T16:50:08Z
title = "The case for stateful load generators"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Programming"]
categories = []
externalLink = ""
series = []
+++

One can usually tell the importance of a certain piece of software by the ammount of effort it goes into testing it. In no other place is this more true than the automotive, medical, and aerospace[^1] industries: the criticality of software in these contexts leads to their development having to be compliant to international safety standards such as [ISO 26262](https://www.iso.org/standard/68383.html) and [IEC 62304](https://www.iso.org/standard/38421.html), which include extensive testing and validation requirements.

[^1]: It's quite hard to reboot your computer when your computer is in orbit.

You don't have to be Airbus to take testing seriously, though. There are open-source projects out there whose testing infraestructure can rival that of major corporations. Take SQLite, for example: it has **590 times** more test code than library code! They have a whole webpage dedicated to their testing efforts:

> 1.1. Executive Summary
>
> - Four independently developed test harnesses
> - 100% branch test coverage in an as-deployed configuration
> - Millions and millions of test cases
> - Out-of-memory tests
> - I/O error tests
> - Crash and power loss tests
> - Fuzz tests
> - Boundary value tests
> - Disabled optimization tests
> - Regression tests
> - Malformed database tests
> - Extensive use of assert() and run-time checks
> - Valgrind analysis
> - Undefined behavior checks
> - Checklists
>
> <span><a href="https://sqlite.org/testing.html">- How SQLite Is Tested</a></span>

While open-source projects such as [Linux](https://www.linux.org/) and [SQLite](https://sqlite.org/) highlight a wide variety of software testing methodologies, the reality is that most enterprise software projects typically employ only a subset of the more popular types of software testing. This leads to certain side effects: functional tests such as unit tests, integration tests and end-to-end tests are very well understood across the industry and have plenty of tooling. On the other hand, less popular types of testing (fuzzing, stress tests, etc) are less well supported and require you to go off the trodden path and invest a lot of effort to adapt them to your project's needs.[^2]

[^2]: On the topic of integrating non-functional tests in a project, you might be interested in the concept of fitness functions, as popularized in the book [Building Evolutionary Architectures](https://www.thoughtworks.com/insights/books/building-evolutionaryarchitectures-second-edition).

Implementing less popular types of tests might be more technically challenging, but once they are up and running you'll start wondering how you managed to live without them. Load tests, for example, will completely reframe your perspective of a project's performance characteristics, and in this blog post I intend to give you new perspectives on how you can shape the design of load generators to suit your needs.

## Load generators: a very abridged overview

Generating load for websites is a solved problem. If you have a straightforward http-based backend, there are a slew of load-generating tools available for you to use[^3]. These tools do far more than just mindlessly hammering an API endpoint! They are very much capable of having scenarios with complex user flows:

[^3]: [Apache JMeter](https://jmeter.apache.org/), [k6](https://k6.io/), [Locust](https://locust.io/) and [NBomber](https://nbomber.com/) come to mind.

```C#
// Example from the NBomber docs
// https://nbomber.com/docs/nbomber/scenario#scenario-create
var scenario = Scenario.Create("my e-commerce scenario", async context =>
{
    await Login();        
    await OpenHomePage();     
    await Logout();

    return Response.Ok();        
});
```

Besides the ability to handle bespoke logic, top-of-the-line load generators also come with plenty of quality of life features such as [E2E load testing](https://grafana.com/docs/k6/latest/using-k6-browser/), [built-in metrics](https://grafana.com/docs/k6/latest/using-k6/metrics/) and [dashboards](https://grafana.com/docs/k6/latest/results-output/web-dashboard/).

However, while their documentation and feature set serves the "e-commerce website" use case well, what should you do when your "users" have a massive ammount of state associated to them, which heavily influences what their next steps are? This is the problem I've been reflecting on at [Critical Manufacturing](https://www.criticalmanufacturing.com/): structuring user behaviours that are dependent on their current state in an elegant and scalable manner, within the context of load generators.

## The TSMC wafer manufacturing scenario

<!-- structure:
- Complex manufacturing scenario: wafer fab
- State machines as a way to encapsulate the state of a given user
  - with an example
-->
