+++ 
draft = true
date = 2024-12-28T22:09:26Z
title = "Solving the mathematics of Factorio Quality: Recycler-Assembler Loop"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["maths", "programming", "Factorio Quality"]
categories = []
externalLink = ""
series = []
+++

<script type="module">
    import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
    mermaid.initialize({ startOnLoad: true });
</script>

The recycler-assembler loop is probably the most commonly used quality grinding method in Factorio. It is a two step looping process where in the first step, the ingredients are crafted by an assembler into items, which are then recycled back into ingredients. By having quality and productivity[^1] modules in both the assembling and the recycling steps, the quality and items and ingredients in the loop will eventually improve all the way to legendary. Emulating a recycler-assembler loop is slightly more complex than the [pure recycling loops](/posts/factorio-pure-recycler-loop/) we have dealt with previously, but the same fundamental ideas still apply. Please note that functions from previous blog posts will be reused here.

[^1]: Productivity modules are only allowed under certain circumstances in the assembling step, whereas quality modules are always allowed in both the recycling and assembling steps.

<div style="text-align:center">
    <img src="/images/factorio-recycler-assembler-loop.webp" alt="Blue circuit crafting recipe"/>
    <figcaption> Example of a recycler-assembler loop setup. <br> (Credit for this design: <a href="https://youtu.be/Z1BEXm4RIfs?si=XCf6b7F-cjY_5G5E&t=584">Konage</a>)</figcaption>
</div>

## Unrolling the infinite loop

Before we start crafting transition matrices, we need to figure out a way to deal with the outputs of the system getting fed back into the system[^2]:

[^2]: You might be getting a strong feeling of _déjà vu_ if you have already read "[Solving the mathematics of Factorio Quality: Pure Recycler Loop](/posts/factorio-pure-recycler-loop/)" previously.

<pre style="text-align:center" class="mermaid">
---
title: Conceptual model of a recycler-assembler loop
config:
  theme: dark
---
flowchart TD
    A[Ingredient Input] -->|Q=0 Ingredients| B[Assembler]
    B -->|Q<=4 Items| C[Recycler]
    C -->|Q<=4 Ingredients| B
    B -->|Q=5 Items| D[Q5 Item Storage]
    C -->|Q=5 Ingredients| E[Q5 Ingredient Storage]
</pre>
Notice how I'm using "Q" as a shorthand for the level of quality of the items. Q=1 means common items and Q=5 means legendary items.

Finding a way to get around loops is never easy. Fortunately for us, we can take a page from a compiler optimization book and unroll the loop:

<pre style="text-align:center" class="mermaid">
---
title: Conceptual model of a pure recycler loop, unrolled into an infinite line
config:
  theme: dark
---
flowchart TD
    A[Ingredient Input] --> A0
    A0 -->|Q<=4 Items| R0
    A0 -->|Q=5 Items| END[Q5 Item/Ingredient Storage]

    R0 -->|Q=5 Ingredients| END
    R0 -->|Q<=4 Ingredients| A1

    A1 -->|Q=5 Items| END
    A1 -->|Q<=4 Items| R1

    R1 -->|Q=5 Ingredients| END
    R1 -->|Q<=4 Ingredients|An[An]

    An -->|Q=5 Items| END
    An -->|Q<=4 Items|Rn1[Rn]

    Rn1 -->|Q=5 Ingredients| END
    Rn1 -->|Q<=4 Ingredients|Rn2[...]
</pre>

Doing this trick does get rid of the loop and gives us a more workable linear problem. Unfortunately, this comes at a cost of having to handle the recycler-assembler line being theoretically infinite. In practice, the system runs out of materials very quickly because all the materials that don't get voided will eventually turn into legendary items/ingredients and be removed from the system.

## Crafting a transition matrix

### An initial approach

In theory, we could use the techniques we created in previous blog posts to calculate the outputs of this recycler-assembler line. We would need to know:

- The production matrix of the recycler $R$.
- The production matrix of the assembler $A$.
- An input vector of ingredients $\vec{a_0}$.
  - Let's assume that $\vec{a_0}= \begin{bmatrix} 1 & 0 & 0 & 0 & 0 \end{bmatrix}$ by default.
- What is the output of the system and what gets recycled (items/ingredients, and of which quality).
  - This information would be incorporated in $A$ and $R$ by zeroing out the necessary matrix rows.

The state of the system would be represented by two vectors $\vec{a_x}$ and $\vec{r_x}$, that respectively represents the amount of ingredients and items after iteration $x$. In order to process $\vec{a_x}$ into $\vec{r_x}$ and vice versa, you must use the production matrices $R$ and $A$:

