use std::collections::HashMap;
use crate::utils;

pub fn plugboard_test() {
    let candidate_keys = [
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (1, 1, 1), ring_settings: (14, 24, 13), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (12, 17, 12), ring_settings: (11, 4, 3), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (25, 7, 16), ring_settings: (19, 13, 20), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (4, 23, 2), ring_settings: (19, 13, 20), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (1, 1, 1), ring_settings: (19, 13, 20), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (25, 7, 21), ring_settings: (14, 24, 13), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (19, 25, 9), ring_settings: (14, 24, 13), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (1, 17, 16), ring_settings: (19, 13, 20), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (1, 1, 1), ring_settings: (11, 4, 3), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (15, 2, 17), ring_settings: (19, 13, 20), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (1, 1, 1), ring_settings: (16, 17, 19), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (2, 23, 16), ring_settings: (16, 17, 19), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (15, 17, 23), ring_settings: (14, 24, 13), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (4, 14, 21), ring_settings: (11, 4, 3), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (24, 20, 1), ring_settings: (11, 4, 3), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (2, 25, 16), ring_settings: (14, 24, 13), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (5, 1, 13), ring_settings: (14, 24, 13), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (24, 23, 7), ring_settings: (14, 24, 13), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (9, 23, 7), ring_settings: (11, 4, 3), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (14, 11, 20), ring_settings: (11, 4, 3), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (2, 17, 8), ring_settings: (14, 24, 13), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (2, 7, 3), ring_settings: (11, 4, 3), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (10, 12, 11), ring_settings: (11, 4, 3), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (24, 21, 15), ring_settings: (16, 17, 19), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (17, 26, 23), ring_settings: (14, 24, 13), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (3, 7, 13), ring_settings: (19, 13, 20), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (18, 22, 15), ring_settings: (11, 4, 3), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (23, 5, 17), ring_settings: (11, 4, 3), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (13, 21, 18), ring_settings: (14, 24, 13), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (6, 26, 6), ring_settings: (19, 13, 20), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (20, 26, 17), ring_settings: (19, 13, 20), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (11, 24, 19), ring_settings: (19, 13, 20), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (21, 14, 20), ring_settings: (19, 13, 20), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (13, 7, 17), ring_settings: (16, 17, 19), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (17, 10, 3), ring_settings: (16, 17, 19), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (26, 8, 22), ring_settings: (16, 17, 19), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (26, 25, 1), ring_settings: (16, 17, 19), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (6, 14, 17), ring_settings: (16, 17, 19), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (10, 14, 15), ring_settings: (16, 17, 19), plugboard: String::new() },
        utils::EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (13, 7, 18), ring_settings: (16, 17, 19), plugboard: String::new() },
    ];

    let cyphertext = "KUKFPCOHMERYCHBEIZTIMXONWOOUHOXGSHQBXEWBZEKNYGZORJQBCECMOZUFJYLQLYVYUNBHCXZUEICIOZVPDMUJ";
    let english_language_model = utils::build_statistical_language_model();

    let mut res: Vec<(f64, utils::EnigmaEncryptionKey)> = Vec::new();

    for key in candidate_keys {
        println!("Processing {:?}", key);
        res.append(&mut test_permutations(key, cyphertext, &english_language_model, 10));
    }

    res.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
    res.truncate(20);

    for candidate in res {
        println!("{:?}", candidate);
        println!("{}", utils::decrypt(&candidate.1, cyphertext));
    }
}

pub fn test_permutations(key: utils::EnigmaEncryptionKey, cyphertext : &str, language_model: &HashMap<String, f64>, number_of_candidates : usize) -> Vec<(f64, utils::EnigmaEncryptionKey)> {
    let mut res: Vec<(f64, Vec<(char, char)>, utils::EnigmaEncryptionKey)> = vec![(utils::english_score(&utils::decrypt(&key, cyphertext), language_model), Vec::new(), key.clone())];
    let alphabet: Vec<char> = ('A'..='Z').collect();

    let mut index = 0;

    loop {
        if index == res.len() {
            break;
        }
        let (score, plugboard, key) = res[index].clone();
        let mut candidates: Vec<(f64, Vec<(char, char)>, utils::EnigmaEncryptionKey)> = Vec::new();

        for i in 0..alphabet.len() {
            for j in (i+1)..alphabet.len() {
                let i = alphabet[i];
                let j = alphabet[j];
                let mut plugboard = plugboard.clone();

                if plugboard.iter().any(|(a, b)| *a == i || *b == i || *a == j || *b == j) {
                    break;
                }

                plugboard.push((i, j));

                let test_plugboard = plugboard
                    .iter()
                    .map(|(a, b)| format!("{}{}", a, b))
                    .collect::<Vec<String>>()
                    .join(" ");

                let mut new_key = key.clone();
                new_key.plugboard = test_plugboard;
                let test_score = utils::english_score(&utils::decrypt(&new_key, cyphertext), language_model);

                if test_score > score + 0.2 {
                    candidates.push((test_score, plugboard.clone(), new_key));
                }

                plugboard.remove(plugboard.len() - 1);
            }
        }

        // candidates.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
        // candidates.truncate(5);
        res.append(&mut candidates);

        index += 1;
    }

    res.iter().map(|(score, _plugboard, key)| (score.clone(), key.clone())).collect::<Vec<(f64, utils::EnigmaEncryptionKey)>>()
}