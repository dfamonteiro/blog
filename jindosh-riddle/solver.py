from ortools.sat.python import cp_model
from typing import Tuple

RIDDLE_DATA = {
    "Origin" : ['Dabokva', 'Fraeport', 'Karnaca', 'Baleton', 'Dunwall'],
    "Drink" : ['Absinthe', 'Rum', 'Wine', 'Beer', 'Whiskey'],
    "Color" : ['White', 'Blue', 'Green', 'Red', 'Purple'],
    "Name" : ['Winslow', 'Marcolla', 'Contee', 'Natsiou', 'Finch'],
    "Heirloom" : ['Snuff Tin', 'Bird Pendant', 'War Medal', 'Ring', 'Diamond'],
}

FAR_LEFT, LEFT, MIDDLE, RIGHT, FAR_RIGHT = (0, 1, 2, 3, 4)

solution_grid = {}
model = cp_model.CpModel()

def setup_solution_grid():
    for category, entities in RIDDLE_DATA.items():
        solution_grid[category] = []

        for seat in range(5):
            solution_grid[category].append({})

            for entity in entities:
                solution_grid[category][seat][entity] = model.new_bool_var(f"{category}-{seat}-{entity}")

def enforce_single_value_in_every_cell():
    for category, entities in RIDDLE_DATA.items():
        for seat in range(5):
            entity_total = 0

            for entity in entities:
                entity_total += solution_grid[category][seat][entity]
            
            model.Add(entity_total == 1)

def enforce_different_values_for_every_category():
    for category, entities in RIDDLE_DATA.items():
        for entity in entities:
            seat_total = 0

            for seat in range(5):
                seat_total += solution_grid[category][seat][entity]
            
            model.Add(seat_total == 1)

def solve() -> cp_model.CpSolver:
    solver = cp_model.CpSolver()
    status = solver.solve(model)

    assert status in (cp_model.OPTIMAL, cp_model.FEASIBLE)
    return solver

def debug_print(solver : cp_model.CpSolver):
    for category, entities in RIDDLE_DATA.items():
        print(f"{category}:")
        for seat in range(5):
            print(f"    Seat {seat}:")
            for entity in entities:
                value = solver.value(solution_grid[category][seat][entity])
                print(f"        {entity}: {value}")

class SolutionCounter(cp_model.CpSolverSolutionCallback):
    def __init__(self):
        cp_model.CpSolverSolutionCallback.__init__(self)
        self.__solution_count = 0

    def on_solution_callback(self) -> None:
        self.__solution_count += 1

    @property
    def solution_count(self) -> int:
        return self.__solution_count

def get_solution_count():
    solver = cp_model.CpSolver()
    solution_printer = SolutionCounter()
    solver.parameters.enumerate_all_solutions = True
    solver.solve(model, solution_printer)

    return solution_printer.solution_count

def print_grid(solver : cp_model.CpSolver):
    print()
    for category, entities in RIDDLE_DATA.items():
        if category == "Name":
            print("=" * (10 + 14 * 4 + 7))

        print(f"{category}:".ljust(10), end="")
        for seat in range(5):
            for entity in entities:
                value = solver.value(solution_grid[category][seat][entity])

                if value == 1:
                    print(entity.ljust(14), end="")
                    break
        print()
    print()
    print(f"{get_solution_count()} solutions")

def enforce_a_next_to_b(a : Tuple[str, str], b : Tuple[str, str]):
    a_category, a_entity = a
    b_category, b_entity = b

    for seat in range(5):
        left = seat - 1
        right = seat + 1

        middle_variable = solution_grid[a_category][seat][a_entity]

        if left >= 0:
            left_variable = solution_grid[b_category][left][b_entity]
        else:
            left_variable = 0
        
        if right < 5:
            right_variable = solution_grid[b_category][right][b_entity]
        else:
            right_variable = 0

        model.Add(middle_variable <= left_variable + right_variable)

def enforce_first_paragraph():
    # Marcolla wears white
    # Lady in green drinks Absinthe
    # Lady from Dabokva wears purple
    for seat in range(5):
        model.Add(solution_grid["Name"][seat]["Marcolla"] == solution_grid["Color"][seat]["White"])
        model.Add(solution_grid['Color'][seat]["Green"] == solution_grid["Drink"][seat]["Absinthe"])
        model.Add(solution_grid["Origin"][seat]["Dabokva"] == solution_grid["Color"][seat]["Purple"])

    # Contee is at the far-left
    model.add(solution_grid["Name"][FAR_LEFT]["Contee"] == 1)

    # The person next to Contee wears blue
    enforce_a_next_to_b(("Name", "Contee"), ("Color", "Blue"))

    # Lady in green seats left of lady in red
    model.Add(solution_grid['Color'][FAR_LEFT]["Red"] == 0)
    for seat in range(5):
        left = seat - 1

        if left < 0:
            continue

        red_variable  = solution_grid['Color'][seat]["Red"]
        left_variable = solution_grid["Color"][left]["Green"]

        model.Add(left_variable == red_variable)

    # Lady with Snuff Tin is next to lady from Dabokva
    enforce_a_next_to_b(("Heirloom", "Snuff Tin"), ("Origin", "Dabokva"))

def enforce_second_paragraph():
    # Lady in the center drinks Whiskey
    model.Add(solution_grid["Drink"][MIDDLE]["Whiskey"] == 1)

    # Winslow has the Diamond
    # Lady from Fraeport has the War Medal
    # Natsiou drinks Wine
    # Lady from Baleton drinks Beer
    # Finch is from Dunwall
    for seat in range(5):
        model.Add(solution_grid["Name"][seat]["Winslow"] == solution_grid["Heirloom"][seat]["Diamond"])
        model.Add(solution_grid["Origin"][seat]["Fraeport"] == solution_grid["Heirloom"][seat]["War Medal"])
        model.Add(solution_grid["Name"][seat]["Natsiou"] == solution_grid["Drink"][seat]["Wine"])
        model.Add(solution_grid["Origin"][seat]["Baleton"] == solution_grid["Drink"][seat]["Beer"])
        model.Add(solution_grid["Name"][seat]["Finch"] == solution_grid["Origin"][seat]["Dunwall"])

    # Lady with Ring is next to lady from Karnaca
    # Lady from Karnaka is next to lady with Rum
    enforce_a_next_to_b(("Heirloom", "Ring"), ("Origin", "Karnaca"))
    enforce_a_next_to_b(("Origin", "Karnaca"), ("Drink", "Rum"))

if __name__ == "__main__":
    setup_solution_grid()

    enforce_single_value_in_every_cell()
    enforce_different_values_for_every_category()

    enforce_first_paragraph()
    enforce_second_paragraph()

    solver = solve()
    print_grid(solver)
