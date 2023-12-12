const ctx = document.getElementById('myChart');

const json_url = '/json/f1-standings-and-wins.json';

function convert_to_datapoints(json_data, n_races) {
    n_races -= 1
    let races = new Array();

    let sorted_semesters = Object.keys(json_data);
    sorted_semesters.sort();
    
    sorted_semesters.forEach(function(semester) {
        races = races.concat(json_data[semester]['atr_races']);
    });
    
    let res = new Map();

    for (const [period, data] of Object.entries(json_data)) {
        let year     = parseInt(period.split("-")[0]);
        let semester = parseInt(period.split("-")[1]);

        let x = year + (semester - 1)/2;
        res.set(x,       new Map());
        res.set(x + 0.1, new Map());
        res.set(x + 0.2, new Map());
        res.set(x + 0.3, new Map());
        res.set(x + 0.4, new Map());

        for (const [team, place] of Object.entries(data['standings'])) {
            let allocation = 65 + 5 * place;
            let start  = Math.max(data['latest_index'] - n_races, 0);
            let finish = Math.min(data['latest_index'] + 1, races.length);
            races.slice(start, finish).forEach(function(race) {
                if (race['winner'] === team) {
                    allocation -= 1;
                }
            });
            res.get(x      ).set(team, allocation);
            res.get(x + 0.1).set(team, allocation);
            res.get(x + 0.2).set(team, allocation);
            res.get(x + 0.3).set(team, allocation);
            res.get(x + 0.4).set(team, allocation);
        }
      }
    console.log(res)
    return res;
}

function generate_datasets(datapoints) {
    let datasets = [];
    const team_data = [
        ['red_bull',     'Red Bull',     '#14398f'],
        ['mercedes',     'Mercedes',     '#00A19C'],
        ['ferrari',      'Ferrari',      '#A6051A'],
        ['mclaren',      'McLaren',      '#FF8000'],
        ['aston_martin', 'Aston Martin', '#39FF14'],
        ['alpine',       'Alpine',       '#FD4BC7'],
        ['williams',     'Williams',     '#00A3E0'],
        ['alphatauri',   'Alphatauri',   '#00293F'],
        ['sauber',       'Sauber',       '#DE3126'],
        ['haas',         'Haas',         '#AFAFAF'],
    ];

    team_data.forEach(function(team_data) {
        let team_name  = team_data[0];
        let team_label = team_data[1];
        let points = [];
        for (let time_point of datapoints.keys()) {
            if (datapoints.get(time_point).has(team_name)) {
                points.push({x : time_point, y : datapoints.get(time_point).get(team_name)});
            }
        };
        datasets.push({
            label: team_label,
            data: points,
            borderColor: team_data[2],
            backgroundColor: team_data[2],
            pointRadius: 0,
            borderWidth: 5,
        });
    });
    return datasets;
}

function update_graphs(out) {
    let datasets = generate_datasets(convert_to_datapoints(out, 0));
    first_chart.data.datasets = datasets;
    first_chart.update();


    let datasets15 = generate_datasets(convert_to_datapoints(out, 15));
    second_chart.data.datasets = datasets15;
    second_chart.update();
}

fetch(json_url)
.then(res => res.json())
.then(out => update_graphs(out))
.catch(err => { throw err });

let first_chart = new Chart(ctx, {
  type: 'line',
  data: {
    datasets: []
  },
  options: {
    scales: {
      y: {
        ticks: {font: {size: 20}}
      },
      x: {
        ticks: {font: {size: 16}},
        type: 'linear',
        max : 2024.5
      }
    }
  }
});

let second_chart = new Chart(document.getElementById('myChart2'), {
    type: 'line',
    data: {
      datasets: []
    },
    options: {
      scales: {
        y: {
          ticks: {font: {size: 20}}
        },
        x: {
          ticks: {font: {size: 16}},
          type: 'linear',
          max : 2024.5
        }
      }
    }
  });