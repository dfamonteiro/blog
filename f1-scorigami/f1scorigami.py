# You'll need a data dump from https://github.com/jolpica/jolpica-f1 for this script to work

import pandas as pd
from pathlib import Path
import duckdb
from typing import List, Dict
from numpy import nan
import plotly.express as px
import plotly.graph_objects as go

CHARTS_PATH = Path(__file__).parent.parent / "Blog" / "static" / "charts"

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
        name = row["current_team_name"]
        
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
        team, points = row["current_team_name"], row["points"]

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
    fig = px.scatter(
        scorigami_df,
        x="date",
        y="current_team_name", 
        color="points",
        hover_data=["grand_prix_name", "team_name", "points"],
        title="Scorigami Timeline",
        template="plotly_dark",
        color_continuous_scale=["#00ffff", "#ff00ff"]
    )

    # 2. Customize Dark Mode UI
    fig.update_layout(
        margin=dict(l=10, r=10, t=50, b=10),
        paper_bgcolor="#212121",  # Background outside the chart
        plot_bgcolor="#212121",   # Background inside the chart
        font_color="#e0e0e0",     # Light text color
        title_font_size=20,
        xaxis=dict(
            gridcolor="#333333",  # Subtle dark grid lines
            rangeslider=dict(
                visible=True,
                bgcolor="#1e1e1e" # Darker background for the slider
            )
        ),
        yaxis=dict(gridcolor="#333333")
    )

    # 3. Save as HTML Snippet
    CHARTS_PATH.mkdir(parents=True, exist_ok=True)
    
    output_file = CHARTS_PATH / f"scorigami-timeline.html"
    
    fig.write_html(
        output_file, 
        full_html=False, 
        include_plotlyjs='cdn'
    )

