using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Script.RefineryBalance.v2
{
    public partial class Program
    {
        [TestFixture]
        public class ConfigurationParserTests
        {
            [Test]
            public void CanEnableIngot()
            {
                var configuration = new ConfigurationParser().Parse("+Uranium");

                RequestedIngotConfiguration ingot;
                Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ingot/Uranium"), out ingot));
                Assert.True(ingot.Enable);
            }

            [Test]
            public void CanDisableIngot()
            {
                var configuration = new ConfigurationParser().Parse("-Platinum");

                RequestedIngotConfiguration ingot;
                Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ingot/Platinum"), out ingot));
                Assert.False(ingot.Enable);
            }

            [Test]
            public void IngotsMayBeIdentifiedByPath()
            {
                var configuration = new ConfigurationParser().Parse("+Ore/Ice");

                RequestedIngotConfiguration ingot;
                Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ore/Ice"), out ingot));
                Assert.True(ingot.Enable);
            }

            [Test]
            public void IngotNamesMayBeQuoted()
            {
                var configuration = new ConfigurationParser().Parse("+\"Ore/Ice\"");

                RequestedIngotConfiguration ingot;
                Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ore/Ice"), out ingot));
                Assert.True(ingot.Enable);
            }

            [Test]
            public void MissingClosingQuoteIsAnError()
            {
                var error = Assert.Throws<Exception>(() => new ConfigurationParser().Parse("+\"Ore/Ice"), "Ore/Ice");
                Assert.That(error.Message, Is.StringContaining("Unterminated quoted string"));
            }

            [Test]
            public void CanConfigureStockpileTargetOnly()
            {
                var configuration = new ConfigurationParser().Parse("+Uranium:50");

                RequestedIngotConfiguration ingot;
                Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ingot/Uranium"), out ingot));
                Assert.That(ingot.StockpileTarget, Is.EqualTo(50));
                Assert.That(ingot.StockpileLimit, Is.Null);
            }

            [Test]
            public void CanConfigureStockpileLimitOnly()
            {
                var configuration = new ConfigurationParser().Parse("+Uranium::100");

                RequestedIngotConfiguration ingot;
                Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ingot/Uranium"), out ingot));
                Assert.That(ingot.StockpileTarget, Is.Null);
                Assert.That(ingot.StockpileLimit, Is.EqualTo(100));
            }

            [Test]
            public void CanConfigureStockpileTargetAndLimit()
            {
                var configuration = new ConfigurationParser().Parse("+Uranium:50:100");

                RequestedIngotConfiguration ingot;
                Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ingot/Uranium"), out ingot));
                Assert.That(ingot.StockpileTarget, Is.EqualTo(50));
                Assert.That(ingot.StockpileLimit, Is.EqualTo(100));
            }

            [Test]
            public void StockpileParametersAreIgnoredForDisabledIngots()
            {
                var configuration = new ConfigurationParser().Parse("-Platinum:50:100");

                RequestedIngotConfiguration ingot;
                Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ingot/Platinum"), out ingot));
                Assert.False(ingot.Enable);
            }

            [Test]
            public void UnparseableNumericValueIsAnError()
            {
                var error = Assert.Throws<Exception>(() => new ConfigurationParser().Parse("+Uranium:5a"), "5a");
                Assert.That(error.Message, Is.StringContaining("Unable to parse"));
            }

            [Test]
            public void TrailingSwitchPrefixIsAnError()
            {
                var error = Assert.Throws<Exception>(() => new ConfigurationParser().Parse("+Uranium:5 +"));
                Assert.That(error.Message, Is.StringContaining("Unexpected end of string"));
            }

            [Test]
            public void CanSpecifyStatusDisplayName()
            {
                var configuration = new ConfigurationParser().Parse("/status:\"Display Name with a : in it\"");
                Assert.That(configuration.StatusDisplayName, Is.EqualTo("Display Name with a : in it"));
            }

            [Test]
            public void CanSpecifyContainersToScan()
            {
                var configuration = new ConfigurationParser().Parse("/scan:\"Ore Containers\" /scan:\"Ingot Containers\"");
                Assert.That(configuration.InventoryBlockNames, Is.EquivalentTo(new [] {
                    "Ore Containers",
                    "Ingot Containers"
                }));
            }

            [Test]
            public void CanSpecifyAssemblerSpeed()
            {
                var configuration = new ConfigurationParser().Parse("/assemblerSpeed:4");
                Assert.That(configuration.AssemblerSpeedFactor, Is.EqualTo(4));
            }

            [Test]
            public void CanSpecifyRefinerySpeed()
            {
                var configuration = new ConfigurationParser().Parse("/refinerySpeed:5");
                Assert.That(configuration.RefinerySpeedFactor, Is.EqualTo(5));
            }
        }
    }
}
