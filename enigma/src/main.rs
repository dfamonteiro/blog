use enigma_simulator::{EnigmaMachine, EnigmaBuilder};
use std::collections::HashMap;

#[derive(Clone, Debug)]
struct EnigmaEncryptionKey {
    reflector: char,
    rotors: (u8, u8, u8),
    ring_positions: (u8, u8, u8),
    ring_settings: (u8, u8, u8),
    plugboard: String,
}

pub fn main() {
    let cyphertext = "kxzzqebhqwvbmhhmgdtgsvoueoogtouisqblukcyfwgnbifmvjmwcwcqoftzewmmcbvtxvyhczfqwgcgrhqodqdj";

    let mut ring_setting_candidates: Vec<(f64, EnigmaEncryptionKey)> = Vec::new();

    for candidate in get_all_rotor_orderings() {
        println!("Processing {:?}", candidate);
        ring_setting_candidates.append(&mut test_ring_settings(candidate, cyphertext, 10));
    }

    ring_setting_candidates.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
    ring_setting_candidates.truncate(100);

    for candidate in &ring_setting_candidates {
        println!("{:?}", candidate);
        println!("{}", decrypt(&candidate.1, cyphertext));
    }

    let mut ring_position_candidates: Vec<(f64, EnigmaEncryptionKey)> = Vec::new();

    for (_, candidate_key) in ring_setting_candidates {
        println!("Processing {:?}", candidate_key);
        ring_position_candidates.append(&mut test_ring_positions(candidate_key, cyphertext, 10));
    }

    ring_position_candidates.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
    ring_position_candidates.truncate(100);

    for candidate in ring_position_candidates {
        println!("{:?}", candidate);
        println!("{}", decrypt(&candidate.1, cyphertext));
    }
    
}

fn test_ring_settings(key: EnigmaEncryptionKey, cyphertext : &str, number_of_candidates : usize) -> Vec<(f64, EnigmaEncryptionKey)> {
    let mut res: Vec<(f64, EnigmaEncryptionKey)> = Vec::new();

    for p1 in 1..27 {
        for p2 in 1..27 {
            for p3 in 1..27 {
                let mut new_key = key.clone();
                new_key.ring_settings = (p1, p2, p3);
                let decryption = decrypt(&new_key, cyphertext);

                res.push((index_of_coincidence(&decryption), new_key));
            }
        }
    }

    res.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
    res.truncate(number_of_candidates);

    res
}

fn test_ring_positions(key: EnigmaEncryptionKey, cyphertext : &str, number_of_candidates : usize) -> Vec<(f64, EnigmaEncryptionKey)> {
    let mut res: Vec<(f64, EnigmaEncryptionKey)> = Vec::new();

    for p1 in 1..27 {
        for p2 in 1..27 {
            for p3 in 1..27 {
                let mut new_key = key.clone();
                new_key.ring_positions = (p1, p2, p3);
                let decryption = decrypt(&new_key, cyphertext);

                res.push((index_of_coincidence(&decryption), new_key));
            }
        }
    }

    res.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
    res.truncate(number_of_candidates);

    res
}

