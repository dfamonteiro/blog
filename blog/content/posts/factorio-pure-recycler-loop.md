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

$\vec{s_x}$ is the state of the system after recycler $x$ and can be represented in the following manner[^1]:

[^1]: It does make sense that $\vec{s_x}$ is recursive. After all, if you want to know the state of the system after the 7th recycler, you need the state of the 6th recycler, which in turn needs the 5th recycler etc etc.

$$ \vec{s_x} = \vec{s_{x-1}} \cdot R_q \newline
\vec{s_1} = \vec{f}$$

$\vec{f}$ represents the input into the system, let's assume assume that it will be a single belt of Q1 items <nobr>($\vec{f}= \begin{bmatrix} 1 & 0 & 0 & 0 & 0 \end{bmatrix}$)</nobr>. $R_q$ is a recycler production matrix with quality chance $q$, and a production ratio of 0.25 for Q1-Q4 and 0 for Q5 to simulate the legendary items being removed from the system and put into a box. Let's write a function to help us create these recycler matrices:

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

For the remainder of this chapter, let's assume that we're working with a quality chance $q$ of 10%:

```python
print(recycler_matrix(10))
```

$$ R_{10} = \begin{bmatrix}
  0.225 & 0.0225 & 0.00225 & 0.000225 & 0.000025 \\\\
  0     & 0.225  & 0.0225  & 0.00225  & 0.00025 \\\\
  0     & 0      & 0.225   & 0.0225   & 0.0025 \\\\
  0     & 0      & 0       & 0.225    & 0.025 \\\\
  0     & 0      & 0       & 0        & 0 \\\\
\end{bmatrix}$$

We have everything we need to compute $\vec{s}$, but instead of calculating a neverending sum of recursive functions, allow me to present a simpler alternative with a simple `for`-loop[^2]:

[^2]: Please note that I cut a big part of the `recycler_loop`'s doc string to make it more readable in the context of this blogpost. If you want the full description of this function, please click [here](https://github.com/dfamonteiro/blog/blob/main/Factorio%20Quality/pure_recycler_loop.py#L28C1-L57).

```python
def recycler_loop(
        input_vector : Union[np.array, float],
        quality_chance : float,
        quality_to_keep : int = 5) -> np.ndarray:
    """Returns a vector with values for each quality level that mean different things,
    depending on whether that quality is kept or recycled.

    Returns:
        np.ndarray: Vector with values for each quality level.
    """
    if type(input_vector) in (float, int):
        input_vector = np.array([input_vector, 0, 0, 0, 0])

    result_flows = [input_vector]
    while True:
        result_flows.append(
            result_flows[-1] @ recycler_matrix(quality_chance, quality_to_keep)
        )

        if sum(result_flows[-2] - result_flows[-1]) < 1E-10:
            # There's nothing left in the system
            break

    return sum(result_flows)
```

### Basic analysis of the function `recycler_loop`'s output

Let's see what happens when we call this function:

```python
recycler_loop(1, 10)
```

$$\vec{s} = \begin{bmatrix} 1.29032258 & 0.03746098 & 0.00483367 & 0.0006237 & 0.0000693 \end{bmatrix}$$

And here we have our first big result: if you feed a belt of common items into a recycler loop with T3 normal modules, you'll get an output of 0.0000693 belts of legendary items, which means that this specific setup has an efficiency of 0.00693%. If we invert this number, we get the number of normal items required to craft one legendary item, which is 14430... yikes, that's quite a bit.

What happens if we use legendary quality modules? Well, in that case the quality chance increases to 24.8%, the efficiency of the recycler loop setup increases to 0.03667% and the number of normal items required to craft a legendary item decreases to 2726.91[^3]: a >5x improvement but still quite inefficient when compared when compared to other quality grinding methods.

[^3]: This is consistent with the results [other](https://www.reddit.com/r/factorio/comments/1gxu6im/comment/lyjujzg/) [people](https://youtu.be/O2QcGNIlFCk?si=rc4xe1xtAySzCEI7&t=120) have been getting.

Finally, we need to understand what that the other 4 values in the return vector mean: they represent the _internal_ flow of the system when it reaches a steady state. If we add the first 4 values of the vector we get 1.332844, which is the number of belts that the recycler loop needs to support in order for 1 input belt to be fully consumed. Should we wish to get the production rates for lower qualities, we would need to set the `quality_to_keep` parameter to a lower value.

## Statistical analysis
