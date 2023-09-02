using System;
using System.Collections.Generic;
using Shared.LinearSolver.Constraints;

namespace Shared.LinearSolver.UnitTests
{
    class FuzzTestGenerator
    {
        public int Seed { get; }
        public int MinimumVariables { get; set; } = 2;
        public int MaximumVariables { get; set; } = 4;

        private readonly Random random;

        public FuzzTestGenerator(int seed)
        {
            Seed = seed;
            this.random = new Random(seed);
        }

        private void FillVector(IList<float> vector, int count)
        {
            for (var i = 0; i < count; i++)
            {
                vector.Add(random.Next(-5, 10));
            }
        }
        private void FillArray(float[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = random.Next(-5, 10);
            }
        }

        private Constraint CreateConstraint(int variableCount)
        {
            var values = new float[variableCount];
            FillArray(values);
            var builder = Constrain.Linear(values);
            switch (random.Next(-2, 3))
            {
                case -2:
                case -1:
                    return builder.LessThanOrEqualTo(random.Next(0, 20));
                case 0:
                    return builder.EqualTo(random.Next(0, 10));

                case 1:
                case 2:
                default:
                    return builder.GreaterThanOrEqualTo(random.Next(0, 20));
            }
        }

        public SimplexTests.Case GenerateCase()
        {
            var testCase = new SimplexTests.Case();
            var variableCount = random.Next(MinimumVariables, MaximumVariables);
            var constraintCount = random.Next(-1, 1) + variableCount;
            for (var i = 0; i < constraintCount; i++)
            {
                testCase.Constraints.Add(CreateConstraint(variableCount));
            }
            FillVector(random.NextDouble() < 0.5 ? testCase.Minimise : testCase.Maximise, variableCount);
            return testCase;
        }
    }
}
