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

[^2]: On the topic of integrating non-functional tests in a project, you might be interested in the concept of fitness functions, as popularized in the book [Building Evolutionary Architectures: Automated Governance for Technical Change](https://www.thoughtworks.com/insights/books/building-evolutionaryarchitectures-second-edition).

Implementing less popular types of tests might be more technically challenging, but once they are up and running you'll start wondering how you managed to live without them. Load tests, for example, will completely reframe your perspective of a project's performance characteristics, and in this blog post I intend to give you new perspectives on how load generators can be designed to suit your needs.

## The load generator state problem

<!-- structure:

- let's talk about load generators
- the load generator should test the whole flow
- State of the art of popular load generators: can they fit complex logic?
- State machines as a way to encapsulate the state of a given user
  - with an example
- Complex manufacturing scenario: wafer fab
- What is the shape of the load being generated?
  - Markov chain! -->
