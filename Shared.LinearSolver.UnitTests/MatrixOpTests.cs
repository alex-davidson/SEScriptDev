using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Shared.LinearSolver.UnitTests
{
    [TestFixture]
    public class MatrixOpTests
    {
        [Test]
        public void Reduce()
        {
            var random = new Random(42);
            const int caseCount = 5000000;
            const int columnCount = 4;
            var cases = new float[caseCount, columnCount];
            for (var i = 0; i < caseCount; i++)
            {
                var magnitude = (float)Math.Pow(2, random.Next(-60, 60));
                cases[i, 0] = magnitude;    // Baseline.
                for (var j = 1; j < columnCount; j++)
                {
                    cases[i, j] = NextFloat(random, -60, 60) * magnitude;
                }
            }

            for (var i = 0; i < caseCount; i++)
            {
                var sign = Math.Sign(cases[i, 0]);
                var baseline = new float[columnCount];
                
                for (var j = 0; j < columnCount; j++)
                {
                    baseline[j] = cases[i, 0] / cases[i, j];
                }

                MatrixOp.Reduce(cases, i, columnCount);

                var comparison = new float[columnCount];
                for (var j = 0; j < columnCount; j++)
                {
                    comparison[j] = cases[i, 0] / cases[i, j];
                }
                Assert.That(comparison, Is.EqualTo(baseline));
                Assert.That(Math.Sign(cases[i, 0]), Is.EqualTo(sign));
            }
        }

        private float NextFloat(Random random, int minPower, int maxPower)
        {
            var value = 1 - ((float)random.NextDouble() * 2);
            var power = random.Next(minPower, maxPower);
            return (float)Math.Pow(2, power) + value;
        }
    }
}
