import pandas
import seaborn
from numpy import nan
from matplotlib import pyplot
from matplotlib.ticker import MaxNLocator
from typing import Literal

QUALIFYING = {
    "FT": ["1:07.125",   "57.652",   "56.817",   "57.383", "1:00.082"],
    "MF": ["1:22.641", "1:05.738",   "58.961",   "57.832", "1:03.613"],
    "DM": ["1:17.055", "1:01.305", "1:14.781",   "59.715", "1:00.090"],
    "EO": ["1:13.242", "1:00.239", "1:00.554",   "59.805", "1:02.660"],
    "RM": ["1:26.934", "1:07.441", "1:02.844", "1:01.027"],
    "DL": ["1:05.973", "1:02.847", "1:02.590", "1:02.340", "1:01.649"],
    "FR": ["1:23.523", "1:13.407", "1:09.519", "1:04.473"],
    "RV": ["1:28.496", "1:10.340", "1:05.187", "1:07.911"],
    "MC": ["1:31.586", "1:14.031", "1:07.582", "1:05.520"],
    "JS": ["1:23.668", "1:18.141", "1:06.277", "1:08.086"],
    "TG": ["1:30.301", "1:17.484", "1:09.547", "1:18.750"],
    "MT": ["1:36.375", "1:21.176", "1:15.015", "1:13.414"],
}

RACE = {
    "FT": ["1:02.441",   "56.949",   "57.274",   "55.973",   "56.578",   "55.750",   "57.367",   "57.996",   "56.340",   "56.539",   "56.519",   "56.368",   "58.273",   "56.262",   "56.242",   "56.738"],
    "MF": ["1:04.832",   "56.730",   "56.809",   "58.492",   "56.922",   "57.012",   "56.730",   "57.649", "1:00.254",   "56.988",   "56.949",   "57.656",   "56.926",   "57.192",   "56.898",   "57.875"],
    "RM": ["1:05.293", "1:00.328",   "59.629", "1:00.051",   "58.464",   "58.551",   "58.313",   "58.441",   "59.285",   "58.844",   "57.625",   "57.641",   "58.043",   "57.961",   "57.957",   "56.828"],
    "DL": ["1:06.437", "1:01.700",   "59.628",   "58.184",   "58.012", "1:04.875",   "57.765",   "59.340",   "59.375",   "58.383",   "58.906",   "58.067",   "57.851",   "58.360",   "57.570",   "59.984"],
    "RV": ["1:05.672", "1:00.828", "1:06.399", "1:01.742",   "59.511",   "58.883",   "58.270",   "58.465",   "58.515",   "58.180",   "58.953",   "58.051",   "58.355",   "57.778",   "58.328",   "58.570"],  
    "EO": ["1:01.371",   "59.590",   "56.785",   "58.789",   "58.781",   "59.278",   "58.113", "1:00.621",   "59.742", "1:01.391",   "58.832", "1:01.023", "1:01.957", "1:09.274", "1:02.312", "1:03.426"],
    "DM": ["1:05.929", "1:01.957", "1:05.582", "1:02.762", "1:05.270", "1:03.050",   "59.492",   "59.555",   "58.590", "1:00.895", "1:00.113",   "57.988",   "58.785",   "58.055",   "58.723"],
    "JS": ["1:07.297", "1:02.656", "1:01.730", "1:00.868", "1:01.402",   "59.836", "1:00.004", "1:02.472", "1:00.883", "1:01.348", "1:02.402", "1:00.282",   "59.570", "1:00.664", "1:00.769"],
    "MC": ["1:17.024", "1:01.070", "1:10.985", "1:01.961", "1:02.449", "1:02.293", "1:01.140",   "57.891",   "58.156",   "58.270",   "58.160",   "58.238",   "57.113", "1:01.989", "1:09.355"],
    "TG": ["1:10.625", "1:05.977", "1:07.887", "1:05.199", "1:05.277", "1:03.434", "1:02.250", "1:01.648", "1:00.156", "1:01.164", "1:01.090", "1:02.098", "1:02.242", "1:00.801", "1:01.531"],
    "FR" :["1:05.433", "1:11.707", "1:05.246", "1:05.453", "1:04.219", "1:04.195", "1:03.840", "1:05.200", "1:01.394", "1:02.121", "1:03.754", "1:03.531", "1:03.438", "1:01.676", "1:01.734"],
    "MT": ["1:11.210", "1:07.872", "1:05.664", "1:07.297", "1:03.574", "1:05.109", "1:10.774", "1:18.726", "1:14.524", "1:11.015"],
}

