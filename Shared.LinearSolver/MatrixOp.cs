﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

        [StructLayout(LayoutKind.Explicit)]
        private struct ConverterStruct
        {
            [FieldOffset(0)] public uint[,] asUint;
            [FieldOffset(0)] public float[,] asFloat;
        }

        /// <summary>
        /// Try to reduce the magnitude of numbers in the target row, without losing (significant) accuracy.
        /// </summary>
        /// <remarks>
        /// This effectively works by calculating the midpoint of the exponents of the floats in the row, then
        /// multiplying or dividing the entire row by a power of two so that the midpoint exponent would be 0.
        ///
        /// Multiplication and division are expensive operations though, so instead we do integer addition and
        /// subtraction on the exponents themselves, using bithacks.
        ///
        /// For some reason, neither the C# compiler nor the JIT seem to be reordering things as I'd expect to
        /// exploit pipelining. At least I assume they aren't, since moving the two 'ignore zeroes' checks to
        /// lines A and B increases our runtime by about 6%.
        /// </remarks>
        public static void ReduceRows(float[,] matrix, int rowCount, int columnCount)
        {
            // Type-pun the entire matrix as uint32, so we can use bithacks.
            // I am amazed that it is legal C# to type-pun an entire array like this, but grateful for it.
            var conv = new ConverterStruct { asFloat = matrix };
            var uintMatrix = conv.asUint;

            const uint exponentBits = 0xFF << 23;
            const uint significantExponentBits = 0xFC << 23;
            const uint signBit = 0x80000000;
            const uint midpoint = 127 << 23;
            const uint maxExponent = 255 << 23;

            for (var r = 0; r < rowCount; r++)
            {
                // Operate directly on the biased exponent in order to save operations. 127 = 2^0.
                uint min = maxExponent;
                uint max = 0;
                for (var c = 0; c < columnCount; c++)
                {
                    var cell = uintMatrix[r, c];
                    // A
                    var biasedExponent = cell & exponentBits;
                    if ((cell & ~signBit) == 0) continue;   // Ignore zeroes.
                    if (min > biasedExponent) min = biasedExponent;
                    if (max < biasedExponent) max = biasedExponent;
                }
                // If any float is subnormal/special, don't touch anything.
                if (min == 0) continue;
                if (max == maxExponent) continue;
                // No updates?
                if (max == 0) continue;

                // We're trying to recentre the range around 0, and do it without repeated
                // shifting left and right.
                var adjust = (((max + min) >> 1) - midpoint) & exponentBits;
                // Ignore small downward reductions, since they're fast to detect.
                if ((adjust & significantExponentBits) == 0) continue;

                // Adjust the exponent only, keeping all bits of the mantissa.
                // Need to keep the sign bit untouched, too.
                for (var c = 0; c < columnCount; c++)
                {
                    var cell = uintMatrix[r, c];
                    // B
                    var sign = cell & signBit;
                    var adjusted = cell - adjust;
                    if ((cell & ~signBit) == 0) continue;   // Ignore zeroes.
                    uintMatrix[r, c] = (adjusted & ~signBit) | sign;
                }
            }
        }
    }
}
