function race_average_power(race_time, n_pitstops, car_energy, pit_delta, stop_delta, stop_energy) {
    let total_power = car_energy + n_pitstops * stop_energy
    let time_spent_racing = race_time - n_pitstops * (pit_delta + stop_delta)
    return total_power * 3600 / time_spent_racing
}

let macau_laptime = 2 * 60 + 30 // I'm guessing
let macau_lap_energy = 4 // kwh

console.log("Macau:   " + macau_lap_energy*3600/macau_laptime + " kw")

for (let n = 0; n < 6; n++) {
    let average_power = race_average_power(45 * 60 + 60, n, 51, 20, 30, 5)
    console.log(n + "stop: " + Math.round(average_power * 10000) / 10000 + " kw")
}


function joule_to_kwh(j){
    return j * 2.77777778 * 10 ** (-7)
}

//* Args:
//*     race_time -> seconds
//*     n_pitstops -> positive number
//*     car_energy -> kwh
//*     pit_delta -> seconds
//*     recharge_power -> kw
//*     seconds -> list of pitstop recharge times (in seconds)
//* Returns:
//*     a list of average lap powers (in kw)
function gen_pitstop_data(race_time, n_pitstops, car_energy, pit_delta, recharge_power, seconds) {
    let res = [];
    seconds.forEach(s => {
        res.push(
            race_average_power(race_time, n_pitstops, car_energy, pit_delta, s, joule_to_kwh(s*recharge_power*1000))
        )
    });
    return res
}

seconds = [];

for (let i = 0; i < 61; i++) {
    seconds.push(i);
}

const data = {
    labels: seconds,
    datasets: [{
      label: 'Guia circuit power requirement',
      backgroundColor: 'gray',
      borderColor: 'gray',
      data: new Array(61).fill(96),
    },{
      label: 'No stops',
      backgroundColor: '#ffec19',
      borderColor: '#ffec19',
      data: gen_pitstop_data(45 * 60 + 120, 0, 51, 20, 600, seconds),
    },{
      label: '1 stop',
      backgroundColor: '#ffc100',
      borderColor: '#ffc100',
      data: gen_pitstop_data(45 * 60 + 120, 1, 51, 20, 600, seconds),
    },{
      label: '2 stops',
      backgroundColor: '#ff9800',
      borderColor: '#ff9800',
      data: gen_pitstop_data(45 * 60 + 120, 2, 51, 20, 600, seconds),
    },{
      label: '3 stops',
      backgroundColor: '#ff5607',
      borderColor: '#ff5607',
      data: gen_pitstop_data(45 * 60 + 120, 3, 51, 20, 600, seconds),
    },{
      label: '4 stops',
      backgroundColor: '#f6412d',
      borderColor: '#f6412d',
      data: gen_pitstop_data(45 * 60 + 120, 4, 51, 20, 600, seconds),
    }]
};
const config = {
  type: 'line',
  data,
  options: {
    elements: {
      point:{
          radius: 2
      }
    }
  }
};


let myChart = new Chart(
    document.getElementById('myChart'),
    config
  );



function updateGraph(event) {
  let power_requirements = document.getElementById("power_requirements").valueAsNumber;
  let race_time = document.getElementById("race_time").valueAsNumber;
  let car_energy = document.getElementById("car_energy").valueAsNumber;
  let recharge_power = document.getElementById("recharge_power").valueAsNumber;
  let pit_delta = document.getElementById("pit_delta").valueAsNumber;
  
  console.log(power_requirements, race_time, car_energy, recharge_power, pit_delta);

  myChart.data.datasets[0].data = new Array(61).fill(power_requirements);

  for (let n_stops = 0; n_stops < 5; n_stops++) {
    myChart.data.datasets[n_stops + 1].data = gen_pitstop_data(race_time * 60, n_stops, car_energy, pit_delta, recharge_power, seconds);
  }

  myChart.update();
}

document.getElementById("power_requirements").addEventListener("input", updateGraph);
document.getElementById("race_time").addEventListener("input", updateGraph);
document.getElementById("car_energy").addEventListener("input", updateGraph);
document.getElementById("recharge_power").addEventListener("input", updateGraph);
document.getElementById("pit_delta").addEventListener("input", updateGraph);

function update_power_requirement(event) {
  let lap_time = document.getElementById("lap time").valueAsNumber;
  let energy = document.getElementById("lap energy").valueAsNumber;
  console.log(lap_time, energy);
  document.getElementById("power_requirements").value = (energy*3600/lap_time).toFixed(1);
  updateGraph(event);
}

document.getElementById("lap time").addEventListener("input", update_power_requirement);
document.getElementById("lap energy").addEventListener("input", update_power_requirement);

function update_lap_energy(event) {
  let power_requirements = document.getElementById("power_requirements").valueAsNumber;
  let lap_time = document.getElementById("lap time").valueAsNumber;

  document.getElementById("lap energy").value = (power_requirements * lap_time / 3600).toFixed(1);
}

document.getElementById("power_requirements").addEventListener("input", update_lap_energy);
