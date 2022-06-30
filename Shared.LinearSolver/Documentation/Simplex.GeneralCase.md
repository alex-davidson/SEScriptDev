# Two-Phase Simplex Algorithm

* [Overview](Simplex.md)
* [Pivoting](Simplex.Pivot.md)
* [Simple Case: Maximise](Simplex.SimpleCaseMaximise.md)
* [Simple Case: Minimise](Simplex.SimpleCaseMinimise.md)
* General Case

## Process (General Case)

Our previous examples started with a valid basic solution and Phase $I$ was not required. 

For this example we will be solving the following:

* $x_1 + x_2 = 30$
* $2x_1 + 8x_2 \ge 70$
* $x_1 \le 15$

Maximising $Z$:

$Z = 5x_1 + 3x_2$

### Construct the Simplex Tableau

Previously we only dealt with $\le$ constraints, which can be converted to equality through the addition of
slack variables. We are now dealing with a *mixed-constraint problem*, which requires additional consideration.

#### Restate the problem

Simplex requires that all input variables take nonnegative values:

* $Z = 5x_1 + 3x_2$ becomes $Z - 5x_1 - 3x_2 = 0$
* $x_1 + x_2 = 30$ is already in 'equals' form.
* $2x_1 + 8x_2 \ge 70$ becomes $2x_1 + 8x_2 - s_1 = 70$
* $x_1 \le 15$ becomes $x_1 + s_2 = 15$

In this case, $s_1$ is the margin by which the function's result is *greater than* the constant. This is known
as a 'surplus' variable.

> :notebook: The terms 'slack' and 'surplus' have specific meanings:
> * If it is *added* (eg. for $x_1 + s_2$) it is a 'slack' variable.
> * If it is *subtracted* (eg. for $2x_1 + 8x_2 - s_1$) it is a 'surplus' variable.
>
> The difference is not usually significant and the terms seem to be used interchangeably.

The matrix form of this problem now looks like this:

$$
\begin{array}{c|ccccc|c}
        & x_1 & x_2 & s_1 & s_2 & Z       \\
    \hline
    Z   & -5  & -3  &  0  &  0  & 1  & 0  \\
    \hline
    -   &  1  &  1  &  0  &  0  & 0  & 30 \\
    -   &  2  &  8  & -1  &  0  & 0  & 70 \\
    s_2 &  1  &  0  &  0  &  1  & 0  & 15 \\
\end{array}
$$

$s_1$ and $s_2$ are our initial basic variables.

> :notebook: Recall that the initial row labels on the left correspond to the basic variable which has a
> *positive* value in that row and zero in all the others. In this case, only the final row has such a
> correspondence. This immediately indicates that our basic solution is probably not valid, as Phase $II$
> cannot commence until all rows have such a correspondence.

> :notebook: This is a slight oversimplification: the sign criterion for a row's basic variable is really that the
> value *has the same sign as the target value*. But it is usually simpler to just ensure that all target values
> are initially nonnegative, by multiplying rows by $-1$ where necessary.

#### Check the basic solution

Setting $x_1$ and $x_2$ to zero gives us:

* $Z = 0$
* $0 = 30$  :no_entry:
* $-s_1 = 70 \rightarrow s_1 = -70$  :no_entry:
* $s_2 = 15$

This is clearly nonsense as $0 \ne 30$. But $s_1$ is required to be nonnegative, so that
result is invalid as well.

:warning: **This matrix represents an invalid basic solution.** This is almost always the case when
you start with an equality constraint or certain patterns of inequality constraint, ie. if
any row lacks a 1-to-1 correspondence with a basic variable.

This must be fixed before we can begin optimising.

#### Prepare for Phase $I$

The goal of Phase $I$ is to get us to a valid basic solution. We do this by adding some more
variables, then pivoting to eliminate them.

For our two invalid rows, we add new (nonnegative) *artificial variables*:

* $x_1 + x_2 + a_1 = 30$
* $2x_1 + 8x_2 - s_1 + a_2 = 70$

But for a valid solution, both of these must be zero. We can express this with a new objective function
in which all variables must be zero:

$a_1 + a_2 + Z' = 0$

> :notebook: In practice, when the problem has valid solutions it does not seem to matter whether $Z^{\prime}$'s
> coefficient is $+1$ or $-1$. A nonzero result's meaning may depend on interpreting the sign properly, though.

