+++ 
draft = true
date = 2021-08-05T19:33:17+01:00
title = "Can Formula E race at Macau?"
description = "Let's take a look at how much of an impact pit stops could have in the Gen3 era of Formula E"
slug = ""
authors = ["Daniel Monteiro"]
tags = ["motorsport", "maths", "Formula E"]
categories = []
externalLink = ""
series = []
+++

When it comes to historical races, the Monaco Grand Prix is definitely an odd one. Older than Formula 1 itself, it is F1's most glamorous and most prestigious race on the calendar, featuring a track layout that remains largely unchanged[^1] since the race's inception in 1929. Unfortunately, when it comes to on-track action, it is an absolutely dreadful race, with cars that are impossible to race on this track. So, when Formula E announced that the full track layout would be used for the 2021 Monaco E-Prix, people were hopeful that the premier electric series would provide a good race in this historical venue.

Well, it surely didn't disappoint: the 2021 Monaco E-prix was probably the best dry race Monaco had seen for over two decades, with a three-way battle for the lead between da Costa, Mitch Evans and Robin Frijns. António Felix da Costa eventually won the race with a daring last-lap overtake on Mitch Evans. It was a moment of vindication for both Formula E and this legendary track.

This race was such a hit that the Monaco E-Prix became a permanent fixture of the FE calendar. The media reaction was also quite positive: [Inside Electric](https://inside-electric.com/)'s[^2] [Hazel Southwell](https://twitter.com/HSouthwellFE "Hazel's twitter profile") even suggested that Formula E should consider going to other legendary street circuits, such as Pau and Macau. I could definitely see a future where FE goes to Pau in every year that we don't go to Paris[^3]. Hosting a Formula E race in Macau's treacherous Guia circuit would be a lot trickier, however...

[^1]: There _have_ been some track changes, including a couple made to prevent [drivers from accidentally launching themselves into the sea](https://www.youtube.com/watch?v=vtxrp52PeDE "Video footage of Formula 1 driver Alberto Ascari in the monegasque sea"). For more Monaco Grand Prix-related information, the [wikipedia page](https://en.wikipedia.org/wiki/Monaco_Grand_Prix "Wikipedia page of the Monaco Grand Prix") is a good place to start.

[^2]: Unfortunately, [this publication was sunsetted](https://inside-electric.com/2021/08/a-note-to-all-our-followers/ "Inside Electric's discontinuation post") after the end of season 7 due to pandemic-related factors.

[^3]: At the time of writing, the Paris E-Prix is only hosted every other year.

## The Macau Grand Prix track layout

<figure>
    <img src="/images/wtcc-circuit-12-macau.jpg" alt="Guia circuit track layout">
    <figcaption><b>Guia circuit track layout. &#169FIA</b></figcaption>
</figure>

The Guia circuit is one of the most challenging race circuits on the planet. It combines an extremely twisty and technical section of road with an enormous 2 kilometer-long straight. Overtaking around this track is done pretty much exclusively on the straight with the aid of slipstream[^4].

This massive straight does pose a problem for Formula E's energy-constrained races: Are the Gen2 cars capable of enduring ~47 minutes[^5] of this track? Probably not. How about the Gen3 cars? Well, they have a trick up their sleeves...

[^4]: Formula 3 cars have the luxury of DRS if they are within 1 second of the car ahead.

[^5]: This is just an educated guess of how long a 45 minutes + 1 lap race _actually_ is.

## The Gen3 recharging pit stop: a track designer's panacea

The [Gen3 Formula E car](https://motorsport.tech/formula-e/gen3-formula-es-big-step-into-unchartered-territory "Gen3 details and stats") represents an improvement on the Gen2 car on almost all areas:

|      Stats                 | Gen2           | Gen3             | Change |
|----------------------------|----------------|------------------|--------|
| Qualifying power output    | 250kw (335hp)  | 350kw (469hp)    | +40%   |
| Race power output          | 200kw (268hp)  | 300kw (402hp)    | +50%   |
| Battery weight             | 284kg          | 180kg            | -36.6% |
| Battery charge             | 54 kwh         | 51kwh            | -5.6%  |
| Regeneration (back/front)  | 250 (250/0) kW | 600 (350/250) kw | +140%  |
| Total weight (inc. driver) | 903kg          | 780kg            | -13.6% |

The one area that wasn't improved on was battery charge... or was it? Surprise: the Gen3 era will feature recharging pit stops! The Gen3 car will be able to come into the pits and stop for 30 seconds to recharge the battery at a rate of 600kw, putting 5kwh back into the battery. This is revolutionary because for every pit stop you add to a race, not only less time is spent racing (and consuming energy), but also more energy is added to the car. Hopefully, this means that circuit designers won't need to add chicanes to their layouts because they can add another pit stop instead.

## How many pit stops would Macau need?

Now that we know that FE could go to Macau by using recharging pit stops. How can we determine the exact number? Lets take a look at the math (feel free to [skip](#seeing-the-bigger-picture "Skip to next chapter") this chapter):

Before we get started, lets name our variables:

- $P\_g$ - Guia circuit power requirement (kw)
- $t\_g$ - Guia circuit lap time in race pace (s)
- $E\_g$ - Energy required to do a lap of the Guia circuit in race pace (kwh)
- $P\_r$ - Average race pace power (kw)
- $t\_r$ - Race time (s)
- $N\_p$ - Number of pit stops
- $E\_s$ - Battery charge at the start of the race (kwh)
- $E\_p$ - Energy gained during a pit stop (kwh)
- $t\_d$ - Time necessary to drive through the pit lane _without stopping_ (s)
- $t\_p$ - Time spent stationary recharging in a pit stop (s)

The goal is to have more than enough power to sustain the power requirements of the Guia Circuit
$$P\_r \geqslant P\_g$$

The power requirement of the guia circuit is straightforward to determine
$$P\_g = \frac {E\_g}{\frac {t\_g}{3600}} = 3600 \frac{E\_g}{t\_g}$$

The _total_ energy available to the car in the entire race can be calculated as follows
$$ E\_s + N\_p E\_p$$

The time spent under racing conditions (measured in hours) is calculated in a similar manner
$$\frac{t\_r - N\_p(t\_d + t\_p)}{3600}$$

Determining the average race pace power is a matter of dividing the total energy by the time
$$ P\_r =  \frac{E\_s + N\_p E\_p}{\frac {t\_r - N\_p(t\_d + t\_p)}{3600}} = 3600\frac{E\_s + N\_p E\_p}{t\_r - N\_p(t\_d + t\_p)}$$

We now have everything necessary to isolate $N\_p$, but there is no point continuing this exercise, now that we know how to define $P\_g$ and $P\_r$. Lets do something a lot more interesting with these formulas instead...

## Seeing the bigger picture

With the formulas that we derived, we can visualize _every_ pit stop strategy worth considering, by putting the charging time ($t\_p$) on the X axis and the average race pace power ($P\_r$) on the Y axis. All the dots that are above the Guia circuit power requirement (gray line) are strategies that work. Feel free to change the values below, if you wish!

<link rel="stylesheet" href="/css/championship_chart.css">
<script src="https://cdn.jsdelivr.net/npm/chart.js@3.5.0/dist/chart.min.js"></script>

Guia circuit power requirement:
- <label title="Guia circuit power requirement" for="power_requirements">$P\_g = $</label> <input id="power_requirements" type="number" value="96" autocomplete="off" style="width: 5ch;"> (kw)
- <label title="Guia circuit lap time in race pace" for="lap time">$t\_g = $</label>           <input id="lap time" type="number" value="150" autocomplete="off"> (s)
- <label title="Energy required to do a lap of the Guia circuit in race pace" for="lap energy">$E\_g = $</label>         <input id="lap energy" type="number" value="4" autocomplete="off"> (kwh)

Pit stop-related values:
- <label title="Race time" for="race_time">$t\_r = $</label>         <input id="race_time" type="number" value="47" autocomplete="off"> (minutes)
- <label title="Battery charge at the start of the race" for="car_energy">$E\_s = $</label>        <input id="car_energy" type="number" value="51" autocomplete="off"> (kwh)
- <label title="Battery recharging rate" for="recharge_power">$P\_b = $</label>        <input id="recharge_power" type="number" value="600" autocomplete="off"> (kw)
- <label title="Time necessary to drive through the pit lane without stopping" for="pit_delta">$t\_d = $</label>        <input id="pit_delta" type="number" value="20" autocomplete="off"> (s)

<canvas id="myChart" width="400" height="200"></canvas>
<script src="/javascript/formula_E_gen3_chart.js" defer></script>

Looking at the chart, only a 4-stop strategy is possible with the current Gen3 capabilities. A 3-stop strategy with a 39s stop time could be possible in the near future, however.

## The actual challenge that Formula E has to overcome

Formula E could have already used the full Monaco layout when it raced there in season 5. It chose not to due to fear of being naively[^6] compared to Formula 1. Luckily for Formula E, the race was so good that whenever a comparison of F1 and FE appeared, they had this fantastic E-Prix on back of their minds and realized that speed doesn't necessarily correlate with good racing.

Formula E will face the same conundrum if it wishes to go to Macau. The Macau Grand Prix features Formula 3 cars that would narrowly beat FE cars in the straights[^7] and soundly beat them in the corners, because they are lighter and have more downforce. Will this dissuade Formula E from coming to Macau? I very much hope not.

[^6]: Lets take the front and rear wings off and put all-weather tyres in a Formula 1 car, then we can talk about comparisons. A Formula E car could easily go 4-5 seconds faster if it used slick tyres. It simply chooses not to because developing all-weather tyres for electric cars is a much better value proposition (and a lot more road-relevant) for a tyre manufacturer.

[^7]: Formula E cars are biased towards acceleration, rather than top end.
