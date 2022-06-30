# Two-Phase Simplex Algorithm

* Overview
* [Pivoting](Simplex.Pivot.md)
* [Simple Case: Maximise](Simplex.SimpleCaseMaximise.md)
* [Simple Case: Minimise](Simplex.SimpleCaseMinimise.md)
* [General Case](Simplex.GeneralCase.md)
* [Back to README](../README.md)

The Simplex algorithm is a technique used in linear programming, for solving optimisation problems
in a system of linear equations.

Strictly speaking what I will be explaining is called Two-Phase Simplex, which is capable of dealing
with a wider variety of constraints and starting conditions than the 'standard' Simplex.

Other variants of Simplex exist which are similarly capable, eg. Big-M Simplex. They will not be
considered here; Two-Phase Simplex is traditionally preferred for implementation as a computer program
as it can operate only with numbers and doesn't need to track concepts like 'an arbitrarily large value'.

## Disclaimer, etc

* My grasp of mathematical jargon is poor.
* My understanding of Simplex comes from writing an implementation in C# and throwing test cases at it in
  order to understand how to put the pieces together for correct results. I may have made errors which
  my test cases do not detect.
* My goal here is to document 'what seems to work' and 'what I learned along the way which turned out
  apparently wrong'.
* I do not try to explain *why* this algorithm works, as I don't have an intuitive grasp of that yet.

## Overview

Inputs:
* one or more variables $x_1 \dotsc x_n$
* one or more constraints, defined in terms of linear functions of those variables
* an objective function, defined as a linear function of those variables

Outputs:
* values of variables $x_1 \dotsc x_n$ which maximise (or minimise) the result of the objective function
* the maximal (or minimal) value of that objective function

Restrictions:
* the input variables $x_1 \dotsc x_n$ must take only nonnegative values
* for a unique result, there must be at least as many constraints as variables

Constraints take forms such as the following:
* $x_1 + 6x_2 \le 10$
* $3x_2 - 2x_4 = 8$
* $-2x_3 + x_4 \ge 5$

That is:
* variables on one side,
* target value (constant) on the other,
* comparison operator must be $\ge$, $=$ or $\le$; eg. 'strictly greater than' $\gt$ isn't allowed.

The objective function should look similar:

$Z = x_1 + 3x_2 - x_4 - 2x_5$

Our goal is to either maximise or minimise $Z$.
