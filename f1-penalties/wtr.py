import requests
import json
from datetime import datetime, date
import pandas as pd
import seaborn as sb
from matplotlib import pyplot
import json

CONSTRUCTORS_LOOKUP = {
    'lotus_f1'     : 'alpine',
    'renault'      : 'alpine',
    'force_india'  : 'aston_martin',
    'racing_point' : 'aston_martin',
    'toro_rosso'   : 'alphatauri',
    'alfa'         : 'sauber',
}

TEAM_COLORS = ["orange", "blue", "red", "cyan", "pink", "green", "blue", "grey", "brown", "black"]

races = []

for year in range(2010, 2024):
    r = requests.get(f"http://ergast.com/api/f1/{year}.json")
    for race in json.loads(r.text)['MRData']['RaceTable']['Races']:
        races.append({
            "name" : race["raceName"],
            "date" : datetime.strptime(race["date"], r'%Y-%m-%d').date(),
            "round" : int(race["round"]),
            "season" : int(race["season"]),
        })

r = requests.get("http://ergast.com/api/f1/results/1.json?offset=700&limit=10000")
for winner_data in json.loads(r.text)["MRData"]['RaceTable']['Races']:
    winner = winner_data["Results"][0]["Constructor"]["constructorId"]
    if winner in CONSTRUCTORS_LOOKUP:
        winner = CONSTRUCTORS_LOOKUP[winner]
    round_ = int(winner_data["round"])
    season = int(winner_data["season"])
    for race in races:
        if race["round"] == round_ and race["season"] == season:
            race["winner"] = winner
            break

# winners = set()
# for race in races:
#     w = race["winner"]
#     winners.add(w)
# print(winners)
# Winners: {'mercedes', 'mclaren', 'lotus_f1', 'racing_point', 'williams', 'alphatauri', 'alpine', 'red_bull', 'ferrari'}

def get_standings(season, round):
    r = requests.get(f"http://ergast.com/api/f1/{season}/{round}/constructorStandings.json")
    standings = json.loads(r.text)["MRData"]['StandingsTable']['StandingsLists'][0]['ConstructorStandings']

    res = {}
    for position in standings:
        constructor = position['Constructor']['constructorId']
        if constructor in ('marussia', 'manor', 'hrt', 'virgin', 'caterham', 'lotus_racing'):
            continue
        if constructor in CONSTRUCTORS_LOOKUP:
            constructor = CONSTRUCTORS_LOOKUP[constructor]
        res[constructor] = int(position['position'])
    return res

atrs = {}
for season in range(2010, 2025):
    for semester in (1, 2):
        if (season, semester) in [(2010, 1), (2024, 2)]:
            continue

        if semester == 1:
            start  = date(season - 1, 7,  1)
            finish = date(season - 1, 12, 31)
        else:
            start  = date(season, 1, 1)
            finish = date(season, 6, 30)

        atr_races = []
        latest_index = None if (season, semester) != (2020, 2) else 197
        for i, r in enumerate(races):
            if start <= r["date"] <= finish:
                atr_races.append(r)
                latest_index = i
        standings = get_standings(races[latest_index]['season'], races[latest_index]['round'])

        atrs[(season, semester)] = {
            "atr_races"    : atr_races,
            "latest_index" : latest_index,
            "standings"    : standings,
        }


def gen_atr_allocation(atrs) -> dict:
    res = {}

    for period, data in atrs.items():
        print(period)
        x = period[0] + (period[1] - 1)/2
        res[x] = {}
        res[x + 0.4] = {}
        for team, place in data['standings'].items():
            allocation = 65 + 5 * place
            start  = max(data['latest_index'] - 15, 0)
            finish = min(data['latest_index'] + 1, len(races))
            for race in races[start: finish]:
                if race["winner"] == team:
                    allocation -= 1
            res[x][team]       = allocation
            res[x + 0.4][team] = allocation
    return pd.DataFrame(res).T

df = gen_atr_allocation(atrs)
print(df)
sb.lineplot(df, palette=TEAM_COLORS, dashes=False, linewidth=4)
pyplot.title("mode")
pyplot.ylim(40, 120)
pyplot.show()

with open("f1-penalties/f1-standings-and-wins.json", "w") as f:
    new_atrs = {}

    for key in atrs:
        print(key)
        year, semester = key
        value = atrs[key]
        for race in value["atr_races"]:
            race["date"] = (race["date"].year, race["date"].month, race["date"].day)
        new_atrs[f"{year}-{semester}"] = value
        print(key, value)
        # print(value)
        # exit()
    print(list(new_atrs.keys()))
    json.dump(new_atrs, f)
# print(atrs)