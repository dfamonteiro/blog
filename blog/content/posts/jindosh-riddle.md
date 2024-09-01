+++ 
draft = true
date = 2024-08-31T21:38:40+01:00
title = "Solving Dishonored 2's Jindosh Riddle with constraint programming"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["maths"]
categories = []
externalLink = ""
series = []
+++

I was playing Dishonored II (great game, by the way), when I came across a locked door. In order to make progress in the game, I had to open this door. When you get close to it, you begin to realise that there is more to this door than it first meets the eye:

<figure>
    <img src="/images/Jindosh-Lock.png" alt="Guia circuit track layout">
    <figcaption><b>The Jindosh Riddle door lock</b></figcaption>
</figure>

There is no key that opens this door. Instead, you have 5 slots with names and 5 slots with objects which need to have the right values to unlock the door. You don't have to brute force the combination, though, because right next to the door you can find the following riddle:

> At the dinner party were Lady Winslow, Doctor Marcolla, Countess Contee, Madam Natsiou, and Baroness Finch.
>
> The women sat in a row. They all wore different colors and Doctor Marcolla wore a jaunty white hat. Countess Contee was at the far left, next to the guest wearing a blue jacket. The lady in green sat left of someone in red. I remember that green outfit because the woman spilled her absinthe all over it. The traveller from Dabokva was dressed entirely in purple. When one of the dinner guests bragged about her Snuff Tin, the woman next to her said they were finer in Dabokva, where she lived.
>
> So Lady Winslow showed off a prized Diamond, at which the lady from Fraeport scoffed, saying it was no match for her War Medal. Someone else carried a valuable Ring and when she saw it, the visitor from Karnaca next to her almost spilled her neighbor's rum. Madam Natsiou raised her wine in toast. The lady from Baleton, full of beer, jumped onto the table, falling onto the guest in the center in the center seat, spilling the poor woman's whiskey. Then Baroness Finch captivated them all with a story about her wild youth in Dunwall.
>
> In the morning, there were four heirlooms under the table: the Snuff Tin, Bird Pendant, the War Medal, and the Ring.
>
> But who owned each?

There are other ways of finding the lock combination without having to solve the riddle, but when I first came across it I was dead set on solving it. It did take me a couple of hours and a lot of scribbles, but I managed to figure it out. However, while I was frying my brain solving this riddle, I started to wonder: wouldn't it be easier to write a program to solve this riddle for me? That's what we will be doing in this blog post.

## Initial setup

Before we start writing code, we should begin by making our lives easier by clearly stating the entities of our riddle:

| Name     | Heirloom     | Color  | Drink    | Origin   |
|----------|--------------|--------|----------|----------|
| Winslow  | Snuff Tin    | White  | Absinthe | Dabokva  |
| Marcolla | Bird Pendant | Blue   | Rum      | Fraeport |
| Contee   | War Medal    | Green  | Wine     | Karnaca  |
| Natsiou  | Ring         | Red    | Beer     | Baleton  |
| Finch    | Diamond      | Purple | Whiskey  | Dunwall  |

We also need to define how the solution will be structured. When solved this riddle manually I used a grid, and there's no reason to not do the same here:

|              | **Far-left** | **Left** | **Middle** | **Right** | **Far-right** |
|--------------|--------------|----------|------------|-----------|---------------|
| **Origin**   |              |          |            |           |               |
| **Drink**    |              |          |            |           |               |
| **Color**    |              |          |            |           |               |
| **Name**     |              |          |            |           |               |
| **Heirloom** |              |          |            |           |               |

### Initial setup (code)
