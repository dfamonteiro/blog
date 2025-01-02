import numpy as np
from typing import Union, List, Tuple
import itertools
from tqdm import tqdm
from enum import Enum
import pandas

from quality import custom_production_matrix

def custom_transition_matrix(recycler_matrix : np.ndarray, assembler_matrix : np.ndarray) -> np.ndarray:
    """Creates a transition matrix based on the 
    provided recycler and assembler production matrices.

    Args:
        recycler_matrix (np.ndarray): Recycler production matrix.
        assembler_matrix (np.ndarray): Assembler production matrix.

    Returns:
        np.ndarray: Transition matrix with the recycler production matrix 
        in the lower left and assembler production matrix in the upper right.
    """
    res = np.zeros((10,10))

    for i in range(5):
        for j in range(5):
            res[i + 5][j] = recycler_matrix[i, j]
            res[i][j + 5] = assembler_matrix[i, j]

    return res

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
        production_ratio = (100 + min(base_prod_bonus + prod_count * prod_module_bonus, 300)) * recipe_ratio / 100
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
    """Returns a vector with values for each quality level that mean different things, depending on whether that quality is kept or recycled:
        - If the quality is kept: the value is the production rate of ingredients/items of that quality level.
        - If the quality is recycled: the value is the internal flow rate of ingredients/items of that quality level in the system.
    
    The first five values represent the ingredients and the last five values represent the items.

    Args:
        input_vector (Union[np.array, float]): The ingredients and items intake of the system.
        assembler_modules_config (Union[Tuple[float, float], List[Tuple[float, float]]]): Number of productivity and quality modules for the assemblers of each quality of item.
        items_quality_to_keep (Union[int, None], optional): Minimum quality level of the items to be removed from the system. Defaults to 5 (Legendary). If set to None, nothing is removed.
        ingredients_quality_to_keep (Union[int, None], optional): Minimum quality level of the ingredients to be removed from the system. Defaults to 5 (Legendary). If set to None, nothing is removed.
        base_prod_bonus (float, optional): Base productivity of assembler + productivity technologies. Defaults to 0.
        recipe_ratio (float, optional): Ratio of items to ingredients of the crafting recipe. Defaults to 1.
        prod_module_bonus (float, optional): Productivity bonus from productivity modules. Defaults to 25%.
        qual_module_bonus (float, optional): Quality chance bonus from quality modules. Defaults to 6.2%.

    Returns:
        np.array: Vector with values for each quality level. The first five values represent the ingredients and the last five values represent the items.
    """
    
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

def factorio_wiki_repro():
    print(custom_production_matrix([(25, 0.25)] * 4 + [(0, 0)]))
    print(custom_production_matrix([(25, 1.5)] * 5))

    print(custom_transition_matrix(
        custom_production_matrix([(25, 0.25)] * 4 + [(0, 0)]), 
        custom_production_matrix([(25, 1.5)] * 5)
    ))
    # https://wiki.factorio.com/Quality

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

def correlation_optimal_modules_max_items():
    print("(F) Optimal modules, max items")

    best_config = None
    best_efficiency = 0

    all_configs = get_all_configs(4)

    for config in tqdm(list(all_configs)):
        if config[4] != (4, 0):
            # Makes no sense to put quality modules on legendary item crafter
            continue

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
        if config[4] != (4, 0):
            # Makes no sense to put quality modules on legendary item crafter
            continue

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

class SystemOutput(Enum):
    INGREDIENTS = 0
    ITEMS = 1

class ModuleStrategy(Enum):
    FULL_QUALITY = 0
    FULL_PRODUCTIVITY = 1
    OPTIMIZE = 2

def get_all_configs(module_slots : int):
    "Generate all possible configurations for an assembler with `n` module slots."
    module_variations_for_assembler = []

    for p in range(module_slots + 1):
        q = module_slots - p
        module_variations_for_assembler.append((p, q))
    
    res = list(itertools.product(* [module_variations_for_assembler] * 5))

    for i in range(len(res)):
        res[i] = list(res[i])
    
    return res

