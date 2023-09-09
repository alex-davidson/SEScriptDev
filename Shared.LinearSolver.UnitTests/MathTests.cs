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
    }
}
