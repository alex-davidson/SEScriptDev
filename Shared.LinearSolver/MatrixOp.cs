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
            if (pivotCoefficient < 0) throw new InvalidOperationException($"Did not expect pivot coefficient to be negative: {pivotCoefficient}");

            for (var targetRow = 0; targetRow < matrix.GetLength(0); targetRow++)
            {
                if (targetRow == pivotRow) continue;

                // Try to eliminate the value at matrix[targetRow, pivotColumn], using pivotRow.

                var eliminateCoefficient = matrix[targetRow, pivotColumn];
                if (eliminateCoefficient == 0) continue;  // Nothing to eliminate?

                for (var i = 0; i < columns; i++)
                {
                    // Warning: values can grow large through many eliminations. Might need to rescale periodically.
                    var cell = (matrix[targetRow, i] * pivotCoefficient) - (matrix[pivotRow, i] * eliminateCoefficient);
                    matrix[targetRow, i] = cell;
                    // Alternative, doesn't suffer from sign problems but does introduce rounding errors:
                    // matrix[targetRow, i] = matrix[targetRow, i] - (matrix[pivotRow, i] * eliminateCoefficient / pivotCoefficient);
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

        public static void Reduce(float[,] matrix, int row, int columnCount)
        {
            const float minMagnitude = 1;
            const float maxMagnitude = 8192;
            var min = float.MaxValue;
            var max = 0f;
            for (var i = 0; i < columnCount; i++)
            {
                var cell = matrix[row, i];
                if (cell == 0) continue;
                if (cell < 0) cell = -cell;
                if (cell < min) min = cell;
                if (cell > max) max = cell;
            }
            // Try to reduce numbers in the target row, without losing (significant) accuracy.
            // If min == float.MaxValue then max is still 0, so no need to check that here.
            if (max <= maxMagnitude) return;
            if (min <= minMagnitude) return;

            // Reduce by powers of two: adjust the exponent only, keeping all bits of the mantissa.
            var reduce = 1f / (float)(Math.Pow(2, (int)Math.Log(min, 2)));
            for (var i = 0; i < columnCount; i++)
            {
                matrix[row, i] *= reduce;
            }
        }
    }
}
