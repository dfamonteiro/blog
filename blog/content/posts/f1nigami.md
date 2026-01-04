+++ 
draft = true
date = 2026-01-04T10:32:30Z
title = "F1nigami - The case for scorigamis in Formula 1"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["motorsport"]
categories = []
externalLink = ""
series = []
+++

If you happen to both be a sports fan and slightly too addicted to youtube, you might have come across the term Scorigami - a concept invented by [Jon Bois](https://bsky.app/profile/jonbois.bsky.social) to denote [**"the act, and art, of producing a final score in a football game that has never happened before"**](https://www.sbnation.com/2014/9/8/6110147/pete-carroll-seahawks-scoragami-weird-nfl-scores). These scorigamis were originally just funny statistical oddities, but after [Secret Base's viral video essay on scorigamis](https://www.youtube.com/watch?v=9l5C8cGMueY), these unique scoring events have gained a life of their own:

<figure>
    <img src="/images/scorigami/scorigami-cultural-impact.png" alt="Scorigami cultural impact">
    <figcaption>Scorigami cultural impact</figcaption>
</figure>

Nowadays it's a massive deal[^1] when a scorigami happens in an american football game, to the point it might even be [mentioned on the official NFL broadcast](https://www.youtube.com/watch?v=16VXdbigeqM)! But as more of a Formula 1 fan myself I kept wondering: how cool would it be if we had scorigamis in F1?

[^1]: Ok I might be slightly exagerating, but don't let facts get in the way of a good story!

## The hunt for a good scorigami statistic for Formula 1

Formula 1 is very different from NFL, and that means that we need to put more thought into what a F1 scorigami should look like.

We have to consider how often these scorigamis happen - if they happen too often the pool of available scorigamis will be exhausted too quickly, but if they don't happen often enough people lose interest on them. The goal we should strive towards is a rate of single-digit scorigamis per year, so that the threat of a scorigami occuring in any given Grand Prix is ever-present.

Other factors to keep in mind: these scorigamis should be easy to calculate and understand by the average F1 fan, and they should be able to happen in every Grand Prix[^2]. The possibility of cursed scorigamis should also exist: things that will either never happen again, or would take divine intervention to happen again.

[^2]: It's [perfectly possible to create scorigamis from things such as the drivers standings](https://www.instagram.com/reel/C1CoIXCRM3J/), but consider this: is it that fun to have to wait for the end of the season to check if you have a scorigami or not?

Taking all of this into consideration, I have an idea:

## My scorigami proposal: points scored over a Grand Prix weekend

I have been thinking about this on and off for a couple of months, and I believe this is the right foundation for us to build our scorigami castle on:

- You can track scorigamis **_per team_**, which I will do in this blog post
- The scorigamis can happen in every Grand Prix weekend
- Easy to calculate: just add the points scored by the team's drivers across the weekend
- Decent combinatorial explosion: in a sprint weekend, there are 4 point-scoring events (2 drivers * 2 races). This leads to potentially rare point scores that can only be achieved by finishing in very specific positions in both the sprint and the race
- Potential for weirdness

### What teams to include?

It's obvious that the teams currently in the grid should have their scorigamis tracked. What is less obvious is how far back through a team's history we should go. Take Mercedes, for example: this team can trace its roots back to [Tyrrell](https://en.wikipedia.org/wiki/Tyrrell_Racing#) which started competing in F1 under its own name in 1970[^3], which would predate William's presence in Formula 1 by ~5 years. That would mean that Mercedes would score scorigamis before Williams!

[^3]: From the data I have this is accurate, but the history records might disagree.

You might consider this to be absolutely nonsensical, and decide that a team's "scorigami history" should only begin from its last ownership change: well, now you have wiped away Team Enstone's (Alpine) and Team Silverstone's (Aston Martin) legacy in Formula 1.

This is a thorny subject with no right answers. I've decided to be generous and include as much of the teams' genealogy as possible:

```python
OLD_TEAM_NAMES = {
    # Teams with stable branding
    "McLaren" :  [(0, "McLaren")],
    "Ferrari" :  [(0, "Ferrari")],
    "Williams" : [(0, "Williams")],
    "Haas F1 Team" : [(2016, "Haas F1 Team")],

    "Red Bull" : [
        (1997, "Stewart", 1999),
        (2000, "Jaguar", 2004),
        (2005, "Red Bull")
    ],
    "Mercedes" : [
        (1968, "Tyrrell", 1998),
        (1999, "BAR", 2005),
        (2006, "Honda", 2008),
        (2009, "Brawn", 2009),
        (2010, "Mercedes")
    ],
    "Aston Martin" : [
        (1991, "Jordan", 2005),
        (2006, "MF1", 2006),
        (2006, "Spyker MF1", 2006),
        (2007, "Spyker", 2007),
        (2008, "Force India", 2018),
        (2019, "Racing Point", 2020),
        (2021, "Aston Martin")
    ],
    "RB F1 Team" : [
        (1985, "Minardi", 2005),
        (2006, "Toro Rosso", 2019),
        (2020, "AlphaTauri", 2023),
        (2024, "RB F1 Team")
    ],
    "Sauber" : [
        (1993, "Sauber", 2005),
        (2006, "BMW Sauber", 2009),
        (2010, "Sauber", 2018),
        (2019, "Alfa Romeo", 2023),
        (2024, "Sauber"),
    ],
    "Alpine F1 Team" : [
        (1981, "Toleman", 1985),
        (1986, "Benetton", 2001),
        (2002, "Renault", 2011),
        (2012, "Lotus F1", 2015),
        (2016, "Renault", 2020),
        (2021, "Alpine F1 Team")
    ]
}
```

## Calculating scorigamis for 70 years of Formula 1

Now that the finer details are figured out, we need to figure out how we're going to process 70 years of Grand Prix racing. I won't go through the technical quandaries today, but I'll give you the gist of it: I downloaded a data dump from the [Jolpica API](https://github.com/jolpica/jolpica-f1) and then developed a [python script](https://github.com/dfamonteiro/blog/blob/main/f1-scorigami/f1scorigami.py) that takes the information from this data dump, and creates a table that keeps track of the points scored by each each team for every Grand Prix, and the number of times that score has occurred for that team.

Here is the table with the Grand Prix score of every team that currently exists on the grid, and the number of times that score has ocurred previously ([html](/charts/score-counts.html), [csv](/charts/score-counts.csv)):

{{< scroll-table "static/charts/score-counts-embed.html" >}}

The column `team_name` is the name of the team _at the time_, while `current_team_name` returns the current team name. `Scorigami` represents the number of times the team has achieved that score at that point in the team's history.

Filtering the last column of the table so that `scorigami` equals 1 will return every time a team scored a hauls of points never before seen in that team's history - a scorigami! ([html](/charts/scorigami.html), [csv](/charts/scorigami.csv))

{{< scroll-table "static/charts/scorigami-embed.html" >}}

## Visualizing our scorigamis

Having our data on a table is nice and all, but the real fun in tracking these scorigamis lies in creating interesting data visualizations for them!

