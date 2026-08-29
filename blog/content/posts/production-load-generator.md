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

This is a problem for us at [Critical Manufacturing](https://www.criticalmanufacturing.com/): how do we ensure that our MES will work as expected at the factory, _before actually deploying_ our MES in said factory? This is not a problem solved by standard functional testing - functional tests do help by validating that features work as expected, but what will happen when the factory is producing at full capacity, putting the MES under maximum load?

This is an especially pertinent problem in the electronics industry: a very nasty mix of high production volumes combined with onerous traceability and quality tracking requirements will bring your MES to its knees if you are not careful! It is therefore critical for projects in this industry to understand how their MES customizations behave under very high loads, because that's the harsh reality in which the MES system will operate, day in and day out.

<figure>
    <img src="/images/production-load-generator/qualitel.png" alt="A screenshot of a broken trace.">
    <figcaption>A factory with 3 SMT lines. You are reading this blog post with a screen powered by electronic circuit boards, which came from a manufacturing line of this kind.<br>(image source: <a href="https://www.qualitel.com/what-is-smt-manufacturing/">Qualitel</a>)</figcaption>
</figure>

There is only one issue: how do you simulate realistic factory conditions, without running the MES against a real factory?
<!-- TODO this sentence still need work maybe talk about electronics being our primary target-->

## Introducing the Production Load Generator

The Production Load Generator project (more informally known as "PLG") is an internal tool that stress-tests an MES system by simulating the factory that the MES system is being built for, hence the name: it's a **Load Generator** that replicates the **Production Load** of a factory.

This simulation-first approach means that this tool is a very good factory simulator in its own right, and it therefore represents a significant leap forward in [Critical Manufacturing](https://www.criticalmanufacturing.com/)'s internal factory simulation capabilities.

The Production Load Generator has been developed with one core tenet: **ease of use**. The reason for this is that teams only adopt new tools if their benefits far outweigh their learning curve[^1]. Therefore, every decision I made during the development of this project was done with the goal to make the usage of the PLG as pleasant as it can possibly be.

[^1]: This obviously assuming that you get to choose whether or not you want to use a given tool. Many times this isn't the case and you get a mandate from above to use xyz tool.

Now that you are more aware of the philosophy and motivations behind the Production Load Generator, let's move on to more technical matters.

## So... how does it work?

You can get started with the PLG by setting up a standalone C# project with following `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="Cmf.LightBusinessObjects">
      <HintPath>..\Libs\LBOs\NetStandard\Cmf.LightBusinessObjects.dll</HintPath>
    </Reference>

    <PackageReference Include="Cmf.Common.TestUtilities" Version="11.2.0.1227846" />
    <PackageReference Include="Cmf.ProductionLoadGenerator" Version="0.4.3" />
    <PackageReference Include="Cmf.LoadBalancing" Version="11.2.5" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="appsettings.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

</Project>
```

Let's go through every reference to understand their purpose:

1. The `Cmf.LightBusinessObjects.dll` is an automatically generated DLL that wraps every single API provided by the MES with C# methods and objects. Every single API call done by the load generator to the MES goes through this DLL.
2. The `Cmf.Common.TestUtilities` and `Cmf.LoadBalancing` nugets play a supporting role to the `Cmf.LightBusinessObjects.dll`: they are here to make interacting with the MES via LBOs a far more ergonomic experience.
3. The `Cmf.ProductionLoadGenerator` is our load generator. It's written in async C# (for scalability purposes) and can be used either in a standalone fashion, or as a smaller component in a larger testing framework.
4. `appsettings.json` contains the settings for our load scenario.

You might be wondering: why C#? The reason is quite simple: Critical Manufacturing lives and breathes C#. Our backend is written in this language, and so are our tests. 

## Excellent documentation is the bare minimum

## Early results

40 simulated lines

Testimonies TODO

## Final thoughs

The project I'm most proud of
