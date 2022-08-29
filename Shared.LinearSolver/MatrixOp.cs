using System;

namespace Shared.LinearSolver
{
    static class MatrixOp
    {
        /// <summary>
        /// Try to eliminate values in pivotColumn using pivotRow.
        /// </summary>
        public static void Pivot(float[,] matrix, int pivotColumn, int pivotRow)
        {
            var columns = matrix.GetLength(1);
            var pivotCoefficient = matrix[pivotRow, pivotColumn];

            // If the pivot coefficient is negative then we'll end up negating the entire target row.
            // The sign of the coefficients is significant to the Simplex algorithm, so this would be a problem.
            // SHOULD NOT HAPPEN?
            if (pivotCoefficient < 0) throw new InvalidOperationException("Did not expect pivot coefficient to be negative.");

            for (var targetRow = 0; targetRow < matrix.GetLength(0); targetRow++)
            {
                if (targetRow == pivotRow) continue;

                // Try to eliminate the value at matrix[targetRow, pivotColumn], using pivotRow.

                var eliminateCoefficient = matrix[targetRow, pivotColumn];
                if (eliminateCoefficient == 0) continue;  // Nothing to eliminate?

                var min = float.MaxValue;
                for (var i = 0; i < columns; i++)
                {
                    // Warning: values can grow large through many eliminations. Might need to rescale periodically.
                    var cell = (matrix[targetRow, i] * pivotCoefficient) - (matrix[pivotRow, i] * eliminateCoefficient);
                    matrix[targetRow, i] = cell;
                    if (cell == 0) continue;
                    if (cell < 0) cell = -cell;
                    if (cell < min) min = cell;
                    // Alternative, doesn't suffer from sign problems but does introduce rounding errors:
                    // matrix[targetRow, i] = matrix[targetRow, i] - (matrix[pivotRow, i] * eliminateCoefficient / pivotCoefficient);
                }
                // Try to reduce numbers in the target row, without losing (significant) accuracy.
                if (min > 1 && min != float.MaxValue)
                {
                    var reduce = 1 << (int)Math.Log(min, 2);
                    for (var i = 0; i < columns; i++)
                    {
                        matrix[targetRow, i] /= reduce;
                    }
                }
                // Should have cancelled out. Set it to 0 directly just in case.
                matrix[targetRow, pivotColumn] = 0;
            }
        }

        /// <summary>
        /// Subtract the row subtractRow from targetRow.
        /// </summary>
        public static void SubtractRow(float[,] matrix, int targetRow, int subtractRow)
        {
            var columns = matrix.GetLength(1);
            for (var i = 0; i < columns; i++)
            {
                matrix[targetRow, i] -= matrix[subtractRow, i];
            }
        }
    }
}