$$ \vec{a_x} = \vec{r_{x}} \cdot R $$

$$ \vec{r_x} = \vec{a_{x-1}} \cdot A $$

The final step would be to collect the production statistics after every iteration:

$$ \vec{a} = \vec{a_0} + \vec{a_1} + \vec{a_2} + \vec{a_3} + \vec{a_4} + \vec{a_n} + \vec{a_{n+1}} + ...$$

$$ \vec{r} = \vec{r_1} + \vec{r_2} + \vec{r_3} + \vec{r_4} + \vec{r_n} + \vec{r_{n+1}} + ...$$

I theory, this approach would work. In practice, however, having to flip-flop between items and ingredient vectors constantly is very annoying, confusing and hard to implement correctly without bugs. Luckily, we can make our lives a lot easier by having the matrix do the flip-flopping for us.

### Introducing [**stochastic matrices**](https://en.wikipedia.org/wiki/Stochastic_matrix)

A stochastic matrix is a a square matrix whose values represent the probability of a state transitioning to another state[^3]. They are incredibly useful in a multitude of scientif fields and are also known as probability matrices, Markov matrices, substitution matrices, or as the [Factorio wiki](https://wiki.factorio.com/Quality) prefers to call them, **transition matrices**.

[^3]: There is an argument to be made that the quality and production matrices used throughout this series of blog posts _are_ stochastic matrices.

In our case, we have two states: ingredient and item. We also have two transitions: item->ingredient and ingredient->item. Creating the transition matrix $M$ for this simple use case is quite straightforward:

$$ M = \begin{bmatrix}
  0 & 1 \\\\
  1 & 0 \\\\
\end{bmatrix} $$

Let's say that we have an input vector $\vec{v_0} = \begin{bmatrix} 1 & 0\end{bmatrix}$ whose first value represents the ingredients and the second value represents the items. Let's multiply $\vec{v}$ by $M$:

$$ \vec{v_1} = \vec{v_0} \cdot M = \begin{bmatrix} 1 & 0\end{bmatrix} \cdot \begin{bmatrix}
  0 & 1 \\\\
  1 & 0 \\\\
\end{bmatrix} = \begin{bmatrix} 0 & 1\end{bmatrix}$$

The values of the vector flipped! Let's try it again:

$$ \vec{v_2} = \vec{v_1} \cdot M = \begin{bmatrix} 0 & 1\end{bmatrix} \cdot \begin{bmatrix}
  0 & 1 \\\\
  1 & 0 \\\\
\end{bmatrix} = \begin{bmatrix} 1 & 0\end{bmatrix}$$

The values flipped back!! This is exactly the behaviour we need from a matrix. Now we only have to find a way to fit our recycler and assembler matrices in $M$.

### Assembling the transition matrix

We have everything we need to assemble our transition matrix $T$. Let's assume that the assembler have a productivity of 50% and both the assembler and the recycler have a quality chance of 25%. Let's also assume that we only want to keep the legendary items of the loop. $A$ and $R$ would look like this:

```python
# The code and logic behind custom_production_matrix()
# was already explained in previous blog posts.

print(custom_production_matrix([(25, 1.5)] * 5)) # Assembler
print(custom_production_matrix([(25, 0.25)] * 4 + [(0, 0)])) # Recycler
```

$$ A = \begin{bmatrix}
  1.125 & 0.3375 & 0.03375 & 0.003375 & 0.000375\\\\
  0     & 1.125  & 0.3375  & 0.03375  & 0.00375 \\\\
  0     & 0      & 1.125   & 0.3375   & 0.0375  \\\\
  0     & 0      & 0       & 1.125    & 0.375   \\\\
  0     & 0      & 0       & 0        & 1.5     \\\\
\end{bmatrix} $$

$$ R = \begin{bmatrix}
  0.1875 & 0.05625 & 0.005625 & 0.0005625 & 0.0000625\\\\
  0      & 0.1875  & 0.05625  & 0.005625  & 0.000625 \\\\
  0      & 0       & 0.1875   & 0.05625   & 0.00625  \\\\
  0      & 0       & 0        & 0.1875    & 0.0625   \\\\
  0      & 0       & 0        & 0         & 0        \\\\
\end{bmatrix} $$

Notice how the last row of $R$ is zeroed out so that legendary items are removed from our system. Let's design our transition matrix $T$ (with some inspiration from the stochastic matrix we created in the previous subchapter):

$$T = \begin{bmatrix}
  0 & A \\\\
  R & 0 \\\\
\end{bmatrix} $$

The only thing left to do is to replace $A$ and $R$ with actual values:

```python
def custom_transition_matrix(
        recycler_matrix : np.ndarray,
        assembler_matrix : np.ndarray) -> np.ndarray:
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

if __name__ == "__main__":
    print(custom_transition_matrix(
        custom_production_matrix([(25, 0.25)] * 4 + [(0, 0)]),
        custom_production_matrix([(25, 1.5)] * 5)
    ))

# [[0.     0.      0.       0.        0.        1.125 0.3375 0.03375 0.003375 0.000375]
#  [0.     0.      0.       0.        0.        0.    1.125  0.3375  0.03375  0.00375 ]
#  [0.     0.      0.       0.        0.        0.    0.     1.125   0.3375   0.0375  ]
#  [0.     0.      0.       0.        0.        0.    0.     0.      1.125    0.375   ]
#  [0.     0.      0.       0.        0.        0.    0.     0.      0.       1.5     ]
#  [0.1875 0.05625 0.005625 0.0005625 0.0000625 0.    0.     0.      0.       0.      ]
#  [0.     0.1875  0.05625  0.005625  0.000625  0.    0.     0.      0.       0.      ]
#  [0.     0.      0.1875   0.05625   0.00625   0.    0.     0.      0.       0.      ]
#  [0.     0.      0.       0.1875    0.0625    0.    0.     0.      0.       0.      ]
#  [0.     0.      0.       0.        0.        0.    0.     0.      0.       0.      ]]
```

$$ T = $$

$$\small{\begin{bmatrix}
  0      & 0       & 0        & 0         & 0         & 1.125 & 0.3375 & 0.03375 & 0.003375 & 0.000375 \\\\
  0      & 0       & 0        & 0         & 0         & 0     & 1.125  & 0.3375  & 0.03375  & 0.00375  \\\\
  0      & 0       & 0        & 0         & 0         & 0     & 0      & 1.125   & 0.3375   & 0.0375   \\\\
  0      & 0       & 0        & 0         & 0         & 0     & 0      & 0       & 1.125    & 0.375    \\\\
  0      & 0       & 0        & 0         & 0         & 0     & 0      & 0       & 0        & 1.5      \\\\
  0.1875 & 0.05625 & 0.005625 & 0.0005625 & 0.0000625 & 0     & 0      & 0       & 0        & 0        \\\\
  0      & 0.1875  & 0.05625  & 0.005625  & 0.000625  & 0     & 0      & 0       & 0        & 0        \\\\
  0      & 0       & 0.1875   & 0.05625   & 0.00625   & 0     & 0      & 0       & 0        & 0        \\\\
  0      & 0       & 0        & 0.1875    & 0.0625    & 0     & 0      & 0       & 0        & 0        \\\\
  0      & 0       & 0        & 0         & 0         & 0     & 0      & 0       & 0        & 0        \\\\
\end{bmatrix}} $$

$T$ matches[^4] the matrix in the [Factorio wiki](https://wiki.factorio.com/Quality), which is a good sign that we generated this matrix correctly:

<div style="text-align:center">
    <img src="/images/transition-matrix-table-wiki.png" alt="Blue circuit crafting recipe"/>
    <figcaption> Example of a transition matrix with P=50% and Q=25% from the <a href="https://wiki.factorio.com/Quality">Factorio Wiki</a>.</figcaption>
</div>

[^4]: The `1` in the bottom right corner of the wiki table doesn't match with $T$. This is expected because I calculate the production rates in a slightly different way than most people do.

## Calculating the production rate of the infinite recycler-assembler line

In order to get the total production of Q5 items (or ingredients), we have to add the Q5 production of every single recycler and assembler in the infinite chain:

$$ \vec{t} = \vec{t_0} + \vec{t_1} + \vec{t_2} + \vec{t_3} + \vec{t_4} + \vec{t_n} + \vec{t_{n+1}} + ...$$

$\vec{t_x}$ is the state of the system after one loop of recycler and assembler $x$. The first five values represent the ingredients in the system (from Q0 in the first position to Q5 in the fifth position) and the last five values represent the items in the system (with the quality increasing from left to right likewise). $\vec{t_x}$ can be calculated in the following manner:

$$ \vec{t_x} = \vec{t_{x-1}} \cdot T $$

$T$ is the previously discussed transition matrix. $ \vec{t_0} $ represents the input of the system, which by default will be a belt of common items:

$$ \vec{t_0} = \begin{bmatrix} 1 & 0 & 0 & 0 & 0 & 0 & 0 & 0 & 0 & 0 \end{bmatrix} $$

We have everything needed to start simulating a recycler-assembler loop. Luckily for us, the code that was written to simulate [pure recycler loops](/posts/factorio-pure-recycler-loop/#calculating-the-production-rate-of-the-infinite-recycler-line) won't require too many tweaks to work with transition matrices:

```python
input_vector = np.array([1] + [0] * 9)

# Transition matrix from the previous chapter
transition_matrix = custom_transition_matrix(
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
# [1.26732673 0.20327419 0.08342292 0.02966277 0.00632351 1.42574257 0.65640623 0.2052281  0.07266358 0.02497469]
```

Simulating a recycler-assembler loop with the transition matrix from the previous chapter yields the following result:

$$\vec{t} = \begin{bmatrix} 1.267 & 0.203 & 0.083 & 0.0297 & 0.0063 & 1.426 & 0.656 & 0.205 & 0.073 & 0.02497 \end{bmatrix}$$

Feeding 1 belt of ingredients to this loop will result in 0.02497 belts of legendary items being produced. In other words, this setup has an efficiency of 2.497%.

These are the fundamentals of simulating recycler-assembler loops. Now, all that remains to be done is to determine which questions about this quality grinding method should be asked, and how to answer them with the tools we have at our disposal.

## The recycler_assembler_loop() function

The simulation from the previous chapter works well enough for simple cases. Nevertheless, if we wish to simulate tons of variations in setups, we'll need a solution that doesn't involve having to manually calculate the productivity and quality chance of every row of the transition matrix.

Having felt the need for a higher level approach so that I could stop dealing with productivity bonuses and quality chances, and start dealing with which modules go into which assemblers, I came up with the following function:

```python
def recycler_assembler_loop(
        input_vector : Union[np.array, float],
        assembler_modules_config : Union[Tuple[float, float], List[Tuple[float, float]]],
        items_quality_to_keep : Union[int, None] = 5,
        ingredients_quality_to_keep : Union[int, None] = 5,
        base_prod_bonus : float = 0,
        recipe_ratio : float = 1,
        prod_module_bonus : float = 25,
        qual_module_bonus : float = 6.2) -> np.array:
    # The docstring was cut from this code snippet for the sake of brevity

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
```

For the sake of keeping the the code snippet above as short as possible, I decided to leave out the auxiliary functions `get_recycler_parameters` and `get_assembler_parameters`, and the docstring for the function above[^5]. Given that we don't have a doc string, I will instead explain below what this function does in writing.

[^5]: All the code written for this series of blog posts can be found [here](https://github.com/dfamonteiro/blog/tree/main/Factorio%20Quality).

### recycler_assembler_loop() details

The `recycler_assembler_loop()` function simulates a recycler-assembler loop by unrolling the loop into a line and processing the ingredients and items in the system until there is nothing left. The successive states of the system are then all added up and returned as a vector with 10 values. The first five values represent the ingredients and the last five values represent the items, going from common to legendary.

The values of the vector mean different things, depending on whether items/ingredients of that quality are kept or reprocessed be the system:

- **If they are kept**: the value is the production rate of ingredients/items of that quality level.
- **If they are reprocessed**: the value is the internal flow rate of ingredients/items of that quality level in the system.

This function has eight arguments:

- `input_vector`: The input vector of the system.
- `assembler_modules_config`: The module configuration (i.e. number of productivity and quality modules) of the assemblers of the system. Some examples:
  - Let's say that you want to load every assembler with quality modules: `(0, 4)`.
  - Now let's say that we want to load the legendary item crafter with productivity modules instead: `[(4, 0), (4, 0), (4, 0), (4, 0), (0, 4)]`.
- `items_quality_to_keep`: Minimum quality level of the items to be removed from the system. If set to `None`, nothing is removed and all items are reprocessed.
- `ingredients_quality_to_keep`: Minimum quality level of the ingredients to be removed from the system. If set to `None`, nothing is removed and all ingredients are reprocessed.
- `base_prod_bonus`: Base productivity of assembler + productivity technologies (as a %).
- `recipe_ratio` Ratio of items to ingredients of the crafting recipe.
- `prod_module_bonus`: Productivity bonus from productivity modules. Defaults to 25%.
- `qual_module_bonus`: Quality chance bonus from quality modules. Defaults to 6.2%.

With this function, we will be able to easily calculate the efficiency of every possible module allocation in the assemblers of the loop.
