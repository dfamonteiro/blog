// Copyright 2021 Daniel Monteiro
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

//! Welcome to the documentation of the Brainfuck IR compiler source code
//!
//! For disambiguation purposes, a cell is a byte in memory, and the pointer points towards the instruction currently being executed

use std::fs;
use std::io::Read;

fn main() {
    // Given that this code was mainly written for blogging purposes, I harcoded the file path.
    // A more polished main() would make use of something like clap (https://clap.rs)
    // to take the path of the source file as a command line argument
    BFVM::new(&fs::read_to_string("mandelbrot.bf").unwrap()).run();
}

#[derive(Eq, PartialEq)]
enum Direction {
    Forward,
    Backward,
}

/// Brainfuck source code gets compiled to an intermediate representation made up of `OpCode`s
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
/// The **B**rain**f**uck **V**irtual **M**achine holds all the data necessary to compile and run a Brainfuck program
struct BFVM {
    memory: [u8; 300000],
    mem_pointer: usize,

    code: Vec<OpCode>,
    code_pointer: usize,
}

impl BFVM {
    fn new(code: &str) -> Self {
        //! Compiles the `code` into IR and sets up the VM to make sure that it's ready to run
        Self {
            memory: [0; 300000],
            mem_pointer: 0,
            code: BFVM::compile(&code.chars().collect()),
            code_pointer: 0,
        }
    }

    fn compile(code: &Vec<char>) -> Vec<OpCode> {
        //! Compiles the `code` into IR i.e. a vector of [OpCode]s
        let mut res: Vec<OpCode> = Vec::new();
        let mut index = 0;
        let mut jumps: Vec<usize> = Vec::new();
    
        while index < code.len() {
            match code[index] {
                '<' | '>' | '+' | '-' => {
                    // These 4 operators can be be treated the same way:
                    // For example, if a sequence of "<<<<<" appears,
                    // they get condensed to a MoveLeft(5) opcode
                    let hit = code[index];
                    let mut len = 1;
                    while index + len < code.len() && code[index + len] == hit {
                        len += 1;
                    }
    
                    let opcode = match code[index] {
                        '<' => OpCode::MoveLeft(len),
                        '>' => OpCode::MoveRight(len),
                        '+' => OpCode::Increment(len as u8),
                        '-' => OpCode::Decrement(len as u8),
                        _ => panic!(),
                    };
    
                    res.push(opcode);
                    index += len;
                }
                '.' => {
                    res.push(OpCode::Write);
                    index += 1;
                }
                ',' => {
                    res.push(OpCode::Read);
                    index += 1;
                }
    
                '[' => {
                    if index + 2 < code.len() && code[index + 1] == '-' && code[index + 2] == ']' {
                        // If this pattern "[-]" appears, it means it is just zeroing out a value
                        res.push(OpCode::Zero);
                        index += 3;
                    } else {
                        res.push(OpCode::Jump {
                            direction: Direction::Forward,
                            destination: 0,
                        });
                        index += 1;
    
                        jumps.push(res.len() - 1); // Updating the jumps stack so that
                                                   // we keep track of the index of this '['
                    }
                }
                ']' => {
                    let dest = jumps.pop().unwrap();
    
                    if !matches!( // Sanity check to make sure there is a forward Jump at dest
                        &res[dest],
                        OpCode::Jump {
                            destination: _,
                            direction: Direction::Forward,
                        }
                    ) {
                        panic!();
                    }
    
                    res[dest] = OpCode::Jump {
                        direction: Direction::Forward,
                        destination: res.len(),
                    };
    
                    res.push(OpCode::Jump {
                        direction: Direction::Backward,
                        destination: dest,
                    });
                    index += 1;
                }
                _ => index += 1, // A comment char, moving on
            }
        }
        res
    }

    fn run(&mut self) {
        //! Runs the VM
        while self.code_pointer < self.code.len() {
            match &self.code[self.code_pointer] {
                // Memory values can and do overflow and underflow,
                // hence the use of overflowing_add and overflowing_sub
                OpCode::Increment(i) => {
                    self.memory[self.mem_pointer] =
                        self.memory[self.mem_pointer].overflowing_add(*i).0
                }
                OpCode::Decrement(i) => {
                    self.memory[self.mem_pointer] =
                        self.memory[self.mem_pointer].overflowing_sub(*i).0
                }
                OpCode::MoveLeft(i) => self.mem_pointer -= i,
                OpCode::MoveRight(i) => self.mem_pointer += i,
                OpCode::Zero => self.memory[self.mem_pointer] = 0,
                OpCode::Read => self.read_input(),
                OpCode::Write => print!("{}", self.memory[self.mem_pointer] as char),
                _ => (),
            }
            
            // Code pointer manipulation logic is handled separately
            if let OpCode::Jump {destination, direction} = &self.code[self.code_pointer] {
                let zero = self.memory[self.mem_pointer] == 0;
                if (direction == &Direction::Forward && zero)
                    || (direction == &Direction::Backward && !zero)
                {
                    self.code_pointer = *destination;
                } else {
                    self.code_pointer += 1;
                }
            } else {
                self.code_pointer += 1;
            }
        }
    }

    fn read_input(&mut self) {
        //! Reads a single `char` from `stdin`
        let input: Option<u8> = std::io::stdin()
            .bytes()
            .next()
            .and_then(|result| result.ok());

        self.memory[self.mem_pointer] = input.unwrap();
    }
}
