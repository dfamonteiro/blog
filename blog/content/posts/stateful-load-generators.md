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
    # No move_next() is required in the last flowpath

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

def wafer_scenario():
    wafer = create_wafer()

    for machine in machines:
        wafer.dispatch(machine)
        wafer.track_in()
        wafer.track_out()

        if machine.name == "Machine10":
            # No move_next() is required in the last flowpath
            break
        else:
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
> - In SimpleFlow\Step9, during processing, a defect tends to be found in 0.7% of the wafers (`wafer.report_defect()` is called in these cases).
>   - If a defect is reported in SimpleFlow\Step9, when the wafer is tracked out, the MES will automatically set its flowpath back to `"SimpleFlow\Step5"` and the system state back to `Queued`.

Sadly, the real world is more complicated than what a simple for-loop can handle. Let's see what we can do.

## Iteration 3: Throw some if-statements in there

Maybe we can meet these new requirements by throwing some if-statements in the right places.

```python
from random import random

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

def wafer_scenario():
    wafer = create_wafer()

    flowpath_index = 0
    while True:
        if flowpath_index == 3 and random() < 0.9:
            wafer.skip_flowpath()
            continue

        wafer.dispatch(machines[flowpath_index])
        if flowpath_index != 1:
            wafer.track_in()
        else:
            wafer.track_in()

            while True:
                is_to_abort = random() < 0.03
                if not is_to_abort:
                    break
                else:
                    wafer.abort()
                    wafer.track_in()
        
        is_to_record_defect = flowpath_index == 8 and random() < 0.007
        if is_to_record_defect:
            wafer.report_defect()

        wafer.track_out()

        if is_to_record_defect:
            flowpath_index = 4
            continue

        if flowpath_index == 9:
            # No move_next() is required in the last flowpath
            break
        else:
            wafer.move_next() # Move to next flowpath
            flowpath_index += 1

    wafer.terminate()

run_every_second(wafer_scenario)
```

If looking at this tangled mess of if-statements doesn't convince you this is a bad idea, I don't know what will.[^7] Imagine if, instead of 3 extra requirements, we had 20! It's obvious this approach not only doesn't scale at all, but is also very bug-prone.[^8]

We find ourselves in a bit of a predicament. While it's clear that the amount of state these wafers carry is warping the way we write our code, what's _not clear_ is how we can deal with it in a elegant manner. It's clear the way forward involves tackling the stateful nature of these wafers head-on. But first, let's take a step back and reflect on the issue at hand.

[^7]: While writing this iteration subchapter, I was reminded of a very funny quote I heard from work: _"Could you bring this up in the next arquitecture meeting, so I can immediately shoot it down?"_. That's how I feel about this subchapter: it's more about what you _shouldn't do_.

[^8]: The first if-statement of the loop is missing a `flowpath_index += 1` line, for example.

## State machines to the rescue

All of our wafers have state in the form of `(flowpath, system_state)`, and every time we perform a MES operation, our wafers transition to a new state... are our wafers state machines? I'd argue that we should think of them as such: once you start embracing this way of thinking, things start falling into place.

As a thought experiment, lets create a state machine that represents our current load scenario (including the extra requirements):

<figure>
    <img src="/images/full-wafer-state-machine.excalidraw.svg" alt="The load scenario as a state machine">
    <figcaption>
        <b>The load scenario as a state machine</b>
        <br> Dashed arrows represent <code>move_next()</code> state transitions
        <br> Wafer system states are being ignored
    </figcaption>
</figure>

It just feels _right_, doesn't it?

We've made a breakthrough here! State machines are definitely the missing piece to our puzzle: the extra requirements are now, in a rather elegant manner, simply an extra transition with a given probability. But how should we fit the wafer's `system_state` in this state machine? Should we even try to merge these two pieces of state (`flowpath` and `system_state`) together?

### The state machine within the state machine

How many states does our state machine have? You might be tempted to look at the state machine above and say 10, but the correct answer would be 40:

```python
("SimpleFlow\Step1", Queued)
("SimpleFlow\Step1", Dispatched)
("SimpleFlow\Step1", InProcess)
("SimpleFlow\Step1", Processed)
# ...
("SimpleFlow\Step10", Queued)
("SimpleFlow\Step10", Dispatched)
("SimpleFlow\Step10", InProcess)
("SimpleFlow\Step10", Processed)
```

When we're focusing on the `flowpath` it can be easy to forget about the `system_state`, and vice-versa. Why does this happen? The answer becomes obvious once you think about it: we're dealing with different levels of abstraction.

Take for example the "Step 4" state in the state machine above. That's not really a state _per se_: it represents instead a set of four states, all of which have `"SimpleFlow\Step4"` as part of them. The same goes for the [state machine earlier in the blog post](#iteration-2-loop-based-na%C3%AFve-approach): these 4 states and their transitions exist within the wider context of a higher-level `flowpath`.

