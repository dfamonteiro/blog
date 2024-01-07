+++ 
draft = false
date = 2024-01-06T02:26:37Z
title = "Applying clustering techniques to the F1 2024 schedule for fun and profit"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["motorsport", "maths"]
categories = []
externalLink = ""
series = []
+++

I was finishing my [Formula 1 Cornering Performance Breakdown](https://github.com/dfamonteiro/Formula-1-Cornering-Performance-Breakdown) project when I came up with the idea of using the existing code infrastructure to generate cornering data for every track on the 2024 calendar. As part of that project, I tagged every corner with a tag highlighting how fast that corner is. With that information, I can determine how much time is spent on each type of corner. The remainder of the time in lap, which isn’t spent negotiating a corner, is the time the drivers spend at full throttle on a lap, tagged below as **STRAIGHT**.

| Corner type       | LOW     | MEDIUM-LOW    | MEDIUM-HIGH   | HIGH    | STRAIGHT      |
|-------------------|---------|---------------|---------------|---------|---------------|
| Corner apex speed | <100kph | 100kph-150kph | 150kph-200kph | >200kph | Full throttle |

## The data

Now that we've gone over the basics, let's take a moment to understand how to interpret the data that will be shown in this blog post. Take a look at the example below:
```text
                   STRAIGHT     LOW  MEDIUM-LOW  MEDIUM-HIGH    HIGH
Azerbaijan GP        51.201  24.801      24.201        0.000   0.000
Qatar GP             29.035   6.520      10.301       16.001  21.921
```

We can see that the Azerbaijan GP is dominated by low and medium-low-speed corners, whereas medium-high and high-speed corners dominate the Qatar GP. This makes complete sense and matches our preconceptions of these two tracks: Azerbaijan is a street track, and Qatar is a flowing MotoGP track.

Without further ado, here’s the data for all 24 GPs of the 2024 F1 calendar:

```text
                   STRAIGHT     LOW  MEDIUM-LOW  MEDIUM-HIGH    HIGH
Bahrain GP           37.177  23.139      16.394        4.899   8.099
Saudi Arabian GP     44.187   7.720       6.880       19.399  10.079
Australian GP        31.573   4.516      14.603        6.166  19.874
Azerbaijan GP        51.201  24.801      24.201        0.000   0.000
Miami GP             39.961  35.099       6.654        0.000   5.100
Monaco GP            24.805  29.750      10.180        6.630   0.000
Spanish GP           30.637   0.000      17.951       14.060   9.624
Austrian GP          34.312   6.522       9.658        2.220  11.679
British GP           39.236   4.900      18.582        4.320  19.682
Hungarian GP         27.946   5.800      27.523       10.561   4.779
Italian GP           44.692   9.579       6.516       13.027   6.480
Singapore GP         33.632  22.850      27.577        4.119   2.806
Japanese GP          42.847  16.477       5.660       15.734   8.159
Qatar GP             29.035   6.520      10.301       16.001  21.921
United States GP     35.539  32.403      10.903        2.963  12.915
Mexico City GP       27.789  25.319      13.496       10.562   0.000
São Paulo GP         33.454   6.120      19.887        7.080   3.480
Las Vegas GP         49.586  26.140      13.200        0.000   3.800
Abu Dhabi GP         37.663   8.720      17.381       13.180   6.501
Chinese GP           35.122  33.319       8.646        6.646   7.814
Dutch GP             26.664   7.822      22.118        5.655   8.083
Belgian GP           46.945  17.060       7.678       21.639  10.343
Canadian GP          34.337  14.883      21.020        0.000   0.000
Emilia Romagna GP    38.293   0.000      21.866       11.245   3.007
```

This is a good starting point, but before we start running clustering algorithms on this data, we should normalize the data in relation to the lap time of each track. Doing this prevents tracks like Spa from having more time spent in corners simply because it has a higher lap time. The Python code below does just that by normalizing the data to a 100-second (1m40s) lap time:

```python
def normalize(dt : pd.DataFrame) -> pd.DataFrame:
    res = dt.copy(True)
    res["Total"] = res["STRAIGHT"] +\
                   res["LOW"] +\
                   res["MEDIUM-LOW"] +\
                   res["MEDIUM-HIGH"] +\
                   res["HIGH"]
    for c in ("STRAIGHT", "LOW", "MEDIUM-LOW", "MEDIUM-HIGH", "HIGH"):
        res[c] = res[c] * 100 / res["Total"]
    res = res.drop(["Total"], axis=1)
    return res
```

The normalized data:

```txt
                    STRAIGHT        LOW  MEDIUM-LOW  MEDIUM-HIGH       HIGH
Bahrain GP         41.442235  25.793686   18.274847     5.461051   9.028180
Saudi Arabian GP   50.061746   8.746389    7.794709    21.978134  11.419022
Australian GP      41.147109   5.885419   19.031173     8.035761  25.900537
Azerbaijan GP      51.097273  24.750756   24.151971     0.000000   0.000000
Miami GP           46.030594  40.430115    7.664662     0.000000   5.874629
Monaco GP          34.757935  41.687102   14.264696     9.290268   0.000000
Spanish GP         42.391244   0.000000   24.838112    19.454284  13.316360
Austrian GP        53.286950  10.128745   14.998991     3.447687  18.137628
British GP         45.244465   5.650369   21.427583     4.981550  22.696033
Hungarian GP       36.478743   7.570912   35.926588    13.785587   6.238170
Italian GP         55.660448  11.929908    8.115177    16.224126   8.070341
Singapore GP       36.964741  25.114306   30.309725     4.527170   3.084059
Japanese GP        48.209323  18.539105    6.368352    17.703118   9.180103
Qatar GP           34.657070   7.782473   12.295591    19.099286  26.165580
United States GP   37.518871  34.208165   11.510404     3.128068  13.634492
Mexico City GP     36.011974  32.811083   17.489568    13.687375   0.000000
São Paulo GP       47.777095   8.740235   28.401480    10.111252   4.969938
Las Vegas GP       53.475832  28.190583   14.235490     0.000000   4.098095
Abu Dhabi GP       45.135119  10.449997   20.829289    15.794835   7.790760
Chinese GP         38.364993  36.395513    9.444329     7.259659   8.535506
Dutch GP           37.906230  11.119957   31.443519     8.039294  11.491001
Belgian GP         45.285294  16.456856    7.406550    20.873969   9.977331
Canadian GP        48.885251  21.188781   29.925968     0.000000   0.000000
Emilia Romagna GP  51.461477   0.000000   29.385440    15.112013   4.041069
```

## Time to run the k-means clustering method

Now that we have our data, we can run k-means clustering on it. I decided to go for 4 clusters, even though the [elbow method](https://en.wikipedia.org/wiki/Elbow_method_(clustering)) only suggested 2.

```python
def kmeans_clustering(dt : pd.DataFrame, n : int = 2) -> Dict[int, Tuple[pd.DataFrame, List[str]]]:
    kmeans = KMeans(n_clusters=n, n_init='auto')
    kmeans.fit(dt)

    dt1 = dt.copy(True)

    dt1["Cluster"] = kmeans.labels_
    
    res = {}
    for i in range(n):
        gp_cluster = dt1[dt1["Cluster"] == i]
        print(gp_cluster)
        print(list(gp_cluster.index))
        print()
        res[i] = (gp_cluster, list(gp_cluster.index))
    
    return res
```

### Cluster 1: The street tracks with high top speeds

Every track on this cluster is dominated by low and medium-low-speed corners. The length of the straights on these circuits (except Singapore) makes top speed a priority, which conflicts with the requirements of the low-speed corners.

```txt
                STRAIGHT        LOW  MEDIUM-LOW  MEDIUM-HIGH      HIGH  Cluster
Azerbaijan GP  51.097273  24.750756   24.151971      0.00000  0.000000        0
Singapore GP   36.964741  25.114306   30.309725      4.52717  3.084059        0
Las Vegas GP   53.475832  28.190583   14.235490      0.00000  4.098095        0
Canadian GP    48.885251  21.188781   29.925968      0.00000  0.000000        0

['Azerbaijan GP', 'Singapore GP', 'Las Vegas GP', 'Canadian GP']
```

### Cluster 2: The flowing high-speed racetracks

Every track on this cluster (except Monza) could be a MotoGP track, looking purely at the track layouts. This set of tracks rewards teams that can put a ton of downforce while maintaining a respectable top speed.

```txt
                   STRAIGHT        LOW  MEDIUM-LOW  MEDIUM-HIGH       HIGH  Cluster
Saudi Arabian GP  50.061746   8.746389    7.794709    21.978134  11.419022        1
Australian GP     41.147109   5.885419   19.031173     8.035761  25.900537        1
Austrian GP       53.286950  10.128745   14.998991     3.447687  18.137628        1
British GP        45.244465   5.650369   21.427583     4.981550  22.696033        1
Italian GP        55.660448  11.929908    8.115177    16.224126   8.070341        1
Japanese GP       48.209323  18.539105    6.368352    17.703118   9.180103        1
Qatar GP          34.657070   7.782473   12.295591    19.099286  26.165580        1
Belgian GP        45.285294  16.456856    7.406550    20.873969   9.977331        1

['Saudi Arabian GP', 'Australian GP', 'Austrian GP', 'British GP',
 'Italian GP', 'Japanese GP', 'Qatar GP', 'Belgian GP']
```

### The traction-sensitive racetracks

You will struggle at these tracks if your car has poor handling characteristics. This cluster is dominated by low-speed corners, highlighting the suspension quality and ride height.

```txt
                   STRAIGHT        LOW  MEDIUM-LOW  MEDIUM-HIGH       HIGH  Cluster
Bahrain GP        41.442235  25.793686   18.274847     5.461051   9.028180        2
Miami GP          46.030594  40.430115    7.664662     0.000000   5.874629        2
Monaco GP         34.757935  41.687102   14.264696     9.290268   0.000000        2
United States GP  37.518871  34.208165   11.510404     3.128068  13.634492        2
Mexico City GP    36.011974  32.811083   17.489568    13.687375   0.000000        2
Chinese GP        38.364993  36.395513    9.444329     7.259659   8.535506        2

['Bahrain GP', 'Miami GP', 'Monaco GP',
 'United States GP', 'Mexico City GP', 'Chinese GP']
```

### The all-rounders

I have nothing interesting to say about these tracks besides that you need a good car at both medium-low and medium-high-speed corners.

```txt
                    STRAIGHT        LOW  MEDIUM-LOW  MEDIUM-HIGH       HIGH  Cluster
Spanish GP         42.391244   0.000000   24.838112    19.454284  13.316360        3
Hungarian GP       36.478743   7.570912   35.926588    13.785587   6.238170        3
São Paulo GP       47.777095   8.740235   28.401480    10.111252   4.969938        3
Abu Dhabi GP       45.135119  10.449997   20.829289    15.794835   7.790760        3
Dutch GP           37.906230  11.119957   31.443519     8.039294  11.491001        3
Emilia Romagna GP  51.461477   0.000000   29.385440    15.112013   4.041069        3

['Spanish GP', 'Hungarian GP', 'São Paulo GP', 
 'Abu Dhabi GP', 'Dutch GP', 'Emilia Romagna GP']
```

## Final remarks

There are multiple ways to improve the clustering of the racetracks: the two I can think of from the top of my head are adding the average speed and the longest continuous throttle-on time (i.e., longest straight) as two new columns. Calculating the [isochronal ratio](https://canopysimulations.com/2017/01/18/aerodynamic-upgrades-facilitated-isochronal-ratio "Blog post about the isochronal ratio") would also be interesting, but generating that data would be far too time-consuming for me.

If you also have ideas to iterate on this project, you are more than welcome to do it! The code is available on [GitHub](https://github.com/dfamonteiro/Formula-1-Cornering-Performance-Breakdown/blob/main/track_clustering.py "Github project link").
