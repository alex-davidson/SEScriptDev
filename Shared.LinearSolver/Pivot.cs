using System;

namespace Shared.LinearSolver
{
    public struct Pivot
    {
        public int Row { get; }
        public int Column { get; }

        public Pivot(int row, int column)
        {
            // Should never pivot on the first row.
            if (row <= 0) throw new ArgumentOutOfRangeException(nameof(row));
            Row = row;
            Column = column;
        }
    }
}
