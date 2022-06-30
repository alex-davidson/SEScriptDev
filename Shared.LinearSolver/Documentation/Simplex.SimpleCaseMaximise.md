# Two-Phase Simplex Algorithm

* [Overview](Simplex.md)
* [Pivoting](Simplex.Pivot.md)
* Simple Case: Maximise
* [Simple Case: Minimise](Simplex.SimpleCaseMinimise.md)
* [General Case](Simplex.GeneralCase.md)

## Process (Simple Case, Maximise)

Applying Simplex involves the following steps:

1. *Construct the Simplex Tableau,* by converting our constraints and objective into equality functions with
   nonnegative inputs only.
2. (Phase $I$) *Pivot the tableau to eliminate all artificial variables,* leaving us with a valid basic solution.
3. (Phase $II$) *Pivot the tableau to optimise* $Z$, yielding the final result.

Hence the name: Two-Phase Simplex

In simple cases Phase $I$ may not be required. For this example we will be solving the following:

* $x_1 + x_2 \le 30$
* $2x_1 + 8x_2 \le 80$
* $x_1 \le 15$

Maximising $Z$:

$Z = 5x_1 - 3x_2$

### Construct the Simplex Tableau

Simplex is easiest to apply when the problem is presented as a matrix. This requires that we express
the entire problem in terms of equations with variables on one side and only constants on the other.

In order to do that, we add some extra variables.

#### Restate the problem

Simplex requires that all input variables take nonnegative values, and that includes any new ones we add.

* $Z = 5x_1 - 3x_2$ becomes $Z - 5x_1 + 3x_2 = 0$
* $x_1 + x_2 \le 30$ becomes $x_1 + x_2 + s_1 = 30$
* $2x_1 + 8x_2 \le 80$ becomes $2x_1 + 8x_2 + s_2 = 80$
* $x_1 \le 15$ becomes $x_1 + s_3 = 15$

$s_1 \dotsc s_3$ represent the *margin by which* the functions' results are less than the constant. These
are known as 'slack' variables, and like our $x_1 \dotsc x_n$ they are required to take nonnegative values.

The matrix form of this problem now looks like this:

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

$s_1 \dotsc s_3$ do not currently appear in our objective function (top row). They
are currently the *basic variables*. The *basic solution* is produced by setting all *non-basic
variables* ( $x_1$ and $x_2$ in this case) to zero and solving for the *basic variables*.

> :notebook: The initial row labels on the left correspond to the basic variable which
> has a positive value in that row and zero in all the others. Since all rows have such
> a correspondence *and* none of the target values are negative, this is the 'simple' case
> and our initial basic solution should be valid.

#### Check the basic solution

Setting $x_1$ and $x_2$ to zero gives us:

* $Z = 0$
* $s_1 = 30$
* $s_2 = 80$
* $s_3 = 15$

All of these are valid, even though they tell us nothing about $x_1$ or $x_2$.

> :notebook: We can visualise our input variables as axes on a graph (treat $x_2$ as $y$).
> This initial basic solution is the origin $(0,0)$ and our constraints represent lines/planes
> walling off parts of the solution space. The objective function is the direction we want
> to push our solution in.

### Phase $I$: Not required here

Because our initial basic solution is valid and no *artificial variables* are required, we can
proceed directly to Phase $II$.

> :notebook: Our slack variables $s_1 \dotsc s_3$ are not considered to be 'artificial' variables.
> See the [General Case](Simplex.GeneralCase.md) for further explanation.

### Phase $II$

To reiterate, our initial tableau looks like this:

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

Consider our objective function: $Z - 5x_1 + 3x_2 = 0$

Initially, $x_1$ and $x_2$ are zero.

* If we allow $x_1$ to take a positive value, $Z$ must increase.
* If we allow $x_2$ to take a positive value, $Z$ must decrease.

Recall that in the basic solution only basic variables are nonzero. We are trying to maximise $Z$,
we want $x_1$ to become positive, and thus we need to make $x_1$ a basic variable. This can be achieved
by *pivoting*.

