+++ 
draft = false
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
    <img src="/images/Pure-Recycler-Loop.webp" alt="Example of a pure recycler loop setup"/>
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
def recycler_matrix(
        quality_chance : float,
        quality_to_keep : int = 5,
        production_ratio : float = 0.25) -> np.ndarray:
    """Returns a matrix of a recycler with quality chance `quality_chance`
    that saves any item of quality level `quality_to_keep` or above.

    Args:
        quality_chance (float): Quality chance of the recyclers (in %).
        quality_to_keep (int): Minimum quality level of the items to be removed from the system
            (By default only removes legendaries).
        production_ratio (float): Productivity ratio of the recyclers (0.25 by default)

    Returns:
        np.ndarray: Standard production matrix.
    """
    assert quality_chance > 0
    assert type(quality_to_keep) == int and 1 <= quality_to_keep <= 5
    assert production_ratio >= 0

    recycling_rows = quality_to_keep - 1
    saving_rows = 5 - recycling_rows

    return custom_production_matrix(
        [(quality_chance, production_ratio)] * recycling_rows + [(0, 0)] * saving_rows
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
        quality_to_keep : int = 5,
        production_ratio : float = 0.25) -> np.ndarray:
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
            result_flows[-1] @ recycler_matrix(
                quality_chance,
                quality_to_keep,
                production_ratio
            )
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

Finally, we need to understand what that the other 4 values in the return vector mean: they represent the _internal_ flow of the system when it reaches a steady state. If we add the first 4 values of the vector we get 1.332844, which is the number of belts that the recycler loop needs to support in order for 1 input belt to be fully consumed. Should we wish to get the production rates for lower qualities, we would need to set the `quality_to_keep` parameter to a lower value and run the `recycler_loop` function specifically for that quality level.

## Statistical analysis

Now that pure recyler loops have been completely figured out, all that remains to be done is to create some helpful charts.

### Number of normal items needed to craft a legendary item

In the previous chapter we calculated how many normal items were needed for a legendary item for the qualities of 10% and 24.8%. Let's do the same calculation for all possible quality chances:

```python
indices = list(range(1, 25)) + [24.8]
ratios = [float(1/recycler_loop(1, i)[4]) for i in indices]

print(f"{indices=}")
print(f"{ratios=}")
# indices=[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 24.8]
# ratios=[275537.6188099327, 126925.19559177945, 78182.11949565915, 54324.731972068614, 40366.883416794, 31320.37503248879, 25052.454970936447, 20500.388421332522, 17076.449879282212, 14430.015645433099, 12339.492586876928, 10658.332376632781, 9285.998982461751, 8151.406056252092, 7203.000009109451, 6402.577635616764, 5721.297803873242, 5137.031906270301, 4632.558279121844, 4194.304003533878, 3811.450711699854, 3475.287804518467, 3178.73735561973, 2916.000006871485, 2726.9095125910867]
```

<pre style="text-align:center" class="mermaid">
---
config:
  theme: dark
---
xychart-beta
    title "Normal items needed to craft a legendary item"
    x-axis "Quality chance (%)" [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 24.8]
    y-axis "Number of normal items" 0 --> 270000
    line [275537.6188099327, 126925.19559177945, 78182.11949565915, 54324.731972068614, 40366.883416794, 31320.37503248879, 25052.454970936447, 20500.388421332522, 17076.449879282212, 14430.015645433099, 12339.492586876928, 10658.332376632781, 9285.998982461751, 8151.406056252092, 7203.000009109451, 6402.577635616764, 5721.297803873242, 5137.031906270301, 4632.558279121844, 4194.304003533878, 3811.450711699854, 3475.287804518467, 3178.73735561973, 2916.000006871485, 2726.9095125910867]
</pre>

<pre style="text-align:center" class="mermaid">
---
config:
  theme: dark
---
xychart-beta
    title "Normal items needed to craft a legendary item (starting from 10%)"
    x-axis "Quality chance (%)" [10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 24.8]
    y-axis "Number of normal items" 0 --> 14000
    line [14430.015645433099, 12339.492586876928, 10658.332376632781, 9285.998982461751, 8151.406056252092, 7203.000009109451, 6402.577635616764, 5721.297803873242, 5137.031906270301, 4632.558279121844, 4194.304003533878, 3811.450711699854, 3475.287804518467, 3178.73735561973, 2916.000006871485, 2726.9095125910867]
</pre>

### Efficiency of pure recycler loops for every quality level

