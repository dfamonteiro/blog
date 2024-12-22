+++ 
draft = true
date = 2024-12-15T23:42:06Z
title = "Solving the mathematics of Factorio Quality: Pure Recycler Loop"
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

The pure recycler loop is the simplest possible way to grind for legendary items, making it the logical next step of our quality analysis journey. Armed with the knowledge we gained from the [previous post](/posts/factorio-quality-1/) of [this series](/tags/factorio-quality/), we're going to calculate the efficiency of this quality grinding method and do some statistical analysis of how the production of quality items is affected by the quality chance of the recyclers.

<div style="text-align:center">
    <img src="/images/Pure-Recycler-Loop.webp" alt="Blue circuit crafting recipe"/>
    <figcaption> Example of a pure recycler loop setup</figcaption>
</div>

## Unrolling the infinite loop

Before we start crafting production matrices, we need to figure out a way to deal with the outputs of the system getting fed back into the system:

<pre style="text-align:center" class="mermaid">
---
title: Conceptual model of a pure recycler loop
config:
  theme: dark
---
flowchart LR
    A[Item Input] --> B[Recycler]
    B -->|Q<=4| B
    B -->|Q=5| C[Q5 Storage]
</pre>
Notice how I'm using "Q" as a shorthand for the level of quality of the items. Q=1 means common items and Q=5 means legendary items.

Finding a way to get around loops is never easy. Fortunately for us, we can take a page from the compiler optimization book and unroll the loop:

<pre style="text-align:center" class="mermaid">
---
title: Conceptual model of a pure recycler loop, unrolled into an infinite line
config:
  theme: dark
---
flowchart TD
    A[Item Input] --> R0
    R0 -->|Q<=4| R1
    R0 -->|Q=5| END[Q5 Storage]

    R1 -->|Q=5| END
    R1 -->|Q<=4| R2

    R2 -->|Q=5| END
    R2 -->|Q<=4|R3

    R3 -->|Q=5| END
    R3 -->|Q<=4|Rn[Rn]

    Rn -->|Q=5| END
    Rn -->|Q<=4|Rn1[Rn+1]

    Rn1 -->|Q=5| END
    Rn1 -->|Q<=4|Rn2[...]
</pre>

Doing this trick does get rid of the loop and gives us a more workable linear problem. Unfortunately, this comes at a cost of having to handle the recycler line being theoretically infinite. In practice, the system runs out of items very quickly because 3/4 of the items are voided in every step of the recycler chain and the items that don't get voided will eventually turn into legendary items and be removed from the system.

## Calculating the production rate of the infinite recycler line

In order to get the total production of Q5 items (the fifth value of vector $\vec{s}$), we have to add the Q5 production of every single recycler in the infinite chain:

$$ \vec{s} = \vec{s_1} + \vec{s_2} + \vec{s_3} + \vec{s_4} + \vec{s_n} + \vec{s_{n+1}} + ...$$

$\vec{s_x}$ is the state of the system after recycler $x$ and can be represented in the following manner:

$$ \vec{s_x} = \vec{s_{x-1}} \cdot R_q \newline
\vec{s_1} = \vec{f}$$
<!-- $$ $$ -->

$R_q$ is a recycler production matrix with quality chance $q$, and a production ratio of 0.25 for Q1-Q4 and 0 for Q5 to simulate the legendary items being removed from the system and put into a box. Maybe a function to create these recycler matrices with ease could be useful:

```python
def recycler_matrix(quality_chance : float, quality_to_keep : int = 5) -> np.ndarray:
    """Returns a matrix of a recycler with quality chance `quality_chance`
    that saves any item of quality level `quality_to_keep` or above.

    Args:
        quality_chance (float): Quality chance of the recyclers (in %).
        quality_to_keep (int): Minimum quality level of the items to be removed from the system
            (By default only removes legendaries).

    Returns:
        np.ndarray: Standard production matrix.
    """
    assert quality_chance > 0
    assert type(quality_to_keep) == int and 1 <= quality_to_keep <= 5

    recycling_rows = quality_to_keep - 1
    saving_rows = 5 - recycling_rows

    return custom_production_matrix(
        [(quality_chance, 0.25)] * recycling_rows + [(0, 0)] * saving_rows
    )
```
