mod utils;
mod plugboard;
mod rotors;


pub fn main() {
    let contents: &str = include_str!("../examples/privacy-plaintext.txt");

    println!("File contents:\n{}", contents);
    
    println!("{}", utils::_standardize_ascii_text(contents));
    
    // plugboard::plugboard_test();
}

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