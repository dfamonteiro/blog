from itertools import product
import pandas

POSSIBILITIES = {
    "Danny Ric" : ["F1", "not F1"],
    "Pato" : ["F1", "Indycar"],
    "Felix" : ["Indycar", "FE"],
    "Herta/Piastri" : ["F1", "not F1"],
    "Palou" : ["Indycar", "F1", "CGR/gets parked"]
}

solutions = []

for entry in product(*POSSIBILITIES.values()):
    if entry.count("F1") != 1 or entry.count("Indycar") != 2:
        continue
    solutions.append(entry)

print(pandas.DataFrame(data=solutions, columns=POSSIBILITIES.keys()).to_markdown())