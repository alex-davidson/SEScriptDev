using System;
using System.Linq;
using Shared.LinearSolver.Constraints;

namespace Shared.LinearSolver
{
    public struct TableauBuilder
    {
        private readonly ConstraintList constraints;
        private readonly float[] coefficients;

        public int VariableCount { get; }

        public TableauBuilder(ConstraintList constraints, float[] coefficients)
        {
            VariableCount = Math.Max(coefficients.Length, constraints.VariableCount);
            this.constraints = constraints;
            this.coefficients = coefficients;
        }

        public Tableau ForMaximise()
        {
            var tableau = new Tableau(VariableCount, constraints.Count);

            tableau.Matrix[Tableau.Phase1ObjectiveRow, tableau.Phase1OptimiseColumn] = 1;
            tableau.Matrix[Tableau.Phase2ObjectiveRow, tableau.Phase2OptimiseColumn] = 1;
            for (var i = 0; i < coefficients.Length; i++) tableau.Matrix[Tableau.Phase2ObjectiveRow, i] = -coefficients[i];

            SetupConstraints(tableau);

            return tableau;
        }

        public Tableau ForMinimise()
        {
            var tableau = new Tableau(VariableCount, constraints.Count);

            tableau.Matrix[Tableau.Phase1ObjectiveRow, tableau.Phase1OptimiseColumn] = 1;
            tableau.Matrix[Tableau.Phase2ObjectiveRow, tableau.Phase2OptimiseColumn] = -1;
            for (var i = 0; i < coefficients.Length; i++) tableau.Matrix[Tableau.Phase2ObjectiveRow, i] = coefficients[i];

            SetupConstraints(tableau);

            return tableau;
        }

        private void SetupConstraints(Tableau tableau)
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
    }
}
