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
        public void ReduceRowsTest()
        {
            var random = new Random(42);
            const int caseCount = 5000000;
            const int columnCount = 4;
            var cases = new float[caseCount, columnCount];
            var results = new float[caseCount, columnCount];

            for (var i = 0; i < caseCount; i++)
            {
                var magnitude = (float)Math.Pow(2, random.Next(-60, 60));
                cases[i, 0] = magnitude;    // Baseline
                results[i, 0] = magnitude;
                for (var j = 1; j < columnCount; j++)
                {
                    cases[i, j] = NextFloat(random, -60, 60) * magnitude;
                    results[i, j] = cases[i, j];
                }
            }

            MatrixOp.ReduceRows(results, caseCount, columnCount);

            for (var i = 0; i < caseCount; i++)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(Math.Sign(results[i, 0]), Is.EqualTo(Math.Sign(cases[i, 0])));
                    for (var j = 0; j < columnCount; j++)
                    {
                        var baseline = cases[i, 0] / cases[i, j];
                        var comparison = results[i, 0] / results[i, j];
                        Assert.That(comparison, Is.EqualTo(baseline));
                    }
                });
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
