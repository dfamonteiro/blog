+++ 
draft = true
date = 2024-12-27T16:59:26Z
title = "Solving the mathematics of Factorio Quality: Asteroid Chunk Recycling"
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

Asteroid chunk recycling is one of the the most effective methods to get legendary resources in Factorio. It is one of the more unique quality grinding setups you'll ever find, but the calculations needed to assess the efficiency of this method are quite straightforward, as it will be shown in this blog post. Please note that I will assume that the reader has read my analysis of [pure recyler loops](/posts/factorio-pure-recycler-loop/).

<div style="text-align:center">
    <img src="/images/quality-recycling-screenshot.jpg" alt="The 3 tiers of quality modules"/>
    <figcaption>Screenshot of an asteroid chunk recycling setup.<br>(image source: <a href="https://youtu.be/gZCFnG8HDCA?si=gj-YBPIGAiUe_OTY&t=2297">Konage</a>)</figcaption>
</div>

## Simply a better version of the pure recycler loop

The core mechanic that enables asteroid chunk recycling is the ability to reprocess asteroids chunks with quality modules in the crushers:

<div style="text-align:center">
    <img src="/images/asteroid-reprocessing-recipes.png" alt="Quality upgrade probability table generalized for any quality chance Q"/>
    <figcaption> The reprocessing recipes for all three asteroid types. <br>(image source: <a href="https://wiki.factorio.com/Crusher">Factorio Wiki</a>)</figcaption>
</div>

At first glance, calculating the efficiency of this setup might seem incredibly complicated, but there is a key simplification that will make our lives a whole lot easier: **we don't care about the type of asteroid we get**. After all, we can use the very same reprocessing recipes to get whichever type of asteroid chunk is needed.

With this knowledge in hand, we can simply treat the crusher as a recycler with only 2 module slots and a production ratio of 0.8. We can even use the python functions written in the previous blog post to save a bunch of work:

```python
import numpy as np
from pure_recycler_loop import recycler_loop, recycler_matrix

# Let's lean on the functions from pure_recycler_loop.py

def asteroid_crusher_matrix(quality_chance : float) -> np.ndarray:
    return recycler_matrix(quality_chance, production_ratio=0.8)

def asteroid_crusher_loop(
        input_vector : float,
        quality_chance : float,
        quality_to_keep : int = 5) -> np.ndarray:
    return recycler_loop(input_vector, quality_chance, quality_to_keep, production_ratio=0.8)
```

We have everything needed to do our statistical analysis, but I still want to see how a crusher production matrix looks like (assume that the quality chance $q$ is 5%):

```python
print(asteroid_crusher_matrix(5))
```

$$ C_{5} = \begin{bmatrix}
  0.76 & 0.036 & 0.0036 & 0.00036 & 0.00004 \\\\
  0    & 0.76  & 0.036  & 0.0036  & 0.0004 \\\\
  0    & 0     & 0.76   & 0.036   & 0.004 \\\\
  0    & 0     & 0      & 0.76    & 0.04 \\\\
  0    & 0     & 0      & 0       & 0 \\\\
\end{bmatrix}$$

## Statistical analysis

A good portion of the statistical analysis I'm about to make was already done in the exact same way in the [previous blog post](/posts/factorio-pure-recycler-loop/), so it's understandable if you have a feeling of _déjà vu_.

### Number of normal asteroids needed to craft a legendary asteroid

```python
indices = list(range(1, 13)) + [12.4]
ratios = [float(1/asteroid_crusher_loop(1, i)[4]) for i in indices]

print(f"{indices=}")
print(f"{ratios=}")
# indices=[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 12.4]
# ratios=[10658.332544237915, 2916.0000561843026, 1231.4670876459584, 643.8607287181613, 384.0000047970801, 250.63328840475552, 174.71534904370753, 128.05539424941833, 97.6291795243653, 76.83200027295233, 62.06060624879639, 51.22968018283643, 47.698631555272826]
```

<pre style="text-align:center" class="mermaid">
---
config:
  theme: dark
---
xychart-beta
    title "Normal items needed to craft a legendary item"
    x-axis "Quality chance (%)" [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 12.4]
    y-axis "Number of normal items" 0 --> 11000
    line [10658.332544237915, 2916.0000561843026, 1231.4670876459584, 643.8607287181613, 384.0000047970801, 250.63328840475552, 174.71534904370753, 128.05539424941833, 97.6291795243653, 76.83200027295233, 62.06060624879639, 51.22968018283643, 47.698631555272826]
</pre>

<pre style="text-align:center" class="mermaid">
---
config:
  theme: dark
---
xychart-beta
    title "Normal items needed to craft a legendary item (starting from 5%)"
    x-axis "Quality chance (%)" [5, 6, 7, 8, 9, 10, 11, 12, 12.4]
    y-axis "Number of normal items" 0 --> 400
    line [384.0000047970801, 250.63328840475552, 174.71534904370753, 128.05539424941833, 97.6291795243653, 76.83200027295233, 62.06060624879639, 51.22968018283643, 47.698631555272826]
