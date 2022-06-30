# Two-Phase Simplex Algorithm

* [Overview](Simplex.md)
* Pivoting
* [Simple Case: Maximise](Simplex.SimpleCaseMaximise.md)
* [Simple Case: Minimise](Simplex.SimpleCaseMinimise.md)
* [General Case](Simplex.GeneralCase.md)

## Pivoting

The matrix operation we will use frequently throughout the algorithm is the 'pivot'. The general aim of a pivot
is to zero out all but one cell in a column, by subtracting multiples of that row from the other rows.

> :notebook: Remember that two matrices are *equivalent* if they can be transformed into one another using
> elementary row operations:
> * Any row may be swapped with any other row.
> * An entire row may be freely multiplied or divided by a non-zero constant (scaling).
> * A row may be replaced by the sum of that row and a multiple of another row.

For example:

$$
\begin{array}{ccc|c}
     2 &  4 &  6 & 2 \\
     3 & -1 &  2 & 4 \\
    -3 &  6 & -2 & 0 \\
\end{array}
$$

Let us select row 1 as our *pivot row* and column 2 as the *pivot column*.

To eliminate the $-1$ in row 2, we can multiply that row by four and then add row 1 (or subtract it minus-one times):

$$
\begin{array}{ccc|c}
     2  & 4 &   6 &  2 \\
\rowcolor[gray]{0.9} 14 &  0 &  14 & 18 \\
    -3  &  6 &  -2 &  0 \\
\end{array}
$$

To eliminate the $6$ in row 3, we can multiply that row by four and subtract row 1 multiplied by six:

$$
\begin{array}{ccc|c}
     2  &  4 &   6 &  2 \\
     14 &  0 &  14 & 18 \\
\rowcolor[gray]{0.9} -24  & 0 & -44 &  0 \\
\end{array}
$$

After the pivot, only the pivot row has a nonzero value in the pivot column.

> :notebook: Subtracting row 1 multiplied by $6/4$ is basically equivalent. Using multiplication only is preferable
> from the point of view of numerical stability when implementing this for a computer, but may require periodic
> reduction by $gcd(row)$ to stop the numbers becoming huge.
