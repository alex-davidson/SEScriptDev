# Two-Phase Simplex Algorithm

* [Overview](Simplex.md)
* [Pivoting](Simplex.Pivot.md)
* [Simple Case: Maximise](Simplex.SimpleCaseMaximise.md)
* Simple Case: Minimise
* [General Case](Simplex.GeneralCase.md)

## Process (Simple Case, Minimise)

The process for minimising an objective function is almost identical to maximising it. Let us
consider our previous problem, but try to minimise $Z$ instead:

* $x_1 + x_2 \le 30$
* $2x_1 + 8x_2 \le 80$
* $x_1 \le 15$

Minimising $Z$:

$Z = 5x_1 - 3x_2$

### Construct the Simplex Tableau

As before, we have the initial tableau:

$$
\begin{array}{c|cccccc|c}
        & x_1 & x_2 & s_1 & s_2 & s_3 &  Z       \\
    \hline
    Z   & -5  &  3  &  0  &  0  &  0  &  1  &  0 \\
    \hline
    s_1 &  1  &  1  &  1  &  0  &  0  &  0  & 30 \\
    s_2 &  2  &  8  &  0  &  1  &  0  &  0  & 80 \\
    s_3 &  1  &  0  &  0  &  0  &  1  &  0  & 15 \\
\end{array}
$$

Our initial basic solution is still valid:

* $Z = 0$
* $s_1 = 30$
* $s_2 = 80$
* $s_3 = 15$

So Phase $I$ is still not required.

### Prepare for Phase $II$

Consider the objective function again, and our observations from the previous example:

$Z - 5x_1 + 3x_2 = 0$

* If we allow $x_1$ to take a positive value, $Z$ must increase.
* If we allow $x_2$ to take a positive value, $Z$ must decrease.

We are trying to decrease $Z$, so clearly we want to pivot on $x_2$.

> :notebook: Once again, note the sign. Here, we are trying to eliminate all *positive* values in our
> objective row.

What happens if we multiply the objective row by $-1$?

$$
\begin{array}{c|cccccc|c}
        & x_1 & x_2 & s_1 & s_2 & s_3 &  Z       \\
    \hline
    Z   &  5  & -3  &  0  &  0  &  0  & -1  &  0 \\
    \hline
    s_1 &  1  &  1  &  1  &  0  &  0  &  0  & 30 \\
    s_2 &  2  &  8  &  0  &  1  &  0  &  0  & 80 \\
    s_3 &  1  &  0  &  0  &  0  &  1  &  0  & 15 \\
\end{array}
$$

This corresponds to $5x_1 - 3x_2 - Z = 0$

The same logic holds:

* If we allow $x_1$ to take a positive value, $Z$ must increase.
* If we allow $x_2$ to take a positive value, $Z$ must decrease.

Except now we have a negative coefficient for $x_2$, and we can apply the same rules as for maximisation! In
effect, we are maximising $-Z$.

Therefore, a more robust rule for which columns to pivot must take into account the sign of $Z$'s coefficient:

* Eliminating coefficients of the same sign as $Z$'s will *minimise* $Z$.
* Eliminating coefficients of the opposite sign from $Z$'s will *maximise* $Z$.

For this reason it is common for explanations (and implementations) of this algorithm to prefer either maximisation
or minimisation, adjusting the objective function accordingly.

### Phase $II$, as maximisation

Let's use our inverted objective row in order to treat this as a maximisation problem and solve it accordingly.

$x_2$ entering, using Bland's rule:

$$
\begin{array}{c|cccccc|c|c}
        & x_1 & \columncolor[gray]{0.9} x_2 & s_1 & s_2 & s_3 &  Z       &  \beta       \\
    \hline
    Z   &  5  & -3  &  0  &  0  &  0  & -1  &  0 &              \\
    \hline
    s_1 &  1  &  1  &  1  &  0  &  0  &  0  & 30 &  30 / 1 = 30 \\
    s_2 &  2  &  8  &  0  &  1  &  0  &  0  & 80 &  80 / 8 = 10 \\
    s_3 &  1  &  0  &  0  &  0  &  1  &  0  & 15 &  15 / 0 = ! \\
\end{array}
$$

$s_2$ leaves and $x_2$ enters:

$$
\begin{array}{c|cccccc|c}
        & x_1 & \columncolor[gray]{0.9} x_2 & s_1 & s_2 & s_3 &  Z        \\
    \hline
    Z   &  46 &  0  &  0  &  3  &  0  & -8  & 240 \\
    \hline
    s_1 &  6  &  0  &  8  & -1  &  0  &  0  & 160 \\
\rowcolor[gray]{0.9}
    x_2 &  2  &  8  &  0  &  1  &  0  &  0  &  80 \\
    s_3 &  1  &  0  &  0  &  0  &  1  &  0  &  15 \\
\end{array}
$$

Basic solution:

* $Z = -30$
* $s_1 = 20$
* $x_2 = 10$
* $s_3 = 15$

> :notebook: $Z$ is not required to be nonnegative as it is an output.

The objective row no longer contains any negative values, so we have the optimal solution:

* $x_1 = 0$ (non-basic variable)
* $x_2 = 10$
* $Z = -30$

### Phase $II$, as minimisation

Let's try again with our original tableau, eliminating positive values in the objective row.

$$
\begin{array}{c|cccccc|c}
        & x_1 & x_2 & s_1 & s_2 & s_3 &  Z       \\
    \hline
    Z   & -5  &  3  &  0  &  0  &  0  &  1  &  0 \\
    \hline
    s_1 &  1  &  1  &  1  &  0  &  0  &  0  & 30 \\
    s_2 &  2  &  8  &  0  &  1  &  0  &  0  & 80 \\
    s_3 &  1  &  0  &  0  &  0  &  1  &  0  & 15 \\
\end{array}
$$

$x_2$ entering, using Bland's Rule:

$$
\begin{array}{c|cccccc|c|c}
        & x_1 & \columncolor[gray]{0.9} x_2 & s_1 & s_2 & s_3 &  Z       &  \beta       \\
    \hline
    Z   & -5  &  3  &  0  &  0  &  0  &  1  &  0 &              \\
    \hline
    s_1 &  1  &  1  &  1  &  0  &  0  &  0  & 30 &  30 / 1 = 30 \\
    s_2 &  2  &  8  &  0  &  1  &  0  &  0  & 80 &  80 / 8 = 10 \\
    s_3 &  1  &  0  &  0  &  0  &  1  &  0  & 15 &  15 / 0 = ! \\
\end{array}
$$

As before, $s_2$ leaves and $x_2$ enters:

$$
\begin{array}{c|cccccc|c}
        & x_1 & \columncolor[gray]{0.9} x_2 & s_1 & s_2 & s_3 &  Z        \\
    \hline
    Z   & -46 &  0  &  0  & -3  &  0  &  8  & -240 \\
    \hline
    s_1 &  6  &  0  &  8  & -1  &  0  &  0  &  160 \\
\rowcolor[gray]{0.9}
    x_2 &  2  &  8  &  0  &  1  &  0  &  0  &   80 \\
    s_3 &  1  &  0  &  0  &  0  &  1  &  0  &   15 \\
\end{array}
$$

Basic solution:

* $Z = -30$
* $s_1 = 20$
* $x_2 = 10$
* $s_3 = 15$

Exactly the same.
