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

Brainfuck is an interesting programming language, to say the least. As the most prominent [Esoteric programming language](https://en.wikipedia.org/wiki/Esoteric_programming_language "Esoteric programming language Wikipedia page") out there, it provides a truly cursed programming experience. It is quite minimalistic as well, featuring only 8 distinct instructions and a programming model that can be best described as a glorified [Turing machine](https://en.wikipedia.org/wiki/Turing_machine "Turing machine Wikipedia page"), making writing a Brainfuck compiler an interesting programming exercise. However, we might as well go a step further and develop an intermediate representation for it, because, well, why not?[^1]

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

If you are looking to better understand some of Brainfuck's nuances, I very much recommend this [informal specification](https://github.com/brain-lang/brainfuck/blob/master/brainfuck.md "An informal Brainfuck specification"), from which the instruction set subchapter is mostly based on.

[^2]: 300000 cells is infinite enough.

## Potential for improvement

For what Brainfuck makes up in simplicity, it loses out in expressiveness and efficiency (or a lack thereof). Please take a look at the code excerpt below:

```brainfuck
+++++++++++++[->++>>>+++++>++>+<<<<<<]>>>>>++++++>--->>>>>>>>>>+++++++++++++++[[
>>[-<<<<<<+>>>>>>]<<<<<<[->>>>>>+<<+<<<+<]>>>>>>>>]<<<<<<<<<[<<<<<<<<<]>>>>>>>>>
[>>>>>>>>[-<<<<<<<+>>>>>>>]<<<<<<<[->>>>>>>+<<+<<<+<<]>>>>>>>>]<<<<<<<<<[<<<<<<<
<<]>>>>>>>[-<<<<<<<+>>>>>>>]<<<<<<<[->>>>>>>+<<+<<<<<]>>>>>>>>>+++++++++++++++[[
>>>>>>>>>]+>[-]>[-]>[-]>[-]>[-]>[-]>[-]>[-]>[-]<<<<<<<<<[<<<<<<<<<]>>>>>>>>>-]+[
```

When I look at Brainfuck code, the thing that strikes me most is how repetitive it looks. For example, the first line starts with 13 `+` instructions in a row. I could just as easily find examples for the other three memory-manipulating instructions (`-` `<` `>`).

Another pattern that comes up often is this one: `[-]`. This loop decrements the value of the cell until it reaches 0.

Lastly, finding the matching `]` for the respective `[` (or vice-versa) can be very time-consuming, depending on how much of your program is enclosed by this bracket pair.

### An improved intermediate representation

Taking these inefficiencies into account, we can develop a higher-level and more performant abstraction:

```rust
/// Brainfuck source code gets compiled
/// to an intermediate representation made up of `OpCode`s
enum OpCode {
    /// Increments the value in the cell by x (can overflow)
    Increment(u8),
    /// Decrements the value in the cell by x (can underflow)
    Decrement(u8),
    /// Moves the pointer x values to the left
    MoveLeft(usize),
    /// Moves the pointer x values to the right
    MoveRight(usize),
    /// Sets the value in cell to 0
    Zero,
    /// Reads a value into the cell
    Read,
    /// Prints the value from the cell as an ASCII character
    Write,
    /// Jump to destination if:
    /// - The value from the cell is 0 and the direction is [Direction::Forward]
    /// - The value from the cell is not 0 and the direction is [Direction::Backward]
    Jump {
        destination: usize,
        direction: Direction,
    },
}

#[derive(Eq, PartialEq)]
enum Direction {
    Forward,
    Backward,
}
```

As you can see, not only did we mitigated the repetitiveness of the memory-manipulating instructions but we also turned the `[` `]` jump instructions into a O(1) operation by precalculating the destination instruction index. An instruction that zeroes out the cell was also introduced, in order to replace the `[-]` pattern.
