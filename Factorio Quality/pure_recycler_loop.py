from quality import custom_production_matrix
import numpy as np
from functools import lru_cache

@lru_cache()
def recycler_matrix(quality_chance : float, quality_to_keep : int = 5) -> np.ndarray:
    """Returns a matrix of a recycler with quality chance `quality_chance`
    that saves any item of quality level `quality_to_keep` or above.

    Args:
        quality_chance (float): Quality chance of the recyclers (in %).
        quality_to_keep (int): Minimum quality level of the items to be removed from the system
            (By default only removes legendaries).

    Returns:
        np.ndarray: Standard production matrix.
    """
    assert quality_chance > 0
    assert type(quality_to_keep) == int and 1 <= quality_to_keep <= 5

    recycling_rows = quality_to_keep - 1
    saving_rows = 5 - recycling_rows

    return custom_production_matrix(
        [(quality_chance, 0.25)] * recycling_rows + [(0, 0)] * saving_rows
    )

def recycler_loop(input_amount : float, quality_chance : float) -> np.ndarray:
    """Returns the amount of output items produced by a simple recycler loop
    as a vector that represents the distribution of qualities.

    Args:
        input_flow (float): Number of items that enter the system in an arbitrary timeframe (items/s, belts, etc.)
        quality_chance (float): Quality chance of the recyclers (in %).

    Returns:
        np.ndarray: Vector with 5 values. The value in position `i` of the vector 
            represents the amount of items of quality `i` produced by the loop (0 = normal, 4 = legendary).
            Amount of normal quality items does not include the `input_amount`
    """
    recycler_matrix = custom_production_matrix([(quality_chance, 0.25)] * 4 + [(0, 0)])
    input_belt = np.array([input_amount, 0, 0, 0, 0])
    result_flows = [input_belt]

    while True:
        result_flows.append(
            result_flows[-1].dot(recycler_matrix)
        )

        if sum(result_flows[-2] - result_flows[-1]) < 1E-10:
            break
    
    return sum(result_flows[1:])


def recycler_loop_quick_stats():
    "Recycler loop quick stats, shown in the terminal output"
    
    for i in list(range(1, 25)) + [24.8]:
        print(f"{i}: {1000/recycler_loop(1000, i)[4]}")
    print()
    for i in list(range(25)) + [24.8]:
        print(f"{i}: {recycler_loop(1000, i)}")

if __name__ == "__main__":
    np.set_printoptions(suppress=True)
    
    print(sum(np.array([1, 0, 0, 0, 0]) @ (recycler_matrix(25) ** i) for i in range(1000)))