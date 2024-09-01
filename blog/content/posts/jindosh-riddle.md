+++ 
draft = false
date = 2024-08-31T21:38:40+01:00
title = "Solving the Jindosh Riddle with constraint programming"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["maths", "programming"]
categories = []
externalLink = ""
series = []
+++

I was playing Dishonored II (great game, by the way), when I came across a locked door. In order to make progress in the game, I had to open this door. When you get close to it, you begin to realise that there is more to this door than it first meets the eye:

<figure>
    <img src="/images/Jindosh-Lock.png" alt="Guia circuit track layout">
    <figcaption><b>The Jindosh Riddle door lock</b></figcaption>
</figure>

There is no key that opens this door. Instead, you have 5 slots with names and 5 slots with objects which need to have the right values to unlock the door. You don't have to brute force the combination, though, because right next to the door you can find the following riddle:

> At the dinner party were Lady Winslow, Doctor Marcolla, Countess Contee, Madam Natsiou, and Baroness Finch.
>
> The women sat in a row. They all wore different colors and Doctor Marcolla wore a jaunty white hat. Countess Contee was at the far left, next to the guest wearing a blue jacket. The lady in green sat left of someone in red. I remember that green outfit because the woman spilled her absinthe all over it. The traveller from Dabokva was dressed entirely in purple. When one of the dinner guests bragged about her Snuff Tin, the woman next to her said they were finer in Dabokva, where she lived.
>
> So Lady Winslow showed off a prized Diamond, at which the lady from Fraeport scoffed, saying it was no match for her War Medal. Someone else carried a valuable Ring and when she saw it, the visitor from Karnaca next to her almost spilled her neighbor's rum. Madam Natsiou raised her wine in toast. The lady from Baleton, full of beer, jumped onto the table, falling onto the guest in the center in the center seat, spilling the poor woman's whiskey. Then Baroness Finch captivated them all with a story about her wild youth in Dunwall.
>
> In the morning, there were four heirlooms under the table: the Snuff Tin, Bird Pendant, the War Medal, and the Ring.
>
> But who owned each?

There are other ways of finding the lock combination without having to solve the riddle, but when I first came across it I was dead set on solving it. It did take me a couple of hours and a lot of scribbles, but I managed to figure it out. However, while I was frying my brain solving this riddle, I started to wonder: wouldn't it be easier to write a program to solve this riddle for me? That's what we will be doing in this blog post.

## Initial setup

Before we start writing code, we should begin by making our lives easier by clearly stating the entities of our riddle:

| Name     | Heirloom     | Color  | Drink    | Origin   |
|----------|--------------|--------|----------|----------|
| Winslow  | Snuff Tin    | White  | Absinthe | Dabokva  |
| Marcolla | Bird Pendant | Blue   | Rum      | Fraeport |
| Contee   | War Medal    | Green  | Wine     | Karnaca  |
| Natsiou  | Ring         | Red    | Beer     | Baleton  |
| Finch    | Diamond      | Purple | Whiskey  | Dunwall  |

We also need to define how the solution will be structured. When solved this riddle manually I used a grid, and there's no reason to not do the same here:

|              | **Far-left** | **Left** | **Middle** | **Right** | **Far-right** |
|--------------|--------------|----------|------------|-----------|---------------|
| **Origin**   |              |          |            |           |               |
| **Drink**    |              |          |            |           |               |
| **Color**    |              |          |            |           |               |
| **Name**     |              |          |            |           |               |
| **Heirloom** |              |          |            |           |               |

### Initial setup (code)

