using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.LinearSolver
{
    /// <summary>
    /// Debugging tool. Renders the state of the tableau as a string.
    /// </summary>
    public struct TableauRenderer
    {
        public string Render(Tableau tableau)
        {
            var sb = new StringBuilder();
            sb.Append("".PadLeft(6));
            foreach (var i in tableau.GetActiveColumns())
            {
                var formatted = tableau.GetVariableName(i);
                sb.Append(formatted.PadLeft(6));
                sb.Append(", ");
            }
            sb.AppendLine();
            // Rows (basic variables):
            foreach (var c in tableau.GetActiveRows())
            {
                sb.Append($"{tableau.GetRowName(c)}):".PadLeft(6));
                foreach (var i in tableau.GetActiveColumns())
                {
                    sb.Append(GetCell(c, i));
                    sb.Append(", ");
                }
                sb.AppendLine();
            }
            return sb.ToString().TrimEnd();

            string GetCell(int c, int i)
            {
                var value = tableau.Matrix[c, i];
                var formatted = value.ToString("0.##");
                return formatted.PadLeft(6);
            }
        }
    }
}
