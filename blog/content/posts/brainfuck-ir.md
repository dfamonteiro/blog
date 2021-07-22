+++ 
draft = true
date = 2021-07-21T23:11:53+01:00
title = "Developing an intermediate representation of Brainfuck using Rust"
description = "If you think that optimizing Brainfuck is a waste of time, you are definitely correct! Nevertheless, it is a very useful programming exercise to leverage the type system of a language."
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Rust", "compilers", "Brainfuck", "intermediate representation"]
categories = []
externalLink = ""
series = []
+++

Brainfuck is an interesting programming language, to say the least. As the most proeminent [Esoteric programming language](https://en.wikipedia.org/wiki/Esoteric_programming_language "Esoteric programming language Wikipedia page") out there, it provides a truly cursed programming experience. It is quite minimalistic as well, featuring only 8 distinct instructions and a programming model that can be best described as a glorified [Turing machine](https://en.wikipedia.org/wiki/Turing_machine "Turing machine Wikipedia page"), making writing a Brainfuck compiler an interesting programming exercise. However, we might as well go a step further and develop an intermediate representation for it, because, well, why not?[^1]

[^1]: I stand in the shoulders of [giants](https://esolangs.org/wiki/Brainfuck_implementations "Brainfuck implementations") that have done a much better job than I have of optimizing Brainfuck.

## The Brainfuck programming model: a refresher
In order to execute a Brainfuck program, four crucial elements are needed:
1. The program source code (the only element that is immutable)
2. The program counter
3. The memory (an infinite[^2] array of byte-sized cells)
4. The memory pointer

### The Brainfuck instruction set
Brainfuck puts the _Reduced_ in RISC, featuring a grand total of 8 instructions:
- `>` - Moves the memory pointer one cell to the right
- `<` - Moves the memory pointer one cell to the left
- `+` - Adds 1 to the value of the cell
- `-` - Subtracts 1 to the value of the cell
- `.` - Outputs the value from the cell to the command line
- `,` - Reads a character from the command line
- `[` - jumps to the matching `]`, if the value of the cell is 0
- `]` - jumps to the matching `[`, if the value of the cell is **not** 0

If you are looking to better understand some of Brainfuck's nuances, I very much recommend this [informal specification](https://github.com/brain-lang/brainfuck/blob/master/brainfuck.md "An informal Brainfuck specification").

[^2]: 300000 cells is infinite enough.