We will be using python as our programming language and [OR-Tools](https://developers.google.com/optimization "OR-Tools home page") as our constraint solver. Let's begin with a very basic MVP:

```python
from ortools.sat.python import cp_model

RIDDLE_DATA = {
    "Name" : ['Winslow', 'Marcolla', 'Contee', 'Natsiou', 'Finch'],
    "Heirloom" : ['Snuff Tin', 'Bird Pendant', 'War Medal', 'Ring', 'Diamond'],
    "Color" : ['White', 'Blue', 'Green', 'Red', 'Purple'],
    "Drink" : ['Absinthe', 'Rum', 'Wine', 'Beer', 'Whiskey'],
    "Origin" : ['Dabokva', 'Fraeport', 'Karnaca', 'Baleton', 'Dunwall'],
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

if __name__ == "__main__":
    setup_solution_grid()
    solver = solve()
    debug_print(solver)

```

The most important thing to understand is the `solution_grid` variable. It has the following structure:

- Category (Name, Heirloom, etc.)
  - Seat (0,1,2,3,4)
    - Entity (White, Blue, Green, etc. if the selected category was "Color")

If, for example, you want to enforce that Countess Contee is at the far left (like it's specified in the riddle), you can write the following code:

```python
model.add(solution_grid["Name"][0]["Contee"] == 1)
```

### Initial setup (basic constraints)

If we run the code shown above, we get the following output:

```txt
PS C:\Users\Daniel\Desktop\github\blog\jindosh-riddle> python solver.py
Name:
    Seat 0:
        Winslow: 0
        Marcolla: 0
        Contee: 0
        Natsiou: 0
        Finch: 0
    Seat 1:
        Winslow: 0
        Marcolla: 0
        Contee: 0
        Natsiou: 0
        Finch: 0
    Seat 2:
        Winslow: 0
        Marcolla: 0
        Contee: 0
        Natsiou: 0
        Finch: 0
...
```

No value is selected for any grid cell, which obviously isn't what we want. Let's fix it:

```python
def enforce_single_value_in_every_cell():
    for category, entities in RIDDLE_DATA.items():
        for seat in range(5):
            entity_total = 0

            for entity in entities:
                entity_total += solution_grid[category][seat][entity]
            
            model.Add(entity_total == 1)
```

This simple constraint solves our initial problem, but another crops up:

```txt
PS C:\Users\Daniel\Desktop\github\blog\jindosh-riddle> python solver.py
Name:
    Seat 0:
        Winslow: 1
        Marcolla: 0
        Contee: 0
        Natsiou: 0
        Finch: 0
    Seat 1:
        Winslow: 1
        Marcolla: 0
        Contee: 0
        Natsiou: 0
        Finch: 0
    Seat 2:
        Winslow: 1
        Marcolla: 0
        Contee: 0
        Natsiou: 0
        Finch: 0
...
```

Notice how Lady Winslow is selected for every seat at the table. This is obviously not the expected behaviour. This will require another constraint:

```python
def enforce_different_values_for_every_category():
    for category, entities in RIDDLE_DATA.items():
        for entity in entities:
            seat_total = 0

            for seat in range(5):
                seat_total += solution_grid[category][seat][entity]
            
            model.Add(seat_total == 1)
```

Now every woman gets her own seat:

```txt
PS C:\Users\Daniel\Desktop\github\blog\jindosh-riddle> python solver.py
Name:
    Seat 0:
        Winslow: 0
        Marcolla: 0
        Contee: 0
        Natsiou: 1
        Finch: 0
    Seat 1:
        Winslow: 0
        Marcolla: 0
        Contee: 1
        Natsiou: 0
        Finch: 0
    Seat 2:
        Winslow: 1
        Marcolla: 0
        Contee: 0
        Natsiou: 0
        Finch: 0
```

The only thing missing now is a pretty-print functionality:

```python
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
```

```txt
PS C:\Users\Daniel\Desktop\github\blog\jindosh-riddle> python solver.py

Origin:   Baleton       Karnaca       Dabokva       Fraeport      Dunwall
Drink:    Absinthe      Beer          Wine          Whiskey       Rum
Color:    Blue          Red           Green         White         Purple
=========================================================================
Name:     Contee        Marcolla      Natsiou       Winslow       Finch
Heirloom: Ring          Bird Pendant  Snuff Tin     War Medal     Diamond
```

## Creating the constraints

### First paragraph

> The women sat in a row. They all wore different colors and Doctor Marcolla wore a jaunty white hat. Countess Contee was at the far left, next to the guest wearing a blue jacket. The lady in green sat left of someone in red. I remember that green outfit because the woman spilled her absinthe all over it. The traveller from Dabokva was dressed entirely in purple. When one of the dinner guests bragged about her Snuff Tin, the woman next to her said they were finer in Dabokva, where she lived.

Let's enumerate the facts and strip away the riddle's prose:

- Marcolla wears white
- Contee is at the far-left
- The person next to Contee wears blue
- Lady in green seats left of lady in red
- Lady in green drinks Absinthe
- Lady from Dabokva wears purple
- Lady with Snuff Tin is next to lady from Dabokva

Now let's enforce these facts in our program:

```python
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
```

`enforce_a_next_to_b` is an auxiliary funcion that enforces "person with characteristic `a` is next to person with characteristic `b`". This constraint shows up multiple times in both paragraphs, which justifies moving this logic to its own seperate function:

```python
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
```

A quick look at the solutions that the solver now provides shows that the constraints from the first paragraph are being enforced:

```txt
PS C:\Users\Daniel\Desktop\github\blog\jindosh-riddle> python solver.py

Origin:   Dabokva       Baleton       Karnaca       Fraeport      Dunwall
Drink:    Whiskey       Rum           Beer          Absinthe      Wine
Color:    Purple        Blue          White         Green         Red
=========================================================================
Name:     Contee        Winslow       Marcolla      Finch         Natsiou
Heirloom: War Medal     Snuff Tin     Bird Pendant  Ring          Diamond

165888 solutions
```

### Second Paragraph

> So Lady Winslow showed off a prized Diamond, at which the lady from Fraeport scoffed, saying it was no match for her War Medal. Someone else carried a valuable Ring and when she saw it, the visitor from Karnaca next to her almost spilled her neighbor’s rum. Madam Natsiou raised her wine in toast. The lady from Baleton, full of beer, jumped onto the table, falling onto the guest in the center in the center seat, spilling the poor woman’s whiskey. Then Baroness Finch captivated them all with a story about her wild youth in Dunwall.

Lets once again enumerate the facts:

- Winslow has the Diamond
- Lady from Fraeport has the War Medal
- Lady with Ring is next to lady from Karnaca
- Lady from Karnaka is next to lady with Rum
- Natsiou drinks Wine
- Lady from Baleton drinks Beer
- Lady in the center drinks Whiskey
- Finch is from Dunwall

Enforcing these facts is pretty straightforward:

```python
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
```

And this is the final result:

```txt
Origin:   Dabokva       Karnaca       Fraeport      Dunwall       Baleton
Drink:    Rum           Wine          Whiskey       Absinthe      Beer
Color:    Purple        Blue          White         Green         Red
=========================================================================
Name:     Contee        Natsiou       Marcolla      Finch         Winslow
Heirloom: Ring          Snuff Tin     War Medal     Bird Pendant  Diamond

1 solution
```

## Conclusion

I was pleasantly surprised by how well the riddle's facts mapped to python code. I had a lot of fun solving the riddle using constraint programming but it remains to be seen if simply solving the riddle by hand would be a quicker approach. It doesn't really matter, in the end: when it comes to games, the only metric that has ever mattered is whether or not you are having fun playing them.
