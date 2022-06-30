namespace Shared.LinearSolver
{
    public struct Solution
    {
        public SimplexResult Result { get; set; }
        public float[] Values { get; set; }
        public float Optimised { get; set; }
    }
}
