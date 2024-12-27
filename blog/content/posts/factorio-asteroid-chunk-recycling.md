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

Asteroid chunk recycling is one of the the most effective methods to get legendary resources in Factorio. It is one of the more unique quality grinding setups you'll ever find, but the calculations needed to assess the efficiency of this method are quite straightforward, as it will be shown in this blog post.

<div style="text-align:center">
    <img src="/images/quality-recycling-screenshot.jpg" alt="The 3 tiers of quality modules"/>
    <figcaption>Screenshot of an asteroid chunk recycling setup.<br>(image source: <a href="https://youtu.be/gZCFnG8HDCA?si=gj-YBPIGAiUe_OTY&t=2297">Konage</a>)</figcaption>
</div>

## Simply a better version of the pure recycler loop

<div style="text-align:center">
    <img src="/images/asteroid-reprocessing-recipes.png" alt="Quality upgrade probability table generalized for any quality chance Q"/>
    <figcaption> The reprocessing recipes for all three asteroid types. <br>(image source: <a href="https://wiki.factorio.com/Crusher">Factorio Wiki</a>)</figcaption>
</div>