def save_dataframe(df : pd.DataFrame, name, only_embed_html = False):
    "Saves the dataframe in 3 different forms: html to be embedded, html to be viewed, and CSV"

    # To be embedded
    with open(CHARTS_PATH / f"{name}-embed.html", "w", encoding="utf-8") as f:
        f.write(df.to_html(classes="dataframe", border=0, index = False))

    if only_embed_html:
        return

    # HTML direct link
    with open(CHARTS_PATH / f"{name}.html", "w", encoding="utf-8") as f:
        html_content = f"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="UTF-8">
                <title>{name}</title>
                <style>
                    body {{ font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif; padding: 20px; background-color: #f8f9fa; }}
                    .dataframe {{ border-collapse: collapse; width: 100%; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
                    .dataframe th {{ background-color: #007bff; color: white; padding: 12px 15px; text-align: left; font-weight: 600; }}
                    .dataframe td {{ padding: 10px 15px; border-bottom: 1px solid #eee; color: #333; }}
                    .dataframe tr:hover {{ background-color: #f1f1f1; }}
                    .dataframe tr:last-child td {{ border-bottom: none; }}
                </style>
            </head>
            <body>
                <h2>{name.replace('_', ' ').title()}</h2>
                {df.to_html(classes="dataframe", border=0, index=False)}
            </body>
            </html>
        """
        f.write(html_content)

    # CSV direct link
    with open(CHARTS_PATH / f"{name}.csv", "w", encoding="utf-8") as f:
        f.write(df.to_csv(index = False))


def scorigamis_per_year(scorigami_df : pd.DataFrame):
    scorigami_df = scorigami_df.copy()

    scorigami_df['year'] = scorigami_df['date'].dt.year # type: ignore
    df = scorigami_df.groupby("year").count().reset_index()[["year", "scorigami"]]

    # 2. Create a full range of years
    all_years = pd.DataFrame({'year': range(df['year'].min(), df['year'].max() + 1)})
    # 3. Merge the actual data with the full range
    df = pd.merge(all_years, df, on='year', how='left')
    # 4. Replace the resulting NaNs with 0
    df['scorigami'] = df['scorigami'].fillna(0).astype(int)

    df = df.rename(columns={'scorigami': 'scorigami count'})
    # 1. Create the Line Chart
    # Use render_mode='svg' for sharper lines or leave default
    fig = px.line(
        df, 
        x="year", 
        y="scorigami count",
        title="Total Scorigamis per Year",
        template="plotly_dark",
        markers=True,         # Adds points to the line
        line_shape="spline",  # This creates the "flow" (curved lines instead of jagged)
        render_mode="svg"
    )

    # 2. Styling and Padding Reduction
    fig.update_layout(
        paper_bgcolor="#212121",
        plot_bgcolor="#212121",
        font_color="#e0e0e0",
        # Tighten margins: l=left, r=right, t=top, b=bottom
        margin=dict(l=10, r=10, t=50, b=10),
        
        xaxis=dict(
            title="Year",
            gridcolor="#333333",
            showline=True,
            linecolor="#444444",
            dtick=5  # Shows a label every 5 years to keep it clean
        ),
        yaxis=dict(
            title="Count",
            gridcolor="#333333",
            showline=True,
            linecolor="#444444"
        )
    )

    # 3. Customize the line color and width
    fig.update_traces(
        line=dict(width=3, color="#00ffff"), # Cyan "glow" line
        marker=dict(size=6, color="#ff00ff")  # Magenta markers
    )

    # 4. Save (using your existing logic)
    fig.write_html(CHARTS_PATH / "scorigami-linechart.html", full_html=False, include_plotlyjs='cdn')

def scorigami_heatmap(score_counts : pd.DataFrame):
    df = score_counts.groupby(["points", "current_team_name"])["scorigami"].max().reset_index()
    df = df.rename(columns={'scorigami': 'score count'})

    df['points'] = df['points'].astype(str)

    # 1. Swap x and y
    fig = px.density_heatmap(
        df, 
        x="current_team_name",  # Now on X
        y="points",             # Now on Y
        z="score count", 
        title="F1 Scores Heatmap",
        labels={
            'current_team_name': 'Team', 
            'points': 'Points Scored', 
            'score count': 'Frequency'
        },
        template="plotly_dark",
        color_continuous_scale=[
            [0, "#000000"],       # Zero values = Pitch Black
            [0.0001, "#212121"],  # Just above zero = Your dark background color
            [0.5, "#00ffff"],     # Middle values = Cyan
            [1, "#ff00ff"]        # Max values = Magenta
        ],
        text_auto=True
    )

    dynamic_height = 2000

    # 2. Refine Layout and Reduce Padding
    fig.update_layout(
        height=dynamic_height,
        paper_bgcolor="#212121",
        plot_bgcolor="#212121",
        font_color="#e0e0e0",
        margin=dict(l=10, r=10, t=50, b=10),
        
        xaxis=dict(
            title="",
            gridcolor="#333333",
            tickangle=-45  # Slant team names to prevent overlapping
        ),
        yaxis=dict(
            title="Points", 
            gridcolor="#333333",
            autorange="reversed", # Keeps 0 at the top (Scorigami style)
            dtick=1               # Shows a tick every point
        )
    )

    # 4. Save (using your existing logic)
    fig.write_html(CHARTS_PATH / "scorigami-heatmap.html", full_html=False, include_plotlyjs='cdn')


def global_scorigami(scorigami_df : pd.DataFrame):
    # This keeps the first occurrence of every unique 'points' value
    # and removes all subsequent ones.
    df = scorigami_df.drop_duplicates(subset=["points"], keep="first").copy()
    save_dataframe(df, "global-scorigami")

    # print(df.groupby("current_team_name").count())

def fractional_scorigami(scorigami_df : pd.DataFrame):
    df = scorigami_df[scorigami_df["points"] % 1 != 0]
    save_dataframe(df, "fractional-scorigami")

def highscorigami(scorigami_df : pd.DataFrame):
    # Find the index label of the maximum point for each team
    idx = scorigami_df.groupby("current_team_name")["points"].idxmax()
    
    # Use .loc to grab those specific rows
    df = scorigami_df.loc[idx].copy().sort_values("points")
    save_dataframe(df, "highscorigami", True)

if __name__ == "__main__":
    points_per_team_per_round = SESSION_ENTRIES_WITH_POINTS_AVAILABLE.groupby(["date", "grand_prix_name", "team_name"])["points"].sum().reset_index()
    points_per_team_per_round['current_team_name'] = points_per_team_per_round.apply(calculate_corrected_name, axis=1)

    score_counts = points_per_team_per_round[points_per_team_per_round["current_team_name"].notna()] # Remove all the teams that have not made it to present day
    score_counts["points"] = score_counts["points"].replace(9.99, 10.0) # Here to fix a rounding error
    score_counts["scorigami"] = calculate_scorigami(score_counts)

    scorigami_df = score_counts[score_counts["scorigami"] == 1]
    save_dataframe(score_counts, "score-counts")
    save_dataframe(scorigami_df, "scorigami")

    scorigami_timeline(scorigami_df)

    scorigamis_per_year(scorigami_df)

    scorigami_heatmap(score_counts)

    global_scorigami(scorigami_df)

    fractional_scorigami(scorigami_df)

    highscorigami(scorigami_df)