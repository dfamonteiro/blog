import shelve
import json
import pathlib
from typing import Any, Dict, List, Union


from pyergast import pyergast

def encode_key(year : int, race : int) -> str:
    return f"{year}{str(race).rjust(2, '0')}"

def decode_key(key : str):
    return int(key[0:4]), int(key[4:])

def update_race_results(db : shelve.Shelf):
    if len(db.keys()) == 0:
        db[encode_key(1950, 1)] = pyergast.get_race_result(1950, 1)
    
    year, race = decode_key(max(db.keys()))
    race += 1
    
    print(f"Updating from year {year}, race {race}...")

    while True:
        try:
            db[encode_key(year, race)] = pyergast.get_race_result(year, race)
            print(f"Downloading race {race} of the {year} season")
            race += 1
        except IndexError:
            if race == 1: # The next season hasn't started yet
                return
            else: # Time to move on to the next season
                year += 1
                race = 1

# https://thesingleseater.com/2020/04/10/ranking-indycar-drivers-with-elo-ratings/
def update_elo(old_rating : Dict[str, float], race_result : List[str], k : float = 2.5) -> Dict[str, float]:
    """Updates the drivers' ratings based on the race result.
    The ratings update is returned as a Dict that maps each driver to their new rating.

    Args:
        old_rating: maps each driver to their rating as it stands befor this race
        race_result: the order in which the drivers finished (the winner is first in the list)
        k: constant used in the rating calculation (we use the Elo rating system). It influences how big the rating adjustment is
    """

    def expected_win_matchup(elo_a : float, elo_b : float) -> float: # Expected win for driver with `elo_a`
        return 1 / (1 + 10**((elo_b - elo_a)/400))

    def update_driver_elo(old_rating : Dict[str, float], race_result : List[str], driver : str) -> float:
        elo_delta = 0
        won_matchup = False

        for competitor in race_result:
            if competitor == driver:
                won_matchup = True
                continue
            elo_delta += k * ((1 if won_matchup else 0) - expected_win_matchup(old_rating[driver], old_rating[competitor]))
        
        return old_rating[driver] + elo_delta
    
    assert old_rating.keys() == set(race_result) and len(old_rating.keys()) == len(race_result)

    return {driver : update_driver_elo(old_rating, race_result, driver) for driver in race_result}

def deduplicate(l : List[Any]) -> List[Any]:
    return list(dict.fromkeys(l))

def generate_rating_history(db : shelve.Shelf, races : List[str]) -> Dict[str, Union[str, Dict[str, float]]]:
    res = {}

    for race in races:
        # You may be wondering why we need to deduplicate the race results.
        # Well, for example: in the third race of the 1950 Formula One season,
        # Tony Bettenhausen retired from the race, and then rejoined the race
        # as a codriver of another car, meaning he appears twice in the same standings.
        drivers    = deduplicate(list(db[race]["driver"]))
        drivers_id = deduplicate(list(db[race]["driverID"]))

        driver_ratings = {}

        for driver, driver_id in zip(drivers, drivers_id):
            if driver_id not in res:
                print(f"Adding new driver: {driver}")
                res[driver_id] = {
                    "name" : driver,
                    "rating history" : {}
                }
                driver_ratings[driver_id] = 1500

                if driver_id == "bettenhausen":
                    print(race)
            else:
                rating_history = res[driver_id]["rating history"]
                latest_race = max(rating_history.keys())
                driver_ratings[driver_id] = rating_history[latest_race]

        assert driver_ratings.keys() == update_elo(driver_ratings, drivers_id).keys()

        for driver_id, new_rating in update_elo(driver_ratings, drivers_id).items():
            res[driver_id]["rating history"][race] = new_rating
    
    return res

if __name__ == "__main__":
    race_results = pathlib.Path(__file__).parent / "race-results"
    driver_ratings = pathlib.Path(__file__).parent / "driver ratings.json"

    with shelve.open(race_results, "c") as db:
        update_race_results(db)
        with open(driver_ratings, "w") as file:
            json.dump(generate_rating_history(db, sorted(db.keys())), file)
        
