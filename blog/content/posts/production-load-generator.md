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

This eponymous load generator works by treating the materials processed by the factory as independent concurrent state machines whose state represents their manufacturing progress, and are able to transition between states by executing MES services.

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

<!-- 
TODO IoT
TODO observability screenshots -->

While the `ProductionLoadGenerator` is an incredibly versatile simulator, it struggles with modelling queue-based manufacturing processes, such as SMT Lines and car assembly lines. The `LineLoadGenerator` is a load generator designed exactly for this purpose: **simulating manufacturing lines**.

<figure>
    <img src="/images/production-load-generator/assembly-line.png" alt="The Wafer system state loop">
    <figcaption>A Boeing 787 assembly line in North Charleston, South Carolina. Issues at one of the assembly stations can result in cascading delays for every airframe that is blocked by lack of progress at the disrupted station. In other words: you have a traffic jam until the bottleneck is fixed.<br>(image source: <a href="https://www.seattletimes.com/business/boeing-aerospace/parts-delays-force-boeing-to-slow-787-jet-assembly-line-in-s-c/">The Seattle Times</a>)</figcaption>
</figure>

The `LineLoadGenerator` has two core components: the `LineEquipment` class which represents a singular machine with inputs and outputs, and the `LineLoadGenerator` which is responsible for piecing and wiring these `LineEquipment` objects together like they're legos.

#### LineEquipment

The `LineEquipment` class is the core building block of the `LineLoadGenerator`: it represents an individual piece of equipment in a manufacturing line: a conveyor belt, a P&P machine, an SMT oven, etc. It has three core properties: the **name**, the **number of inputs** and the **number of outputs**.

The `LineEquipment` is an **abstract class**, meaning that in order to use this class you will need to first create a class that inherits from `LineEquipment` and implements the `RunAsync` method. This method governs the behaviour of the line equipment you are trying to emulate.

```csharp
/// <summary>
/// Represents a generic MES resource that receives a panel from the input, 
/// tracks the panel in and out in the MES, and sends the panel to the next line equipment.
/// </summary>
class MESResource : LineEquipment
{
    public MESResource(string name) : base(name) {}

    public async override Task SetupAsync()
    {
        // Line equipment setup logic goes here, if necessary
        // Implementation of this method is optional
    }

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        Resource resource = await GenericGetsScenarioAsync.GetObjectByNameAsync<Resource>(Name);

        while (true)
        {
            Material panel = await ReceiveAsync(cancellationToken);

            await resource.LoadAsync();
            await panel.ComplexTrackInAsync(resource);

            await resource.LoadAsync();
            await panel.ComplexTrackOutMaterialAsync();

            await SendAsync(panel, cancellationToken);
        }
    }
}
```

#### LineLoadGenerator

The `LineLoadGenerator` is where the the `LineEquipment` building blocks are linked together into simulated manufacturing lines.

The following `LineLoadGenerator` code:

```csharp
LineLoadGenerator SMTLine = new LineLoadGenerator()
    .SetName("SMT Line")
    .AddEquipment(new MESResource("PRT01"))
    .AddEquipment(new MESResource("SPI01"))
    .AddEquipment(new MESResource("PnP01"))
    .AddEquipment(new MESResource("PnP02"))
    .AddEquipment(new MESResource("PnP03"))
    .AddEquipment(new MESResource("OVN01"))
    .AddEquipment(new MESResource("AOI01"));
```

Would result in the following manufacturing line:

<figure>
    <img src="/images/production-load-generator/BasicSMTLineDiagram.excalidraw.png" alt="A very simple simulated SMT line">
    <figcaption>A very simple simulated SMT line.</figcaption>
</figure>

Notice how all the `LineEquipment` are linked for you - by default, the newly added simulated equipment is automatically connected to the equipment at the end of the line. Sometimes you get to have nice things!

## Early results look promising

The Production Load Generator has already been used by some teams to validate some performance-critical scenarios, and so far the feedback I've gotten by my colleagues is that it's pretty intuitive to use, which is a massive relief! One of the goals of the PLG is to make load tests against our MES dramatically easier to perform - mission completed, it seems.

I terms of performance, the PLG has proven to be so effective at stress-testing the MES that teams are becoming limited by our internal development infrastructure - to the point that requests for dedicated database hardware are now being made to open the door for bigger load tests. This request for more hardware is completely understandable, though... if you want to simulate a factory with 40 SMT lines running concurrently, there's no going around it: [you're gonna need a bigger boat](https://www.youtube.com/watch?v=2I91DJZKRxs).

<figure>
    <img src="/images/production-load-generator/bigger-boat.png" alt="A shark named PLG attacking a boat named MES">
    <figcaption>Extremely accurate visualization of the PLG stress-testing our MES.</figcaption>
</figure>

And last but not least, we need to discuss how teams are collecting performance data from their load tests. While the PLG is capable of logging every HTTP request it makes (via the `HttpClient`'s [ActivitySource](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-builtin-activities#http-client-request)), teams prefer to lean on our MES's excellent [observability](https://www.criticalmanufacturing.com/observability/) dashboards. One key advantage on leaning on the MES itself for monitoring its performance, is that the techniques used to analyse the performance of the MES during the load test are directly transferrable to analysing the performance of the MES in production.

## Final thoughts

It's very rare to be given the opportunity to start a completely a new project from scratch, and I couldn't be happier with how the Production Load Generator is flourishing. To get this far it took a lot of effort from a lot of people, to whom I'm eternally grateful:

- I'd like to thank [Óscar Martins](https://www.linkedin.com/in/oscarmartins/) for the political sponsorship and [Miguel Torres](https://www.linkedin.com/in/miguelangelotorres/) for the technical sponsorship & guidance.
- I'd like to thank [Fábio Reis](https://www.linkedin.com/in/fabioreis23/) for promoting this tool to other parts of the company.
- I'd like to thank the entire Electronics Template team for their contributions to the PLG. In no particular order, thank you to [Eduardo Oliveira](https://www.linkedin.com/in/eduardoliv/), [Flávio Rodrigues](https://www.linkedin.com/in/fl%C3%A1vio-rodrigues-63505110b/), [Erik Barba](https://www.linkedin.com/in/erik-alejandro-olalde-barba-b062251ab/), [Roberto García](https://www.linkedin.com/in/robertramgar/), Oscar Trejo, [Isaac Mateus](https://www.linkedin.com/in/isaac-mateus-b30240207/), [Daniel Pereira](https://www.linkedin.com/in/daniel-pereira-902905153/), and [Pedro Limas](https://www.linkedin.com/in/pedrolimas/).
- I would like to thank [Diogo Paredes](https://www.linkedin.com/in/diogo-paredes/), [Eliana Vieira](https://www.linkedin.com/in/eliana-vieira-553710100/) and [Gonçalo Dias](https://www.linkedin.com/in/gon%C3%A7alo-dias/) for taking a chance on this project, with a special thank you to [Francisco Azevedo](https://www.linkedin.com/in/francisco-azevedo-48bb0b16b/) for his extensive feedback.

And finally, I would like to thank you for reading my blog post!
