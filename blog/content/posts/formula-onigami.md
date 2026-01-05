+++ 
draft = false
date = 2026-01-05T01:03:05Z
title = "Formula Onigami - The case for Scorigamis in Formula 1"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["motorsport"]
categories = []
externalLink = ""
series = []
+++

If you happen to be both a sports fan and slightly too addicted to youtube, you might have come across the term Scorigami - a concept invented by [Jon Bois](https://bsky.app/profile/jonbois.bsky.social) to denote [**"the act, and art, of producing a final score in a football game that has never happened before"**](https://www.sbnation.com/2014/9/8/6110147/pete-carroll-seahawks-scoragami-weird-nfl-scores). These Scorigamis were originally just funny statistical oddities, but after [Secret Base's viral video essay on Scorigami](https://www.youtube.com/watch?v=9l5C8cGMueY), these unique scoring events have gained a life of their own:

<figure>
    <img src="/images/scorigami/scorigami-cultural-impact.png" alt="Scorigami cultural impact">
    <figcaption>Scorigami cultural impact</figcaption>
</figure>

Nowadays it's a massive deal[^1] when a Scorigami occurs in an NFL game, to the point it might even be [mentioned on the official broadcast](https://www.youtube.com/watch?v=16VXdbigeqM)! But as more of a Formula 1 fan myself I kept wondering: how cool would it be if we had Scorigamis in F1?

[^1]: Ok I might be slightly exaggerating, but don't let facts get in the way of a good story!

## The hunt for a good Scorigami statistic for Formula 1

Formula 1 is very different from NFL, and that means that we need to put some more thought into what a F1 Scorigami should look like.

We have to consider how often these Scorigamis happen - if they happen too often the pool of available Scorigamis will be exhausted too quickly, but if they don't happen often enough people lose interest in them. The goal we should strive towards is a rate of single-digit Scorigamis per year, so that the threat of a Scorigami occurring in any given Grand Prix is ever-present.

Other factors to keep in mind: these Scorigamis should be meaningful and easy to understand by the average F1 fan, and they should be able to happen in every Grand Prix[^2]. The possibility of "cursed" Scorigamis should also exist: things that will either never happen again, or would take divine intervention to happen again.

[^2]: It's [perfectly possible to create Scorigamis from things such as the drivers standings](https://www.instagram.com/reel/C1CoIXCRM3J/), but consider this: is it that fun to have to wait for the end of the season to check if you have a Scorigami or not?

Taking all of this into consideration, I have an idea:

## My Scorigami proposal: Points scored over a Grand Prix weekend

I have been thinking about this on and off for a couple of months, and I believe this is the right foundation for us to build our Scorigami castle on:

- We can track Scorigamis **_per team_**, which I will do in this blog post
- The Scorigamis can happen in every Grand Prix weekend
- Easy to calculate: just add the points scored by the team's drivers across the weekend
- Decent combinatorial potential: in a sprint weekend, there are 4 point-scoring events (2 drivers * 2 races). This leads to the existence of rare point scores that can only be achieved by finishing in very specific positions in both the sprint and the race
- Potential for weirdness

### What teams to include?

It's obvious that the teams currently in the grid should have their Scorigamis tracked. What is less obvious is how far back through a team's history we should go. Take Mercedes, for example: this team can trace its roots back to [Tyrrell](https://en.wikipedia.org/wiki/Tyrrell_Racing#) which started competing in F1 under its own name in 1970[^3], which would predate Williams' presence in Formula 1 by ~5 years. That would mean that Mercedes would score Scorigamis before Williams!

[^3]: From the data I have this is accurate, but the historical records might disagree.

You might consider this to be absolutely nonsensical, and argue that a team's "Scorigami history" should only begin from its last ownership change: well, now you have wiped away the legacies of Team Enstone (Alpine) and Team Silverstone (Aston Martin) in Formula 1.

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

## Calculating Scorigamis for 70 years of Formula 1

Now that the finer details are figured out, we need to figure out how we're going to process 70 years of Grand Prix racing. I won't go through the technical details today, but I'll give you the gist of it: I downloaded a data dump from the [Jolpica API](https://github.com/jolpica/jolpica-f1) and then developed a [python script](https://github.com/dfamonteiro/blog/blob/main/f1-scorigami/f1scorigami.py) that takes the information from this data dump, and creates a table that keeps track of the points scored by each each team for every Grand Prix, and the number of times that score has occurred for that team.

Here is the table with the Grand Prix scores of every team that currently exists on the grid, and the number of times that score has occurred previously ([html](/charts/score-counts.html), [csv](/charts/score-counts.csv)):

{{< scroll-table "static/charts/score-counts-embed.html" >}}

The column `team_name` is the name of the team _at the time_, while `current_team_name` returns the current team name. `Scorigami` represents the number of times the team has achieved that score at that point in the team's history.

Filtering the last column of the table so that `scorigami` equals 1 will return every time a team scored a haul of points never before seen in that team's history - a Scorigami! ([html](/charts/scorigami.html), [csv](/charts/scorigami.csv))

{{< scroll-table "static/charts/scorigami-embed.html" >}}

## Visualizing our Scorigamis

Having our data in tables is nice and all, but the real fun in tracking these Scorigamis lies in creating interesting data visualizations for them!

### Scorigami timeline

{{< raw "static/charts/scorigami-timeline.html" >}}

Ferrari does what they always do in these kinds of stats, and stat-pads like crazy in the first 20 years of the sport. They end up paying the price later by only having 3 Scorigamis the following 50 years, which are:

- 0.5 points at the [1975 Austrian GP](https://en.wikipedia.org/wiki/1975_Austrian_Grand_Prix#) (???)
- 2.5 points at the [1984 Monaco GP](https://en.wikipedia.org/wiki/1984_Monaco_Grand_Prix) (???)
- 16 points at the [1998 French GP](https://en.wikipedia.org/wiki/1998_French_Grand_Prix)... somehow it took that long for the Scarlet team to score that specific amount of points

### Number of Scorigamis per year

{{< raw "static/charts/scorigami-linechart.html" >}}

Ok, we're more or less guaranteed to have at least a couple of Scorigamis per year! We can also spot three spikes:

- 1971 (10 Scorigamis): McLaren and Tyrrell (later to become Mercedes) enter the grid
- 1991 (10 Scorigamis): Not really sure what happened here, I guess it's a random vintage Scorigami year
- 2010 (27 Scorigamis): The introduction of the 25-18-15-12-10-8-6-4-2-1 points distribution led to this explosion in Scorigamis, which came primarily from the Big 4 (FER, MER, RBR, MCL) because those were the only ones with cars good enough to reach those high points-paying positions

### Formula 1 scores heatmap

In the previous charts we took a look at the Scorigamis that _have already happened_. With this heatmap of all the point scores of the current teams, I wanted to take a look at what the potential Scorigami opportunities are for all the teams in the future. The pitch-black cells with 0 point occurrences are Scorigami opportunities:

{{< raw "static/charts/scorigami-heatmap.html" >}}

## Interesting Scorigami oddities

For this final section of the blog post, I'm going to answer some questions have been nagging me while I've been writing this blog post.

### Global Scorigamis

What if we didn't track Scorigamis per team? Once _any_ team achieved a certain score, that's it: that value is removed off the board. How would our Scorigami table look? ([html](/charts/global-scorigami.html), [csv](/charts/global-scorigami.csv))

{{< scroll-table "static/charts/global-scorigami-embed.html" >}}

As always, Ferrari benefits from being in Formula 1 before anyone else:

```txt
Team       Global Scorigami Count
Ferrari                        33
McLaren                         8
Mercedes                        7
Red Bull                       14
Williams                        1
```

### Fractional Scorigamis

You just know something out of the ordinary has happened when you get Scorigamis with fractional parts. Let's make a list of them: ([html](/charts/fractional-scorigami.html), [csv](/charts/fractional-scorigami.csv))

{{< scroll-table "static/charts/fractional-scorigami-embed.html" >}}

### High-Scorigamis

For every team, was what the highest points haul they achieved?

{{< scroll-table "static/charts/highscorigami-embed.html" >}}

There's some nice moments from these races: the Gasly win in Monza, Esteban Ocon's win in Hungary, Sergio Perez's win and George's heartbreak in Sakhir, followed by George's redemption in Brazil.

You might be wondering how on earth Williams managed to score 66 points in a weekend: well, to spice things up, the FIA decided to double the points of the final race of the 2014 season... and Williams happened to be good there.

## Final notes

Sometimes I write on this blog because I want to share something interesting, other times I want to show how something can be done, but today I just wanted to get this idea of an F1 Scorigami out of my head.

I would also like to include the disclaimer that while I did my best to make sure the Scorigamis are correct, I can't really provide any guarantees that the data I presented here is 100% accurate. If anything, it's very likely I overlooked some detail!

I hope you found this pontification of F1 Scorigamis interesting. In a perfect world, this blog post would be accompanied by a [Secret Base](https://www.youtube.com/channel/UCDRmGMSgrtZkOsh_NQl4_xw)-style video that would follow the history of F1 through these Scorigamis, but alas, that would require an astronomical amount of talent that I simply do not possess. I guess I'll leave it as an exercise to the reader!