fn get_all_rotor_orderings() -> Vec<EnigmaEncryptionKey> {
    let mut res: Vec<EnigmaEncryptionKey> =  Vec::new();

    for reflector in ['B'] { // ['A', 'B', 'C'] {
        for rotor1 in 1..5 { // 1..9 {
            for rotor2 in 1..5 { // 1..9 {
                for rotor3 in 1..5 { // 1..9 {
                    if (rotor1 != rotor2) &&( rotor1 != rotor3) && (rotor2 != rotor3) {
                        res.push(EnigmaEncryptionKey {
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

fn decrypt(key : &EnigmaEncryptionKey, cyphertext: &str) -> String {
    EnigmaMachine::new()
    .reflector(&key.reflector.to_string())
    .rotors(key.rotors.0, key.rotors.1, key.rotors.2)
    .ring_settings(key.ring_settings.0, key.ring_settings.1, key.ring_settings.2)
    .ring_positions(key.ring_positions.0, key.ring_positions.1, key.ring_positions.2)
    .plugboard(&key.plugboard).unwrap().decrypt(cyphertext)
}

fn index_of_coincidence(text: &str) -> f64 {
    // keep only A–Z letters and uppercase
    let filtered: String = text
        .chars()
        .filter(|c| c.is_ascii_alphabetic())
        .map(|c| c.to_ascii_uppercase())
        .collect();

    let n = filtered.len();
    if n <= 1 {
        return 0.0;
    }

    let mut freqs = HashMap::new();
    for c in filtered.chars() {
        *freqs.entry(c).or_insert(0usize) += 1;
    }

    let numerator: usize = freqs.values().map(|&f| f * (f - 1)).sum();
    let denominator = n * (n - 1);

    numerator as f64 / denominator as f64
}

fn _demo() {
    let machine = EnigmaMachine::new()
    .reflector("B")
    // .plugboard("BY EW FZ GI QM RV UX")
    .rotors(1, 2, 3)
    .ring_settings(10, 12, 14)
    .ring_positions(5, 22, 3).unwrap();
    let plaintext = machine.decrypt("thetimeandlaborinvolvedinanexactperformancecalculationhadtwoquitepredictableconsequences");
    println!("{plaintext}");
}

// Decrypted:                  thetimeandlaborinvolvedinanexactperformancecalculationhadtwoquitepredictableconsequences
// Encrypted (with plugboard): KUKFPCOHMERYCHBEIZTIMXONWOOUHOXGSHQBXEWBZEKNYGZORJQBCECMOZUFJYLQLYVYUNBHCXZUEICIOZVPDMUJ
// Encrypted (no   plugboard): kxzzqebhqwvbmhhmgdtgsvoueoogtouisqblukcyfwgnbifmvjmwcwcqoftzewmmcbvtxvyhczfqwgcgrhqodqdj


// (0.05721003134796238, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 4, 2), ring_positions: (26, 13, 14), ring_settings: (16, 12, 17), plugboard: "" })
// AALFIVGLGTAMFLRIAILJKGYKAFPDLSJLMLGSJVAHMPMAKFJUMHPOOSGLSLHIDODSSDPOMTFLHADJHVMSHAWDQHPR
// (0.056948798328108674, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 4), ring_positions: (17, 3, 3), ring_settings: (18, 19, 12), plugboard: "" })
// VYIRCRXPFJFKTBJDQSLAKGPVAQKDYVFYZWADYALNNHYYJNJIOVHAGVYWFMXYYSFSAAFDYALEAAYTXNLZWLOZYYFY
// (0.056948798328108674, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (1, 1, 1), ring_settings: (2, 1, 11), plugboard: "" })
// QGUQPNQZSNHKANSKNYAHAZGWSUNBXSTZBGGAZBXIHSKHKPNRDQBNZRKGQQHHQNUVNZGZFPLBQBYKQCGXLRYGQDMQ
// (0.056948798328108674, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (1, 1, 1), ring_settings: (6, 17, 12), plugboard: "" })
// THETIMEANDLABORINVOCHEDINANEXACTPERFORMANCECABXULATIONHADTWOQUITEPREDICKFBLECONSEQUENCES <---------------
// (0.056948798328108674, EnigmaEncryptionKey { reflector: 'B', rotors: (4, 1, 3), ring_positions: (1, 1, 1), ring_settings: (16, 7, 12), plugboard: "" })
// WJNSMRDJPBCVUDWBOJJBEWSLLSJHEZLJQFUYWEDIWJJXTTMYGYYMMJZSSJFJUVJWSFPSJJXXJXXRTSOCIVWPHBUV
// (0.056948798328108674, EnigmaEncryptionKey { reflector: 'B', rotors: (4, 2, 3), ring_positions: (14, 3, 19), ring_settings: (15, 23, 19), plugboard: "" })
// JLBWOFGGCLIHNLLNLIINPANGVTNHUMJUIWTXIBQZOOVIUPIDBZOIMMMDULUULNFBUNOOIICNLJKMOIUVQAOYPYIA
// (0.056948798328108674, EnigmaEncryptionKey { reflector: 'B', rotors: (3, 1, 4), ring_positions: (13, 9, 3), ring_settings: (10, 13, 13), plugboard: "" })
// QNTGMYNQGUGNWQMQLCZXFJJVGWNQGNNVFBNHLVLOZNXCNYONGNNXICSCSNIGAYINOMEYDBFIFQJPVAXEOIANLYMC
// (0.056948798328108674, EnigmaEncryptionKey { reflector: 'B', rotors: (2, 1, 4), ring_positions: (10, 17, 13), ring_settings: (14, 8, 16), plugboard: "" })
// LLSXFKJFLQTMKZCOCIVTQQSQJDJEKXGKFHRKOQTTQQOUPGQXRVVIVFQLLEQTMDEQKHTWFSTFFOQZSWDODVXQFXKS