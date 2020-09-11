using IngameScript;
using NUnit.Framework;

namespace Script.Hephaestus.Thrusters.UnitTests
{
    [TestFixture]
    public class RotorLimitsTests
    {
        [Test]
        public void CalculatesLimitsOfOpposingRotor()
        {
            var limits = new RotorLimits(35, 110);
            var opposing = new RotorLimits(250, 325);

            Assert.That(limits.Opposing(), Is.EqualTo(opposing));
        }

        [TestCase(30, 60, 120, 90)]
        [TestCase(60, 60, 120, 120)]
        [TestCase(60, 60, 110, 110)]
        [TestCase(100, 60, 90, 160)]
        [TestCase(100, 360, 90, 90)]
        [TestCase(100, 720, 90, 90)]
        [TestCase(100, 700, 90, 90)]
        [TestCase(100, 700, float.MaxValue, 80)]
        public void AppliesUpperLimitToDeltasWhichCrossIt(float value, float delta, float limit, float expected)
        {
            var limits = new RotorLimits(float.MinValue, limit);
            Assert.That(limits.ClampDelta(value, delta), Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(120, -60, 10, 60)]
        [TestCase(70, -60, 10, 10)]
        [TestCase(70, -60, 30, 30)]
        [TestCase(90, -60, 100, 30)]
        [TestCase(80, -360, 100, 100)]
        [TestCase(80, -720, 90, 90)]
        [TestCase(100, -700, float.MinValue, 120)]
        public void AppliesLowerLimitToDeltasWhichCrossIt(float value, float delta, float limit, float expected)
        {
            var limits = new RotorLimits(limit, float.MaxValue);
            Assert.That(limits.ClampDelta(value, delta), Is.EqualTo(expected).Within(0.0001f));
        }
    }
}
