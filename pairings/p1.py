from typing import List

total_count = 0

def compute_pairings(n : int, available_teams : List[int] = None, current_solution : List[int] = None):
    if available_teams == None:
        available_teams = [i for i in range(n)]
        current_solution = []
        compute_pairings(n, available_teams, current_solution)
        return
    
    if n == 2:
        # print(current_solution + available_teams)
        global total_count
        total_count += 1
        return
    
    current_solution.append(available_teams.pop(0))

    for i in range(len(available_teams)):
        available_teams_new_instance = list(available_teams)
        current_solution_new_instance = list(current_solution)

        current_solution_new_instance.append(available_teams_new_instance.pop(i))
        compute_pairings(n - 2, available_teams_new_instance, current_solution_new_instance)

compute_pairings(14)
print(f"{total_count} pairings found")