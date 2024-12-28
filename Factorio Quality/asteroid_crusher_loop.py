import numpy as np
from pure_recycler_loop import recycler_loop, recycler_matrix

# Let's lean on the functions from pure_recycler_loop.py

def asteroid_crusher_matrix(quality_chance : float) -> np.ndarray:
    return recycler_matrix(quality_chance, production_ratio=0.8)

def asteroid_crusher_loop(input_vector : float, quality_chance : float, quality_to_keep : int = 5) -> np.ndarray:
    return recycler_loop(input_vector, quality_chance, quality_to_keep, production_ratio=0.8)

def normal_to_legendary_ratio():
    indices = list(range(1, 13)) + [12.4]
    ratios = [float(1/asteroid_crusher_loop(1, i)[4]) for i in indices]

    print(f"{indices[4:]=}")
    print(f"{ratios[4:]=}")


def efficiency_data():
    indices = list(range(1, 13)) + [12.4]

    uncommon  = [float(asteroid_crusher_loop(100, i, 2)[1]) for i in indices]
    rare      = [float(asteroid_crusher_loop(100, i, 3)[2]) for i in indices]
    epic      = [float(asteroid_crusher_loop(100, i, 4)[3]) for i in indices]
    legendary = [float(asteroid_crusher_loop(100, i, 5)[4]) for i in indices]

    print(f"{uncommon=}")
    print(f"{rare=}")
    print(f"{epic=}")
    print(f"{legendary=}")

def number_of_crushers_required_per_quality_level():
    print(asteroid_crusher_loop(1, 12.4)[:4])

    print(asteroid_crusher_loop(1, 12.4)[:4] / asteroid_crusher_loop(1, 12.4)[3])
    
    print(asteroid_crusher_loop(1, 12.4)[:4] * 3 / asteroid_crusher_loop(1, 12.4)[3])

if __name__ == "__main__":
    np.set_printoptions(suppress=True)
    number_of_crushers_required_per_quality_level()