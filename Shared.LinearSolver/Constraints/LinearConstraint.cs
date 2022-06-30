namespace Shared.LinearSolver.Constraints
{
    public struct LinearConstraint
    {
        public float[] coefficients;

        public Constraint LessThanOrEqualTo(float value)
        {
            return new Constraint
            {
                Coefficients = coefficients,
                SurplusCoefficient = 1,
                Target = value,
            };
        }
        public Constraint EqualTo(float value)
        {
            return new Constraint
            {
                Coefficients = coefficients,
                SurplusCoefficient = 0,
                Target = value,
            };
        }

        public Constraint GreaterThanOrEqualTo(float value)
        {
            return new Constraint
            {
                Coefficients = coefficients,
                SurplusCoefficient = -1,
                Target = value,
            };
        }
    }
}
