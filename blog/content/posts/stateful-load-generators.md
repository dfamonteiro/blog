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

Besides the ability to handle bespoke logic, top-of-the-line load generators also come with plenty of quality of life features such as [E2E load testing](https://grafana.com/docs/k6/latest/using-k6-browser/), [load shaping](https://docs.locust.io/en/stable/custom-load-shape.html), [built-in metrics](https://grafana.com/docs/k6/latest/using-k6/metrics/) and [dashboards](https://grafana.com/docs/k6/latest/results-output/web-dashboard/).

However, while their documentation and feature set serves the "e-commerce website" use case well, what should you do when your "users" have a massive amount of state associated to them, which heavily influences what their next steps are? This is the problem I've been reflecting on at [Critical Manufacturing](https://www.criticalmanufacturing.com/): structuring user behaviours that are dependent on their current state in an elegant and scalable manner, within the context of load generators.

Experience with a practical problem will help us get a better feel of this challenge.

## The TSMC wafer manufacturing scenario

Let's say that we are in charge of assessing the performance of a Manufacturing Execution System[^4] (MES). This MES is in charge of keeping track of both the wafers and the machines in a TSMC fab, and then determining which wafers should be sent to which machine. This MES has the following entities:

[^4]: A _Manufacturing Execution System_ is a software system responsible for the bookkeeping of a factory's production. It is generally used in highly sophisticated industries, such as the semiconductor industry and the medical devices industry, where a high level of material tracking and control is required.

```python
# Represents a WIP wafer in a fab
class Wafer:
    name: str # Unique identifier

    flowpath: str # In which step this wafer is at in its manufacturing flow
                  # Tends to have this format: "flow\step"

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

# Auxiliary functions
def load_wafer(name: str) -> Wafer:
    "Loads the wafer by name from the MES database."
    pass

def load_machine(name: str) -> Machine:
    "Loads the machine by name from the MES database."
    pass

def get_valid_dispatch_candidates(wafer: Wafer) -> List[Machine]:
    "Returns a list of machines to which the wafer can be dispatched to."
    pass

# Wafer tracking operations
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

We primarily care about the performance impact of the wafer tracking operations, because they happen very frequently and perform writes on the database.

### Load generation details

In order to ensure that our MES can cope with the full demand generated by a fab, we will have our load generator do a barebones simulation of a fab's wafer movements. To keep things simple, this will be our load generator scenario:

> The manufacturing flow our wafers will go through has the following flowpaths:
>
> - SimpleFlow\Step1
> - SimpleFlow\Step2
> - ...
> - SimpleFlow\Step9
> - SimpleFlow\Step10
>
> Wafers in SimpleFlow\Step1 can only be dispatched to Machine1, wafers in SimpleFlow\Step2 can only be dispatched to Machine2, etc.
>
> **Every second**:
>
> - A new wafer is created
> - This newly created wafer is moved through the manufacturing flow
> - Once it reaches the end, it's terminated

We have all the pieces of the puzzle, now it's time to put them together.

## Iteration 1: The naïve approach

Let's try the simplest thing that comes to mind:

```python
def wafer_scenario():
    wafer = create_wafer()

    # SimpleFlow\Step1
    machine1 = load_machine("Machine1")
    wafer.dispatch(machine1)
    wafer.track_in()
    wafer.track_out()
    wafer.move_next() # Move to next flowpath

    # SimpleFlow\Step2
    machine2 = load_machine("Machine2")
    wafer.dispatch(machine2)
    wafer.track_in()
    wafer.track_out()
    wafer.move_next() # Move to next flowpath

    # ...

    # SimpleFlow\Step9
    machine9 = load_machine("Machine9")
    wafer.dispatch(machine9)
    wafer.track_in()
    wafer.track_out()
    wafer.move_next() # Move to next flowpath

    # SimpleFlow\Step10
    machine10 = load_machine("Machine10")
    wafer.dispatch(machine10)
    wafer.track_in()
    wafer.track_out()
    wafer.move_next()

    wafer.terminate()

run_every_second(wafer_scenario)
```

This _does_ technically work, but it doesn't scale at all. We're dealing with a very simple scenario and I've already had to omit flowpaths 3 through 8, now imagine if this manufacturing flow had 1000 flowpaths![^6]

[^6]: This might surprise/scare you, but manufacturing flows this big are not that unrealistic for the semiconductor industry.

Luckily, you notice that the repetitiveness of this code can be easily addressed...

## Iteration 2: Loop-based naïve approach

Taking a closer look at the code from the previous chapter, its cylical nature becomes quite obvious:

<figure>
    <img src="/images/wafer-system-state-loop.excalidraw.svg" alt="The Wafer system state loop">
    <figcaption><b>The Wafer system state loop</b></figcaption>
</figure>

We can refactor our code by taking advantage of this cyclicality:

```python
def wafer_scenario():
    machines = [
        load_machine("Machine1"),
        load_machine("Machine2"),
        load_machine("Machine3"),
        load_machine("Machine4"),
        load_machine("Machine5"),
        load_machine("Machine6"),
        load_machine("Machine7"),
        load_machine("Machine8"),
        load_machine("Machine9"),
        load_machine("Machine10"),
    ]

    wafer = create_wafer()

    for machine in machines:
        wafer.dispatch(machine)
        wafer.track_in()
        wafer.track_out()
        wafer.move_next() # Move to next flowpath

    wafer.terminate()

run_every_second(wafer_scenario)
```

While the `machines` list still scales linearly with the size of the manufacturing flow, this is still a marked improvement over our previous solution. In matter of fact this approach might be good enough for this scenario, as long as the load-testing requirements don't change!

## A change of requirements

Well I just had to jinx it, didn't I? Let's see what the new requirements are:

> - On SimpleFlow\Step2, there is a 3% chance of the machine aborting the processing on a wafer (by calling `wafer.abort()`, which undoes the `wafer.track_in()` operation), due to a lack of consumables.
>
> - SimpleFlow\Step4 is actually a quality control step that only 10% of randomly selected wafers have to perform. The other 90% can skip SimpleFlow\Step4 by calling `wafer.skip_flowpath()`.
>
> - In SimpleFlow\Step9, during processing, a defect tends to be found in 0.7% of the wafers (`wafer.report_defect()` is used to report the defect).
>   - If a defect is reported in SimpleFlow\Step9, when the wafer is tracked out, the MES will automatically set its flowpath back to `"SimpleFlow\Step9"` and the system state back to `Queued`

<!-- structure:
- State machines as a way to encapsulate the state of a given user
  - with an example
- Stateful load generator pattern
-->
