mod utils;
mod plugboard;
mod rotors;

fn _wip_main() {
    let cyphertext = "KUKFPCOHMERYCHBEIZTIMXONWOOUHOXGSHQBXEWBZEKNYGZORJQBCECMOZUFJYLQLYVYUNBHCXZUEICIOZVPDMUJ";
    let _english_language_model = utils::build_statistical_language_model();
    
    let mut ring_setting_candidates: Vec<(f64, utils::EnigmaEncryptionKey)> = Vec::new();
    
    for candidate in rotors::get_all_rotor_orderings() {
        println!("Processing {:?}", candidate);
        ring_setting_candidates.append(&mut rotors::test_ring_settings(candidate, cyphertext, 10));
    }
    
    ring_setting_candidates.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
    ring_setting_candidates.truncate(100);
    
    for candidate in &ring_setting_candidates {
        println!("{:?}", candidate);
        println!("{}", utils::decrypt(&candidate.1, cyphertext));
    }
    
    let mut ring_position_candidates: Vec<(f64, utils::EnigmaEncryptionKey)> = Vec::new();
    
    for (_, candidate_key) in ring_setting_candidates {
        println!("Processing {:?}", candidate_key);
        ring_position_candidates.append(&mut rotors::test_ring_positions(candidate_key, cyphertext, 10));
    }
    
    ring_position_candidates.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
    ring_position_candidates.truncate(100);
    
    for candidate in ring_position_candidates {
        println!("{:?}", candidate);
        println!("{}", utils::decrypt(&candidate.1, cyphertext));
    }
}

pub fn find_best_plugboard(key: &utils::EnigmaEncryptionKey, cyphertext: &str, baseline_plugboard: &Vec<(char, char)>, baseline_score: f64) -> Vec<(f64, String)> {
    let alphabet: Vec<char> = ('A'..='Z').collect();

    let mut candidates: Vec<(f64, String)> = vec![(baseline_score, convert_plugboard_to_string(baseline_plugboard))];

    for i in 0..alphabet.len() {
        for j in (i+1)..alphabet.len() {
            let i = alphabet[i];
            let j = alphabet[j];
            let mut plugboard: Vec<(char, char)> = baseline_plugboard.clone();

            if plugboard.iter().any(|(a, b)| *a == i || *b == i || *a == j || *b == j) {
                continue;
            }

            match plugboard.last() {
                None => (),
                Some(last) => if i < last.1 {continue;}
            }

            plugboard.push((i, j));

            let test_plugboard = convert_plugboard_to_string(&plugboard);

            let mut new_key = key.clone();
            new_key.plugboard = test_plugboard;
            let test_score = utils::index_of_coincidence(&utils::decrypt(&new_key, cyphertext));

            if test_score > baseline_score {
                candidates.append(&mut find_best_plugboard(&new_key, cyphertext, &plugboard, test_score));
            }
            else {
                candidates.push((test_score, new_key.plugboard));
            }
        }
    }

    candidates
}

fn convert_plugboard_to_string(plugboard: &Vec<(char, char)>) -> String {
    plugboard
        .iter()
        .map(|(a, b)| format!("{}{}", a, b))
        .collect::<Vec<String>>()
        .join(" ")
}

pub fn main() {
    let cyphertext: &str = include_str!("../examples/privacy-cyphertext.txt");
    let key = utils::EnigmaEncryptionKey {
        reflector: 'B',
        rotors: (2, 3, 1),
        ring_positions: (21, 17, 3),
        ring_settings: (10, 23, 4),
        plugboard: String::new()
    };

    let mut res = find_best_plugboard(&key, cyphertext, &Vec::new(), utils::index_of_coincidence(&utils::decrypt(&key, cyphertext)));
    
    res.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
    res.truncate(20);


    for candidate in res {
        let mut new_key = key.clone();
        new_key.plugboard = candidate.1.clone();
        println!("{:?} {:?}", candidate, utils::decrypt(&key, cyphertext));
    }
}