using IngameScript;
using NUnit.Framework;

namespace Shared.Core.UnitTests
{
    [TestFixture]
    public class BlockDetailsParserTests
    {
        private readonly string RefineryDetails = @"
Type: Refinery
Max Required Input: 748.99 kW
Required Input: 748.99 kW

Productivity:200%
Effectiveness:141%
Power Efficiency:150%

Used upgrade module slots: 8 / 8
Attached modules: 4
 - Yield Module
 - Yield Module
 - Power Efficiency Module
 - Speed Module
".TrimStart();
        [Test]
        public void CanParseRefineryDetails()
        {
            var parser = new BlockDetailsParser();

            Assert.That(parser.Parse(RefineryDetails), Is.True);

            Assert.That(parser.Get("Type"), Is.EqualTo("Refinery"));
            Assert.That(parser.Get("Max Required Input"), Is.EqualTo("748.99 kW"));
            Assert.That(parser.Get("Required Input"), Is.EqualTo("748.99 kW"));
            Assert.That(parser.Get("Productivity"), Is.EqualTo("200%"));
            Assert.That(parser.Get("Effectiveness"), Is.EqualTo("141%"));
            Assert.That(parser.Get("Power Efficiency"), Is.EqualTo("150%"));
            Assert.That(parser.Get("Used upgrade module slots"), Is.EqualTo("8 / 8"));
            Assert.That(parser.Get("Attached modules"), Is.EqualTo("4"));
        }

        [Test]
        public void SupportUtilParseModuleBonuses_ReturnsPercentages()
        {
            // Simply looking for percentages in order after a blank line provides a language-agnostic means of determining module bonuses.
            var percentages = SupportUtil.ParseModuleBonuses(RefineryDetails);

            Assert.That(percentages,
                Is.EqualTo(new [] {
                    2f,
                    1.41f,
                    1.5f,
                }).Within(0.001));
        }
    }
}
