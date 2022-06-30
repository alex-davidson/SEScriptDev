using System;
using System.Text;

namespace Shared.LinearSolver
{
    public class DebugWriter : IDebugWriter
    {
        private readonly Action<string> debug;
        private readonly StringBuilder buffer = new StringBuilder();

        public DebugWriter(Action<string> debug)
        {
            this.debug = debug;
        }

        public void Write(string phase, Tableau tableau)
        {
            WriteInternal(phase);
            WriteInternal(new TableauRenderer().Render(tableau));
            var sb = new StringBuilder();
            for (var c = Tableau.FirstConstraintRow; c < tableau.RowCount; c++)
            {
                var basic = tableau.BasicVariables[c];
                sb.Append(tableau.GetVariableName(basic));
                sb.Append(" = ");
                if (SimplexOp.TryGetBasicSolution(tableau, basic, out var value))
                {
                    sb.Append(value);
                }
                else
                {
                    sb.Append("***");
                }
                sb.Append("    ");
            }
            if (tableau.IsPhase1)
            {
                sb.Append("z' = ");
                sb.Append(tableau.Matrix[Tableau.Phase1ObjectiveRow, tableau.TargetColumn] / tableau.Matrix[Tableau.Phase1ObjectiveRow, tableau.Phase1OptimiseColumn]);
                sb.Append("    ");
            }
            sb.Append("z = ");
            sb.Append(tableau.Matrix[Tableau.Phase2ObjectiveRow, tableau.TargetColumn] / tableau.Matrix[Tableau.Phase2ObjectiveRow, tableau.Phase2OptimiseColumn]);
            sb.AppendLine();

            WriteInternal(sb.ToString());
        }

        public void Write(string message)
        {
            WriteInternal(message);
        }

        public void WritePivot(Tableau tableau, int row, int column)
        {
            WriteInternal($"Pivot: leaving {tableau.GetRowName(row)} (row {row + 1 - tableau.ObjectiveRow}), entering {tableau.GetVariableName(column)}");
        }

        private void WriteInternal(string message)
        {
            debug.Invoke(message);
            buffer.AppendLine(message);
        }

        internal string Buffer => buffer.ToString();
    }
}
