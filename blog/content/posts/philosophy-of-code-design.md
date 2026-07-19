+++ 
draft = false
date = 2026-07-12T18:23:07+01:00
title = "John Ousterhout's \"A Philosophy of Software Design\" is a must-read book"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Programming", "Books"]
categories = []
externalLink = ""
series = []
+++

Software engineers are not a fungible commodity. Two people could have the same age, graduate from the same course, have the exact same work experience, have the exact same life experience... and one of them writes much more elegant code than the other. How does this happen? Do some people just have an innate ability to write better structured code than others? Can this be taught?

What is the name of this intangible quality that some engineers have and some don't?

It's called **taste**.

## Defining taste

You won't find a [wikipedia](https://en.wikipedia.org/wiki/Taste_(disambiguation)) page for taste in the way that is described here - this idea of "taste" is frankly more part of programming folklore than anything else.

Nevertheless, there are some who have tried to define taste - to several degrees of formality:

### Linus Torvalds' take

From this [interview](https://www.tag1.com/blog/interview-linus-torvalds-linux-and-git/):

> But it's not like I handed the project off to the first random person to show up. I did maintain Git for a few months, and the thing that made me ask Junio if he wanted to be the maintainer is that very-hard-to-describe notion of "good taste". I don't really have a better description for it: **programming is about solving technical problems, but how you solve them, and how you think about them is important too, and it's one of those things you start to recognize over time: certain people have that "good taste" thing and pick the "right" solution.**
>
> I don't want to claim that programming is an art, because it really is mostly just about "good engineering". I'm a big believer in Thomas Edison's "one percent inspiration and ninety-nine percent perspiration" mantra: it's almost all about the little details and the everyday grunt-work. But there is that occasional "inspiration" part, **that "good taste" thing that is about more than just solving some problem - solving it cleanly and nicely and yes, even beautifully.**
>
> And Junio had that "good taste".
>
> [...]
>
> Btw, this whole "good taste" thing and finding people who have it, and trusting them - that's very much not just about Git. It's very much the history of Linux too. Unlike Git, Linux is obviously a project that I still do actively maintain, but very much like Git, it's also a project with lots of other people involved, and I think one of the big successes of Linux is having literally hundreds of maintainers around, all with that hard-to-define "good taste", and all people who maintain parts of the kernel.

### Mitchell Hashimoto's take

From this [article](https://x.com/mitchellh/article/2070665127331037290):

> “Taste” is the ability to consistently make high-quality qualitative judgments where no objective metric exists. It’s the creation of something that feels right intuitively, with no real justifiable way to measure that. But when you do it, people feel it.
>
> A person with “good taste” is someone who can do this repeatedly, consistently. The funny thing about taste is that it’s hard to create, but its result is very easy to copy. Once someone makes a tasteful decision, others can imitate it almost immediately.
>
> This is usually an argument against the existence of taste: “look how easy I can copy your work!” And yet, you couldn’t create the work without first having someone to copy it from. One has taste, the other doesn’t.
>
> There have always been people with consistently good taste. But taste is coming up more regularly than ever before. It is becoming a critical differentiator.

### My take

Taste is the ability to tell whether a piece of code **_feels_** right. In the same way that physics systems trend towards a state of low entropy, taste is a driving force within you that makes you reshape your software astractions until it reaches its natural _minima_.

Your ability to tell whether code feels right or not will naturally be dependent on your areas of expertize as a software engineer. Take me, the writer of this blog post, as an example: while I can detect potential issues in backend code almost subconsciously, the best I can do with frontend code is say "it looks ok".

Taste is relevant at any level of abstraction: in same same manner you can have elegant code, you can also have an elegant software architecture.

As a final note (and this more of a personal conjecture), taste appears to be intertwined with other positive attributes that are important to software engineering, such as proactiveness, attention to detail, high personal quality standards and clear technical vision.

## Taste is about to become really freaking important

You might be wondering why I am pontificating on this concept of "taste" in a blog post that is meant to be recommending a book. This is why: coding agents (and by extension LLMs) are reshaping the skillset required to be a competent software engineer.

What does it mean to live in a world where the cost of writing code is a mere fraction of what it used to be?[^1] It means that all other aspects of your role as a software engineer become more important by proxy. To be more specific, **your ability to review the code written by your coding agent is going to become absolutely critical**. If you don't have this skill, you risk turning your your codebase into an incomprehensible vibecoded mess.

[^1]: I'm not making any claims on whether or not we already live in this reality. Some of us probably already do, probably. As always, [the future is already here — it's just not very evenly distributed.](https://en.wikiquote.org/wiki/William_Gibson)

So in short... you need to have _taste_.

## Can you improve your taste in software design? John Ousterhout believes you can

All of this to say that you should read John Ousterhout's [_A Philosophy of Software Design_](https://www.goodreads.com/en/book/show/39996759-a-philosophy-of-software-design), which speaks at length about how software systems should be designed and how to manage complexity in software design. When I first read this book, I was taken aback by how much of my personal philosophy of how code should be structured is reflected in the author's words. If there's any way to improve your taste, it's by reading this book.

Chapter 3 alone makes it worth it: **Working Code Isn't Enough**. The first edition of this book was originally published in 2018 _and yet_ this specific chapter couldn't have been better targetted for this agentic era we're diving into headfirst.
