from typing import List
from sys import argv
from io import TextIOWrapper

total_count = 0

def compute_pairings(n : int, file : TextIOWrapper, available_teams : List[int] = None, current_solution : List[int] = None):
    if available_teams == None:
        available_teams = [i for i in range(n)]
        current_solution = []
        compute_pairings(n, file, available_teams, current_solution)
        return
    
    if n == 2:
        print(current_solution + available_teams, file=file)
        global total_count
        total_count += 1
        return
    
    current_solution.append(available_teams.pop(0))

    for i in range(len(available_teams)):
        available_teams_new_instance = list(available_teams)
        current_solution_new_instance = list(current_solution)

        current_solution_new_instance.append(available_teams_new_instance.pop(i))
        compute_pairings(n - 2, file, available_teams_new_instance, current_solution_new_instance)

if __name__ == "__main__":
    n_teams = int(argv[1])
    new_file_path = argv[2]
    with open(new_file_path, "w") as f:
        compute_pairings(n_teams, f)
        
    print(f"{total_count} pairings found")