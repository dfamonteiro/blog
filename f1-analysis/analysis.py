import json
import pathlib
from pprint import pprint

file_path = pathlib.Path(__file__).parent / "driver ratings.json"

with open(file_path, "r") as file:
    data = json.load(file)

top_ratings = []

for driver, details in data.items():
    top_ratings.append(
        (round(max(details['rating history'].values()), 2), details["name"])
    )
pprint(sorted(top_ratings)[-30:])
