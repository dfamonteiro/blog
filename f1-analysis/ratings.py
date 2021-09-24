import shelve
from typing import Dict, List

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
    
    return {driver : update_driver_elo(old_rating, race_result, driver) for driver in race_result}

if __name__ == "__main__":
    with shelve.open("race-results", "c") as db:
        update_race_results(db)