In the same manner that we calculated the efficiency of the creation of legendary items, we can do the same for every quality level (except common, that would be always 100%) and quality chance. In the charts below, the lines are color-coded to match their quality level's color (green for common, blue for rare, etc).

```python
indices = list(range(1, 25)) + [24.8]

uncommon  = [float(recycler_loop(100, i, 2)[1]) for i in indices]
rare      = [float(recycler_loop(100, i, 3)[2]) for i in indices]
epic      = [float(recycler_loop(100, i, 4)[3]) for i in indices]
legendary = [float(recycler_loop(100, i, 5)[4]) for i in indices]

print(f"{uncommon=}")
print(f"{rare=}")
print(f"{epic=}")
print(f"{legendary=}")
# uncommon=[0.2990033222590812, 0.5960264900661364, 0.8910891089107843, 1.1842105263156755, 1.4754098360654597, 1.7647058823524755, 2.0521172638432112, 2.337662337661936, 2.621359223300609, 2.9032258064512915, 3.183279742764992, 3.461538461538218, 3.738019169328864, 4.012738853502352, 4.285714285713574, 4.556962025315852, 4.826498422712422, 5.094339622641082, 5.360501567397762, 5.624999999999703, 5.887850467289476, 6.149068322980344, 6.408668730649312, 6.666666666665972, 6.8719211822654165]
# rare=[0.030794362093117447, 0.06315512477518555, 0.09704930889121104, 0.13244459833784739, 0.16930932545002106, 0.2076124567472599, 0.24732357902985952, 0.28841288581532015, 0.3308511641057835, 0.3746097814774666, 0.4196606734833703, 0.4659763313603146, 0.5135297900350304, 0.5622946164139104, 0.6122448979586821, 0.6633552315329551, 0.7156007125153923, 0.7689569241719442, 0.8233999272802733, 0.8789062499997253, 0.9354528779803724, 0.9930172447040724, 1.0515772220562896, 1.111111111110377, 1.159425125578718]
# epic=[0.0031715123750366684, 0.006691933751003337, 0.010569726710900342, 0.014812882708795454, 0.0194289389860028, 0.02442499491135333, 0.02980772776570127, 0.035583407990068, 0.04175791391611173, 0.0483367459969256, 0.05532504055550809, 0.06272758306761636, 0.07054882099510452, 0.07879287618526323, 0.0874635568511152, 0.09656436914707982, 0.10609852835329592, 0.11606896968564748, 0.12647835873520066, 0.1373291015618632, 0.14862335444493466, 0.16036303330587975, 0.17254982281385564, 0.18518518518476199, 0.1956172933547551]
# legendary=[0.00036292684985614993, 0.0007878656366129634, 0.001279064838494827, 0.0018407822079469461, 0.002477278194905434, 0.003192809792284794, 0.003991624782162819, 0.004877956362181414, 0.005856018132670216, 0.006929999425940391, 0.008104060960091449, 0.009382330800525801, 0.010768900613177278, 0.012267822194231644, 0.013883104261852726, 0.015618709496127328, 0.017478551814108522, 0.019466493867471745, 0.02158634475090182, 0.02384185790990489, 0.02623672923728639, 0.02877459534805905, 0.03145903202229863, 0.03429355281117717, 0.0366715506144467]
```

<pre style="text-align:center" class="mermaid" id="pure-recycler-loop-efficiency-chart">
---
config:
  theme: dark
