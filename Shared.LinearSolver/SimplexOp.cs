using System;
using Shared.LinearSolver.UnitTests.Debug;

namespace Shared.LinearSolver
{
    /// <summary>
    /// Tools to help solve a linear system with constraints represented as a matrix.
    /// </summary>
    /// <remarks>
    /// Conventions:
    /// * Matrix addressing is row,column.
    /// * c is the constraint number, ie. the row number of the matrix.
    ///   * Now actually just the row number, as rows 0 and 1 are used for the optimisation targets.
    /// * i is the coefficient index, ie. the column number of the matrix.
    /// </remarks>
    internal static class SimplexOp
    {
        /// <summary>
        /// Return the ratio of the specified cell to the target value in the same row.
        /// </summary>
        /// <remarks>
        /// * Used to extract solution values of basic variables.
        /// * Used as per Bland's Rule for choosing pivots.
        /// </remarks>
        public static float Score(ref Tableau tableau, int row, int column) => tableau.Matrix[row, tableau.TargetColumn] / tableau.Matrix[row, column];

        public static bool MaximiseStep(ref Tableau tableau, IDebugWriter debugWriter)
        {
            // Eliminate each variable from all but one row.
            // Must include surplus variables in this.
            for (var i = 0; i < tableau.SolveFor; i++)
            {
                var eliminateCoefficient = tableau.Matrix[Tableau.Phase2ObjectiveRow, i];
                if (eliminateCoefficient >= 0) continue;   // Nothing to eliminate in row 0?

                if (!TrySelectPivotRow(ref tableau, i, out var leavingRow, debugWriter)) continue;
                if (TryPivot(ref tableau, i, leavingRow, debugWriter)) return true;
                // Nothing to eliminate?
            }
            return false;
        }

        public static BasicSolutionType CollectSolution(ref Tableau tableau, float[] solution)
        {
            var type = BasicSolutionType.Unique;
            for (var i = 0; i < tableau.VariableCount; i++)
            {
                if (TryGetBasicSolution(ref tableau, i, out solution[i]) == BasicSolutionType.NotUnique)
                {
                    type = BasicSolutionType.NotUnique;
                }
            }
            return type;
        }

        public static bool TryPivot(ref Tableau tableau, int enteringColumn, int leavingRow, IDebugWriter debugWriter)
        {
            var leavingColumn = tableau.BasicVariables[leavingRow];
            if (leavingColumn == enteringColumn) throw new InvalidOperationException("Entering and leaving variables must be different.");

            debugWriter?.WritePivot(tableau, leavingRow, enteringColumn);
            MatrixOp.Pivot(tableau.Matrix, enteringColumn, leavingRow);

            tableau.BasicVariables[leavingRow] = enteringColumn;
            tableau.Reduce();
            return true;
        }


        private static bool TrySelectPivotRow(ref Tableau tableau, int pivotColumn, out int pivotRow, IDebugWriter debugWriter)
        {
            pivotRow = 0;
            var best = float.MaxValue;
            for (var c = Tableau.FirstConstraintRow; c < tableau.RowCount; c++)
            {
                if (tableau.Matrix[c, pivotColumn] == 0) continue;
                if (tableau.BasicVariables[c] == pivotColumn) continue; // Cannot pivot a variable on itself.
                var candidate = Score(ref tableau, c, pivotColumn);
                debugWriter?.Write($"Candidate row: {tableau.GetRowName(c)} = {candidate} (pivot {tableau.Matrix[c, pivotColumn]})");

                // We don't care whether the candidate score is positive, only that the pivot coefficient is.
                if (tableau.Matrix[c, pivotColumn] < 0) continue;
                if (candidate >= best) continue;
                if (!CheckValidityOfBasicResult(tableau, c, pivotColumn, debugWriter)) continue;

                best = candidate;
                pivotRow = c;
            }
            return pivotRow != 0;
        }

        public static bool TrySelectPivotColumn(ref Tableau tableau, int pivotRow, out int pivotColumn, IDebugWriter debugWriter)
        {
            pivotColumn = -1;
            var best = float.MaxValue;
            for (var i = 0; i < tableau.SolveFor; i++)
            {
                if (tableau.Matrix[pivotRow, i] == 0) continue;
                if (tableau.BasicVariables[pivotRow] == i) continue; // Cannot pivot a variable on itself.
                var candidate = Score(ref tableau, pivotRow, i);
                debugWriter?.Write($"Candidate column: {tableau.GetVariableName(i)} = {candidate} (pivot {tableau.Matrix[pivotRow, i]})");

                // We don't care whether the candidate score is positive, only that the pivot coefficient is.
                if (tableau.Matrix[pivotRow, i] < 0) continue;
                if (candidate >= best) continue;
                if (!CheckValidityOfBasicResult(tableau, pivotRow, i, debugWriter)) continue;

                best = candidate;
                pivotColumn = i;
            }
            return pivotColumn != -1;
        }

        private static bool CheckValidityOfBasicResult(Tableau tableau, int pivotRow, int pivotColumn, IDebugWriter debugWriter)
        {
            var pivotCoefficient = tableau.Matrix[pivotRow, pivotColumn];
            for (var targetRow = Tableau.FirstConstraintRow; targetRow < tableau.RowCount; targetRow++)
            {
                if (targetRow == pivotRow) continue;
                var i = tableau.BasicVariables[targetRow];

                // See MatrixOp.Pivot.

                var eliminateCoefficient = tableau.Matrix[targetRow, pivotColumn];

                var postPivotValue = (tableau.Matrix[targetRow, i] * pivotCoefficient) - (tableau.Matrix[pivotRow, i] * eliminateCoefficient);
                var postPivotTarget = (tableau.Matrix[targetRow, tableau.TargetColumn] * pivotCoefficient) - (tableau.Matrix[pivotRow, tableau.TargetColumn] * eliminateCoefficient);

                // We only care about the sign of the Score, not the actual value, so use multiplication instead of division here.
                if (postPivotValue * postPivotTarget < 0)
                {
                    debugWriter?.Write($"  Invalid: would leave {tableau.GetVariableName(i)} negative");
                    return false;
                }
            }
            return true;
        }

        public static BasicSolutionType TryGetBasicSolution(ref Tableau tableau, int variable, out float value)
        {
            value = 0;
            if (tableau.Matrix[tableau.ObjectiveRow, variable] != 0) return BasicSolutionType.NonBasicVariable;
            if (Array.IndexOf(tableau.BasicVariables, variable, Tableau.FirstConstraintRow) < 0) return BasicSolutionType.NonBasicVariable;

            var alreadyGotValue = false;
            for (var c = Tableau.FirstConstraintRow; c < tableau.RowCount; c++)
            {
                if (tableau.Matrix[c, variable] == 0) continue;
                value = Score(ref tableau, c, variable);
                if (alreadyGotValue) return BasicSolutionType.NotUnique;
                alreadyGotValue = true;
            }
            return BasicSolutionType.Unique;
        }
    }

    public enum BasicSolutionType
    {
        Unique,
        NotUnique,
        NonBasicVariable,
    }
}
