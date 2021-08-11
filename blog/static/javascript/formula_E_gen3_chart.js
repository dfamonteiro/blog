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