from typing import List
from sys import argv

def compute_pairings(n : int, available_teams : List[int] = None, current_solution : List[int] = None, res : List[List[int]] = None) -> List[List[int]]:
    """Compute all possible pairings in a group of `n` teams

    Args:
        n (int): Number of teams
        available_teams (List[int], optional): List of ids of the teams (internal variable). Defaults to None.
        current_solution (List[int], optional): Current pairing permutation being assembled (internal variable). Defaults to None.
        res (List[List[int]], optional): Current list of all permutations, which will be returned by the function when it's complete (internal variable). Defaults to None.

    Returns:
        List[List[int]]: All possible pairings in a group of `n` teams
    """
    if available_teams == None: # If the internal variables are not set, initialize them
        available_teams = [i for i in range(n)]
        current_solution = []
        res = []

        compute_pairings(n, available_teams, current_solution, res)
        return res
    
    if n == 2:
        # The current permutation is complete, add it to the results list
        res.append(current_solution + available_teams)
    else:
        # Get the lowest id and add it to the current permutation
        current_solution.append(available_teams.pop(0))

        for i in range(len(available_teams)):
            # Make a copy of the internal variables
            available_teams_new_instance = list(available_teams)
            current_solution_new_instance = list(current_solution)

            current_solution_new_instance.append(available_teams_new_instance.pop(i))
            compute_pairings(n - 2, available_teams_new_instance, current_solution_new_instance, res)

if __name__ == "__main__":
    n_teams = int(argv[1])
    new_file_path = argv[2]

    res = compute_pairings(n_teams)

    with open(new_file_path, "w") as f:
        f.writelines(str(r) for r in res)
        
    print(f"{len(res)} pairings found")