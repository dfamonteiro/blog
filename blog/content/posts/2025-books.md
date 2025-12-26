+++ 
draft = false
date = 2025-12-08T17:26:11Z
title = "Reviewing a year's worth of book reading"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Books", "Programming"]
categories = []
externalLink = ""
series = []
+++

As we're nearing the end of the calendar year, I wanted to reflect on the books I've read in these past 12 months. And thankfully, I had the foresight of taking notes throughout the year! Yay, past me!

This won't be a normal blog post in my usual writing style, but I'll keep it simple: the books are organized by theme... that's it.

Let's get started!

## Software engineering as a career path

### The Missing README (Chris Riccomini & Dmitriy Ryaboy)

I was recommended this book by a work colleague, and I have to say that for junior engineers it's a must read, but I must say that it somewhat loses its value once you have over a year of experience in this profession.

Nevertheless, I'd recommend this book to anyone starting to work as a software engineer in a professional setting.

### The Manager's Path (Camille Fournier)

After reading the [_The Missing README_](#the-missing-readme-chris-riccomini--dmitriy-ryaboy) it was only natural that I would pick up this book next.

It was interesting to gather some perspective on how things work on the management side of things. In general this book helped me be more mindful of the types of problems and the kind of work that managers have to do.

But as I somewhat expected before picking up this book, while the first few chapters were very interesting and full of useful details, the final chapters where we go up the management ladder (i.e. managing multiple teams or being a CTO) were a bit more difficult to grasp.[^1]

[^1]: This is more of a reflection of myself and my relative lack of experience in corporate enviroments, rather than a flaw in the book.

### The Staff Engineer's Path : A Guide for Individual Contributors Navigating Growth and Change (Tanya Reilly)

With this book I've felt I completed the triumvirate of the SE career progression books (the other two thirds of this trio being the two books immediately above this review).

While I'm not even remotely close to the level of a staff engineer, I've felt I still managed to extract useful insights from Reilly's writings.

I'm particularly fond of the _Three Maps_ chapter: being able to understand why you're doing what you're doing, what's the goal, and how to navigate a specific company's corporate environment are all extremely useful information for engineers of any level!

## Semiconductor design & manufacturing

### Semiconductor Manufacturing Technology (Michael Quirk & Julian Serda)

I'm well aware that this is a university-level textbook. Nevertheless, I would recommend it for anyone with both a technical mindset and an interest in semiconductor manufacturing because it is very readable and doesn't require much background knowledge, if any.

You can learn a lot about semiconductor manufacturing from the internet, but with this book you will get a more complete picture of the entire manufacturing process.

### Fabless: The Transformation of the Semiconductor Industry (Daniel Nenni & Paul McLellan)

This was a book that highlighted some of the smaller players in the semiconductor industry. It also really highlighted the importance of intellectual property (IP) in the semiconductor industry, and made me understand how the business case for companies such as ARM makes sense.

The foundries (TSMC, Intel, Samsung, etc) and NVIDIA get all the the attention in the semiconductor industry nowadays. This book grounds you, showing you how deep this industry truly goes by telling the story of some of the lesser-known players, such as the EDA design tool companies for example.

## Science Fiction

### Artemis (Andy Weir)

I wasn't able to finish reading this book because I was getting depressed watching the protagonist throw away her life. Nevertheless, there are some aspects of the book that I found fascinating.

When you read a book by Andy Weir, you just know that the world-building will be top notch from a science and engineering point of view, but this book surprised me by how societally deep it is: the harsh conditions of living in the Moon turns the habitats into a petri dish of human interactions, and the exploration of this micro-society by the author is a deeply interesting thought experiment.

### Project Hail Mary (Andy Weir)

_Project Hail Mary_ is just an incredible book. I thought my days of reading fiction were over, but I guess this book is _the_ exception to the rule.

Its scientific foundations are top notch, and I found the "fake" science that drives the plot to be very cohesively and elegantly designed: I'd like to highlight everything related to astrophage as the book's strong point, starting from the scientific discoveries all the way to the practicalities of using and handling large quantities of this fictitious life form.

Another aspect that I really enjoyed was Weir's take on making first contact with another sentient species with a roughly equivalent level of science and technology to ours. I wasn't expecting this at all when I first started reading the book and it was a nice surprise.

Ultimately, it's an excellent page turner: I managed to go through the whole book in two days.

## Rocketry

### Reentry: SpaceX, Elon Musk, and the Reusable Rockets that Launched a Second Space Age (Eric Berger)

Reading this book immediately after [_Project Hail Mary_](#project-hail-mary-andy-weir) was a bit of a surreal experience: one day I was finishing reading a book which was very much science fiction, and the following day I started reading about the exploits of the SpaceX, which in some cases could _might as well_ be science fiction.

I started following SpaceX after watching a video of a Falcon 9 landing on a barge and becoming completely fascinated with the [guidance algorithms](https://www.researchgate.net/publication/258676350_G-FOLD_A_Real-Time_Implementable_Fuel_Optimal_Large_Divert_Guidance_Algorithm_for_Planetary_Pinpoint_Landing) that made this possible. I also recall watching the first Falcon Heavy launch live, the first Starlink launches (which inspired my [master's dissertation](https://repositorio-aberto.up.pt/bitstream/10216/155205/3/649984.pdf)), the inflight abort test, and a whole bunch of Starship tests. What this ends up meaning is that I don't start with a clean slate, when it came to reading this book. Nevertheless, it didn't end up mattering very much because the book is chock full of inside stories and details which provide a ton of context to the SpaceX launches that I witnessed in this past decade.

My only gripe with this book is that the Starlink project/division doesn't get a lot of coverage, but I get that Berger had to make some tough decisions on what makes the cut and what doesn't. Still, I hope that one day in the future there is a Starlink book that details the trials and tribulations of mass-producing satelites and building a megaconstellation.

Somewhat suprisingly, the author's reflections on the future of SpaceX in the book's epilogue was the part of the book that left the biggest impression in me.

### Ignition! An Informal History of Liquid Rocket Propellants (John D. Clark)

This book is a bit of a cult classic in the space & rocket community, and in my opinion, this reputation is completely deserved: John D. Clark makes a wonderful job of not only detailing the advancements of rocket propellant technology, but doing so in an incredibly charismatic manner, almost as if he is telling you a tale after a couple of drinks.

The number of different chemicals that have been tried as rocket propellants is something that is hard to believe when you are reading through the chapters: they really tried everything you can think of, and probably some substances you **_really_** shouldn't be thinking of.

As a final note, this book does a really good job of filling in the blanks between the V2 program, and the american Redstone program. In hindsight it makes complete sense, but the primary stakeholders of liquid rocket propellant development was not necessarily NASA nor orbital rockets, but actually the US military branches which needed storable (and ideally [hypergolic](https://en.wikipedia.org/wiki/Hypergolic_propellant)) propellants for all sorts of missiles they were procuring.

## Pure software engineering

### Designing Data-Intensive Applications (Martin Kleppmann)

This book should be mandatory reading for any software engineer. While university does teach you SQL and database normalization, when you start working professionally as a software engineer, you acquire a whole other set of very specific practical database knowledge, related to _only_ the technologies your workplace uses. This book does a good job of filling in the blanks of your knowledge and gives you a knowledge and understanding of data systems that would take 5-10 years to acquire organically in a normal working environment.

My favorite chapter was the chapter about transactions. From all the chapters in the book, I felt this one was the most immediately useful for me.

I found it funny that after so many pages spent talking about traditional DBs, the book aggressively pivots towards using durable message queues with idempotent message processing, which is a fine and interesting idea! I just don't think that enough time was spent on the practicalities of building an architecture around the idea of [having events as the "source of truth"](https://microservices.io/patterns/data/event-sourcing.html)... but maybe that topic deserves a wholly dedicated book.

### Fundamentals of Software Architecture: An Engineering Approach (Mark Richards & Neal Ford)

I really enjoyed this book and I felt that it was a very good launching pad for the field of software architecture. I have some opinions on the pacing of the book, though:

- In Part I of the book, there are four consecutive chapters about architecture characteristics. Whilst it is a very important topic to discuss, the way it was laid out on the book felt a bit too granular: maybe instead of 4 small chapters, 3 or 2 bigger chapters would suffice.
- Part II, in which we go through a selection of architecture styles in detail, is just fantastic. I read this section of the book in one sitting and enjoyed seeing some architecture patterns present at work being explained in excellent detail.
- Part III focused more on the more human and day-to-day aspects of being a software engineer (being a leader, negotiating, etc.). Some of the do's and dont's are a bit obvious, but I can't fault the authors for wanting to cover all the bases.

All in all, it's a book that I would recommend to anyone as their first literary foray into software architecture.

### Domain-Driven Design: Tackling Complexity in the Heart of Software (Eric Evans)

This book along with [_Designing Data-Intensive Applications_](#designing-data-intensive-applications-martin-kleppmann) belong in my personal list of foundational software engineering books.

Before reading this book, I had somewhat of a theory that different software engineers (and programmers in general) had diferent levels of _taste_, with _taste_ being a sixth sense for how things should go together in a software project. After reading _DDD_ I recognize that, instead of _taste_, the term that perhaps should be used is _design acumen_.

Even two decades after the publishing of this book, many of its fundamental concepts are still relevant to this day:

- The concepts of `Entity`, `Aggregate`, `Value Object`, `Factory` and `Repository` have become popular design patterns and concepts.
- One of my biggest takeaways is the emphasis the author placed on creating, upholding and promoting a **_ubiquitous language_** shared between the softare engineers and the domain experts. It's a key detail that I have always taken for granted at my workplace and never thought much about.

Personally speaking, I felt that I got 80% percent of the value of this book from reading the first 40% of it.

### Software Architecture: The Hard Parts (Neal Ford, Mark Richards, Pramod Sadalage & Zhamak Dehgani)

This might be the best technical book I've read in 2025: Not only is it filled to the brim with technical insights, but is also very engaging from a narrative standpoint. The pattern is quite forward:

1. The beginning of the chapter starts with the architects of a fictional company strugling with a problem.
2. The chapter provides you with a framework to address, mitigate and/or solving the problem.
3. The chapter ends with the fictional architects using insights from the chapter to find a way forward.

It sounds straightforward at first glance but it's so well executed that it becomes the narrative engine of the book. It also brings with it the added benefit of grounding the contents of the book in the real-world scenarios from the fictitional company.

_Software Architecture: The Hard Parts_ might just become my new gold standard when judging tecnical books. The best compliment I can give it is that I can't recommend a specific part of the book: the whole book is great and you are rewarded by reading the book from start to finish with the enjoyment of following these fictional architects while they find solutions for their conundrums. By the end of the book you'll be cheering them on!

## P.S. 2025-12-26

This was meant to be the end of the blog post, but as it turns out, I managed to sneak a few more books in before the end of the year! I learned a valuable lesson though: be **_absolutely sure_** that your year is truly done before you publish a "Year in Review" post.

Without further ado, here are the books I managed to read after the original publishing date of this blog post.

### The Kubernetes Book: 2023 Edition (Nigel Poulton & Pushkar Joglekar)

If you don't know anything about Kubernetes, you should read this book. If you _kinda_ know Kubernetes, you should read this book anyway.

Before delving into this book I already had some working knowledge of operating a kubernetes cluster, but none related to creating new K8s resources from scratch. That has now changed after reading Poulton's work: he does a great job of methodically and thoroughly explaining all the fundamental concepts of this technology, while keeping a good pace throughout the book. I now feel I have a solid grasp on the fundamentals of Kubernetes.

I'd like to take a moment to compliment the way the practical sections of the book are written: while the idea is to reproduce the console commands in your computer, the book makes such an effort of including the expected command line outcomes that these sections become perfectly readable even without a computer in front of you.

### OpenShift for Developers: A Guide for Impatient Beginners (Joshua Wood & Brian Tannous)

This book mainly focuses on the OpenShift features that are exclusive to this platform (and therefore not part of Kubernetes), so I would only recommend this book to you if:

- **A:** You are a user of this platform.
- **B:** You have intentions to make heavy use of the OpenShift-exclusive functionalities.

If you intend to treat OpenShift as a "Kubernetes with a web frontend", I would recomend a [standard Kubernetes book](#the-kubernetes-book-2023-edition-nigel-poulton--pushkar-joglekar) instead.

### Patterns of Enterprise Application Architecture (Martin Fowler _et al._)

Where do I begin? On one hand I want to place this book on the same tier that [Domain-Driven Design](#domain-driven-design-tackling-complexity-in-the-heart-of-software-eric-evans) and [Designing Data-Intensive Applications](#designing-data-intensive-applications-martin-kleppmann) reside. On the other hand, sometimes you can really tell this book was written in 2002. Take this excerpt for example:

> **The Allure of Distributed Objects**
>
> There is a recurring presentation that I used to see two or three times a year during design reviews. Proudly
the system architect of a new OO system lays out his plan for a new distributed object system - let's pretend it's
a some kind of ordering system. He shows me a design that looks rather like Figure 7.1. With separate remote
objects for customers, orders, products, and deliveries. Each one is a separate component that can be placed
on a separate processing node.
>
> I ask, "Why do you do this?"
>
> "Performance, of course," the architect replies, looking at me a little oddly. "We can run each component on a
separate box. If one component gets too busy we add extra boxes for it so we can load-balance our
application." The look is now curious as if he wonders if I really know anything about real distributed object
stuff at all.
>
> Meanwhile I'm faced with an interesting dilemma. Do I just say out and out that this design sucks like an
inverted hurricane and get shown the door immediately?

So this fictional architect's idea is to design small services that map to the domain's entities and can scale independently?
<a href="https://en.wikipedia.org/wiki/Microservices">Surely that's never gonna catch on</a>. To be fair to the author there are plenty of good reasons to **_not_** use microservices[^2], I just wanted to highlight that there are some aspects of this book that are dated.

The chapter on web presentation patterns also didn't age well: aside from the model-view-controller pattern, the rest of the chapter can be disregarded, as 2 decades of innovation on web development frameworks have superseeded most of this chapter's insights.

[^2]: Even in the [microservices wikipedia page](https://en.wikipedia.org/wiki/Microservices#Criticism_and_concerns), the list of cons is far larger that the list of pros.

While some parts of the book aged like milk, others have aged like wine: this book is a treasure trove of insights regarding mapping database data to objects. These insights were valid in 2004, and will continue to be relevant until the end of time[^3]. On a personal note, I found Martin Fowler's insights on the dangers of lazy loading to hit a little close to home:

[^3]: SQL will never die.

> Another danger with Lazy Load is that it can easily cause more database accesses than you need. A good example of this ripple loading is if you fill a collection with Lazy Loads and then look at them one at a time.
>
> This will cause you to go to the database once for each object instead of reading them all in at once. I've seen ripple loading cripple the performance of an application. One way to avoid it is never to have a collection of Lazy Loads but, rather make the collection itself a Lazy Load and, when you load it, load all the contents.

_"I've seen ripple loading cripple the performance of an application."_ Trust me Martin, so have I! We're constantly battling this performance issue at my day job.

So, is this book worth reading? Absolutely! Just don't be afraid of glossing over the parts of the book that don't interest you. I personally skipped over the code examples because I was far more interested in the core concepts and motivations behind the book's patterns, for example.
