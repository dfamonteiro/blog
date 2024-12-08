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

Getting a complete understanding of the probabilities behind the mass production of quality items is not easy, and neither is converting this set of probabilities into workable flow rates. With this is mind, and after struggling with this myself, I have decided to write a series of blog posts detailing the math required to truly understand quality.

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
