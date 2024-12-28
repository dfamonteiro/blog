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

def asteroid_crusher_loop(input_vector : float, quality_chance : float) -> np.ndarray:
    return recycler_loop(input_vector, quality_chance, production_ratio=0.8)
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
