using System.Text;

namespace Shared.LinearSolver.UnitTests.Debug
{
    internal static class DebugWriterExtensions
    {
        public static void Write(this IDebugWriter writer, string phase, ref Tableau tableau)
        {
            if (writer == null) return;
            writer.Write(phase);
            writer.Write(new TableauRenderer().Render(ref tableau));
            var sb = new StringBuilder();
            for (var c = Tableau.FirstConstraintRow; c < tableau.RowCount; c++)
            {
                var basic = tableau.BasicVariables[c];
                sb.Append(tableau.GetVariableName(basic));
                sb.Append(" = ");
                var resultType = SimplexOp.TryGetBasicSolution(ref tableau, basic, out var value);
                if (resultType == BasicSolutionType.Unique)
                {
                    sb.Append(value);
                }
                else if (resultType == BasicSolutionType.NotUnique)
                {
                    sb.Append(value);
                    sb.Append("*");
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

            writer.Write(sb.ToString());
        }

        public static void WritePivot(this IDebugWriter writer, Tableau tableau, int row, int column)
        {
            if (writer == null) return;
            writer.Write($"Pivot: leaving {tableau.GetRowName(row)} (row {row + 1 - tableau.ObjectiveRow}), entering {tableau.GetVariableName(column)}");
        }
    }
}
