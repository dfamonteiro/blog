import numpy as np
from pure_recycler_loop import recycler_loop, recycler_matrix

# Let's lean on the functions from pure_recycler_loop.py

def asteroid_crusher_matrix(quality_chance : float) -> np.ndarray:
    return recycler_matrix(quality_chance, production_ratio=0.8)

def asteroid_crusher_loop(input_vector : float, quality_chance : float) -> np.ndarray:
    return recycler_loop(input_vector, quality_chance, production_ratio=0.8)

if __name__ == "__main__":
    np.set_printoptions(suppress=True)
    print(asteroid_crusher_matrix(5))