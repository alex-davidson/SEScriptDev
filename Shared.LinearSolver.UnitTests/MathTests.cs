using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Shared.LinearSolver.UnitTests
{
    [TestFixture]
    public class MathTests
    {
        [TestCase(81, 72, 9)]
        [TestCase(81, 73, 1)]
        [TestCase(26, 6, 2)]
        public void Gcd(int a, int b, int gcd)
        {
            Assert.That(MathOp.Gcd(a, b), Is.EqualTo(gcd));
        }

        [Test]
        public void Log2Floor()
        {
            var random = new Random(42);
            var cases = new float[500000];
            for (var i = 0; i < cases.Length; i++)
            {
                cases[i] = (float)random.NextDouble() * float.MaxValue;
            }

            foreach (var value in cases)
            {
                var reference = MathOp.Log2FloorReference(value);
                var opt = MathOp.Log2Floor(value);
                Assert.That(opt, Is.EqualTo(reference));
            }
        }
    }
}
