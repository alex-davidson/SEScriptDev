using System;
using Shared.LinearSolver.Constraints;
using Shared.LinearSolver.UnitTests.Debug;

namespace Shared.LinearSolver
{
    public struct SimplexSolver
    {
        private readonly ConstraintList constraints;
        private readonly IDebugWriter debug;

        public static SimplexSolver Given(ConstraintList constraints, IDebugWriter debug = null) => new SimplexSolver(constraints, debug);

        private SimplexSolver(ConstraintList constraints, IDebugWriter debug = null)
        {
            this.constraints = constraints;
            this.debug = debug;
        }

        public Solution Maximise(params float[] coefficients)
        {
            var variableCount = Math.Max(coefficients.Length, constraints.VariableCount);
            var tableau = new Tableau(variableCount, constraints.Count);

            tableau.Matrix[Tableau.Phase1ObjectiveRow, tableau.Phase1OptimiseColumn] = 1;
            tableau.Matrix[Tableau.Phase2ObjectiveRow, tableau.Phase2OptimiseColumn] = 1;
            for (var i = 0; i < coefficients.Length; i++) tableau.Matrix[Tableau.Phase2ObjectiveRow, i] = -coefficients[i];

            SetupConstraints(ref tableau, constraints);

            return Solve(ref tableau);
        }

        public Solution Minimise(params float[] coefficients)
        {
            var variableCount = Math.Max(coefficients.Length, constraints.VariableCount);
            var tableau = new Tableau(variableCount, constraints.Count);

            tableau.Matrix[Tableau.Phase1ObjectiveRow, tableau.Phase1OptimiseColumn] = 1;
            tableau.Matrix[Tableau.Phase2ObjectiveRow, tableau.Phase2OptimiseColumn] = -1;
            for (var i = 0; i < coefficients.Length; i++) tableau.Matrix[Tableau.Phase2ObjectiveRow, i] = coefficients[i];

            SetupConstraints(ref tableau, constraints);

            return Solve(ref tableau);
        }

        private Solution Solve(ref Tableau tableau)
        {
            if (!DoPhase1(ref tableau)) return new Solution { Result = SimplexResult.NoSolution };

            debug?.Write("Phase 2, start", ref tableau);

            while (SimplexOp.MaximiseStep(ref tableau, debug))
            {
                debug?.Write("Phase 2, step", ref tableau);
            }

            debug?.Write("Phase 2, end");

            return ExtractSolution(ref tableau);
        }

        private static void SetupConstraints(ref Tableau tableau, ConstraintList constraints)
        {
            for (var c = 0; c < constraints.Count; c++)
            {
                var constraint = Normalise(constraints[c]);
                // Populate coefficients for variables.
                for (var i = 0; i < constraint.Coefficients.Length; i++) tableau.Matrix[c + Tableau.FirstConstraintRow, i] = constraint.Coefficients[i];
                // Populate surplus variable coefficient if necessary.
                if (constraint.SurplusCoefficient != 0)
                {
                    var surplusIndex = tableau.AddSurplusVariable();
                    tableau.Matrix[c + Tableau.FirstConstraintRow, surplusIndex] = constraint.SurplusCoefficient;
                    tableau.BasicVariables[c + Tableau.FirstConstraintRow] = surplusIndex;
                }
                if (constraint.SurplusCoefficient <= 0)
                {
                    var artificialIndex = tableau.AddArtificialVariable();
                    tableau.Matrix[c + Tableau.FirstConstraintRow, artificialIndex] = 1;
                    tableau.BasicVariables[c + Tableau.FirstConstraintRow] = artificialIndex;
                    tableau.Matrix[Tableau.Phase1ObjectiveRow, artificialIndex] = 1;
                }

                // Populate target.
                tableau.Matrix[c + Tableau.FirstConstraintRow, tableau.TargetColumn] = constraint.Target;
            }
        }

        private static Constraint Normalise(Constraint constraint)
        {
            if (constraint.Target >= 0) return constraint;
            var coefficients = new float[constraint.Coefficients.Length];
            for (var i = 0; i < coefficients.Length; i++) coefficients[i] = -constraint.Coefficients[i];
            return new Constraint
            {
                Coefficients = coefficients,
                SurplusCoefficient = -constraint.SurplusCoefficient,
                Target = -constraint.Target,
            };
        }

        /// <summary>
        /// Given an optimised tableau, extract the variables' values.
        /// </summary>
        private Solution ExtractSolution(ref Tableau tableau)
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
            SimplexOp.CollectSolution(ref tableau, solution);

            return new Solution
            {
                Result = result,
                Values = solution,
                Optimised = SimplexOp.Score(ref tableau, Tableau.Phase2ObjectiveRow, tableau.Phase2OptimiseColumn),
            };
        }

        private bool DoPhase1(ref Tableau tableau)
        {
            if (tableau.ArtificialVariableCount <= 0) return true;

            tableau.BeginPhase1();

            debug?.Write("Phase 1, start", ref tableau);

            for (var c = Tableau.FirstConstraintRow; c < tableau.RowCount; c++)
            {
                if (!tableau.IsArtificialVariable(tableau.BasicVariables[c])) continue;

                MatrixOp.SubtractRow(tableau.Matrix, Tableau.Phase1ObjectiveRow, c);
            }

            debug?.Write("Phase 1, prep", ref tableau);

            var remainingArtificialVariables = tableau.ArtificialVariableCount;
            while (remainingArtificialVariables > 0)
            {
                tableau.Pivots.Clear();
                for (var c = Tableau.FirstConstraintRow; c < tableau.RowCount; c++)
                {
                    if (!tableau.IsArtificialVariable(tableau.BasicVariables[c])) continue;
                    SimplexOp.CollectPivotsForRow(ref tableau, c);
                }
                if (!SimplexOp.TryApplyPivot(ref tableau, debug)) return false;
                debug?.Write("Phase 1, step", ref tableau);
                remainingArtificialVariables--;
            }

            if (tableau.Matrix[Tableau.Phase1ObjectiveRow, tableau.TargetColumn] != 0) return false;

            tableau.EndPhase1();

            return true;
        }
    }
}
