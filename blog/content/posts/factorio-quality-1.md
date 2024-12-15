+++ 
draft = true
date = 2024-12-08T13:52:39Z
title = "Solving the mathematics of Factorio Quality: Part 1"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["maths", "programming"]
categories = []
externalLink = ""
series = []
+++

## Prologue

Video games have this incredible ability to evoque certain feelings and states of mind on the people that play them. You can experience pretty much every emotion, starting from happiness, amazement, and wonder going all the way to sadness, frustation, and heartbreak. Games can make you laugh and cry, they can make you focus, and sometimes they make you _think_.

Factorio is a game that makes you think. It gives such fascinating production and logistics challenges to its players that sometimes they stop playing and write [research papers](https://scholar.google.pt/scholar?hl=en&q=factorio "Factorio research") about the problems they face in this game[^1]. In the recently released Factorio [Space Age](https://www.factorio.com/space-age/content "Space Age web page") expansion, a new feature was added that allows the player to gamble their way into having better versions of every item in the game: [Quality](https://www.factorio.com/blog/post/fff-375 "Dev post about quality").

[^1]: I'm particularly fond of the effort and research that has gone into [belt balancers](https://tuprints.ulb.tu-darmstadt.de/17621/8/thesis.pdf).

<div style="text-align:center">
    <img src="/images/fff-375-quality-modules.png" alt="The 3 tiers of quality modules"/>
    <figcaption>The 3 tiers of quality modules.<br>(image source: fff-375)</figcaption>
</div>

Getting a complete understanding of the probabilities behind the mass production of quality items is not easy, and neither is converting this set of probabilities into workable flow rates. With this is mind, and after struggling with this myself, I have decided to write a series of blog posts detailing step by step the math required to truly understand quality. I will assume that the reader has played Factorio: Space Age and has a working understanding of the quality mechanic, but even if you haven't played Factorio before, feel free to follow along. Without further ado, let's begin:

## The fundamentals: Building the quality matrix

When you insert a quality module in a assembler[^2], a new stat appears on the machine's tooltip: Quality. The goal of this chapter is to convert this singular value into a probability matrix.

[^2]: Or a recycler, crusher, smelter, etc. All of them are valid quality module recipients.

<div style="text-align:center">
    <img src="/images/Quality-stat-example.png" alt="An assembly machine with 4 T3 quality modules and a quality chance of 10%."/>
    <figcaption> An assembly machine with 4 T3 quality modules.<br> Each module contributes 2.5% quality chance for a grand total of 10%.</figcaption>
</div>

This quality chance, which I will call $q$ from now on, represents the probability of a machine crafting items of a higher quality than their ingredients. Please note that a quality upgrade of multiple tiers is entirely possible, but becomes more unlikely by a factor of 10 for every "bonus" quality upgrade. Using this knowledge we can craft a template quality upgrade probability table, which indicates the probability of ingredients of quality `Input` turning into items of quality `Output`:

<div style="text-align:center">
    <img src="/images/Quality-Upgrade-Table.png" alt="Quality upgrade probability table generalized for any quality chance Q"/>
    <figcaption> Quality upgrade probability table generalized for any quality chance $q$. <br>(image source: <a href="https://wiki.factorio.com/Quality">Factorio Wiki</a>)</figcaption>
</div>

At this point, we can think of writing a function $f(q)$ that returns a 5x5 probability matrix $Q_q$

```python
class QualityTier(IntEnum):
    Normal    = 0
    Uncommon  = 1
    Rare      = 2
    Epic      = 3
    Legendary = 4


def quality_probability(
        quality_chance : float,
        input_tier : QualityTier,
        output_tier : QualityTier) -> float:
    """Calculates the probability of a machine craft with a certain `quality_chance` upgrading 
    the resulting product from the tier of the products (`input_tier`) to the `output_tier`.

    Args:
        quality_chance (float): Quality chance (in %).
        input_tier (QualityTier): Quality tier of the ingredients.
        output_tier (QualityTier): Quality tier of the product.
    
    Returns:
        float: A probability from 0 to 1.
    """
    
    # Basic validations
    assert 0 <= quality_chance <= 100
    assert 0 <= input_tier  <= 4 and type(input_tier)  == int
    assert 0 <= output_tier <= 4 and type(output_tier) == int

    # Some QoL conversions
    quality_chance /= 100
    i = input_tier
    o = output_tier

    # An item can never be downgraded
    if input_tier > output_tier:
        return 0

    # If the item is already legendary, it will remain legendary
    if input_tier == QualityTier.Legendary:
        return 1
    
    # Probability of item staying in the same tier
    if input_tier == output_tier:
        return 1 - quality_chance
    
    # Probability of item going straight to legendary
    if output_tier == QualityTier.Legendary:
        return quality_chance / (10 ** (3 - i))
    
    # else
    return (quality_chance * 9/10) / (10 ** (o - i - 1))


def quality_matrix(quality_chance : float) -> np.ndarray:
    """Returns the quality matrix for the corresponding `quality_chance` which indicates 
    the probabilities of any input tier jumping to any other tier.

    Args:
        quality_chance (float): Quality chance (in %).

    Returns:
        np.ndarray: 5x5 matrix. The columns represent the input quality tier and go
            from legendary to normal, from left to right. The lines represent the output
            quality tier and go from normal to legendary, from top to bottom.
    """

    res = np.zeros((5,5))
    
    for row in range(5):
        for column in range(5):
            res[row][column] = quality_probability(quality_chance, row, column)
    
    return res
```

```text














<script type="module">
    import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
    mermaid.initialize({ startOnLoad: true });
</script>
Here is a mermaid diagram:
<pre class="mermaid">
      graph TD
      A[Client] --> B[Load Balancer]
      B --> C[Server01]
      B --> D[Server02]
</pre>
```
