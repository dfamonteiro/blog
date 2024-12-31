+++ 
draft = true
date = 2024-12-28T22:09:26Z
title = "Solving the mathematics of Factorio Quality: Recycler-Assembler Loop"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["maths", "programming", "Factorio Quality"]
categories = []
externalLink = ""
series = []
+++

<script type="module">
    import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
    mermaid.initialize({ startOnLoad: true });
</script>

The recycler-assembler loop is probably the most commonly used quality grinding method in Factorio. It is a two step looping process where in the first step, the ingredients are crafted by an assembler into items, which are then recycled back into ingredients. By having quality and productivity[^1] modules in both the assembling and the recycling steps, the quality and items and ingredients in the loop will eventually improve all the way to legendary. Emulating a recycler-assembler loop is slightly more complex than the [pure recycling loops](/posts/factorio-pure-recycler-loop/) we have dealt with previously, but the same fundamental ideas still apply.

[^1]: Productivity modules are only allowed under certain circumstances in the assembling step, whereas quality modules are always allowed in both the recycling and assembling steps.

<div style="text-align:center">
    <img src="/images/factorio-recycler-assembler-loop.webp" alt="Blue circuit crafting recipe"/>
    <figcaption> Example of a recycler-assembler loop setup. <br> (Credit for this design: <a href="https://youtu.be/Z1BEXm4RIfs?si=XCf6b7F-cjY_5G5E&t=584">Konage</a>)</figcaption>
</div>

## Unrolling the infinite loop

Before we start crafting transition matrices, we need to figure out a way to deal with the outputs of the system getting fed back into the system[^2]:

[^2]: You might be getting a strong feeling of _déjà vu_ if you have already read "[Solving the mathematics of Factorio Quality: Pure Recycler Loop](/posts/factorio-pure-recycler-loop/)" previously.

<pre style="text-align:center" class="mermaid">
---
title: Conceptual model of a recycler-assembler loop
config:
  theme: dark
---
flowchart TD
    A[Ingredient Input] -->|Q0 Ingredients| B[Assembler]
    B -->|Q<=4 Items| C[Recycler]
    C -->|Q<=4 Ingredients| B
    B -->|Q=5 Items| D[Q5 Item Storage]
    C -->|Q=5 Ingredients| E[Q5 Ingredient Storage]
</pre>
Notice how I'm using "Q" as a shorthand for the level of quality of the items. Q=1 means common items and Q=5 means legendary items.

Finding a way to get around loops is never easy. Fortunately for us, we can take a page from a compiler optimization book and unroll the loop:

<pre style="text-align:center" class="mermaid">
---
title: Conceptual model of a pure recycler loop, unrolled into an infinite line
config:
  theme: dark
---
flowchart TD
    A[Ingredient Input] --> A0
    A0 -->|Q<=4 Items| R0
    A0 -->|Q5 Items| END[Q5 Item/Ingredient Storage]

    R0 -->|Q5 Ingredients| END
    R0 -->|Q<=4 Ingredients| A1

    A1 -->|Q5 Items| END
    A1 -->|Q<=4 Items| R1

    R1 -->|Q5 Ingredients| END
    R1 -->|Q<=4 Ingredients|An[An]

    An -->|Q5 Items| END
    An -->|Q<=4 Items|Rn1[Rn]

    Rn1 -->|Q5 Ingredients| END
    Rn1 -->|Q<=4 Ingredients|Rn2[...]
</pre>

Doing this trick does get rid of the loop and gives us a more workable linear problem. Unfortunately, this comes at a cost of having to handle the recycler-assembler line being theoretically infinite. In practice, the system runs out of materials very quickly because all the materials that don't get voided will eventually turn into legendary items/ingredients and be removed from the system.