Answering the question posed at the end of the previous chapter: does it make sense to reason about this load scenario as a gargantuan state machine with 40 nodes? No: the 10 step state machine is fine, as long as you always keep in mind that each step is actually a mini state machine.

The image below should make this idea of nested state machines clearer, by highlighting the states hidden by "Step 1" and "Step 2" of the state machine in the previous chapter.

<figure>
    <img src="/images/state-machine-abstraction.excalidraw.svg" alt="The load scenario as a state machine">
</figure>

### Using state handlers to structure our load test as a state machine

So we decided that structuring our scenario as a state machine is the way to go. So, how do we go about it? Our MES is in charge of keeping track of the state of the wafers, so the part we're responsible for are the state transitions.[^9] With that in mind our goal will be to answer this simple question:

[^9]: It does make sense if you think about it: the MES operations we've been calling in out load tests can be thought of as state transitions for the wafer.

> Given a wafer's current state, what transition should the wafer undertake?

To tackle this question, let me introduce the concept of a state handler:

```python
def state_handler(wafer: Wafer) -> Tuple[str, Enum]:
    # do something
    return (wafer.flowpath, wafer.system_state)
```

A state handler takes a wafer as an input, performs some action, and then returns the updated wafer state. To determine which state handler should be applied to which wafer state, a handler table is used:

```python
handler_table = [
    (state1, handler1),
    (state2, handler2),
    (state3, handler3),
    (state4, handler4),
]
```

The handler table supports the usage of wildcards (`*`) for the flowpath, which enables handler reuse. State matches are assessed from top to bottom, meaning that if there are multiple potential matches in the handler table, the one that appears first will always take priority. This enables the possibility of having handlers that handle specific scenarios at the top of the table, while catch-all handlers would sit at the bottom.

With these details in mind, a handler table for the original load scenario would look something like this:

```python
handler_table = [
    (("SimpleFlow\Step10", Processed),  terminate_handler),
    (("*",                 Queued),     dispatch_handler),
    (("*",                 Dispatched), track_in_handler),
    (("*",                 InProcess),  track_out_handler),
    (("*",                 Processed),  move_next_handler),
]
```

By taking advantage of the wildcards, our handler table can be kept very succinct.

## The stateful load generator pattern

Our final goal will be to rewrite our load scenario using what we learned in the [previous chapter](#state-machines-to-the-rescue). Lets start with the handlers:

```python
from random import random

def dispatch_handler(wafer: Wafer):
    machine = get_valid_dispatch_candidates(wafer)[0] # Get the first machine in the list
    wafer.dispatch(machine)
    return (wafer.flowpath, wafer.system_state)

def track_in_handler(wafer: Wafer):
    wafer.track_in()
    return (wafer.flowpath, wafer.system_state)

def track_out_handler(wafer: Wafer):
    wafer.track_out()
    return (wafer.flowpath, wafer.system_state)

def move_next_handler(wafer: Wafer):
    wafer.move_next()
    return (wafer.flowpath, wafer.system_state)

def terminate_handler(wafer: Wafer):
    wafer.terminate()
    # By returning None, the goal is to not match with any row in the handler table
    # and stop the execution of handlers on this wafer
    return None

### Handlers for the extra requirements

def track_in_and_maybe_abort_handler(wafer: Wafer):
    wafer.track_in()

    if random() < 0.03:
        wafer.abort()

    return (wafer.flowpath, wafer.system_state)

def dispatch_handler_or_maybe_skip_handler(wafer: Wafer):
    if random() < 0.9:
        wafer.skip_flowpath()
    else:
        dispatch_handler(wafer)

    return (wafer.flowpath, wafer.system_state)
    
def track_in_and_maybe_report_defect_handler(wafer: Wafer):
    wafer.track_in()

    if random() < 0.007:
        wafer.report_defect()

    # We don't have to worry about the wafer being sent to a previous step on track-out,
    # the state machine and the handler table will simply deal with it.
    return (wafer.flowpath, wafer.system_state)
```

Now let's build our handler table:

```python
handler_table = [
    # Specific handlers for the extra requirements
    (("SimpleFlow\Step2", Dispatched),  track_in_and_maybe_abort_handler),
    (("SimpleFlow\Step4", Queued),      dispatch_handler_or_maybe_skip_handler),
    (("SimpleFlow\Step9", Dispatched),  track_in_and_maybe_report_defect_handler),

    # Terminate handler
    (("SimpleFlow\Step10", Processed),  terminate_handler),

    # Catch-all handlers
    (("*",                 Queued),     dispatch_handler),
    (("*",                 Dispatched), track_in_handler),
    (("*",                 InProcess),  track_out_handler),
    (("*",                 Processed),  move_next_handler),
]
```

Notice the flatness of our code so far: need to apply the same handler to different flowpaths? Add another row to the handler table. Need to implement a new requirement? Write a new handler and add it to the handler table. We also get really good scalability: the source file might grow in size, but it's complexity will remain the same.

## Conclusion