def recycler_assembler_efficiency(
        module_slots : int,
        base_productivity : float,
        system_output : SystemOutput,
        module_strategy : ModuleStrategy) -> float:
    "Returns the efficiency of the setup with the given parameters (%)."
    assert module_slots >= 0 and base_productivity >= 0

    if system_output == SystemOutput.ITEMS:
        keep_items = 5
        keep_ingredients = None
    else: # system_output == SystemOutput.INGREDIENTS:
        keep_items = None
        keep_ingredients = 5
    
    # What is the output of the system: ingredients or items?
    result_index = 4 if system_output == SystemOutput.INGREDIENTS else 9
    
    if module_strategy != ModuleStrategy.OPTIMIZE:
        if module_strategy == ModuleStrategy.FULL_PRODUCTIVITY:
            config = (module_slots, 0)
        else:
            config = (0, module_slots)
        
        output = recycler_assembler_loop(100, config, keep_items, keep_ingredients, base_productivity)
        return output[result_index]
    else:
        best_config = None
        best_efficiency = 0

        all_configs = get_all_configs(module_slots)

        for config in tqdm(list(all_configs)):
            if config[4] != (module_slots, 0):
                # Makes no sense to put quality modules on legendary item crafter
                continue
            
            output = recycler_assembler_loop(100, list(config), keep_items, keep_ingredients, base_productivity)
            efficiency = float(output[result_index])

            if best_efficiency < efficiency:
                best_config = config
                best_efficiency = efficiency
        
        return best_efficiency

def efficiency_table():
    DATA = { # (number of slots, base productivity)
        "Electric furnace/Centrifuge" : (2, 0),
        "Chemical Plant"              : (3, 0),
        "Assembling machine"          : (4, 0),
        "Foundry/Biochamber"          : (4, 50),
        "Electromagnetic plant"       : (5, 50),
        "Cryogenic plant"             : (8, 0),
    }
    OUTPUTS = (SystemOutput.ITEMS, SystemOutput.INGREDIENTS)
    STRATEGIES = (
        ModuleStrategy.FULL_QUALITY,
        ModuleStrategy.FULL_PRODUCTIVITY,
        ModuleStrategy.OPTIMIZE
    )
    KEY_NAMES = {
        (SystemOutput.ITEMS, ModuleStrategy.FULL_QUALITY) : "(D) Quality only, max items",
        (SystemOutput.ITEMS, ModuleStrategy.FULL_PRODUCTIVITY) : "(E) Prod only, max items",
        (SystemOutput.ITEMS, ModuleStrategy.OPTIMIZE) : "(F) Optimal modules, max items",
        (SystemOutput.INGREDIENTS, ModuleStrategy.FULL_QUALITY) : "(G) Quality only, max ingredients",
        (SystemOutput.INGREDIENTS, ModuleStrategy.FULL_PRODUCTIVITY) : "(H) Prod only, max ingredients",
        (SystemOutput.INGREDIENTS, ModuleStrategy.OPTIMIZE) : "(I) Optimal modules, max ingredients",
    }

    table = {key : {} for key in DATA}

    for assembler_type, (slots, base_prod) in DATA.items():
        for output in OUTPUTS:
            for strategy in STRATEGIES:
                eff = recycler_assembler_efficiency(slots, base_prod, output, strategy)
                table[assembler_type][KEY_NAMES[(output, strategy)]] = eff
    
    print(pandas.DataFrame(table).T.to_string())

def simple_recycler_assembler_loop():
    input_vector = np.array([1] + [0] * 9)

    transition_matrix = custom_transition_matrix( # Transition matrix from the previous subchapter
        custom_production_matrix([(25, 0.25)] * 4 + [(0, 0)]),
        custom_production_matrix([(25, 1.5)] * 5)
    )
    
    result_flows = [input_vector]
    while True:
        result_flows.append(result_flows[-1] @ transition_matrix)

        if sum(abs(result_flows[-2] - result_flows[-1])) < 1E-10:
            # There's nothing left in the system
            break

    print(sum(result_flows))
    print(" & ".join([str(float(round(i, 5))) for i in sum(result_flows)]))

if __name__ == "__main__":
    np.set_printoptions(suppress=True, linewidth = 1000)
    efficiency_table()