> :notebook: The crucial thing to note here is the sign in the $x_1$ column of our objective row.
> We want to eliminate all *negative* values in this row.

Our goal is to leave only a single nonzero value in the $x_1$ column (the *pivot column*). We need to
choose the best row to use as the *pivot row*.

For the sake of example, let's try $s_1$:

$$
\begin{array}{c|cccccc|c}
        & \columncolor[gray]{0.9} x_1 & x_2 & s_1 & s_2 & s_3 &  Z       \\
    \hline
    Z   &  0  &  8  &  5  &  0  &  0  &  1  & 150 \\
    \hline
\rowcolor[gray]{0.9}
    x_1 &  1  &  1  &  1  &  0  &  0  &  0  &  30 \\
    s_2 &  0  &  6  & -2  &  1  &  0  &  0  &  20 \\
    s_3 &  0  & -1  & -1  &  0  &  1  &  0  & -15 \\
\end{array}
$$

We can say that $x_1$ has now *entered* as a basic variable and $s_1$ has *left*.

> :notebook: As indicated above, the signs of the values in the objective row are very important.
> If the process of pivoting changes the sign of Z, multiply the row by $-1$ to restore it.
> I don't *think* this should ever happen (maybe the preconditions require an invalid basic solution?) but
> it is important to maintain $Z$'s sign throughout the algorithm.

Is this basic solution valid?

* $Z = 150$
* $x_1 = 30$
* $s_2 = 20$
* $s_3 = -15$ :no_entry:

:warning: **Since our input variables cannot be negative this is not a valid solution,** therefore $s_1$ was the
wrong row to choose.

> :notebook: Some trial and error may be required here. There are often multiple *valid* rows to
> choose. In such a case, what is stopping us from repeatedly pivoting valid rows which don't
> improve the solution?

One way to identify a 'preferred' row is via *Bland's Rule*. For each row we calculate the ratio
of the target value to the value in the pivot column, then we prefer the row with the smallest
positive ratio:

$$
\begin{array}{c|cccccc|c|c}
        & \columncolor[gray]{0.9} x_1 & x_2 & s_1 & s_2 & s_3 &  Z  &    & \beta       \\
    \hline
    Z   & -5  &  3  &  0  &  0  &  0  &  1  &  0 &             \\
    \hline
    s_1 &  1  &  1  &  1  &  0  &  0  &  0  & 30 & 30 / 1 = 30 \\
    s_2 &  2  &  8  &  0  &  1  &  0  &  0  & 80 & 80 / 2 = 40 \\
    s_3 &  1  &  0  &  0  &  0  &  1  &  0  & 15 & 15 / 1 = 15 \\
\end{array}
$$

> :notebook: Bland's Rule was developed by Robert G. Bland. The rule avoids cycles, guaranteeing
> that the algorithm makes progress. According to Wikipedia it is not the most efficient solution,
> but it is simple and effective.

Since $s_3$ has the lowest ratio, let's try pivoting on it next:

$$
\begin{array}{c|cccccc|c}
        & \columncolor[gray]{0.9} x_1 & x_2 & s_1 & s_2 & s_3 &  Z       \\
    \hline
    Z   &  0  &  3  &  0  &  0  &  5  &  1  & 75 \\
    \hline
    s_1 &  0  &  1  &  1  &  0  & -1  &  0  & 15 \\
    s_2 &  0  &  8  &  0  &  1  & -2  &  0  & 50 \\
\rowcolor[gray]{0.9}
    x_1 &  1  &  0  &  0  &  0  &  1  &  0  & 15 \\
\end{array}
$$

Is this basic solution valid?

* $Z = 75$
* $s_1 = 15$
* $s_2 = 50$
* $x_1 = 15$

**Yes.**

> :notebook: The row 'preferred' by Bland's Rule may not always yield a viable solution, so it's
> important to check. However, working through the rows from lowest to highest ratio will save time.

Note that the objective row no longer contains any negative values. We have therefore found the
optimal solution:

* $x_1 = 15$
* $x_2 = 0$ (non-basic variable)
* $Z = 75$
