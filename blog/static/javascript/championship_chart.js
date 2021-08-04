// The formula in the blog post
function formula(D0, Sm, Sl, N) {
    return (-D0 - Sm*N) / (Sl - Sm)
}

function computeData(D0, Sl, N, roundUp) {
    res = [];
    for (let Sm = -25; Sm <= 0; Sm++) {
        if (roundUp) {
            res.push(Math.ceil(formula(D0, Sm, Sl, N)));
        } else {
            res.push(formula(D0, Sm, Sl, N));
        }
    }

    return res;
}

function updateGraph(event) {
    let D0 = document.getElementById("D0").valueAsNumber;
    let Sl = document.getElementById("Sl").valueAsNumber;
    let N =  document.getElementById("N").valueAsNumber;
    console.log(D0, Sl, N);
    myChart.data.datasets[0].data = computeData(D0, Sl, N, false);
    myChart.data.datasets[1].data = computeData(D0, Sl, N, true);
    myChart.update();
}

document.getElementById("D0").addEventListener("input", updateGraph);
document.getElementById("Sl").addEventListener("input", updateGraph);
document.getElementById("N").addEventListener("input", updateGraph);

//Chart code is below 
const labels = [];

for (let i = -25; i <= 0; i++) {
    labels.push(i);
}

const data = {
  labels: labels,
  datasets: [{
    label: 'Number of races that Lewis has to win',
    backgroundColor: 'rgb(255, 99, 132)',
    borderColor: 'rgb(255, 99, 132)',
    data: computeData(0, 7, 17, false),
  },{
    label: 'Number of races that Lewis has to win (rounded up)',
    backgroundColor: '#1167b1',
    borderColor: '#1167b1',
    data: computeData(0, 7, 17, true),
  }]
};
const config = {
  type: 'line',
  data,
  options: {}
};


let myChart = new Chart(
    document.getElementById('myChart'),
    config
  );