def parse_lap_time(lap_time : str) -> float:
    "Converts a lap time string (eg: 1:05.664) to seconds (eg: 65.664)"
    if ":" in lap_time:
        minutes, seconds = lap_time.split(":")
    else:
        minutes, seconds = 0, lap_time
    
    return int(minutes) * 60 + float(seconds)

def get_data() -> pandas.DataFrame:
    "Converts the raw data from the `QUALIFYING` and `RACE` dictionaries to a pandas dataframe"
    res = []

    for session_name, session in zip(("QUALIFYING", "RACE"), (QUALIFYING, RACE)):
        for driver, laps in session.items():
            for lap_number, lap_time in enumerate(laps, start=1):
                res.append({
                    "Session": session_name,
                    "Driver": driver,
                    "Lap Number": lap_number,
                    "Lap Time": parse_lap_time(lap_time),
                })

    return pandas.DataFrame(res)


def qualifying_standings(df: pandas.DataFrame):
    res = (
        df[df["Session"] == "QUALIFYING"]
        .groupby(["Driver"])["Lap Time"]
        .min()
        .sort_values()
    )

    print(res)

def lap_time_progression(df: pandas.DataFrame, session: Literal["QUALIFYING", "RACE"]):
    res = df[df["Session"] == session]

    seaborn.lineplot(res, x="Lap Number", y="Lap Time", hue="Driver", style="Driver", linewidth=3)
    
    # Force integer x ticks
    pyplot.gca().xaxis.set_major_locator(MaxNLocator(integer=True))

    # Draw horizontal grid lines
    pyplot.grid(axis='y', alpha=0.7)

    pyplot.show()

def lap_time_distribution(df: pandas.DataFrame, session: Literal["QUALIFYING", "RACE"], ignore_first_lap: bool = True):
    res = df[df["Session"] == session]

    if ignore_first_lap:
        res = res[res["Lap Number"] > 1]

    seaborn.catplot(res, kind="swarm", x="Driver", y="Lap Time", hue="Lap Number", s=100)
    
    # Draw horizontal grid lines
    pyplot.grid(axis='y', alpha=0.7)
    
    pyplot.show()

def calculate_cumulative_lap_times(df: pandas.DataFrame) -> pandas.DataFrame:
    res = df[df["Session"] == "RACE"].drop(columns=["Session"])

    cumulative_times = []

    # Calculate the cumulative sum of lap times _per driver_
    for driver in res["Driver"].unique():
        cumulative_times.append(
            res[res["Driver"] == driver]["Lap Time"].cumsum()
        )

    res["Cumulative Lap Time"] = pandas.concat(cumulative_times)

    return res

def calculate_race_trace_offsets(df: pandas.DataFrame) -> pandas.DataFrame:
    df["Race Trace Offset"] = df["Cumulative Lap Time"] - df["Lap Number"] * 60

    return df

def race_trace(df: pandas.DataFrame):
    seaborn.lineplot(df, x="Lap Number", y="Race Trace Offset", hue="Driver", style="Driver", linewidth=3)
    
    # Force integer x ticks
    pyplot.gca().xaxis.set_major_locator(MaxNLocator(integer=True))

    # Draw horizontal grid lines
    pyplot.grid(axis='y', alpha=0.7)

    pyplot.show()

if __name__ == "__main__":
    df = get_data()
    # print(get_data())

    # Qualifying
    # qualifying_standings(df)
    # lap_time_progression(df, "QUALIFYING")
    # lap_time_distribution(df, "QUALIFYING")

    # Race
    df = calculate_cumulative_lap_times(df)
    df = calculate_race_trace_offsets(df)
    print(df)
    # lap_time_progression(df, "RACE")
    # lap_time_distribution(df, "RACE", False)
    race_trace(df)

