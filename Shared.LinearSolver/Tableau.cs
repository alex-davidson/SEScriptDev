using System;
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
    internal struct Tableau
    {
        public int VariableCount { get; }
        private readonly int firstSurplusVariable;
        public int SurplusVariableCount { get; private set; }
        private readonly int firstArtificialVariable;
        public int ArtificialVariableCount { get; private set; }
        public int ConstraintCount { get; }

        public int TargetColumn => ColumnCount - 1;
        public int Phase2OptimiseColumn => ColumnCount - 3;
        public int Phase1OptimiseColumn => ColumnCount - 2;
        public int SolveFor => firstSurplusVariable + SurplusVariableCount;
        public int RowCount => Matrix.GetLength(0);
        public int ColumnCount => Matrix.GetLength(1);
        public const int Phase1ObjectiveRow = 0;
        public const int Phase2ObjectiveRow = 1;
        public const int FirstConstraintRow = 2;

        public bool IsPhase1 { get; private set; }
        public int ObjectiveRow => IsPhase1 ? Phase1ObjectiveRow : Phase2ObjectiveRow;

        public BitSet Columns;
        public BitSet Rows;

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

            IsPhase1 = false;

            var columnCount = 3 + variableCount + constraintCount + constraintCount;
            var rowCount = 2 + constraintCount;

            Columns = new BitSet(columnCount);
            Rows = new BitSet(rowCount);

            Pivots = new ReadSortedPivotList(firstArtificialVariable * ConstraintCount);

            // Initialised with zeroes.
            BasicVariables = new int[rowCount];
            Matrix = new float[rowCount, columnCount];

            for (var i = 0; i < variableCount; i++)
            {
                Columns.Add(i);
            }
            Columns.Add(TargetColumn);
            Columns.Add(Phase2OptimiseColumn);

            for (int i = 0, j = FirstConstraintRow; i < constraintCount; i++, j++)
            {
                Rows.Add(j);
            }
            Rows.Add(Phase2ObjectiveRow);
        }

        /// <summary>
        /// Temporary list used to hold candidate pivots.
        /// </summary>
        public ReadSortedPivotList Pivots { get; }
        /// <summary>
        /// Associates basic variable column indices with rows.
        /// </summary>
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
            Columns.Add(index);
            return index;
        }

        public int AddArtificialVariable()
        {
            if (ArtificialVariableCount >= ConstraintCount) throw new InvalidOperationException("Already at limit of artificial variables.");
            var index = firstArtificialVariable + ArtificialVariableCount;
            ArtificialVariableCount++;
            Columns.Add(index);
            return index;
        }

        public bool IsArtificialVariable(int index) => index >= firstArtificialVariable;

        public void BeginPhase1()
        {
            Columns.Add(Phase1OptimiseColumn);
            Rows.Add(Phase1ObjectiveRow);
            IsPhase1 = true;
        }
        public void EndPhase1()
        {
            for (int i = 0, j = firstArtificialVariable; i < ArtificialVariableCount; i++, j++)
            {
                Columns.Remove(j);
            }
            Columns.Remove(Phase1OptimiseColumn);
            Rows.Remove(Phase1ObjectiveRow);
            IsPhase1 = false;
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

        public IEnumerable<int> GetActiveRows() => Rows;

        public IEnumerable<int> GetActiveColumns() => Columns;
    }
}
