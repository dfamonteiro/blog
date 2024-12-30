import numpy as np
from typing import Union, List, Tuple
import itertools
from tqdm import tqdm

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
        assembler_modules_config : Union[Tuple[float, float], List[Tuple[float, float]]], # Modules configuration of assemblers for every quality level
        quality_to_keep : int = 5, # Don't assemble legendary ingredients (default)
        base_prod_bonus : float = 0, # base productivity of assembler + productivity technologies
        recipe_ratio : float = 1, # Ratio of items to ingredients of the recipe
        prod_module_bonus : float = 25,
        qual_module_bonus : float = 6.2) -> List[Tuple[float, float]]:
    
    production_rows = quality_to_keep - 1

    res = [(0, 0)] * 5

    for i, (prod_count, qual_count) in enumerate(assembler_modules_config):
        if i == production_rows:
            break

        # Assembler stats
        production_ratio = (100 + base_prod_bonus + prod_count * prod_module_bonus) * recipe_ratio / 100
        quality_chance = qual_count * qual_module_bonus

        res[i] = (quality_chance, production_ratio)

    return res

def recycler_assembler_loop(
        input_vector : Union[np.array, float],
        assembler_modules_config : Union[Tuple[float, float], List[Tuple[float, float]]], # Modules configuration of assemblers for every quality level
        items_quality_to_keep : Union[int, None] = 5, # Don't recycle legendary items (default)
        ingredients_quality_to_keep : Union[int, None] = 5, # Don't assemble legendary ingredients (default)
        base_prod_bonus : float = 0, # base productivity of assembler + productivity technologies
        recipe_ratio : float = 1, # Ratio of items to ingredients of the recipe
        prod_module_bonus : float = 25,
        qual_module_bonus : float = 6.2) -> np.array:
    
    if type(assembler_modules_config) == tuple:
        assembler_modules_config = [assembler_modules_config] * 5

    # Parameters for the production matrices
    recycler_parameters  = get_recycler_parameters(
        items_quality_to_keep if items_quality_to_keep != None else 6,
        recipe_ratio,
        qual_module_bonus
    )
    assembler_parameters = get_assembler_parameters(
        assembler_modules_config,
        ingredients_quality_to_keep if ingredients_quality_to_keep != None else 6,
        base_prod_bonus,
        recipe_ratio,
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
    res = recycler_assembler_loop(100, (0, 4), ingredients_quality_to_keep = None)
    print(res)
    print(f"{res[9]}%")
    # https://docs.google.com/spreadsheets/d/1fGQry4MZ6S95vWrt59TQoNRy1yJMx-er202ai0r4R-w/edit?gid=0#gid=0&range=E14

def correlation_prod_only_max_items():
    print("(E) Prod only, max items")
    res = recycler_assembler_loop(100, (4, 0), ingredients_quality_to_keep = None)
    print(res)
    print(f"{res[9]}%")
    # https://docs.google.com/spreadsheets/d/1fGQry4MZ6S95vWrt59TQoNRy1yJMx-er202ai0r4R-w/edit?gid=0#gid=0&range=F14

def get_config_string(config : List[Tuple[int, int]]):
    return [f"{p}P{q}Q" for (p, q) in config]

def get_all_configs(module_slots : int):
    module_variations_for_assembler = []

    for p in range(module_slots + 1):
        q = module_slots - p
        module_variations_for_assembler.append((p, q))
    
    res = list(itertools.product(* [module_variations_for_assembler] * 5))

    for i in range(len(res)):
        res[i] = list(res[i])
    
    return res
    

def correlation_optimal_modules_max_items():
    print("(F) Optimal modules, max items")

    best_config = None
    best_efficiency = 0

    all_configs = get_all_configs(4)

    for config in tqdm(list(all_configs)):
        efficiency = float(recycler_assembler_loop(100, config, ingredients_quality_to_keep = None)[9])

        if best_efficiency < efficiency:
            best_config = config
            best_efficiency = efficiency
    
    print(f"Optimal config: {get_config_string(best_config)}")
    print(f"Optimal efficiency: {best_efficiency}%")
    # https://docs.google.com/spreadsheets/d/1fGQry4MZ6S95vWrt59TQoNRy1yJMx-er202ai0r4R-w/edit?gid=0#gid=0&range=G14

def correlation_quality_only_max_ingredients():
    print("(G) Quality only, max ingredients")
    res = recycler_assembler_loop(100, (0, 4), items_quality_to_keep = None)
    print(res)
    print(f"{res[4]}%")
    # https://docs.google.com/spreadsheets/d/1fGQry4MZ6S95vWrt59TQoNRy1yJMx-er202ai0r4R-w/edit?gid=0#gid=0&range=H14

def correlation_prod_only_max_ingredients():
    print("(H) Prod only, max ingredients")
    res = recycler_assembler_loop(100, (4, 0), items_quality_to_keep = None)
    print(res)
    print(f"{res[4]}%")
    # https://docs.google.com/spreadsheets/d/1fGQry4MZ6S95vWrt59TQoNRy1yJMx-er202ai0r4R-w/edit?gid=0#gid=0&range=I14

def correlation_optimal_modules_max_ingredients():
    print("(I) Optimal modules, max ingredients")

    best_config = None
    best_efficiency = 0

    all_configs = get_all_configs(4)

    for config in tqdm(list(all_configs)):
        efficiency = float(recycler_assembler_loop(100, list(config), items_quality_to_keep = None)[4])

        if best_efficiency < efficiency:
            best_config = config
            best_efficiency = efficiency
    
    print(f"Optimal config: {get_config_string(best_config)}")
    print(f"Optimal efficiency: {best_efficiency}%")
    # https://docs.google.com/spreadsheets/d/1fGQry4MZ6S95vWrt59TQoNRy1yJMx-er202ai0r4R-w/edit?gid=0#gid=0&range=J14

def print_regulations():
    correlation_quality_only_max_items()
    print("=" * 100)
    correlation_prod_only_max_items()
    print("=" * 100)
    correlation_optimal_modules_max_items()
    print("=" * 100)
    correlation_quality_only_max_ingredients()
    print("=" * 100)
    correlation_prod_only_max_ingredients()
    print("=" * 100)
    correlation_optimal_modules_max_ingredients()

if __name__ == "__main__":
    np.set_printoptions(suppress=True, linewidth = 1000)
    print_regulations()