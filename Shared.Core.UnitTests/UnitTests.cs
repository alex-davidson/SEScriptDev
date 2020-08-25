using NUnit.Framework;

namespace Shared.Core.UnitTests
{
    [TestFixture]
    public class UnitTests
    {
        public static Case[] Cases =
        {
            new Case { Unit = Unit.Mass, Value = 1, Formatted = "1 Kg" },
            new Case { Unit = Unit.Energy, Value = 2.1f, Formatted = "2.1 MWh" },
            new Case { Unit = Unit.Energy, Value = 0.01f, Formatted = "10 KWh" },
            new Case { Unit = Unit.Power, Value = 0.2f, Formatted = "200 KW" },
        };

        [TestCaseSource(nameof(Cases))]
        public void Formats(Case testCase)
        {
            var formatted = testCase.Unit.FormatSI(testCase.Value, testCase.DecimalPlaces);
            Assert.That(formatted, Is.EqualTo(testCase.Formatted));
        }

        [TestCaseSource(nameof(Cases))]
        public void Parses(Case testCase)
        {
            Unit unit;
            float value;
            var didParse = Unit.TryParseSI(testCase.Formatted, out value, out unit);

            Assert.That(didParse, Is.True);
            Assert.That(value, Is.EqualTo(testCase.Value).Within(0.001));
            Assert.That(unit, Is.EqualTo(testCase.Unit));
        }

        public class Case
        {
            public Unit Unit { get; set; }
            public float Value { get; set; }
            public int DecimalPlaces { get; set; } = 1;
            public string Formatted { get; set; }

            public override string ToString() => Formatted;
        }
    }
}
