+++ 
draft = true
date = 2023-12-03T22:00:08Z
title = "The Formula 1 ATR rules have to account for dominance"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["motorsport", "maths"]
categories = []
externalLink = ""
series = []
+++

Dominance is a common occurrence in Formula 1. While most other forms of motorsport have made efforts to limit the amount of design freedom that teams have (to save costs), F1 and MotoGP swim against the current and remain the only premier competitions where pretty much everything is bespoke[^1]. This leads to the creation of incredible works of engineering in the form of the fastest racing bikes and cars on this planet. Still, it also carries a grave risk of having a season entirely dominated by a manufacturer: RedBull has won 21/22 races in 2023, and Ducati had a similar MotoGP performance in the same year.

[^1]: One can make the argument that Le Mans Hypercars are also fully bespoke, but given that their performance is limited by BOP, they're not relevant for this post

## The competition vs the show

Dominant manufacturers and teams naturally lead to less entertaining championships, leading to a less engaged audience, _unless_ the intra-team battle is entertaining to watch. The MotoGP 2023 season was still very good because even though Ducati won most of the races, there was a close battle in the driver's championship featuring Francesco Bagnaia and Jorge Martín. The same could be said about the 2021 F1 season: having Mercedes and Red Bull lap the field didn't matter because Max Verstappen and Lewis Hamilton had a titanic title fight for the championship title. When there is no title fight of any sort, things get incredibly dull: Red Bull and Max dominating in 2023 is an excellent example of such a season.

If you do a better job than the opponent, you'll probably win. If you can consistently do a better job than the rest of the competition, you'll probably win the championship[^2]. This is how sports work. The problems start appearing when a team is so consistently good that it is detrimental to the competition itself. When considering this possibility, the governing entities of the leagues and championships have to consider how much sporting integrity and fairness they have to give up to safeguard the future of the competitions they oversee. The NFL and the NBA punish good teams by giving them worse draft picks. MotoGP has wholly overhauled its concession system at the time of writing, which offers more opportunities for struggling manufacturers to improve their bikes. The FIA shakes up the rules every so often to knock dominant teams off their perch.

[^2]: This is true for points-accruing competitions like the Premier League and most motorsport championships. American sports tend to use playoff formats, which are more random by nature

## The Formula 1 Aerodynamic Testing Restrictions (ATR)

As part of Formula 1's efforts to improve the competitiveness of all ten teams in the sport, a new set of Aerodynamic Restriction Rules was introduced in 2021. Instead of awarding everyone with the same amount of wind tunnel time, this new set of regulations awards wind tunnel time (and CFD time) based on a team's placing in the constructors' championship and is reset every 6 months. The championship leader gets 70% of the previous regulations' allocation, and the worst team gets 115%:

| Championship position   | P1  | P2  | P3  | P4  | P5  | P6  | P7   | P8   | P9   | P10  |
|-------------------------|-----|-----|-----|-----|-----|-----|----- |----- |----- |----- |
| Aero testing allocation | 70% | 75% | 80% | 85% | 90% | 95% | 100% | 105% | 110% | 115% |

If $p$ is the championship position of a team $t$, the aero testing allocation $A$ can be expressed as follows:

$$A(t) = 65 + 5p_t$$

Using this formula, we can visualize what the aero testing allocation would look like if this ruleset were used since 2010:

<script src="https://cdn.jsdelivr.net/npm/chart.js@3.5.0/dist/chart.min.js"></script>
<canvas id="myChart" width="400" height="200"></canvas>
<script src="/javascript/atr_chart.js" defer></script>

The broad strokes of the last 14 years of Formula 1 are reflected in this chart: Vettel's reign of terror at the beginning of the decade, the fall (and rise) of McLaren, the establishment of the "big three" of Mercedes, Ferrari and Red Bull, Mercedes' dominance of the turbo hybrid era and the late resurgence of Red Bull.

## Adding a dominance penalty to the ATR

This method of allocating more aerodynamic resources to underperforming teams is an excellent example of a negative feedback loop: the better you are doing, the more headwinds you face. My only issue with the ATR is that it needs to account for dominance better. Take, for example, the 2020 and 2021 F1 seasons: Mercedes wins the constructors championship in both seasons, and Red Bull finishes second. In the eyes of the ATR, these two seasons are equal: Mercedes gets 70%, and Red Bull gets 75% of the wind tunnel time. What the ATR doesn't recognize is that the 2020 Mercedes is one of the most dominant cars of all time, whereas, in 2021, Mercedes could have easily lost the championship to Red Bull.

In order to address this gap in the ATR, I would like to propose a modification to the ATR formula:

$$A(t) = 65 + 5p_t - D_{15}(t)$$

The term $D_{15}(t)$ indicates the number of wins a team $t$ has from the last 15 races. It might not be a perfect measurement of dominance, but it's easy to understand and good enough for this purpose. The chart below shows what the aero-testing allocation would look like with this adjusted formula:

<canvas id="myChart2" width="400" height="200"></canvas>

It's evident that the dominant teams (Red Bull, Mercedes, and Red Bull again) are much more harshly punished with this new formula. The gap between P1 and P2 can grow up to 20%, depending on the level of domination. Every team that didn't win races remains unaffected.

## Final remarks

Red Bull had to take a 7% hit to their wind tunnel allocation in 2023, which had little to no impact on their competitiveness. As a matter of fact, they had the dominant season in F1 history, only dropping one race and one sprint. This is why I think a 15% penalty (the absolute maximum a team gets if they win the last 15 races and only 2 times bigger than the penalty Red Bull got) on top of the ATR wouldn't be overly unfair to a dominant team. It's clear that having the best car on the grid as your starting point for next year far outweighs having to manage with less wind tunnel time.
