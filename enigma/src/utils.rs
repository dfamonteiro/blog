use enigma_simulator::{EnigmaMachine, EnigmaBuilder};
use std::collections::HashMap;
use std::fs::File;
use std::io::{self, BufRead};

#[derive(Clone, Debug)]
pub struct EnigmaEncryptionKey {
    pub reflector: char,
    pub rotors: (u8, u8, u8),
    pub ring_positions: (u8, u8, u8),
    pub ring_settings: (u8, u8, u8),
    pub plugboard: String,
}

pub fn _demo() {
    let machine = EnigmaMachine::new()
    .reflector("B")
    // .plugboard("BY EW FZ GI QM RV UX")
    .rotors(1, 2, 3)
    .ring_settings(10, 12, 14)
    .ring_positions(5, 22, 3).unwrap();
    let plaintext = machine.decrypt("thetimeandlaborinvolvedinanexactperformancecalculationhadtwoquitepredictableconsequences");
    println!("{plaintext}");
}

pub fn decrypt(key : &EnigmaEncryptionKey, cyphertext: &str) -> String {
    EnigmaMachine::new()
    .reflector(&key.reflector.to_string())
    .rotors(key.rotors.0, key.rotors.1, key.rotors.2)
    .ring_settings(key.ring_settings.0, key.ring_settings.1, key.ring_settings.2)
    .ring_positions(key.ring_positions.0, key.ring_positions.1, key.ring_positions.2)
    .plugboard(&key.plugboard).unwrap().decrypt(cyphertext)
}

pub fn _standardize_ascii_text(text: &str) -> String {
    text
        .chars()
        .filter(|c| c.is_ascii_alphabetic())
        .map(|c| c.to_ascii_uppercase())
        .collect()
}

pub fn index_of_coincidence(text: &str) -> f64 {
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

pub fn english_score(text: &str, language_model: &HashMap<String, f64>) -> f64 {
    text.chars()
        .collect::<Vec<char>>() // collect into a Vec<char>
        .windows(3)             // take rolling windows of size 3
        .map(|w| w.iter().collect::<String>())
        .map(|s| language_model.get(&s).unwrap_or(&0f64)).sum::<f64>() / ((text.chars().count() - 2) as f64)
}

pub fn build_statistical_language_model() -> HashMap<String, f64> {
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