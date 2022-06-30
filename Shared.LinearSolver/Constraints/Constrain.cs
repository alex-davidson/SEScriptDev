namespace Shared.LinearSolver.Constraints
{
    public struct Constrain
    {
        public static LinearConstraint Linear(params float[] coefficients) => new LinearConstraint { coefficients = coefficients };
    }
}
