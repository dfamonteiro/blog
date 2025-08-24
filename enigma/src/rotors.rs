use std::collections::HashMap;
use crate::utils;

pub fn test_ring_settings(key: utils::EnigmaEncryptionKey, cyphertext : &str, number_of_candidates : usize) -> Vec<(f64, utils::EnigmaEncryptionKey)> {
    let mut res: Vec<(f64, utils::EnigmaEncryptionKey)> = Vec::new();

    for p1 in 1..27 {
        for p2 in 1..27 {
            for p3 in 1..27 {
                let mut new_key = key.clone();
                new_key.ring_settings = (p1, p2, p3);
                let decryption = utils::decrypt(&new_key, cyphertext);

                res.push((utils::index_of_coincidence(&decryption), new_key));
            }
        }
    }

    res.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
    res.truncate(number_of_candidates);

    res
}

pub fn test_ring_positions(key: utils::EnigmaEncryptionKey, cyphertext : &str, number_of_candidates : usize) -> Vec<(f64, utils::EnigmaEncryptionKey)> {
    let mut res: Vec<(f64, utils::EnigmaEncryptionKey)> = Vec::new();

    for p1 in 1..27 {
        for p2 in 1..27 {
            for p3 in 1..27 {
                let mut new_key = key.clone();
                new_key.ring_positions = (p1, p2, p3);
                let decryption = utils::decrypt(&new_key, cyphertext);

                res.push((utils::index_of_coincidence(&decryption), new_key));
            }
        }
    }

    res.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
    res.truncate(number_of_candidates);

    res
}

pub fn get_all_rotor_orderings() -> Vec<utils::EnigmaEncryptionKey> {
    let mut res: Vec<utils::EnigmaEncryptionKey> =  Vec::new();

    for reflector in ['B'] { // ['A', 'B', 'C'] {
        for rotor1 in 1..5 { // 1..9 {
            for rotor2 in 1..5 { // 1..9 {
                for rotor3 in 1..5 { // 1..9 {
                    if (rotor1 != rotor2) &&( rotor1 != rotor3) && (rotor2 != rotor3) {
                        res.push(utils::EnigmaEncryptionKey {
                            reflector : reflector,
                            rotors: (rotor1, rotor2, rotor3),
                            ring_positions: (1, 1, 1),
                            ring_settings: (1, 1, 1),
                            plugboard : String::new(),
                        });
                    }
                }
            }
        }
    }
    res
}