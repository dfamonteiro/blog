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

The Production Load Generator is equal parts a load generator and a factory simulator, which makes it very useful for other purposes within [Critical Manufacturing](https://www.criticalmanufacturing.com/), such as showcasing features of the MES that can only be assessed properly when the MES is running around the clock (for example, our reports and dashboards).

Now that you get the broad strokes of what the Production Load Generator is meant to be, let's see how it works in practice.

## So... how does it work?

The first thing you should know about the PLG is that it's not a load generator application that works out-of-the-box.[^2] It is instead an async C# project that provides you with the tools you need to write your load tests (_a la_ [K6](https://k6.io/) or [NBomber](https://nbomber.com/)). You are responsible for setting up the business logic, and the PLG is responsible for executing your business logic in a way that accurately represents a factory's production flow.

[^2]: But you can absolutely develop a load generator application backed by the PLG if you want!

### Getting started

In order to help users of the Production Load Generator getting started with creating scenarios, we provide a `LoadScenarioRunner` class that acts as the entry point of the application. This class is responsible for connecting to a specific MES environment and running the specified load scenario against that environment.[^3]

[^3]: Providing this component also comes with another advantage: standardization across projects of how load scenarios are defined, configured, and run.

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        await new LoadScenarioRunner()
            .AddLoadScenarios([
                new FirstLoadScenario() // Add your load scenarios here
            ])
            .SetDefaultConfiguration() // Load the appsettings.json file and the env variables
            .RunAsync();
    }
}
```

Which load scenario is executed, for how long, and against which MES environment is determined by the `appsettings.json` file:

```json
{
    "TargetEnvironment": "Local",
    "ScenarioToRun": "First load scenario",
    "ScenarioDuration": "00:01:00",

    "Environments": {
        "Local": {
            "HostAddress": "localhost:80",
            "ClientTenantName": "IndustryTemplates",

            "IsUsingLoadBalancer": false,
            "UseSSL": false,

            "SecurityPortalClientId": "MES",
            "SecurityPortalBaseAddress": "http://localhost/SecurityPortal/",
            "SecurityPortalAccessToken": "[Your PAT]",

            "Culture": "en-US"
        }
    }
}
```

### Creating a load scenario

Creating a new load scenario is as easy as creating a new class that implements the PLG's `ILoadScenario` interface:

```csharp
internal class FirstLoadScenario : ILoadScenario
{
    public string ScenarioName => "First load scenario";

    // Sets up the load scenario.
    public async Task SetupAsync(IConfiguration configuration)
    {
        // ...
    }

    // Runs the load scenario.
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // ...
    }

    // Cleanup called after the load scenario execution.
    public async Task TeardownAsync()
    {
        // ...
    }
}
```

The methods in this class should be pretty self-explanatory: you setup your load generators and the MES in `SetupAsync`, run the load generators in `RunAsync` until the `cancellationToken` is triggered, and finally revert all MES configurations to their original state in `TeardownAsync`.

-----------------

Everything that I've shown so far is just generic infrastructure for running load tests in a standardized manner and doesn't differ that much from other publicly available load generators. It is nevertheless a necessary foundation on top of which the PLG's load generators run on.

These load generator classes are the reason for the PLG's existence: they are _excellent_ at simulating manufacturing processes, and are what makes the Production Load Generator uniquely suited for [Critical Manufacturing](https://www.criticalmanufacturing.com/)'s factory simulation needs:

### The ProductionLoadGenerator class

This eponymous load generator works by treating the materials processed by the factory as independent concurrent state machines whose state represents their manufacturing progress, and are able to transition between states by executing MES services. When the materials reach an unrecognized state, they are dropped by the load generator.

Users are able to define a state machine for the material by defining **handlers** which map a particular state to an action. For example, the following state handler table:

$$\begin{array}{ll} \hline \mathbf{State\ Pattern} & \mathbf{Action} \\\\ \hline \mathtt{\ast @Queued} & \mathtt{dispatch()} \\\\ \mathtt{\ast @Dispatched} & \mathtt{track\\_in()} \\\\ \mathtt{\ast @InProcess} & \mathtt{track\\_out()} \\\\ \mathtt{\ast @Processed} & \mathtt{move\\_next()} \\\\ \hline \end{array}$$

Would yield the following state machine:

<figure>
    <img src="/images/wafer-system-state-loop.excalidraw.svg" alt="The Wafer system state loop">
    <figcaption>A very simple state machine.</figcaption>
</figure>

In practice, writing the actual code requires a bit more business logic, but the core idea remains unchanged:

```csharp
public async Task RunAsync()
{
    ProductionLoadGenerator loadGenerator = new ProductionLoadGenerator()
        .AddHandler(new Handler
        {
            Name = "Queued Handler",
            StatePattern = "*@Queued",
            Handle = async input =>
            {
                // Find a resource to dispatch our material to
                Resource dispatchResource = await input.Entity.GetResourceForDispatchAsync();

                // Dispatch our material
                await input.Entity.DispatchAsync(dispatchResource);

                // Return the new material state.
                // This string will be used by the PLG to find a new handler for the material.
                return input.Entity.State();
            },
        })
        .AddHandler(new Handler
        {
            Name = "Dispatched Handler",
            StatePattern = "*@Dispatched",
            Handle = async input =>
            {
                await input.Entity.TrackInAsync();
                return input.Entity.State();
            },
        })
        .AddHandler(new Handler
        {
            Name = "InProcess Handler",
            StatePattern = "*@InProcess",
            Handle = async input =>
            {
                await input.Entity.TrackOutAsync();
                return input.Entity.State();
            },
        })
        .AddHandler(new Handler
        {
            Name = "Processed Handler",
            StatePattern = "*@Processed",
            Handle = async input =>
            {
                await input.Entity.ComplexMoveNextAsync();
                return input.Entity.State();
            },
        });
}
```

It's understandably difficult to get an intuition for how this load generator works just from reading this short sinopsys. If you are looking for more details on the inner workings and motivations behind this load generator, I suggest reading this [blog post](/posts/stateful-load-generators/) which delves deep into this state machine-based load generator concept.

### The LineLoadGenerator class

TODO

<!-- While PLG focused on the materials, LLG focused on the machines that process the materials -->

<!-- While the `ProductionLoadGenerator` can (albeit with varying degrees of effort) simulate most manufacturing processes you can thing of, it's not a one-size-fits all solution. For example, it struggles with manufacturing lines -->

## Early results

TODO

## Final thoughts

It's very rare to have the opportunity to start a completely a new project from start blabla
