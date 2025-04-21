from typing import List
from ortools.sat.python import cp_model
from sys import argv


class VarArraySolutionAccumulator(cp_model.CpSolverSolutionCallback):
    "Accumulates all the solutions in the `__res` variable"
    def __init__(self, variables):
        cp_model.CpSolverSolutionCallback.__init__(self)
        self.__variables = variables
        self.__res = []

    def on_solution_callback(self):
        self.__res.append([self.Value(v) for v in self.__variables])

    def res(self) -> List[List[int]]:
        return self.__res


def search_for_all_solutions_sample_sat(n : int):
    assert n % 2 == 0

    # Creates the model.
    model = cp_model.CpModel()

    values = []
    for i in range(n):
        values.append(
            model.NewIntVar(0, n-1, f"v{i}")
        )
    
    # Only list permutations
    model.AddAllDifferent(values)

    # The first value of each pairing must be lower than the first value of the following pairing
    for i in range(n//2 - 1):
        model.Add(values[i * 2] < values[(i + 1) * 2])

    # For all pairings, the first value of the pairing must be lower than the second value of the pairing
    for i in range(n//2):
        model.Add(values[i * 2] < values[i * 2 + 1])

    # Create a solver and solve
    solver = cp_model.CpSolver()

    # Print all solutions to a file
    solution_printer = VarArraySolutionAccumulator(values)

    # Enumerate all solutions
    solver.parameters.enumerate_all_solutions = True

    # Solve
    solver.Solve(model, solution_printer)

    return solution_printer.res()

if __name__ == "__main__":
    n_teams = int(argv[1])
    new_file_path = argv[2]

    res = search_for_all_solutions_sample_sat(n_teams)
        
    with open(new_file_path, "w") as f:
        f.writelines(str(r) + "\n" for r in res)
        
    print(f"{len(res)} pairings found")

# 4  - 3
# 6  - 15
# 8  - 105
# 10 - 945
# 12 - 10395
# 14 - 135135
# 16 - 2027025
