import json
import pandas as pd
import seaborn as sb
from matplotlib import pyplot

TEAM_COLORS = ["orange", "blue", "red", "cyan", "pink", "green", "blue", "grey", "brown", "black"]

with open("f1-penalties/f1-standings-and-wins.json", "r") as f:
    json_data = json.load(f)

races = []
for semester in sorted(json_data.keys()):
    print(semester)
    races.extend(json_data[semester]['atr_races'])

res = {}
for period, data in json_data.items():
    year, semester = map(int, period.split("-"))
    x = year + (semester - 1)/2
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
res = pd.DataFrame(res).T

sb.lineplot(res, palette=TEAM_COLORS, dashes=False, linewidth=4)
pyplot.title("ATR restrictions")
pyplot.ylim(40, 120)
pyplot.show()