</pre>

### Efficiency of asteroid chunk recycling for every quality level

In the charts below, the lines are color-coded to match their quality level's color (green for common, blue for rare, etc).

```python
indices = list(range(1, 13)) + [12.4]

uncommon  = [float(asteroid_crusher_loop(100, i, 2)[1]) for i in indices]
rare      = [float(asteroid_crusher_loop(100, i, 3)[2]) for i in indices]
epic      = [float(asteroid_crusher_loop(100, i, 4)[3]) for i in indices]
legendary = [float(asteroid_crusher_loop(100, i, 5)[4]) for i in indices]

print(f"{uncommon=}")
print(f"{rare=}")
print(f"{epic=}")
print(f"{legendary=}")
# uncommon=[3.4615384615260094, 6.6666666666465115, 9.642857142830861, 12.413793103415511, 14.999999999958789, 17.41935483866937, 19.687499999959517, 21.818181818139728, 23.82352941171918, 25.714285714234272, 27.49999999995658, 29.189189189135714, 29.83957219245846]
# rare=[0.465976331353591, 1.1111111110940393, 1.8941326530371276, 2.782401902468548, 3.749999999959269, 4.7762747137958605, 5.844726562452839, 6.942148760279247, 8.057958477466594, 9.18367346933998, 10.312499999943766, 11.439006574092652, 11.887957905523466]
# epic=[0.06272758306376097, 0.1851851851747421, 0.3720617711167263, 0.623641805702194, 0.9374999999701643, 1.3096237118236027, 1.7351531982007549, 2.2088655146028944, 2.7254859555867896, 3.2798833818761275, 3.8671874999424296, 4.48285392765304, 4.736111571954229]
# legendary=[0.009382330798851889, 0.034293552803847605, 0.08120395796876448, 0.15531309335629098, 0.26041666663694074, 0.3989893028560425, 0.572359561880939, 0.7809120505857308, 1.0242839375283508, 1.3015410245193235, 1.6113281249513514, 1.9519934519426527, 2.096496209768022]
```

<pre style="text-align:center" class="mermaid" id="asteroid-chunk-recycling-loop-efficiency-chart">
---
config:
  theme: dark
---
xychart-beta
    title "Efficiency of asteroid chunk recycling"
    x-axis "Quality chance (%)" [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 12.4]
    y-axis "Efficiency (%)" 0 --> 30
    line [3.4615384615260094, 6.6666666666465115, 9.642857142830861, 12.413793103415511, 14.999999999958789, 17.41935483866937, 19.687499999959517, 21.818181818139728, 23.82352941171918, 25.714285714234272, 27.49999999995658, 29.189189189135714, 29.83957219245846]
    line [0.465976331353591, 1.1111111110940393, 1.8941326530371276, 2.782401902468548, 3.749999999959269, 4.7762747137958605, 5.844726562452839, 6.942148760279247, 8.057958477466594, 9.18367346933998, 10.312499999943766, 11.439006574092652, 11.887957905523466]
    line [0.06272758306376097, 0.1851851851747421, 0.3720617711167263, 0.623641805702194, 0.9374999999701643, 1.3096237118236027, 1.7351531982007549, 2.2088655146028944, 2.7254859555867896, 3.2798833818761275, 3.8671874999424296, 4.48285392765304, 4.736111571954229]
    line [0.009382330798851889, 0.034293552803847605, 0.08120395796876448, 0.15531309335629098, 0.26041666663694074, 0.3989893028560425, 0.572359561880939, 0.7809120505857308, 1.0242839375283508, 1.3015410245193235, 1.6113281249513514, 1.9519934519426527, 2.096496209768022]
</pre>

<style>
#asteroid-chunk-recycling-loop-efficiency-chart svg[aria-roledescription="xychart"] g.line-plot-0 path {stroke: #3eec57 !important;}
#asteroid-chunk-recycling-loop-efficiency-chart svg[aria-roledescription="xychart"] g.line-plot-1 path {stroke: #2495ff !important;}
#asteroid-chunk-recycling-loop-efficiency-chart svg[aria-roledescription="xychart"] g.line-plot-2 path {stroke: #c400ff !important;}
#asteroid-chunk-recycling-loop-efficiency-chart svg[aria-roledescription="xychart"] g.line-plot-3 path {stroke: #ff9500 !important;}
</style>

The fact that we don't even need a separate chart for epic and legendary efficiency speaks volumes to how good asteroid recycling is. If you have normal T3 quality modules the efficiency will be 0.26%, otherwise if you happen to have legendary T3 quality modules, the efficiency improves by an order of magnitude to 2.0964%[^1].

[^1]: This is consistent with the results [other people](https://youtu.be/gZCFnG8HDCA?si=2veyz-4isrfpC62v&t=2383) have been getting.
