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

            SetupConstraints(tableau, constraints);

            return Solve(tableau);
        }

        public Solution Minimise(params float[] coefficients)
        {
            var variableCount = Math.Max(coefficients.Length, constraints.VariableCount);
            var tableau = new Tableau(variableCount, constraints.Count);

            tableau.Matrix[Tableau.Phase1ObjectiveRow, tableau.Phase1OptimiseColumn] = 1;
            tableau.Matrix[Tableau.Phase2ObjectiveRow, tableau.Phase2OptimiseColumn] = -1;
            for (var i = 0; i < coefficients.Length; i++) tableau.Matrix[Tableau.Phase2ObjectiveRow, i] = coefficients[i];

            SetupConstraints(tableau, constraints);

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

        private static void SetupConstraints(Tableau tableau, ConstraintList constraints)
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
        private Solution ExtractSolution(Tableau tableau)
        {
            for (var i = 0; i < tableau.SolveFor; i++)
            {
                var eliminateCoefficient = tableau.Matrix[Tableau.Phase2ObjectiveRow, i];
                if (eliminateCoefficient < 0) return new Solution { Result = SimplexResult.Unbounded };
            }

            var solution = new float[tableau.VariableCount];
            var type = SimplexOp.CollectSolution(tableau, solution);

            return new Solution
            {
                Result = type == BasicSolutionType.NotUnique ? SimplexResult.OptimalSolutionNotUnique : SimplexResult.OptimalSolution,
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
                var initial = remainingArtificialVariables;
                for (var c = Tableau.FirstConstraintRow; c < tableau.RowCount; c++)
                {
                    if (!tableau.IsArtificialVariable(tableau.BasicVariables[c])) continue;

                    if (!SimplexOp.TrySelectPivotColumn(tableau, c, out var i, debug)) continue;
                    if (!SimplexOp.TryPivot(tableau, i, c, debug)) continue;

                    remainingArtificialVariables--;

                    debug?.Write("Phase 1, step", tableau);
                }

                // Made an entire pass with no pivots.
                if (initial == remainingArtificialVariables) return false;
            }

            if (tableau.Matrix[Tableau.Phase1ObjectiveRow, tableau.TargetColumn] != 0) return false;

            tableau.EndPhase1();

            return true;
        }
    }
}
