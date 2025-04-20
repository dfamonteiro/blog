from io import TextIOWrapper
from ortools.sat.python import cp_model
from sys import argv


class VarArraySolutionPrinter(cp_model.CpSolverSolutionCallback):
    """Print intermediate solutions to a file"""

    def __init__(self, variables, file : TextIOWrapper):
        cp_model.CpSolverSolutionCallback.__init__(self)
        self.__variables = variables
        self.__solution_count = 0
        self.file = file

    def on_solution_callback(self):
        self.__solution_count += 1
        print([self.Value(v) for v in self.__variables], file=self.file)

    def solution_count(self):
        return self.__solution_count


def search_for_all_solutions_sample_sat(n : int, file : TextIOWrapper):
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
    solution_printer = VarArraySolutionPrinter(values, file)

    # Enumerate all solutions
    solver.parameters.enumerate_all_solutions = True

    # Solve
    status = solver.Solve(model, solution_printer)

    # Debug info
    print(f"Status = {solver.StatusName(status)}")
    print(f"Number of solutions found: {solution_printer.solution_count()}")

if __name__ == "__main__":
    new_file_path = argv[1]
    with open(new_file_path, "w") as f:
        search_for_all_solutions_sample_sat(14, f)

# 4  - 3
# 6  - 15
# 8  - 105
# 10 - 945
# 12 - 10395
# 14 - 135135
# 16 - 2027025
