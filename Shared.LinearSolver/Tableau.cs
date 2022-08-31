﻿using System;
using System.Collections.Generic;

namespace Shared.LinearSolver
{
    /// <summary>
    /// A Simplex tableau.
    /// </summary>
    /// <remarks>
    /// A float[y+2,x+3] matrix of coefficients, where y is the number of constraints and x is the total number
    /// of variables (including slack/surplus and artificial).
    /// </remarks>
    public class Tableau
    {
        public readonly int VariableCount;
        private readonly int firstSurplusVariable;
        public int SurplusVariableCount { get; private set; }
        private readonly int firstArtificialVariable;
        public int ArtificialVariableCount { get; private set; }
        public readonly int ConstraintCount;

        public int TargetColumn => ColumnCount - 1;
        public int Phase2OptimiseColumn => ColumnCount - 3;
        public int Phase1OptimiseColumn => ColumnCount - 2;
        public int SolveFor => firstSurplusVariable + SurplusVariableCount;
        public readonly int RowCount;
        public readonly int ColumnCount;
        public const int Phase1ObjectiveRow = 0;
        public const int Phase2ObjectiveRow = 1;
        public const int FirstConstraintRow = 2;

        public bool IsPhase1 { get; private set; }
        public int ObjectiveRow => IsPhase1 ? Phase1ObjectiveRow : Phase2ObjectiveRow;

        public Tableau(int variableCount, int constraintCount)
        {
            if (constraintCount < 1) throw new ArgumentException("Must have at least one constraint.");
            if (variableCount < 1) throw new ArgumentException("Must solve for at least one variable.");

            // Reserve space for surplus and artificial variables: two columns per constraint.
            VariableCount = variableCount;
            ConstraintCount = constraintCount;

            firstSurplusVariable = variableCount;
            firstArtificialVariable = firstSurplusVariable + constraintCount;

            SurplusVariableCount = 0;
            ArtificialVariableCount = 0;

            IsPhase1 = true;

            ColumnCount = 3 + variableCount + constraintCount + constraintCount;
            RowCount = 2 + constraintCount;

            // Initialised with zeroes.
            BasicVariables = new int[RowCount];
            Matrix = new float[RowCount, ColumnCount];
        }

        public int[] BasicVariables { get; }
        /// <remarks>
        ///                 x1 ... xn   s1 ... sn   a1 ... an   z   z'  |   target
        /// Phase I                                                 1   |
        /// Phase II                                            1       |
        /// ...                                                         |
        /// constraints                                                 |
        /// ...                                                         |
        /// </remarks>
        public float[,] Matrix { get; }

        public int AddSurplusVariable()
        {
            if (SurplusVariableCount >= ConstraintCount) throw new InvalidOperationException("Already at limit of slack/surplus variables.");
            var index = firstSurplusVariable + SurplusVariableCount;
            SurplusVariableCount++;
            return index;
        }

        public int AddArtificialVariable()
        {
            if (ArtificialVariableCount >= ConstraintCount) throw new InvalidOperationException("Already at limit of artificial variables.");
            var index = firstArtificialVariable + ArtificialVariableCount;
            ArtificialVariableCount++;
            return index;
        }

        public bool IsArtificialVariable(int index) => index >= firstArtificialVariable;

        public void BeginPhase1() { IsPhase1 = true; }
        public void EndPhase1() { IsPhase1 = false; }

        public void Reduce()
        {
            const float minMagnitude = 1;
            const float maxMagnitude = 8192;
            for (var c = 0; c < RowCount; c++)
            {
                var min = float.MaxValue;
                var max = 0f;
                for (var i = 0; i<ColumnCount; i++)
                {
                    var cell = Matrix[c, i];
                    if (cell == 0) continue;
                    if (cell < 0) cell = -cell;
                    if (cell < min) min = cell;
                    if (cell > max) max = cell;
                }
                // Try to reduce numbers in the target row, without losing (significant) accuracy.
                if (max > maxMagnitude && min > minMagnitude && min != float.MaxValue)
                {
                    var reduce = 1f / (1 << (int)Math.Log(min, 2));
                    for (var i = 0; i<ColumnCount; i++)
                    {
                        Matrix[c, i] *= reduce;
                    }
                }
            }
        }

        internal string GetRowName(int c)
        {
            if (c == Phase1ObjectiveRow) return "I";
            if (c == Phase2ObjectiveRow) return "II";
            return GetVariableName(BasicVariables[c]);
        }

        internal string GetVariableName(int i)
        {
            if (i == TargetColumn) return "tgt";
            if (i == Phase1OptimiseColumn) return "z'";
            if (i == Phase2OptimiseColumn) return "z";
            if (i < VariableCount) return $"x{i + 1}";
            var s = i - firstSurplusVariable;
            if (s < SurplusVariableCount) return $"s{s + 1}";
            var a = i - firstArtificialVariable;
            if (a < ArtificialVariableCount) return $"a{a + 1}";
            return "ERR";
        }

        public IEnumerable<int> GetActiveRows()
        {
            if (IsPhase1) yield return Phase1ObjectiveRow;
            yield return Phase2ObjectiveRow;
            for (var i = FirstConstraintRow; i < RowCount; i++) yield return i;
        }

        public IEnumerable<int> GetActiveColumns()
        {
            for (var i = 0; i < VariableCount; i++) yield return i;
            for (int i = 0, j = firstSurplusVariable; i < SurplusVariableCount; i++, j++) yield return j;
            if (IsPhase1) for (int i = 0, j = firstArtificialVariable; i < ArtificialVariableCount; i++, j++) yield return j;
            yield return Phase2OptimiseColumn;
            if (IsPhase1) yield return Phase1OptimiseColumn;
            yield return TargetColumn;
        }
    }
}
