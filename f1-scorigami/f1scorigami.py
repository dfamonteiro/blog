import pandas as pd
from pathlib import Path
import duckdb
from typing import List, Dict
from numpy import nan
import seaborn as sns
import matplotlib.pyplot as plt
import plotly.express as px
import plotly.graph_objects as go

SESSION_ENTRIES_WITH_POINTS_AVAILABLE = duckdb.sql("""
    SELECT
        round.date,
        round.name AS grand_prix_name,
        round.number AS race_number,

        driver.forename || ' ' || driver.surname AS driver_name,
        round_entry.car_number,

        team.name AS team_name,

        session_entry.points,

    FROM 'formula_one_sessionentry.csv' AS session_entry
    JOIN 'formula_one_roundentry.csv'   AS round_entry ON session_entry.round_entry_id = round_entry.id
    JOIN 'formula_one_round.csv'        AS round       ON round_entry.round_id         = round.id
    JOIN 'formula_one_teamdriver.csv'   AS team_driver ON round_entry.team_driver_id   = team_driver.id
    JOIN 'formula_one_driver.csv'       AS driver      ON team_driver.driver_id        = driver.id
    JOIN 'formula_one_team.csv'         AS team        ON team_driver.team_id          = team.id

    WHERE NOT isnan(points)

    ORDER BY date
""").df()

OLD_TEAM_NAMES = {
    # Teams with stable branding
    "McLaren" :  [(0, "McLaren")],
    "Ferrari" :  [(0, "Ferrari")],
    "Williams" : [(0, "Williams")],
    "Haas F1 Team" : [(2016, "Haas F1 Team")],

    "Red Bull" : [
        (1997, "Stewart", 1999),
        (2000, "Jaguar", 2004),
        (2005, "Red Bull")
    ],
    "Mercedes" : [
        (1968, "Tyrrell", 1998),
        (1999, "BAR", 2005),
        (2006, "Honda", 2008),
        (2009, "Brawn", 2009),
        (2010, "Mercedes")
    ],
    "Aston Martin" : [
        (1991, "Jordan", 2005),
        (2006, "MF1", 2006),
        (2006, "Spyker MF1", 2006),
        (2007, "Spyker", 2007),
        (2008, "Force India", 2018),
        (2019, "Racing Point", 2020),
        (2021, "Aston Martin")
    ],
    "RB F1 Team" : [
        (1985, "Minardi", 2005),
        (2006, "Toro Rosso", 2019),
        (2020, "AlphaTauri", 2023),
        (2024, "RB F1 Team")
    ],
    "Sauber" : [
        (1993, "Sauber", 2005),
        (2006, "BMW Sauber", 2009),
        (2010, "Sauber", 2018),
        (2019, "Alfa Romeo", 2023),
        (2024, "Sauber"),
    ],
    "Alpine F1 Team" : [
        (1981, "Toleman", 1985),
        (1986, "Benetton", 2001),
        (2002, "Renault", 2011),
        (2012, "Lotus F1", 2015),
        (2016, "Renault", 2020),
        (2021, "Alpine F1 Team")
    ]
}

def teams_in_f1_in(year : int) -> List[str]:
    "Returns the teams that were in F1 in a specific year"
    return duckdb.sql(f"""
        SELECT team.name,
        FROM 'formula_one_teamchampionship.csv' AS team_championship
        JOIN 'formula_one_team.csv' AS team ON team_championship.team_id = team.id
        WHERE team_championship.year = {year}
    """).df()["name"].unique().tolist()

def sanity_check():
    "Make sure the ranges in OLD_TEAM_NAMES are valid"
    for year in range(1979, 2026):
        teams = teams_in_f1_in(year)

        for old_names in OLD_TEAM_NAMES.values():
            for old_name_range in old_names:
                start, old_name, end = old_name_range if len(old_name_range) == 3 else (*old_name_range, 3000)
                
                if start <= year <= end:
                    if old_name not in teams:
                        print(year, old_name_range, teams) # If the code reaches here, we messed up our ranges

def calculate_corrected_name(row):
    "If the team is still in the grid, return the current name. Otherwise return NaN"
    year, team = row["date"].year, row["team_name"]

    if year == 2025 and row["grand_prix_name"] == "Abu Dhabi Grand Prix":
        pass

    for contemporary_team, old_name_ranges in OLD_TEAM_NAMES.items():
        for old_name_range in old_name_ranges:
            start, old_name, end = old_name_range if len(old_name_range) == 3 else (*old_name_range, 3000)

            if team == old_name and start <= year <= end:
                return contemporary_team
    else:
        return nan

def print_contemporary_teams_per_year_since_1979(points_per_contemporary_team_per_round : pd.DataFrame):
    teams_per_year = {}

    for _, row in points_per_contemporary_team_per_round.iterrows():
        year = row["date"].year
        name = row["corrected_team_name"]
        
        if year not in teams_per_year:
            teams_per_year[year] = set()
        teams_per_year[year].add(name)

    for year, teams in teams_per_year.items():
        print(f"{year}: {sorted(teams)}")

def calculate_scorigami(df : pd.DataFrame) -> List[int]:
    "Calculates the number of times a specific (team, point) combo has appeared up to that point, for every row in the table"
    res = []

    score_count : Dict[str, Dict[float, int]] = {}

    for _, row in df.iterrows():
        team, points = row["corrected_team_name"], row["points"]

        if team not in score_count:
            score_count[team] = {}
        
        count = score_count[team].get(points, 1)
        res.append(count)
        score_count[team][points] = count + 1

    return res

def save_as_plotly_html(df, name):
    fig = go.Figure(data=[go.Table(
        header=dict(values=list(df.columns),
                    fill_color='paleturquoise',
                    align='left'),
        cells=dict(values=[df[col] for col in df.columns],
                fill_color='lavender',
                align='left'))
    ])
    # Save as an HTML fragment
    fig.write_html(Path(__file__).parent.parent / "Blog" / "static" / "charts" / name)

def scorigami_timeline(scorigami_df):
    # Using your specific columns
    fig = px.scatter(scorigami_df, x="date", y="corrected_team_name", 
                    color="points",
                    hover_data=["grand_prix_name", "points"],
                    title="F1 Team Timeline (Zoomable)")
    # This adds a 'range slider' at the bottom to navigate the decades
    fig.update_xaxes(rangeslider_visible=True)
    fig.show()

if __name__ == "__main__":
    points_per_team_per_round = SESSION_ENTRIES_WITH_POINTS_AVAILABLE.groupby(["date", "grand_prix_name", "race_number", "team_name"])["points"].sum().reset_index()
    points_per_team_per_round['corrected_team_name'] = points_per_team_per_round.apply(calculate_corrected_name, axis=1)

    score_counts = points_per_team_per_round[points_per_team_per_round["corrected_team_name"].notna()] # Remove all the teams that have not made it to present day
    score_counts["scorigami"] = calculate_scorigami(score_counts)

    scorigami_df = score_counts[score_counts["scorigami"] == 1]
