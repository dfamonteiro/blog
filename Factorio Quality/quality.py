import numpy as np

from enum import IntEnum
from typing import Union, List, Tuple

class QualityTier(IntEnum):
    Normal    = 0
    Uncommon  = 1
    Rare      = 2
    Epic      = 3
    Legendary = 4


def quality_probability(quality_chance : float, input_tier : QualityTier, output_tier : QualityTier) -> float:
    """Calculates the probability of a machine craft with a certain `quality_chance` upgrading 
    the resulting product from the tier of the products (`input_tier`) to the `output_tier`.

    Args:
        quality_chance (float): Quality chance (in %).
        input_tier (QualityTier): Quality tier of the ingredients.
        output_tier (QualityTier): Quality tier of the product.
    
    Returns:
        float: A probability from 0 to 1.
    """
    
    # Basic validations
    assert 0 <= quality_chance <= 100
    assert 0 <= input_tier  <= 4 and type(input_tier)  == int
    assert 0 <= output_tier <= 4 and type(output_tier) == int

    # Some QoL conversions
    quality_chance /= 100
    i = input_tier
    o = output_tier

    # An item can never be downgraded
    if input_tier > output_tier:
        return 0
    
    # If the item is already legendary, it will remain legendary
    if input_tier == QualityTier.Legendary:
        return 1
    
    # Probability of item staying in the same tier
    if input_tier == output_tier:
        return 1 - quality_chance
    
    # Probability of item going straight to legendary
    if output_tier == QualityTier.Legendary:
        return quality_chance / (10 ** (3 - i))
    
    # else
    return (quality_chance * 9/10) / (10 ** (o - i - 1))


def quality_matrix(quality_chance : float) -> np.ndarray:
    """Returns the quality matrix for the corresponding `quality_chance` which indicates 
    the probabilities of any input tier jumping to any other tier.

    Args:
        quality_chance (float): Quality chance (in %).

    Returns:
        np.ndarray: 5x5 matrix. The columns represent the input quality tier and go from legendary to normal, from left to right.
            The lines represent the output quality tier and go from normal to legendary, from top to bottom.
    """

    res = np.zeros((5,5))
    
    for row in range(5):
        for column in range(5):
            res[row][column] = quality_probability(quality_chance, row, column)
    
    return res


def basic_production_matrix(quality_chance : float, production_ratio : float = 1) -> np.ndarray:
    "Returns the production matrix for the corresponding `quality_chance` and `production_ratio`"
    return quality_matrix(quality_chance) * production_ratio

def custom_production_matrix(parameters_per_row : List[Tuple[float, float]]) -> np.ndarray:
    """_summary_

    Args:
        parameters_per_row (List[Tuple[float, float]]): _description_

    Returns:
        np.ndarray: _description_
    """
    pass #TODO

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
    recycler_matrix = basic_production_matrix(quality_chance, 0.25)
    recycler_matrix[4][4] = 0
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
    # print(quality_matrix(10))
    # recycler_loop_quick_stats()
    print(basic_production_matrix(10, 19/220))
