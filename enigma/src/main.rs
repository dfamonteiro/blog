use enigma_simulator::{EnigmaMachine, EnigmaBuilder};
use std::collections::HashMap;
use std::fs::File;
use std::io::{self, BufRead};

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
    let english_language_model = build_statistical_language_model();

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
        ring_position_candidates.append(&mut test_ring_positions(candidate_key, cyphertext, &english_language_model, 10));
    }

    ring_position_candidates.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
    ring_position_candidates.truncate(100);

    for candidate in ring_position_candidates {
        println!("{:?}", candidate);
        println!("{}", decrypt(&candidate.1, cyphertext));
    }
    
}

fn english_score(text: &str, language_model: &HashMap<String, f64>) -> f64 {
    text.chars()
        .collect::<Vec<char>>() // collect into a Vec<char>
        .windows(3)             // take rolling windows of size 3
        .map(|w| w.iter().collect::<String>())
        .map(|s| language_model.get(&s).unwrap_or(&0f64)).sum::<f64>() / ((text.chars().count() - 2) as f64)
}

fn build_statistical_language_model() -> HashMap<String, f64> {
    let file = File::open("english_trigrams.txt").unwrap();
    let reader = io::BufReader::new(file);

    let mut map = HashMap::new();

    for line in reader.lines() {
        let line = line.unwrap();
        let mut parts = line.split_whitespace();

        if let (Some(trigram), Some(count_str)) = (parts.next(), parts.next()) {
            if let Ok(count) = count_str.parse::<usize>() {
                map.insert(trigram.to_string(), count);
            }
        }
    }

    let total = map.values().sum::<usize>();

    let mut res: HashMap<String, f64> = HashMap::new();

    for (trigram, count) in map {
        res.insert(trigram, (count as f64 / total as f64).log2());
    }

    res
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

fn test_ring_positions(key: EnigmaEncryptionKey, cyphertext : &str, language_model: &HashMap<String, f64>, number_of_candidates : usize) -> Vec<(f64, EnigmaEncryptionKey)> {
    let mut res: Vec<(f64, EnigmaEncryptionKey)> = Vec::new();

    for p1 in 1..27 {
        for p2 in 1..27 {
            for p3 in 1..27 {
                let mut new_key = key.clone();
                new_key.ring_positions = (p1, p2, p3);
                let decryption = decrypt(&new_key, cyphertext);

                res.push((english_score(&decryption, language_model), new_key));
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


// (-11.825969165601718, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (1, 1, 1), ring_settings: (6, 17, 12), plugboard: "" })
// THETIMEANDLABORINVOCHEDINANEXACTPERFORMANCECABXULATIONHADTWOQUITEPREDICKFBLECONSEQUENCES
// (-11.894460953987181, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (23, 11, 26), ring_settings: (2, 1, 11), plugboard: "" })
// THETIMEANDLABORINVOCHYDINANEXACTPERFORMANCECABXSLATIONHADTWOQUITEPREDICKFLLECONSEQUENCES
// (-12.511133780267013, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (11, 20, 7), ring_settings: (16, 10, 18), plugboard: "" })
// THETIMEANDLABORJEGWLVEDINANEXACTPERFORMANBLGJLCULATIONHADTWOQUITEPROKMATABLECONSEQUENCES
// (-13.44272259427939, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (22, 4, 3), ring_settings: (2, 21, 14), plugboard: "" })
// CDQDELQZRUKSAXJUHQLLVEDINANEXACTPERFORMANCECALCULATIONHADTWOQUITEPREDICTABLECONSEQUENCES
// (-14.256240941164712, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (14, 17, 11), ring_settings: (19, 7, 22), plugboard: "" })
// THETIMEANDLZCNGJEGWLVEDINANEXACTPERFOXERKBLGJLCULATIONHADTWOQUIGBKQOKMATABLECONSEQUENCES
// (-14.76857335320216, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (13, 25, 19), ring_settings: (18, 16, 4), plugboard: "" })
// JRDTIMEANDLABORINVOCHYHVWZEJJACTPERFORMANCECABXSCRSNBXLADTWOQUITEPREDICKFLKTFYOYKQUENCES
// (-15.344821978384964, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (22, 24, 18), ring_settings: (1, 15, 3), plugboard: "" })
// JRDBIMEANDLABORINVOCHYHVWZEJJVCTPERFORMANCECABXSCRSNBXLRDTWOQUITEPREDICKFLKTFYOYKIUENCES
// (-15.618564703768623, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 4, 3), ring_positions: (4, 12, 26), ring_settings: (4, 11, 21), plugboard: "" })
// YNDCUSWESESOQJOZNNSEFJACTJNNSATYTVVJFGETUIWODQIFEROLBDSGHDGWOLORMMMOSTELLHRAMEAHEROHTEPX
// (-15.709021598556395, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (16, 11, 15), ring_settings: (21, 1, 26), plugboard: "" })
// THETIMEWIOBZCNGJEGWLVEDINANEXACTPBKSQXERKBLGJLCULATIONHADTWKDARGBKQOKMATABLECONSEQUENCXS
// (-15.711703490508183, EnigmaEncryptionKey { reflector: 'B', rotors: (4, 1, 3), ring_positions: (19, 17, 11), ring_settings: (2, 4, 2), plugboard: "" })
// ERCEDOEASMDKCTSSTXFDXBEINQRZUISMAXLELFHANOVTUNICBNOCSSNBAASJXUCELLDRAKTOLLVELITBATMJRRNT