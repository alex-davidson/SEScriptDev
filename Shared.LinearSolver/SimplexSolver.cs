using System;
using Shared.LinearSolver.UnitTests.Debug;

namespace Shared.LinearSolver
{
    public struct SimplexSolver
    {
        private readonly Constraints.ConstraintList constraints;
        private readonly IDebugWriter debug;

        public static SimplexSolver Given(Constraints.ConstraintList constraints, IDebugWriter debug = null) => new SimplexSolver(constraints, debug);

        private SimplexSolver(Constraints.ConstraintList constraints, IDebugWriter debug = null)
        {
            this.constraints = constraints;
            this.debug = debug;
        }

        public Solution Maximise(params float[] coefficients)
        {
            var builder = new TableauBuilder(constraints, coefficients);

            var tableau = builder.ForMaximise();

            return Solve(tableau);
        }

        public Solution Minimise(params float[] coefficients)
        {
            var builder = new TableauBuilder(constraints, coefficients);

            var tableau = builder.ForMinimise();

            return Solve(tableau);
        }

        private Solution Solve(Tableau tableau)
        {
            if (!DoPhase1(tableau)) return new Solution { Result = SimplexResult.NoSolution };

            debug?.Write("Phase 2, start", tableau);

            while (SimplexOp.MaximiseStep(tableau, debug))
            {
                debug?.Write("Phase 2, step", tableau);
            }

            debug?.Write("Phase 2, end");

            return ExtractSolution(tableau);
        }

        /// <summary>
        /// Given an optimised tableau, extract the variables' values.
        /// </summary>
        private Solution ExtractSolution(Tableau tableau)
        {
            var result = SimplexResult.OptimalSolution;
            for (var i = 0; i < tableau.SolveFor; i++)
            {
                var eliminateCoefficient = tableau.Matrix[Tableau.Phase2ObjectiveRow, i];
                if (eliminateCoefficient < 0) return new Solution { Result = SimplexResult.Unbounded };
                if (eliminateCoefficient == 0)
                {
                    if (Array.IndexOf(tableau.BasicVariables, i, Tableau.FirstConstraintRow) < 0)
                    {
                        result = SimplexResult.MultipleSolutions;
                    } 
                }
            }

            var solution = new float[tableau.VariableCount];
            SimplexOp.CollectSolution(tableau, solution);

            return new Solution
            {
                Result = result,
                Values = solution,
                Optimised = SimplexOp.Score(tableau, Tableau.Phase2ObjectiveRow, tableau.Phase2OptimiseColumn),
            };
        }

        private bool DoPhase1(Tableau tableau)
        {
            if (tableau.ArtificialVariableCount <= 0) return true;

            tableau.BeginPhase1();

            debug?.Write("Phase 1, start", tableau);

            for (var c = Tableau.FirstConstraintRow; c < tableau.RowCount; c++)
            {
                if (!tableau.IsArtificialVariable(tableau.BasicVariables[c])) continue;

                MatrixOp.SubtractRow(tableau.Matrix, Tableau.Phase1ObjectiveRow, c);
            }

            debug?.Write("Phase 1, prep", tableau);

            var remainingArtificialVariables = tableau.ArtificialVariableCount;
            while (remainingArtificialVariables > 0)
            {
                tableau.Pivots.Clear();
                for (var c = Tableau.FirstConstraintRow; c < tableau.RowCount; c++)
                {
                    if (!tableau.IsArtificialVariable(tableau.BasicVariables[c])) continue;
                    SimplexOp.CollectPivotsForRow(tableau, c);
                }
                if (!SimplexOp.TryApplyPivot(tableau, debug)) return false;
                debug?.Write("Phase 1, step", tableau);
                remainingArtificialVariables--;
            }

            if (tableau.Matrix[Tableau.Phase1ObjectiveRow, tableau.TargetColumn] != 0) return false;

            tableau.EndPhase1();

            return true;
        }
    }
}
