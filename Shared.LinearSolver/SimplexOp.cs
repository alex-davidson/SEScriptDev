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
        public static float Score(Tableau tableau, int row, int column) => tableau.Matrix[row, tableau.TargetColumn] / tableau.Matrix[row, column];

        public static bool MaximiseStep(Tableau tableau, IDebugWriter debugWriter)
        {
            tableau.Pivots.Clear();
            // Eliminate each variable from all but one row.
            // Must include surplus variables in this.
            for (var i = 0; i < tableau.SolveFor; i++)
            {
                var eliminateCoefficient = tableau.Matrix[Tableau.Phase2ObjectiveRow, i];
                if (eliminateCoefficient >= 0) continue;   // Nothing to eliminate in row 0?

                CollectPivotsForColumn(tableau, i);
            }
            return TryApplyPivot(tableau, debugWriter);
        }

        public static bool TryApplyPivot(Tableau tableau, IDebugWriter debugWriter)
        {
            foreach (var pivot in tableau.Pivots)
            {
                debugWriter?.Write($"Candidate: row {tableau.GetRowName(pivot.Row)}, column {tableau.GetVariableName(pivot.Column)}");
                if (!CheckValidityOfBasicResult(tableau, pivot.Row, pivot.Column, debugWriter)) continue;
                if (TryPivot(tableau, pivot.Column, pivot.Row, debugWriter)) return true;
            }
            // Nothing to eliminate?
            return false;
        }

        public static void CollectSolution(Tableau tableau, float[] solution)
        {
            for (var c = Tableau.FirstConstraintRow; c < tableau.RowCount; c++)
            {
                var basic = tableau.BasicVariables[c];
                if (basic < tableau.VariableCount) TryGetBasicSolution(tableau, basic, out solution[basic]);
            }
        }

        public static bool TryPivot(Tableau tableau, int enteringColumn, int leavingRow, IDebugWriter debugWriter)
        {
            var leavingColumn = tableau.BasicVariables[leavingRow];
            if (leavingColumn == enteringColumn) throw new InvalidOperationException("Entering and leaving variables must be different.");

            debugWriter?.WritePivot(tableau, leavingRow, enteringColumn);
            MatrixOp.Pivot(tableau.Matrix, enteringColumn, leavingRow);

            tableau.BasicVariables[leavingRow] = enteringColumn;
            return true;
        }

        private static void CollectPivotsForColumn(Tableau tableau, int pivotColumn)
        {
            for (var c = Tableau.FirstConstraintRow; c < tableau.RowCount; c++)
            {
                if (tableau.Matrix[c, pivotColumn] == 0) continue;
                if (tableau.BasicVariables[c] == pivotColumn) continue; // Cannot pivot a variable on itself.
                var candidate = Score(tableau, c, pivotColumn);
                if (candidate <= 0) continue;
                tableau.Pivots.Add(new Pivot(c, pivotColumn), candidate);
            }
        }

        public static void CollectPivotsForRow(Tableau tableau, int pivotRow)
        {
            for (var i = 0; i < tableau.SolveFor; i++)
            {
                if (tableau.Matrix[pivotRow, i] == 0) continue;
                if (tableau.BasicVariables[pivotRow] == i) continue; // Cannot pivot a variable on itself.
                var candidate = Score(tableau, pivotRow, i);
                if (candidate <= 0) continue;
                tableau.Pivots.Add(new Pivot(pivotRow, i), candidate);
            }
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

        public static bool TryGetBasicSolution(Tableau tableau, int variable, out float value)
        {
            value = 0;
            if (tableau.Matrix[tableau.ObjectiveRow, variable] != 0) return false;

            var alreadyGotValue = false;
            for (var c = Tableau.FirstConstraintRow; c < tableau.RowCount; c++)
            {
                if (tableau.Matrix[c, variable] == 0) continue;
                value = Score(tableau, c, variable);
                if (alreadyGotValue)
                {
                    // Should have only one possible value if this is a basic variable?
                    throw new InvalidOperationException($"No unique solution: {variable}");
                }
                alreadyGotValue = true;
            }
            return true;
        }
    }
}
