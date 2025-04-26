use std::{env, fs::File};
use std::io::{BufWriter, Write};

fn compute_pairings(n : u8) -> Vec<Vec<u8>> {
    let mut res = Vec::new();
    let available_teams : Vec<u8> = (0..=(n - 1)).collect();
    let current_solution = Vec::new();

    compute_pairings_rec(n, available_teams, current_solution, &mut res);

    res
}

fn compute_pairings_rec(n : u8, mut available_teams : Vec<u8>, mut current_solution : Vec<u8>, res : &mut Vec<Vec<u8>>) {
    if n == 2 {
        current_solution.extend(&available_teams);
        res.push(current_solution);
    } else {
        current_solution.push(available_teams.remove(0));

        for i in 0..available_teams.len() {
            let mut current_solution = current_solution.clone();
            let mut  available_teams = available_teams.clone();

            current_solution.push(available_teams.remove(i));
            compute_pairings_rec(n - 2, available_teams, current_solution, res);
        }
    }
}

fn main() {
    let args: Vec<String> = env::args().collect();
    let n_teams = args[1].parse::<u8>().unwrap();
    let file_path = &args[2];

    let res = compute_pairings(n_teams);
    println!("{:?}", res.len());

    let file = File::create(file_path).unwrap();
    let mut writer = BufWriter::new(file);

    for vector in res {
        writeln!(writer, "{:?}", vector).unwrap();
    }

    writer.flush().unwrap();
}
