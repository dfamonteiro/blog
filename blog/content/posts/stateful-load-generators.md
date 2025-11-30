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

One can usually tell the importance of a certain piece of software by the amount of effort it goes into testing it. In no other place is this more true than the automotive, medical, and aerospace[^1] industries: the criticality of software in these contexts leads to their development having to be compliant to international safety standards such as [ISO 26262](https://www.iso.org/standard/68383.html) and [IEC 62304](https://www.iso.org/standard/38421.html), which include extensive testing and validation requirements.

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

Generating web traffic load for websites is a solved problem: if you have a straightforward http-based backend, there are a slew of load-generating tools available for you to use[^3]. These tools do far more than just mindlessly hammering an API endpoint, however! They are very much capable of having scenarios with complex user flows:

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

However, while their documentation and feature set serves the "e-commerce website" use case well, what should you do when your "users" have a massive amount of state associated to them, which heavily influences what their next steps are? This is the problem I've been reflecting on at [Critical Manufacturing](https://www.criticalmanufacturing.com/): structuring user behaviours that are dependent on their current state in an elegant and scalable manner, within the context of load generators.

Experience with a practical problem will help us get a better feel of this challenge:

## The TSMC wafer manufacturing scenario

Let's say that we are in charge of assessing the performance of a Manufacturing Execution System[^4] (MES). This MES is in charge of keeping track of both the wafers and the machines in a TSMC fab, and then determining which wafers should be sent to which machine. This MES has the following entities:

[^4]: A _Manufacturing Execution System_ is a software system responsible for the bookkeeping of a factory's production. It is generally used in highly sophisticated industries, such as the semiconductor industry and the medical devices industry, where a high level of material tracking and control is required.

```python
# Represents a WIP wafer in a fab
class Wafer:
    name: str # Unique identifier

    flowpath: str # In which step this wafer is at in its manufacturing flow
                  # Tends to have this format: "flow\subflow\step"

    system_state: Enum # In a given flowpath, the "manufacturing state" of this wafer.
    # The enum above has the following states:
    #     - Queued: The wafer is ready to be dispatched
    #     - Dispatched: The wafer has been dispatched to a specific machine
    #     - InProcess: This wafer is being processed by the machine
    #     - Processed: The wafer has been processed and is now ready for the next flowpath
    # The system state evolves in this manner:
    #     Queued -> Dispatched -> InProcess -> Processed -> Queued (new flowpath)

    # ...

# Represents a machine that performs a manufacturing process on a wafer
class Machine:
    name: str # Unique identifier
    # ...
```

I took some inspiration from how the [Critical Manufacturing MES](https://www.criticalmanufacturing.com/) does things, but please note that I'm cutting _a lot_ of corners with this scenario to make it easier to digest.[^5]

[^5]: [XKCD 2501](https://xkcd.com/2501/) comes to mind, so lets keep things simple.

With that out of the way, let's take a look at the MES operations we intend to stress-test:

```python
# Please pretend that these functions make http calls to an MES instance hosted somewhere

def load_wafer_by_name(name: str) -> Wafer:
    "Loads the wafer by name from the MES DB."
    pass

def load_machine_by_name(name: str) -> Machine:
    "Loads the machine by name from the MES DB."
    pass

class Wafer:
    # ...

  def dispatch(self, machine: Machine):
      "Dispatches the wafer to the specified machine. The wafer goes from Queued to Dispatched."
      pass

  def track_in(self):
      "Begins processing of the wafer. The wafer goes from Dispatched to InProcess."
      pass

  def track_out(self):
      "Ends processing of the wafer. The wafer goes from InProcess to Processed."
      pass

  def move_next(self):
      """Moves the wafer to the next step of its manufacturing flow.
      The wafer goes from InProcess to Processed.
      The wafer's flowpath is updated to reflect the fact the wafer is in the bext step of its flow.
      """
      pass
```

<!-- structure:
- Complex manufacturing scenario: wafer fab
- State machines as a way to encapsulate the state of a given user
  - with an example
-->
