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

    // let mut ring_position_candidates: Vec<(f64, EnigmaEncryptionKey)> = Vec::new();

    // for rotor_order in get_all_rotor_orderings() {
    //     println!("Processing {:?}", rotor_order);
    //     ring_position_candidates.append(&mut test_ring_positions(rotor_order, cyphertext, 10));
    // }

    // ring_position_candidates.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
    // ring_position_candidates.truncate(100);

    let mut ring_setting_candidates: Vec<(f64, EnigmaEncryptionKey)> = Vec::new();

    for candidate in get_all_rotor_orderings() {
        println!("Processing {:?}", candidate);
        ring_setting_candidates.append(&mut test_ring_settings(candidate, cyphertext, 10));
    }

    ring_setting_candidates.sort_by(|a, b| b.0.partial_cmp(&a.0).unwrap());
    ring_setting_candidates.truncate(100);

    for candidate in ring_setting_candidates {
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
    let n = text.len();
    if n <= 1 {
        return 0.0;
    }

    let mut freqs = HashMap::new();
    for c in text.chars() {
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


// (0.06269592476489028, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 4), ring_positions: (1, 1, 1), ring_settings: (18, 19, 12), plugboard: "" })
// EGTIYOKZKZRXCVVQYPYVDEHYYXBJPLLXXLXNPZLTNPDVMYODXKWPYXYXRXHXCVZCVOOOPQXBUHOLXCONYVPXZXCD
// (0.0603448275862069, EnigmaEncryptionKey { reflector: 'B', rotors: (3, 2, 4), ring_positions: (1, 1, 1), ring_settings: (19, 20, 2), plugboard: "" })
// ZZNMOLHCLZXLFXMGWIVLFJLVSENTASEMXUSSOPSKZYNCSJNSSZVZWZVZSQLWYHJLSUSZWSSFBHGXXJNYZXSTMLFR
// (0.05851619644723093, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 4, 3), ring_positions: (1, 1, 1), ring_settings: (2, 2, 16), plugboard: "" })
// OJBOLGLXXDBLBBQYSGDKHXYWTGPXIUPLNOUDCDGQBXHOIKEHKQLHLZBONYKYBXXKHFSDOPDGXDSXHBDYWBABLYLX
// (0.05851619644723093, EnigmaEncryptionKey { reflector: 'B', rotors: (2, 3, 1), ring_positions: (1, 1, 1), ring_settings: (18, 2, 4), plugboard: "" })
// AGWJGDAARPUWJZCVWIRDGWFWWGBTWNKJXKQJQWQWJPIXJZBKLTPJJXKCNWDLUFEWWLRULPFEJFCCVWANKLWJOJTI
// (0.05799373040752351, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 4, 3), ring_positions: (1, 1, 1), ring_settings: (18, 10, 15), plugboard: "" })
// TLBYOZMTLMBMSCSKDQABWBVBPMFTICBFYXXZTBAJJBOQMPGRQZSJIBQLBSSXRMSJYFBIIIAAPHLBBQBXSBACXOGO
// (0.0577324973876698, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 4, 3), ring_positions: (1, 1, 1), ring_settings: (1, 24, 4), plugboard: "" })
// PQAHNBPKXDYLNNBNXOJLVXUMJXSNVQYDTYNFNYXEBCYMNUXJSBAPVNULDDXBNPXWVJNMDCULXIMPIXEMVXZDNLSY
// (0.0577324973876698, EnigmaEncryptionKey { reflector: 'B', rotors: (4, 1, 2), ring_positions: (1, 1, 1), ring_settings: (17, 10, 20), plugboard: "" })
// QQGREARRCURLKBUBQRUKEOVEYDJLMVHWRTGRGJLJLRLHSEQLGRSCHVGKSHBMKLSCUMBLIRRVLJRRUUISXVTMYLGP
// (0.0577324973876698, EnigmaEncryptionKey { reflector: 'B', rotors: (4, 3, 1), ring_positions: (1, 1, 1), ring_settings: (20, 4, 18), plugboard: "" })
// ESXLYNXLHGCOSNPAYMSIHWVSXHMDVLVDDWXISDVDBDDPNYDHDFHUWBDPFBNJXSFUDXXHDGEBAHRMGHVMDBGDAXQT
// (0.05747126436781609, EnigmaEncryptionKey { reflector: 'B', rotors: (4, 1, 2), ring_positions: (1, 1, 1), ring_settings: (13, 3, 6), plugboard: "" })
// YQELNOILWXETTUNGQRKTQQWZTCRBNWJYQTTVJGXQKQPQXXBFTERVNZHTTRHUXGENHKQDQWTETCWZTBQECTJFHESI
// (0.05747126436781609, EnigmaEncryptionKey { reflector: 'B', rotors: (4, 3, 1), ring_positions: (1, 1, 1), ring_settings: (23, 15, 10), plugboard: "" })
// EUOGWJKZEUNHWEEQQEWEHYRYAEIRDSYOYEPEBLLGYCCFGMNRFZWYBSWCBWPGWESWDRWMYUJMYWGUONEWUIDMLWEX
// (0.05721003134796238, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 3, 4), ring_positions: (1, 1, 1), ring_settings: (1, 17, 11), plugboard: "" })
// SPAKIILQLFAMELAQFTQBEKEOULLNFFLQTJNEBSBWLXFHFDXLMCFNOAZGNPFIQBEVVYADWAAAPFCNAAFAVNUAFDVE
// (0.05721003134796238, EnigmaEncryptionKey { reflector: 'B', rotors: (2, 3, 4), ring_positions: (1, 1, 1), ring_settings: (14, 2, 14), plugboard: "" })
// HRILYHRYADYLWGPKKYJRYMBRZYHMBMMQYYIZFMUQZPSZVGHUTRVKYAOUHIEYAYDSUJHPYYRMYXOREUMQKOHFOHER
// (0.056948798328108674, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (1, 1, 1), ring_settings: (2, 1, 11), plugboard: "" })
// QGUQPNQZSNHKANSKNYAHAZGWSUNBXSTZBGGAZBXIHSKHKPNRDQBNZRKGQQHHQNUVNZGZFPLBQBYKQCGXLRYGQDMQ
// (0.056948798328108674, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (1, 1, 1), ring_settings: (6, 17, 12), plugboard: "" })
// THETIMEANDLABORINVOCHEDINANEXACTPERFORMANCECABXULATIONHADTWOQUITEPREDICKFBLECONSEQUENCES <-------------------
// (0.056948798328108674, EnigmaEncryptionKey { reflector: 'B', rotors: (4, 1, 3), ring_positions: (1, 1, 1), ring_settings: (16, 7, 12), plugboard: "" })
// WJNSMRDJPBCVUDWBOJJBEWSLLSJHEZLJQFUYWEDIWJJXTTMYGYYMMJZSSJFJUVJWSFPSJJXXJXXRTSOCIVWPHBUV
// (0.056687565308254965, EnigmaEncryptionKey { reflector: 'B', rotors: (4, 2, 3), ring_positions: (1, 1, 1), ring_settings: (20, 6, 10), plugboard: "" })
// TMMSIFVWSNNCXQNRMWQSPZRLIRSCNBXSDFSSLNXNTTARWYWXSXXRTRENWQWWWYEKURFRLTOEITNKTXOLGSGBNSAS
// (0.05642633228840126, EnigmaEncryptionKey { reflector: 'B', rotors: (1, 2, 3), ring_positions: (1, 1, 1), ring_settings: (19, 7, 22), plugboard: "" })
// AUIUHHHPPULPXPPHPEUZPWPPSIZTQAEOJWMCQXGXZPWZHWRNIAPFKPQKYZZHHCYEGVWWBUIUINNYLIQBPFYZGPBY