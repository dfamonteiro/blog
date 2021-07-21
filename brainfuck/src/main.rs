use std::fs;
use std::io::Read;

fn main() {
    BFVM::new(&fs::read_to_string("mandelbrot.bf").unwrap()).run();
}

#[derive(Eq, PartialEq)]
enum Direction {
    Forward,
    Backward,
}

enum OpCode {
    Increment(u8),
    Decrement(u8),
    MoveLeft(usize),
    MoveRight(usize),
    Zero,
    Read,
    Write,
    Jump {
        destination: usize,
        direction: Direction,
    },
}

struct BFVM {
    memory: [u8; 300000],
    mem_pointer: usize,

    code: Vec<OpCode>,
    code_pointer: usize,
}

impl BFVM {
    fn new(code: &str) -> Self {
        Self {
            memory: [0; 300000],
            mem_pointer: 0,
            code: BFVM::compile(&code.chars().collect()),
            code_pointer: 0,
        }
    }

    fn compile(code: &Vec<char>) -> Vec<OpCode> {
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
        while self.code_pointer < self.code.len() {
            match &self.code[self.code_pointer] {
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
                    || direction == &Direction::Backward && !zero
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
        let input: Option<u8> = std::io::stdin()
            .bytes()
            .next()
            .and_then(|result| result.ok());

        self.memory[self.mem_pointer] = input.unwrap();
    }
}
