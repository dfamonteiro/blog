+++ 
draft = true
date = 2025-12-30T11:20:18Z
title = "Diagnosing performance issues in .NET applications with dotnet-trace and Perfetto"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Programming", "Tracing"]
categories = []
externalLink = ""
series = []
+++

As the software industry transitioned towards more cloud-centric environments, programming languages have had to focus on treating virtualized Linux environments as first-class citizens or risk getting left behind. C# was no exception and for this language to maintain its popularity, Microsoft had to perform a [complete overhaul](https://devblogs.microsoft.com/dotnet/announcing-net-core-1-0/) of the .NET runtime to make it cloud-ready.

As part of this initiative to make .NET platform-agnostic, a [supporting cast of CLI diagnostic tools](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/tools-overview) was [developed from scratch](https://devblogs.microsoft.com/dotnet/introducing-diagnostics-improvements-in-net-core-3-0) with the goal of uniformizing the developer experience of diagnosing C# applications across operating systems. One of these tools is `dotnet-trace`: a tool that can be hooked into a running .NET application to collect tracing data which can then be analysed with trace viewers.

In an ongoing effort to popularize the usage of performance diagnostics tools during development at my workplace, I've been [working out the kinks](../dotnet-trace-100-limit) of using `dotnet-trace` to analyse the performance of our multithreaded [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) application. I have also been testing various trace viewers to analyse `dotnet-trace` files with, and in my opinion there is no discussion be had: `dotnet-trace` and the [Perfetto trace viewer](https://perfetto.dev/) are a match made in heaven, especially when you have to analyse complex trace files with multiple threads.

In this blog post I will show how you can use a combination of `dotnet-trace` & [Perfetto](https://perfetto.dev/) to collect traces from a running application and then perform post-processing on those traces, so that the focus is always on the parts of the call stack that matter to you.

## Collecting traces with dotnet-trace

### Installing dotnet-trace

If you don't have this tool already installed there are two ways of installing it:

1. `dotnet tool install --global dotnet-trace`
2. [Download the executable from the official page](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)

Please note that the `dotnet-trace` tool needs to be on the same execution environment as the target .NET application.[^1]

[^1]: I know there are ways to collect traces from outside of the docker container, but let's not complicate things.

If you're running your .NET application in a locked-down docker container, it might not be possible to run `dotnet tool install`, so your only option will be to do option 2: download the linux executable and move it to the container's filesystem by either:

- Running something like `docker cp dotnet-trace custom_host:/opt/app/dotnet-trace`[^2], or...
- Placing the executable in a folder that your .NET container can reach[^3]

[^2]: The name of my container is `custom_host` - replace it with your container's name whenever you see it.
[^3]: I personally know that `custom_host` will copy everything under `LocalEnvironment\BusinessTier`, so I just throw my `dotnet-trace` executable in there and it works like a charm!

### Running dotnet-trace

Now that `dotnet-trace` is installed, we can run it against our application. The first step towards that goal is figuring out in which process our application is running:

1. Log in to your container first if necessary

    ```txt
    docker exec -it custom_host /bin/bash
    ```

2. Run `dotnet-trace ps` to get a list of docker processes

    ```txt
    bash-4.4# ./dotnet-trace ps
     1  CmfEntrypoint  /usr/share/CmfEntrypoint/CmfEntrypoint  r=host --target-directory=/opt/app --allow-custom-certificates --distro=ubi8
    19  dotnet         /usr/lib64/dotnet/dotnet                dotnet Cmf.Foundation.Services.HostService.dll -p 8080
    ```

Our application is running on pid 19. Let's collect some data by running `dotnet-trace collect`! The target process is determined by the `-p` flag, and `--format Chromium` determines the intended format: a `.chromium.json` file that can be read by the Perfetto trace viewer.

```txt
bash-4.4# ./dotnet-trace collect -p 19 --format Chromium
No profile or providers specified, defaulting to trace profile 'cpu-sampling'

Provider Name                           Keywords            Level               Enabled By
Microsoft-DotNETCore-SampleProfiler     0x0000F00000000000  Informational(4)    --profile
Microsoft-Windows-DotNETRuntime         0x00000014C14FCCBD  Informational(4)    --profile

Process        : /usr/lib64/dotnet/dotnet
Output File    : /opt/app/dotnet_20251230_003728.nettrace
[00:00:00:06]   Recording trace 1.0175   (MB)
Press <Enter> or <Ctrl+C> to exit...
```

Now that `dotnet-trace` is collecting data, make sure that the application is doing something interesting: click some buttons, call some APIs, run some integration tests, do whatever you seem fit.

When you're done, press Enter or Ctrl+C to stop the collection process and create a trace file. You'll get some output similar to this:

```txt
Stopping the trace. This may take several minutes depending on the application being traced.

Trace completed.
Processing trace data file '/opt/app/dotnet_20251230_003728.nettrace' to create a new Chromium file '/opt/app/dotnet_20251230_003728.chromium.json'.
Conversion complete
```

### Retrieving the trace file

If our .NET application is running in a container, we might still need to rescue the trace file from the container's filesystem:

```txt
docker cp custom_host:/opt/app/dotnet_20251230_003728.chromium.json dotnet_20251230_003728.chromium.json
```

## Analysing traces with Perfetto

Now that we captured our tracing data, head over to the [Perfetto trace viewer webpage](https://ui.perfetto.dev/) and open your trace file. You should get something like this:

<figure>
    <img src="/images/dotnet-trace-perfetto/perfetto-opening-view.png" alt="The Perfetto trace viewer">
    <figcaption>The Perfetto trace viewer</figcaption>
</figure>

The trace file that will be analysed in this blog post was captured from [Critical Manufacturing](https://www.criticalmanufacturing.com/)'s host, which serves as the backend of the MES[^4] system sold by the company. This will naturally mean that the analysis done here will be tailored towards this application, but the techniques I will show here can be applied to any trace file you analyse with Perfetto.

[^4]: A _Manufacturing Execution System_ is a software system responsible for the bookkeeping of a factory's production. It is generally used in highly sophisticated industries, such as the semiconductor industry and the medical devices industry, where a high level of material tracking and control is required.

### [Navigating the Perfetto UI](https://perfetto.dev/docs/visualization/perfetto-ui)

These are the basic controls needed to navigate this trace viewer:

- Use the scroll wheel to scroll up and down the traces
- Use the WASD keys to zoom in and out (WS) and move right or left

If you click on a trace slice, a tab called "Current Selection" will show up in the bottom of the screen with all the information related to this slice. If you can't find your selected slice, you can press F to center the trace viewer on it.

<figure>
    <img src="/images/dotnet-trace-perfetto/current-selection.png" alt="The &quot;Current Selection&quot; tab with all the details of the selected slice">
    <figcaption>The "Current Selection" tab with all the details of the selected slice</figcaption>
</figure>

This is just the tip of the iceberg though: for more information on all things Perfetto, head over to their [documentation page](https://perfetto.dev/docs/).

### Taking a bird's eye view of the trace file

Before diving head-first into a service call, let's stay zoomed out and take stock of what's in this trace file.

Starting from the top, the first thing I notice are about 10 threads that are sitting around doing nothing of interest.

<figure>
    <img src="/images/dotnet-trace-perfetto/nothing-interesting.png" alt="Nothing interesting is going on here">
    <figcaption>Nothing interesting is going on here</figcaption>
</figure>

#### Thread 302

Actually, I'd like to make an exception: Thread 302 is checking every 5 seconds if there are any [integration entries](https://devblog.criticalmanufacturing.com/blog/20250429_integration_entries/) waiting to be executed.

<figure style="padding-bottom: 2em;">
    <img src="/images/dotnet-trace-perfetto/int-entries.png" alt="The thread wakes up every 5 seconds to check for new integration entries">
    <figcaption>The arrows point to the moments where this thread wakes up and checks for any new integration entries.</figcaption>
</figure>

<figure>
    <img src="/images/dotnet-trace-perfetto/int-entries-zoomed-in.png" alt="One of the spikes zoomed in">
    <figcaption>
        Zooming in on one of these moments confirms that indeed, this is part of the host machinery that makes integration entries work!
    </figcaption>
</figure>

#### The service call threads

Now that we've left the boring threads behind us, the only threads remaining are the threads that execute host services, which are **_by far_** the threads we care about the most! In the same way that all roads lead to Rome, every single MES service call goes through the host. It is therefore imperative that the host doesn't become a bottleneck for the MES by making sure these services are fast and access the database efficiently.

Lets now take a look at how these service threads look like in the Perfetto Trace viewer:

<figure style="padding-bottom: 2em;">
    <img src="/images/dotnet-trace-perfetto/394.png" alt="Thread 394">
    <figcaption>Thread 394</figcaption>
</figure>

<figure style="padding-bottom: 2em;">
    <img src="/images/dotnet-trace-perfetto/409.png" alt="Thread 409">
    <figcaption>Thread 409</figcaption>
</figure>

<figure style="padding-bottom: 2em;">
    <img src="/images/dotnet-trace-perfetto/411.png" alt="Thread 411">
    <figcaption>Thread 411</figcaption>
</figure>

<figure style="padding-bottom: 2em;">
    <img src="/images/dotnet-trace-perfetto/427.png" alt="Thread 427">
    <figcaption>Thread 427</figcaption>
</figure>

<figure style="padding-bottom: 2em;">
    <img src="/images/dotnet-trace-perfetto/449.png" alt="Thread 449">
    <figcaption>Thread 449</figcaption>
</figure>

<figure>
    <img src="/images/dotnet-trace-perfetto/450.png" alt="Thread 450">
    <figcaption>Thread 450</figcaption>
</figure>

These threads spend most of their time waiting for an service call, and once they receive an API request, they execute the requested service (represented by the call stack "towers" in the images above) and return an answer. The thicker these service calls are in the trace viewer, the longer they took to run.

##### dotnet-trace's limit of 100 stack frames

There is a detail in these pictures that I want to point out: in some threads, the `System.​Threading.​PortableThreadPool+​WorkerThread.​WorkerThreadStart()` base function goes continuously from the start to the end of the thread, but in the threads `409`, `449` and `450` you will notice that this function is interrupted in the spots indicated by the arrows. There is a reason for this: `dotnet-trace` can only capture a maximum of 100 stack frames, meaning that if your call stack is more than 100 stack frames long, the oldest frames get cut to meet this limit. Thankfully this can be [fixed](https://github.com/dfamonteiro/blog/blob/main/dotnet-trace/fix_spikes.py) but trust me, [figuring out a workaround for this limitation wasn't easy](../dotnet-trace-100-limit).

Using the script is quite straightforward:

```txt
PS C:\Users\Daniel\Desktop\github\blog\dotnet-trace> python .\fix_spikes.py .\dotnet_20251230_003728.chromium.json
fixed_dotnet_20251230_003728.chromium.json
```

### The anatomy of a host service call

We took the 30000-foot view in the previous section, now it's time to put one of these service calls under the microscope. These services' call stacks are absolutely massive (~100 stack frames), so we're going to start at the base of the trace and work our way down:

#### Part 1: ASP.NET middleware

<figure>
    <img src="/images/dotnet-trace-perfetto/pan1.png" alt="Picture with a column of stack frames">
</figure>

We start off with a bang and a metric ton of ASP.NET middleware. Just from this picture we can tell that our host is configured to have the following feautures:

- Rate Limiting
- CORS
- Authentication
- Authorization
- Logging
- etc.

#### Part 2: More middleware & handling of the HTTP request

<figure>
    <img src="/images/dotnet-trace-perfetto/pan2.png" alt="Picture with a column of stack frames">
</figure>

Going further down we get even more middleware, but perhaps more interestingly we have the first big time sink of the service call: parsing the incoming REST JSON request (43ms).

On the right side of the trace (towards the top-right of the picture) we can also see the HTTP answer being created and sent back to the caller of this service. To the surprise of absolutely no one, serializing the answer back to JSON is an order of magnitude quicker, clocking in at 4ms.

#### Part 3: DB transaction

<figure>
    <img src="/images/dotnet-trace-perfetto/pan3.png" alt="Picture with a column of stack frames">
</figure>

It's transaction time. The arrows indicate the time spent opening and closing this database transaction:

1. Cmf.Foundation.Common.HistoryObjectCollection.CreateTransaction(int64, String) - 4.25ms
2. Cmf.Foundation.Common.HistoryObjectCollection.CloseTransaction() - 4.1ms
3. System.Transactions.TransactionScope.Dispose() - 23ms

#### Part 4: Business logic

<figure>
    <img src="/images/dotnet-trace-perfetto/pan4.png" alt="Picture with a column of stack frames">
</figure>

We finally know what service is being called! FullUpdateObject() is being called to update a Product, we can tell this by looking at the call stack:

- Cmf.Services.GenericServiceManagement.GenericServiceController.FullUpdateObject()
  - Cmf.Foundation.[...].GenericServiceOrchestration.FullUpdateObject()
    - ...
    - Cmf.Navigo.BusinessObjects.**Product.Save()**
      - Database calls, etc

Every host service call is structured in this manner: The service layer calls the orchestration layer, which in turn calls other services and/or operations on the required entities.

### Making sense of all these service call traces

So, to take stock of the situation: every time we want to analyse the performance of a service, we have to skip 60 stack frames of middleware just to get to the business logic. Oh, and these service traces are spread across 10 different threads, so good luck finding the service you are interested in... well isn't that great.

Luckily for us we have an ace in our sleeve: how good is your SQL?

#### Introducing [PerfettoSQL](https://perfetto.dev/docs/analysis/perfetto-sql-getting-started)

Perfetto is already pretty good if you only use it as a basic trace viewer. What makes it exceptional for dealing with massive amounts of trace data though, is the ability to use SQL queries to analyse your traces, and we're going to make full use of this capability to make our life easier.

To access Perfetto's SQL editor, please select "Query (SQL)" on the left tab of your screen:

<figure>
    <img src="/images/dotnet-trace-perfetto/query-sql.png" alt="Perfetto's SQL editor">
    <figcaption>Perfetto's SQL editor</figcaption>
</figure>

Everything in your trace file is queryable through PerfettoSQL, but today we will only focus on the `slices` table. In the screenshot above we executed the following query:

```sql
select * from slices
```

This query will naturally feature every single function slice in the trace file. Crafting a query that will only return the service spans we're interested in takes a bit more nuance, but is perfectly doable:

```sql
select * from slices where name glob 'Cmf*.Services.*Controller.*';
```

<figure>
    <img src="/images/dotnet-trace-perfetto/services-query-screenshot.png" alt="Screenshot of a Perfetto SQL query">
    <figcaption>There are 121 service calls in this trace file</figcaption>
</figure>

#### Visualizing your queries with [debug tracks](https://perfetto.dev/docs/analysis/debug-tracks)

While a lot can be gleaned just from querying your trace file, we can go one step further and visualize our query results directly on the timeline where all the the other traces sit by creating a **debug track**.

Please follow the instructions below to create a debug track from a query:

<figure>
    <img src="/images/dotnet-trace-perfetto/show-debug-track.png" alt="Instructions for how to create a debug track">
    <figcaption>Switch to the <b>“Timeline”</b> view, then select the <b>“Standalone Query”</b> tab at the bottom of the screen, and finally click on <b>“Show Debug Track”</b> button</figcaption>
</figure>

Clicking "Add Track" will lead to the debug track being created and added to the top of your workspace, as can be seen here:

<figure>
    <img src="/images/dotnet-trace-perfetto/debug-track.png" alt="Screenshot of debug track">
    <figcaption>Notice the debug track named "Host Services"</figcaption>
</figure>

And there we go! What was previously information spread across multiple threads of execution, is now easily accessible through this debug track. And the cherry on top of the cake is that the slices on the debug track act as links to their "real" conterparts: for example, by selecting the purple span in the "Host Services" debug track (see image above) you can reach its counterpart on Thread 450.

### Pushing debug tracks to the limit

As you will have realized, debug tracks are an incredibly powerful visualization tool: it gives you the power to shape traces to your will. Take for example this query:

```sql
select * from slices where name glob 'Cmf.*';
```

It looks simplistic at first glance, but watch what happens when we create a debug track out of it:

<figure>
    <img src="/images/dotnet-trace-perfetto/cmf-asterisk.png" alt="Screenshot of debug track">
    <figcaption>Debug track created from the query above</figcaption>
</figure>

Just like that, we get all the business logic in a _single track_! Yes, this visualization is admittedly a bit rough around the edges, but it is amazing how much you get out of such a simple query.

And it goes without saying that the more effort you put into your SQL query, the better your debug track will look. This little tweak, for example, will cut most of the middleware slices:

```sql
select * from slices where name glob 'Cmf.*' and depth > 50;
```

<figure>
    <img src="/images/dotnet-trace-perfetto/cmf-asterisk-above-50.png" alt="Screenshot of debug track">
    <figcaption>Debug track created from the query above</figcaption>
</figure>

While this kind of works, it's a crude approach to a problem that requires more finesse. We must ask ourselves first: what would a perfect debug track look like?

For me, the answer is obvious:

> **I want a debug track where the service slices sit at the base of the debug track, and all of the business logic of these services is included**.

#### Striving for perfection

Conceptually speaking, the recipe for this "perfect trace" is easy to explain:

1. Take the slices from this query

    ```sql
    select * from slices where name glob 'Cmf.*';
    ```

2. And only keep the slices that are descendants of one of the slices from this query

    ```sql
    select * from slices where name glob 'Cmf*.Services.*Controller.*';
    ```

Turning concepts into reality with SQL can be tricky sometimes, but today we are in luck: this can be done with a simple `join` statement and Perfetto's [`slice_is_ancestor()`](https://perfetto.dev/docs/analysis/stdlib-docs#tags) utility function[^5].

[^5]: Please note that while writing this blog post I found an [issue](https://github.com/google/perfetto/issues/4207) with `slice_is_ancestor()` which you might also encounter when using this function. My [PR with the bugfix](https://github.com/google/perfetto/pull/4208) has already been merged, but it will take a while for the fix to reach the `stable` version of Perfetto.

```sql
select *
from
    (select id as service_id, track_id as service_track_id from slices where name glob 'Cmf*.Services.*Controller.*')
    join
    (select * from slices where name glob 'Cmf.*')
    on track_id=service_track_id -- Query optimization
where slice_is_ancestor(service_id, id) or service_id = id;
```

<figure>
    <img src="/images/dotnet-trace-perfetto/perfect.png" alt="Screenshot of a zoomed-in debug track">
    <figcaption>The resulting debug track created from the query above. Notice how the services are the base, and how all the business logic falls under them.</figcaption>
</figure>

We can be proud of what was accomplished here: we've gone from having our services spread across multiple threads and buried under a ton of middleware, to now having those same services and their business logic located on a single debug track, ready to be analysed with ease.

### Hunting for performance issues

Now that the hard work of creating a good debug track is done, we can finally achieve the original goal of this blog post: finding performance issues in our .NET application. As my host application is heavily I/O bound by the database, the performance issues I'm hunting are related to inefficient database access patterns - think multiple small loads instead of one big batch load.

These inefficent DB accesses can be easily spotted in the debug track by looking for visual repetitions. Here are a couple of examples:

<figure style="padding-bottom: 2em;">
    <img src="/images/dotnet-trace-perfetto/rep1.png" alt="Example of a inneficent database access pattern">
    <figcaption><code>ConfigurationController.FullUpdateConfig</code> validates and saves the input configs one by one on some sort of for-loop.</figcaption>
</figure>

<figure>
    <img src="/images/dotnet-trace-perfetto/rep2.png" alt="Example of a inneficent database access pattern">
    <figcaption>
        <code>NameGenerator.GenerateName</code> calls <code>INameGeneratorCollection.InternalLoad</code> and <code>NameGenerator.InternalGenerateName</code> for every name that is generated. At first glance, it looks to me that the number of database accesses scales linearly with the number of names to be generated, which is very much something we want to avoid at all costs.
    </figcaption>
</figure>

Once you're done analysing your trace file, you should have a list of performance issues to tackle.

## Next steps
