+++ 
draft = true
date = 2021-07-23T21:40:37+01:00
title = "How soon can a driver clinch the title: a nuanced analysis"
description = ""
slug = ""
authors = []
tags = []
categories = []
externalLink = ""
series = []
+++

The 2020 Formula 1 championship was one of the most uncompetitive seasons in recent history. Facing an underpowered Ferrari[^1] and a RedBull plagued with rear instability problems, Mercedes in some people's eyes had already won the title[^2] before even turning a wheel in a free practice session. Lewis Hamilton, however, did need to beat his teammate Valttery Bottas in order to win the driver's championship, but anyone who watches F1 knows that Hamilton is simply on another level, compared to Bottas. Ultimately, Hamilton clinched the title with three races to go at the Turkish Grand Prix with a memorable charge through the field. Watching this domination, I wondered to myself: what is the theoretical soonest that a championship can be wrapped up? Lets find out.

[^1]: I look forward to the day that the details of the deal made between Ferrari and the FIA regarding Ferrari's _creativity_ are disclosed (i.e. never)

[^2]: It doesn't help that they found a loophole in the regulations that allows them to adjust the [toe](https://en.wikipedia.org/wiki/Toe_(automotive) "Toe Wikipedia page") of the car by longitudinally pushing and pulling the steering column. I do wonder if they regretted not saving this innovation for a more rainy day, now that we see how the 2021 season is playing out...

## Lewis always wins, Max always comes second

Lets start by concretely defining our problem

> Let:
>
> - $N$ be the total number of races in a season
> - $l$ be the number of races that Lewis wins and Max comes second
> - $m$ be the number of races that Max wins and Lewis doesn't score points
> - $P\_l$ be the number of points that Lewis got by the end of the season
> - $P\_m$ be the number of points that Max got by the end of the season
>
> Such that $N = l + m$.
>
> A win ($P\_1$) is worth 25 points, a second place ($P\_2$) is worth 18 points.
>
> Determine how low $l$ can be, given that $P\_l > P\_m$ and $N = 17$.

This will be our starting point
$$P\_l > P\_m$$

We can start by expanding $P\_l$ and $P\_m$
$$P\_1 l > P\_2 l + P\_1 m$$

$m$ can be replaced by $N - l$
$$P\_1 l > P\_2 l + P\_1 (N - l)$$
$$P\_1 l > P\_2 l + P\_1 N - P\_1 l$$

Now, it's simply a matter of isolating $l$
$$P\_1 l - P\_2 l + P\_1 l > P\_1 N$$
$$(P\_1 - P\_2 + P\_1) l > P\_1 N$$
$$(2 P\_1 - P\_2) l > P\_1 N$$
$$l > \frac {P\_1 N}{2 P\_1 - P\_2}$$

Let's replace the constants by their values
$$l > \frac {25 * 17}{2 * 25 - 18}$$
$$l > \frac {425}{32}$$
$$l > 13.28125$$
$$l \geqslant 14$$

Lewis Hamilton would need to win 14 races in order to clinch the title. However, I'd argue that given that RedBull were the only team capable of catching Mercedes, I'd declare Lewis Hamilton the champion if he had a points tally so big that RedBull could finish 1-2 in every remaining race and it wouldn't be enough to catch Hamilton.

## Lets think about gaps

Instead of assuming that Hamilton doesn't finish in the points every, lets presume that RedBull outdeveloped Mercedes and they now finish 1-2 every race, Max in first and Lewis in third. We will also approach the problem by considering the points difference between Lewis and Max, rather than the total points tally.

> Let:
>
> - $N$ be the total number of races in a season
> - $l$ be the number of races that Lewis wins and Max comes second
> - $m$ be the number of races that Max wins and Lewis comes third
> - $D\_o$ be the starting points difference between Lewis and Max
> - $S\_l$ be the points swing that happens if Lewis comes first and Max second (25 - 18 = 7)
> - $S\_m$ be the points swing that happens if Lewis comes third and Max third (15 - 25 = -10)
>
> Such that $N = l + m$.
>
> Determine how low $l$ can be, given that $D\_o + S\_l l + S\_m m > 0$ and $N = 17$.

We'll start here
$$D\_o + S\_l l + S\_m m > 0$$

$m$ can be replaced by $N - l$
$$D\_o + S\_l l + S\_m (N - l) > 0$$
$$D\_o + S\_l l + S\_m N - S\_m l > 0$$

Let's solve for $l$
$$S\_l l - S\_m l > - D\_o - S\_m N$$
$$(S\_l - S\_m) l > - D\_o - S\_m N$$
$$l > \frac{- D\_o - S\_m N}{S\_l - S\_m}$$

Let's replace the constants by their values
$$l > \frac {-0 - (-10)*17}{7 - (-10)}$$
$$l > \frac {170}{17}$$
$$l > 10$$
$$l \geqslant 11$$

Lewis Hamilton would only need to win 11 races and be on the podium for the rest of the season to secure the championship.