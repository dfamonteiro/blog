+++ 
draft = true
date = 2025-12-07T18:49:59Z
title = "Reviewing a year's worth of book-reading"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Books", "programming"]
categories = []
externalLink = ""
series = []
+++

As we're nearing the end of the calendar year, I wanted to reflect on the books I've read in these past 12 months. And thankfully, I had the foresight of taking notes throughout the year! Yay, past me!

This won't be a normal blog post in my usual writing style, but I'll keep it simple: the books are organized by theme... that's it.

Let's get started!

## Software engineering career books

### The Missing README (Chris Riccomini & Dmitriy Ryaboy)

I was recommended this book by a work colleague, and I have to say that for junior engineers, it's a must read, but I must say that it somewhat loses its value once you have over a year of experience in this profession.

Nevertheless, I'd recommend this book to anyone starting to work as a software engineer in a professional setting.

### The Manager's Path (Camille Fournier)

After reading the [The Missing README](#the-missing-readme-chris-riccomini--dmitriy-ryaboy) it was only natural that I would pick up this book.

It was interesting to gather some perspective on how things work on the management side of things. In general this book helped me be more mindful of the types of problems and the kind of work that managers have to do.

But as I somewhat expected before picking up this book, while the first few chapters were very interesting and full of useful details, the final chapters where we go up the management ladder (i.e. managing multiple teams or being a CTO) were a bit more difficult to grasp.[^1]

[^1]: This is more of a reflection of myself and my relative lack of experience in corporate enviroments, rather than a flaw in the book.

### The Staff Engineer's Path : A Guide for Individual Contributors Navigating Growth and Change (Tanya Reilly)

With this book I've felt I completed the triumvirate of the SE career progression books (the other two thirds of this trio being the two books immediately above this review).

While I'm not even remotely close to the level of a staff engineer when it comes to my own career progression, I've felt I still managed to extract useful insights from Reilly's writings

I'm particularly fond of the _Three Maps_ chapter: being able to understand why you're doing what you're doing, what's the goal, and how to navigate a specific company's corporate environment are all extremely useful information for engineers of any level!

### Designing Data-Intensive Applications (Martin Kleppmann)

- IMO, borderline mandatory reading for any software engineer
  - uni spends a lot of time teaching SQL and database normalization (which is completely fine), but when you start working professionally as a software engineer, you acquire a whole other set of practical working knowledge out when working with databases and the great thing about this book is that it gives you a knowledge and understading of databases and data systems that you take 5 to 10 years to acquire organically in a normal working environment.

- My favorite chapter was. And the chapter about the transaction installations. It was very useful. To back my work knowledge with the very extensive explanations that were present in this book.

- Funny: all the talk about consensus... and then the problem is outsourced to zookeper

- All the time spent talking about traditional DBs... and then the book aggressively pivots towards using durable message queues with idempotent message processing, which is fine and a great idea but I don't think that enough time was spent the practicalities of building an architecture around the idea of having events as the "source of truth" and the traditional database almost as a state machine.

### Artemis (Andy Weir)

- We wasn't able to finish reading this book because I was starting getting depressed with reading the seeing the main character through life throw away air life. However, I would like to explain on some topics that. I found very interesting the book, more specifically the word building. I found extremely interesting to start at the author put into the technical considerations of having an habitat on the moon and all the safety and security protocols that they have, specially around the airlocks And preventing a sudden depressurization events.

- Another interesting aspect of this book is how the harsh conditions of the habitats. Went to a formation of a mini society. Inside it. With several strata.

### Semiconductor Manufacturing Technology (Michael Quirk & Julian Serda)

- I think that for anyone with a passing interest in semiconductor manufacturing technologyAnd technical mindset. I would recommend that they should read this book. I'm well aware that this is a text, a university level textbook. But I found it very very readable. And gives you a somewhat complete knowledge of the semiconductor manufacturing process Wish wish you would, you wouldn't be able to get. Just from reading books and. And listening to Youtube videos.

### Fabless: The Transformation of the Semiconductor Industry (Daniel Nenni & Paul McLellan)

- This was a book that highlighted some of the smaller players in the semiconductor industry. And it also really highlighted the importance of IP in the semiconductor industry, and showcased why it's a very viable business model. I feel like foundries (and NVIDIA) get all the the attention in the semiconductor industry. But this book does a good job of lighting some of the lesser-know players, such as the EDA design tool companies.

### Fundamentals of Software Architecture: An Engineering Approach (Mark Richards & Neal Ford)

- I really enjoyed this book and I felt that it was a very good launching pad for the field of software architecture. I have some opinions on the pacing of the book, though:
  - In Part I of the book, there are four consecutive chapters about architecture characteristics. Whilst it is a very important topic to discuss, the way it was laid out on the book felt a bit too granular: maybe instead of 4 small chapters, 3 or 2 bigger chapters would suffice.
  - Part II, in which we go through a selection of architecture styles in detail, is just fantastic. I read this section of the book in one sitting and enjoyed seeing some architecture patterns present at work being explained in excellent detail.
  - Part III focused more on the more human and day-to-day aspects of being a software engineer (being a leader, negotiating, etc.). Some of the do's and dont's are a bit obvious, but I can't fault the authors for wanting to cover all the bases.
- All in all, it's a book that I would recommend to anyone as their first literary foray into software architecture.

### Ignition! An Informal History of Liquid Rocket Propellants (John D. Clark)

- This book is a bit of a cult classic in the space & rocket community, and in my opinion, this reputation is completely deserved: John D. Clark makes a wonderful job of not only detailing the advancements of rocket propellant technology, but doing so in an incredibly charismatic manner, almost as if he is telling a tale to you personally after a couple of drinks. The number of different chemicals that have been tried as rocket propellants is something that is hard to believe when you are reading through the chapters: they really tried everything you can think of, and probably some substances you _really_ shouldn't be thinking of.
- As a final note, this book does a really good job of filling in the blanks between the V2 program, and the american Redstone program. In hindsight it makes complete sense, but the primary stakeholders of liquid rocket propellant development was not necessarily NASA or orbital rockets, but actually the US military branches which needed storable (and ideally hypergolic) propellants for all sorts of missiles they were procuring.

### Project Hail Mary (Andy Weir)

- _Project Hail Mary_ is just an incredible book. I thought my days of reading fiction were over, but I guess this book is _the_ exception to the rule. Its scientific foundations are top notch, and I found the "fake" science to be very cohesively and elegantly designed: I'd like to highlight everything related to astrophage as the book's strong point, starting from the scientific discoveries (which are critical for the book's plot) all the way to the practicalities of using and handling large quantities of this fictitious life form.
- Another aspect that I really enjoyed was Weir's take on making first contact with another sentient species with a roughly equivalent level of science and technology to ours: I wasn't expecting this at all when I first started reading the book and it was a nice surprise.
- Ultimately, it's a very good page turner: I managed to read the whole book in two days.

### Reentry: SpaceX, Elon Musk, and the Reusable Rockets that Launched a Second Space Age (Eric Berger)

- Reading this book immediately after _Project Hail Mary_ was a bit of a surreal experience: one day I was finishing reading a book which was very much science fiction, and the following day I started reading about the exploits of the SpaceX, which in some cases could might as well be science fiction. I started following SpaceX after watching a video of a Falcon 9 landing on a barge and becoming completely fascinated with the [guidance algorithms](https://www.researchgate.net/publication/258676350_G-FOLD_A_Real-Time_Implementable_Fuel_Optimal_Large_Divert_Guidance_Algorithm_for_Planetary_Pinpoint_Landing) that made this possible. I also recall watching the first Falcon Heavy launch live, the first Starlink launches (which inspired my [master's dissertation](https://repositorio-aberto.up.pt/bitstream/10216/155205/3/649984.pdf)), the inflight abort test, and a whole bunch of Starship tests. What this ends up meaning is that I don't start with a clean slate, when it came to reading this book. Nevertheless, it didn't end up mattering very much because the book is chock full of inside stories and details which provide a ton of context to the SpaceX launches that I witnessed in this past decade.
- My only gripe with this book is that the Starlink project/division doesn't get a lot of coverage, but I get that Berger had to make some tough decisions on what makes the cut and what doesn't. Still, I hope that one day in the future there is a Starlink book that details the trials and tribulations of mass-producing satelites and building a megaconstellation.
- Somewhat suprisingly, the author's reflections on the future of SpaceX in the book's epilogue was the part of the book that left the biggest impression in me.

### Domain-Driven Design: Tackling Complexity in the Heart of Software (Eric Evans)

- This book along with [Designing Data-Intensive Applications](#designing-data-intensive-applications-martin-kleppmann) belong in my personal list of foundational software engineering books.
- Before reading this book, I had somewhat of a theory that different software engineers (and programmers in general) had diferent levels of _taste_, with _taste_ being a sixth sense for how things should go together in a software project. After reading DDD I recognize that, instead of _taste_, the term that perhaps should be used is _design acumen_.
- Even two decades after the publishing of this book, many of its fundamental concepts are still relevant to this day:
  - The concepts of `Entity`, `Aggregate`, `Value Object`, `Factory` and `Repository` have become popular design patterns and concepts.
  - One of my biggest takeaways is the emphasis the author placed on creating, upholding and promoting a **_ubiquitous language_** shared between the softare engineers and the domain experts. It's a key detail that I have always taken for granted at my workplace and never thought much about.
- Personal opinion, but I felt that I got 80% percent of the value of this book from reading the first 40% of it.

### Software Architecture: The Hard Parts (Neal Ford, Mark Richards, Pramod Sadalage & Zhamak Dehgani)

This might be the best technical book I've read in 2025: Not only is it filled to the brim with technical insights, but is also very engaging from a narrative standpoint. The pattern is quite forward:

1. The beginning of the chapter starts with the architects of a fictional company strugling with a problem.
2. The chapter provides you with a framework to address, mitigate and/or solving the problem.
3. The chapter ends with the fictional architects using insights from the chapter to find a way forward.

It sounds straightforward at first glance but it's so well executed that it becomes, for lack of better words, the narrative engine of the book. It also brings with it the added benefit of grounding the contents of the book in the real-world scenarios from the fictitional company.

_Software Architecture: The Hard Parts_ might just become my new gold standard when judging tecnical books. The best compliment I can give it is that I can't recommend a specific part of the book: the whole book is great and you are rewarded by reading the book from start to finish with the enjoyment of following these fictional architects finding solutions for their conudrums. By the end of the book you'll be cheering them on!