---
xychart-beta
    title "Efficiency of a pure recycler loop"
    x-axis "Quality chance (%)" [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 24.8]
    y-axis "Efficiency (%)" 0 --> 7
    line [0.2990033222590812, 0.5960264900661364, 0.8910891089107843, 1.1842105263156755, 1.4754098360654597, 1.7647058823524755, 2.0521172638432112, 2.337662337661936, 2.621359223300609, 2.9032258064512915, 3.183279742764992, 3.461538461538218, 3.738019169328864, 4.012738853502352, 4.285714285713574, 4.556962025315852, 4.826498422712422, 5.094339622641082, 5.360501567397762, 5.624999999999703, 5.887850467289476, 6.149068322980344, 6.408668730649312, 6.666666666665972, 6.8719211822654165]
    line [0.030794362093117447, 0.06315512477518555, 0.09704930889121104, 0.13244459833784739, 0.16930932545002106, 0.2076124567472599, 0.24732357902985952, 0.28841288581532015, 0.3308511641057835, 0.3746097814774666, 0.4196606734833703, 0.4659763313603146, 0.5135297900350304, 0.5622946164139104, 0.6122448979586821, 0.6633552315329551, 0.7156007125153923, 0.7689569241719442, 0.8233999272802733, 0.8789062499997253, 0.9354528779803724, 0.9930172447040724, 1.0515772220562896, 1.111111111110377, 1.159425125578718]
    line [0.0031715123750366684, 0.006691933751003337, 0.010569726710900342, 0.014812882708795454, 0.0194289389860028, 0.02442499491135333, 0.02980772776570127, 0.035583407990068, 0.04175791391611173, 0.0483367459969256, 0.05532504055550809, 0.06272758306761636, 0.07054882099510452, 0.07879287618526323, 0.0874635568511152, 0.09656436914707982, 0.10609852835329592, 0.11606896968564748, 0.12647835873520066, 0.1373291015618632, 0.14862335444493466, 0.16036303330587975, 0.17254982281385564, 0.18518518518476199, 0.1956172933547551]
    line [0.00036292684985614993, 0.0007878656366129634, 0.001279064838494827, 0.0018407822079469461, 0.002477278194905434, 0.003192809792284794, 0.003991624782162819, 0.004877956362181414, 0.005856018132670216, 0.006929999425940391, 0.008104060960091449, 0.009382330800525801, 0.010768900613177278, 0.012267822194231644, 0.013883104261852726, 0.015618709496127328, 0.017478551814108522, 0.019466493867471745, 0.02158634475090182, 0.02384185790990489, 0.02623672923728639, 0.02877459534805905, 0.03145903202229863, 0.03429355281117717, 0.0366715506144467]
</pre>

<pre style="text-align:center" class="mermaid" id="pure-recycler-loop-efficiency-chart-only-epic-and-legendary">
---
config:
  theme: dark
---
xychart-beta
    title "Efficiency of a pure recycler loop (only epic and legendary)"
    x-axis "Quality chance (%)" [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 24.8]
    y-axis "Efficiency (%)" 0 --> 0.25
    line [0.0031715123750366684, 0.006691933751003337, 0.010569726710900342, 0.014812882708795454, 0.0194289389860028, 0.02442499491135333, 0.02980772776570127, 0.035583407990068, 0.04175791391611173, 0.0483367459969256, 0.05532504055550809, 0.06272758306761636, 0.07054882099510452, 0.07879287618526323, 0.0874635568511152, 0.09656436914707982, 0.10609852835329592, 0.11606896968564748, 0.12647835873520066, 0.1373291015618632, 0.14862335444493466, 0.16036303330587975, 0.17254982281385564, 0.18518518518476199, 0.1956172933547551]
    line [0.00036292684985614993, 0.0007878656366129634, 0.001279064838494827, 0.0018407822079469461, 0.002477278194905434, 0.003192809792284794, 0.003991624782162819, 0.004877956362181414, 0.005856018132670216, 0.006929999425940391, 0.008104060960091449, 0.009382330800525801, 0.010768900613177278, 0.012267822194231644, 0.013883104261852726, 0.015618709496127328, 0.017478551814108522, 0.019466493867471745, 0.02158634475090182, 0.02384185790990489, 0.02623672923728639, 0.02877459534805905, 0.03145903202229863, 0.03429355281117717, 0.0366715506144467]
</pre>

<style>
#pure-recycler-loop-efficiency-chart svg[aria-roledescription="xychart"] g.line-plot-0 path {stroke: #3eec57 !important;}
#pure-recycler-loop-efficiency-chart svg[aria-roledescription="xychart"] g.line-plot-1 path {stroke: #2495ff !important;}
#pure-recycler-loop-efficiency-chart svg[aria-roledescription="xychart"] g.line-plot-2 path {stroke: #c400ff !important;}
#pure-recycler-loop-efficiency-chart svg[aria-roledescription="xychart"] g.line-plot-3 path {stroke: #ff9500 !important;}

#pure-recycler-loop-efficiency-chart-only-epic-and-legendary svg[aria-roledescription="xychart"] g.line-plot-0 path {stroke: #c400ff !important;}
#pure-recycler-loop-efficiency-chart-only-epic-and-legendary svg[aria-roledescription="xychart"] g.line-plot-1 path {stroke: #ff9500 !important;}
</style>

## Next step: [**Asteroid Chunk Recycling**](/posts/factorio-recycling-asteroid-chunk-recycling/)

I previously considered going straight to [recycler-assembler loops](/posts/factorio-recycler-assembler-loop/) and tackle the final frontier of recycling[^4] later. However, after thinking about it for bit, I concluded that asteroid chunk recycling is the logical next step for the series and builds elegantly on the analysis & tools created in this blog post that were required to tackle pure recycling loops.

[^4]: Please pardon my lousy humor.