$$
\begin{array}{c|cccccccc|c}
            & x_1 & x_2 & s_1 & s_2 & a_1 & a_2 &  Z  &  Z'      \\
    \hline
     I (Z') &  0  &  0  &  0  &  0  &  1  &  1  &  0  &  1  &  0 \\
    II (Z)  & -5  & -3  &  0  &  0  &  0  &  0  &  1  &  0  &  0 \\
    \hline
    -       &  1  &  1  &  0  &  0  &  1  &  0  &  0  &  0  & 30 \\
    -       &  2  &  8  & -1  &  0  &  0  &  1  &  0  &  0  & 70 \\
    s_2     &  1  &  0  &  0  &  1  &  0  &  0  &  0  &  0  & 15 \\
\end{array}
$$

The objective rows have been labelled according to their Phase. We are currently trying to solve $I$, but we
must also perform operations on $II$ as we proceed.

We want $a_1$ and $a_2$ to be basic variables in order to yield a valid solution, so now we pivot twice to
leave zeroes in the top row. Because their coefficients are all 1, this is equivalent to simply subtracting
the first and second constraint row from the Phase $I$ row:

$$
\begin{array}{c|cccccccc|c}
            & x_1 & x_2 & s_1 & s_2 & a_1 & a_2 &  Z  &  Z'        \\
    \hline
\rowcolor[gray]{0.9} 
     I (Z') & -3  & -9  &  1  &  0  &  0  &  0  &  0  &  1  & -100 \\
    II (Z)  & -5  & -3  &  0  &  0  &  0  &  0  &  1  &  0  &    0 \\
    \hline
\rowcolor[gray]{0.9} 
    a_1     &  1  &  1  &  0  &  0  &  1  &  0  &  0  &  0  &   30 \\
\rowcolor[gray]{0.9} 
    a_2     &  2  &  8  & -1  &  0  &  0  &  1  &  0  &  0  &   70 \\
    s_2     &  1  &  0  &  0  &  1  &  0  &  0  &  0  &  0  &   15 \\
\end{array}
$$

Since $a_1$ and $a_2$ are now basic variables, we can fill in the associations of the first two rows.

Checking the basic solution again:

* $Z' = -100$
* $Z = 0$
* $a_1 = 30$
* $a_2 = 70$
* $s_2 = 15$

**These are all reasonable, and fit the requirement of nonnegative inputs.** Therefore we have a valid basic
solution, although it's not the one we want because $a_1$ and $a_2$ are nonzero.

We can also check that $a_1 + a_2 + Z' = 0 \rightarrow 30 + 70 - 100 = 0$

> :notebook: As with $Z$, $Z'$ is an output and is not required to be nonnegative. This is largely irrelevant
> though as we actually want it to be zero.

We can now pivot to maximise $Z'$, squeezing our artificial variables to zero.

### Phase $I$: Pivot the tableau to eliminate all artificial variables

We want to force out $a_1$ and $a_2$ so that they become nonbasic and therefore zero.

> :notebook: This time, we are starting with a pivot row and then selecting a pivot column. However, Bland's
> Rule is still applicable.

#### Eliminate $a_1$, first attempt

$$
\begin{array}{c|cccccccc|c}
            & x_1 & x_2 & s_1 & s_2 & a_1 & a_2 &  Z  &  Z'        \\
    \hline
     I (Z') & -3  & -9  &  1  &  0  &  0  &  0  &  0  &  1  & -100 \\
    II (Z)  & -5  & -3  &  0  &  0  &  0  &  0  &  1  &  0  &    0 \\
    \hline
\rowcolor[gray]{0.9}
    a_1     &  1  &  1  &  0  &  0  &  1  &  0  &  0  &  0  &   30 \\
    a_2     &  2  &  8  & -1  &  0  &  0  &  1  &  0  &  0  &   70 \\
    s_2     &  1  &  0  &  0  &  1  &  0  &  0  &  0  &  0  &   15 \\
    \hline
    \beta   & 30  & 30  \\
\end{array}
$$

Try $x_1$:

$$
\begin{array}{c|cccccccc|c}
            & \columncolor[gray]{0.9} x_1 & x_2 & s_1 & s_2 & a_1 & a_2 &  Z  &  Z'        \\
    \hline
     I (Z') &  0  & -6  &  1  &  0  &  3  &  0  &  0  &  1  &  -10 \\
    II (Z)  &  0  &  2  &  0  &  0  &  5  &  0  &  1  &  0  &  150 \\
    \hline
\rowcolor[gray]{0.9}
    x_1     &  1  &  1  &  0  &  0  &  1  &  0  &  0  &  0  &   30 \\
    a_2     &  0  &  6  & -1  &  0  & -2  &  1  &  0  &  0  &   10 \\
    s_2     &  0  & -1  &  0  &  1  & -1  &  0  &  0  &  0  &  -15 \\
\end{array}
$$

> :notebook: The pivot operation applies to the Phase $II$ row as well, removing $x_1$ from $Z$.

* $Z' = -10$
* $x_1 = 30$
* $a_2 = 10$
* $s_2 = -15$ :no_entry:

Try $x_2$:

$$
\begin{array}{c|cccccccc|c}
            & x_1 & \columncolor[gray]{0.9} x_2 & s_1 & s_2 & a_1 & a_2 &  Z  &  Z'        \\
    \hline
     I (Z') &  6  &  0  &  1  &  0  &  9  &  0  &  0  &  1  &  170 \\
    II (Z)  & -2  &  0  &  0  &  0  &  3  &  0  &  1  &  0  &   90 \\
    \hline
\rowcolor[gray]{0.9}
    x_2     &  1  &  1  &  0  &  0  &  1  &  0  &  0  &  0  &   30 \\
    a_2     & -6  &  0  & -1  &  0  & -8  &  1  &  0  &  0  & -170 \\
    s_2     &  1  &  0  &  0  &  1  &  0  &  0  &  0  &  0  &   15 \\
\end{array}
$$

* $Z' = 170$
* $x_2 = 30$
* $a_2 = -170$ :no_entry:
* $s_2 = 15$

:warning: We cannot eliminate $a_1$ yet.

#### Eliminate $a_2$

$$
\begin{array}{c|cccccccc|c}
            & x_1 & x_2 & s_1 & s_2 & a_1 & a_2 &  Z  &  Z'        \\
    \hline
     I (Z') & -3  & -9  &  1  &  0  &  0  &  0  &  0  &  1  & -100 \\
    II (Z)  & -5  & -3  &  0  &  0  &  0  &  0  &  1  &  0  &    0 \\
    \hline
    a_1     &  1  &  1  &  0  &  0  &  1  &  0  &  0  &  0  &   30 \\
\rowcolor[gray]{0.9}
    a_2     &  2  &  8  & -1  &  0  &  0  &  1  &  0  &  0  &   70 \\
    s_2     &  1  &  0  &  0  &  1  &  0  &  0  &  0  &  0  &   15 \\
    \hline
    \beta   & 35 & 8.75 & -70 \\
\end{array}
$$

Try $x_2$:

$$
\begin{array}{c|cccccccc|c}
            & x_1 & \columncolor[gray]{0.9} x_2 & s_1 & s_2 & a_1 & a_2 &  Z  &  Z'        \\
    \hline
     I (Z') & -6  &  0  & -1  &  0  &  0  &  9  &  0  &  8  & -170 \\
    II (Z)  & -34 &  0  & -3  &  0  &  0  &  3  &  8  &  0  &  210 \\
    \hline
    a_1     &  6  &  0  &  1  &  0  &  8  & -1  &  0  &  0  &  170 \\
\rowcolor[gray]{0.9}
    x_2     &  2  &  8  & -1  &  0  &  0  &  1  &  0  &  0  &   70 \\
    s_2     &  1  &  0  &  0  &  1  &  0  &  0  &  0  &  0  &   15 \\
\end{array}
$$

* $Z' = -170/8 = -21.25$
* $a_1 = 170/8 = 21.25$
* $x_2 = 70/8 = 8.75$
* $s_2 = 15$

Progress! This solution is still valid and our value for $Z'$ has moved closer to zero.

#### Eliminate $a_1$, second attempt

$$
\begin{array}{c|cccccccc|c}
            & x_1 & x_2 & s_1 & s_2 & a_1 & a_2 &  Z  &  Z'        \\
    \hline
     I (Z') & -6  &  0  & -1  &  0  &  0  &  9  &  0  &  8  & -170 \\
    II (Z)  & -34 &  0  & -3  &  0  &  0  &  3  &  8  &  0  &  210 \\
    \hline
\rowcolor[gray]{0.9} 
    a_1     &  6  &  0  &  1  &  0  &  8  & -1  &  0  &  0  &  170 \\
    x_2     &  2  &  8  & -1  &  0  &  0  &  1  &  0  &  0  &   70 \\
    s_2     &  1  &  0  &  0  &  1  &  0  &  0  &  0  &  0  &   15 \\
    \hline
    \beta   & 28.\overline{3} & & 170 \\
\end{array}
$$

> :notebook: $a_2$ is not considered as an option. It will not be entering again, and in fact
> we can ignore it from now on.

Try $x_1$:

$$
\begin{array}{c|cccccccc|c}
            & \columncolor[gray]{0.9} x_1 & x_2 & s_1 & s_2 & a_1 & a_2 &  Z  &  Z'        \\
    \hline
     I (Z') &  0  &  0  &  0  &  0  &  8  &  8  &  0  &  8  &    0 \\
    II (Z)  &  0  &  0  &  8  &  0  & 136 & -8  & 24  &  0  & 3520 \\
    \hline
\rowcolor[gray]{0.9}
    x_1     &  6  &  0  &  1  &  0  &  8  & -1  &  0  &  0  &  170 \\
    x_2     &  0  & 24  & -4  &  0  & -8  &  4  &  0  &  0  &   40 \\
    s_2     &  0  &  0  & -1  &  6  & -8  &  1  &  0  &  0  &  -80 \\
\end{array}
$$

* $Z' = 0$
* $x_1 = 170/6 = 28.\overline{3}$
* $x_2 = 40/24 = 1.\overline{6}$
* $s_2 = -80/6 = -13.\overline{3}$ :no_entry:

:warning: While this does successfully eliminate both artificial variables and leave $Z'$ at zero, it is not a valid
solution.

Try $s_1$:

$$
\begin{array}{c|cccccccc|c}
            & x_1 & x_2 & \columncolor[gray]{0.9} s_1 & s_2 & a_1 & a_2 &  Z  &  Z'        \\
    \hline
     I (Z') &  0  &  0  &  0  &  0  &  8  &  8  &  0  &  8  &    0 \\
    II (Z)  & -16 &  0  &  0  &  0  & 24  &  0  &  8  &  0  &  720 \\
    \hline
\rowcolor[gray]{0.9}
    s_1     &  6  &  0  &  1  &  0  &  8  & -1  &  0  &  0  &  170 \\
    x_2     &  8  &  8  &  0  &  0  &  8  &  0  &  0  &  0  &  240 \\
    s_2     &  1  &  0  &  0  &  1  &  0  &  0  &  0  &  0  &   15 \\
\end{array}
$$

* $Z' = 0$
* $s_1 = 170$
* $x_2 = 240/8 = 30$
* $s_2 = 15$

**This solution is valid.** $Z'$ is zero and all artificial variables have been forced out. We can now drop
$a_1$, $a_2$ and $Z'$ from the tableau and proceed with Phase $II$:

$$
\begin{array}{c|cccccccc|c}
            & x_1 & x_2 & s_1 & s_2 & Z  &     \\
    \hline
    II (Z)  & -16 &  0  &  0  &  0  & 8  & 720 \\
    \hline
    s_1     &  6  &  0  &  1  &  0  & 0  & 170 \\
    x_2     &  8  &  8  &  0  &  0  & 0  & 240 \\
    s_2     &  1  &  0  &  0  &  1  & 0  &  15 \\
\end{array}
$$

### Phase $II$

Our original objective was to *maximise* $Z$ and its coefficient is still positive, thus we seek to eliminate
negative coefficients.

$x_1$ entering:

$$
\begin{array}{c|cccccccc|c}
            & \columncolor[gray]{0.9} x_1 & x_2 & s_1 & s_2 & Z  &     & \beta       \\
    \hline
    II (Z)  & -16 &  0  &  0  &  0  & 8  & 720 &             \\
    \hline
    s_1     &  6  &  0  &  1  &  0  & 0  & 170 & 170 / 6 = 28.\overline{3} \\
    x_2     &  8  &  8  &  0  &  0  & 0  & 240 & 240 / 8 = 30 \\
    s_2     &  1  &  0  &  0  &  1  & 0  &  15 & 15 / 1 = 15 \\
\end{array}
$$

$s_2$ leaves and $x_1$ enters:

$$
\begin{array}{c|cccccccc|c}
            & \columncolor[gray]{0.9} x_1 & x_2 & s_1 & s_2 & Z  &     \\
    \hline
    II (Z)  &  0  &  0  &  0  & 16  & 8  & 960 \\
    \hline
    s_1     &  0  &  0  &  1  & -6  & 0  &  80 \\
    x_2     &  0  &  8  &  0  & -8  & 0  & 120 \\
\rowcolor[gray]{0.9}
    x_1     &  1  &  0  &  0  &  1  & 0  &  15 \\
\end{array}
$$

* $Z = 120$
* $s_1 = 80$
* $x_2 = 15$
* $x_1 = 15$

**Solution is valid and optimal.**
