import numpy as np
from typing import Union, List, Tuple

from quality import custom_production_matrix

def custom_transition_matrix(recycler_matrix : np.ndarray, assembler_matrix : np.ndarray) -> np.ndarray:
    res = np.zeros((10,10))

    for i in range(5):
        for j in range(5):
            res[i + 5][j] = recycler_matrix[i, j]
            res[i][j + 5] = assembler_matrix[i, j]

    return res

def factorio_wiki_repro():
    print(custom_transition_matrix(
        custom_production_matrix([(25, 0.25)] * 4 + [(0, 0)]), 
        custom_production_matrix([(25, 1.5)] * 5)
    ))
    # https://wiki.factorio.com/Quality

def get_recycler_parameters(
        quality_to_keep : int = 5, # Don't recycle legendary items (default)
        recipe_ratio : float = 1, # Ratio of items to ingredients of the recipe
        qual_module_bonus : float = 6.2) -> List[Tuple[float, float]]:
    
    recycling_rows = quality_to_keep - 1
    saving_rows = 5 - recycling_rows

    # Recycler stats
    production_ratio = 0.25 / recipe_ratio
    quality_chance = 4 * qual_module_bonus

    return [(quality_chance, production_ratio)] * recycling_rows + [(0, 0)] * saving_rows

def get_assembler_parameters(
        assembler_prod_modules : int, # Number of prod modules in assembler
        assembler_qual_modules : int, # Number of qual modules in assembler
        quality_to_keep : int = 5, # Don't assemble legendary ingredients (default)
        base_prod_bonus : float = 0, # base productivity of assembler + productivity technologies
        full_prod_in_legendary_assembler : bool = False, # Load assembler of legendary items with prod modules
        recipe_ratio : float = 1, # Ratio of items to ingredients of the recipe
        assembler_modules : int = 4, # Number of module slots in assemblers
        prod_module_bonus : float = 25,
        qual_module_bonus : float = 6.2) -> List[Tuple[float, float]]:
    
    assert assembler_prod_modules + assembler_qual_modules <= assembler_modules

    prod_from_modules = assembler_prod_modules * prod_module_bonus # Helper variable
    recycling_rows = quality_to_keep - 1
    saving_rows = 5 - recycling_rows

    # Assembler stats
    production_ratio = (100 + base_prod_bonus + prod_from_modules) * recipe_ratio / 100
    quality_chance = assembler_qual_modules * qual_module_bonus

    res = [(quality_chance, production_ratio)] * recycling_rows + [(0, 0)] * saving_rows

    if full_prod_in_legendary_assembler and quality_to_keep <= 5: # Load assembler of legendary items with prod modules
        res[4] = (0, assembler_modules * prod_module_bonus)

    return res

def recycler_assembler_loop(
        input_vector : Union[np.array, float],
        assembler_prod_modules : int, # Number of prod modules in assembler
        assembler_qual_modules : int, # Number of qual modules in assembler
        items_quality_to_keep : Union[int, None] = 5, # Don't recycle legendary items (default)
        ingredients_quality_to_keep : Union[int, None] = 5, # Don't assemble legendary ingredients (default)
        base_prod_bonus : float = 0, # base productivity of assembler + productivity technologies
        full_prod_in_legendary_assembler : bool = False, # Load assembler of legendary items with prod modules
        recipe_ratio : float = 1, # Ratio of items to ingredients of the recipe
        assembler_modules : int = 4, # Number of module slots in assemblers
        prod_module_bonus : float = 25,
        qual_module_bonus : float = 6.2) -> np.array:
    
    # Parameters for the production matrices
    recycler_parameters  = get_recycler_parameters(
        items_quality_to_keep if items_quality_to_keep != None else 6,
        recipe_ratio,
        qual_module_bonus
    )
    assembler_parameters = get_assembler_parameters(
        assembler_prod_modules,
        assembler_qual_modules,
        ingredients_quality_to_keep if ingredients_quality_to_keep != None else 6,
        base_prod_bonus,
        full_prod_in_legendary_assembler,
        recipe_ratio,
        assembler_modules,
        prod_module_bonus,
        qual_module_bonus
    )

    if type(input_vector) in (float, int):
        input_vector = np.array([input_vector] + [0] * 9)
    
    result_flows = [input_vector]
    while True:
        result_flows.append(
            result_flows[-1] @ custom_transition_matrix(
                custom_production_matrix(recycler_parameters), 
                custom_production_matrix(assembler_parameters)
            )
        )

        if sum(abs(result_flows[-2] - result_flows[-1])) < 1E-10:
            # There's nothing left in the system
            break

    return sum(result_flows)

def correlation_quality_only_max_items():
    print("(D) Quality only, max items")
    res = recycler_assembler_loop(100, 0, 4, ingredients_quality_to_keep = None)
    print(res)
    print(f"{res[9]}%")
    # https://docs.google.com/spreadsheets/d/1fGQry4MZ6S95vWrt59TQoNRy1yJMx-er202ai0r4R-w/edit?gid=0#gid=0&range=E14

def correlation_prod_only_max_items():
    print("(E) Prod only, max items")
    res = recycler_assembler_loop(100, 4, 0, ingredients_quality_to_keep = None)
    print(res)
    print(f"{res[9]}%")
    # https://docs.google.com/spreadsheets/d/1fGQry4MZ6S95vWrt59TQoNRy1yJMx-er202ai0r4R-w/edit?gid=0#gid=0&range=F14

if __name__ == "__main__":
    np.set_printoptions(suppress=True, linewidth = 1000)
    