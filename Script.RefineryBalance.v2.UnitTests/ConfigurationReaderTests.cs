using IngameScript;
using NUnit.Framework;

namespace Script.RefineryBalance.v2
{
    [TestFixture]
    public class ConfigurationReaderTests
    {
        [Test]
        public void CanEnableIngot()
        {
            var configuration = new RequestedConfiguration();
            new ConfigurationReader().Read(configuration, new [] { "enable", "Uranium" });

            RequestedIngotConfiguration ingot;
            Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ingot/Uranium"), out ingot));
            Assert.True(ingot.Enable);
        }

        [Test]
        public void CanDisableIngot()
        {
            var configuration = new RequestedConfiguration();
            new ConfigurationReader().Read(configuration, new [] { "disable", "Platinum" });

            RequestedIngotConfiguration ingot;
            Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ingot/Platinum"), out ingot));
            Assert.False(ingot.Enable);
        }

        [Test]
        public void IngotsMayBeIdentifiedByPath()
        {
            var configuration = new RequestedConfiguration();
            new ConfigurationReader().Read(configuration, new [] { "enable", "Ore/Ice" });

            RequestedIngotConfiguration ingot;
            Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ore/Ice"), out ingot));
            Assert.True(ingot.Enable);
        }

        [Test]
        public void CanConfigureStockpileTargetOnly()
        {
            var configuration = new RequestedConfiguration();
            new ConfigurationReader().Read(configuration, new [] { "enable", "Uranium:50" });

            RequestedIngotConfiguration ingot;
            Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ingot/Uranium"), out ingot));
            Assert.That(ingot.StockpileTarget, Is.EqualTo(50));
            Assert.That(ingot.StockpileLimit, Is.Null);
        }

        [Test]
        public void CanConfigureStockpileLimitOnly()
        {
            var configuration = new RequestedConfiguration();
            new ConfigurationReader().Read(configuration, new [] { "enable", "Uranium::100" });

            RequestedIngotConfiguration ingot;
            Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ingot/Uranium"), out ingot));
            Assert.That(ingot.StockpileTarget, Is.Null);
            Assert.That(ingot.StockpileLimit, Is.EqualTo(100));
        }

        [Test]
        public void CanConfigureStockpileTargetAndLimit()
        {
            var configuration = new RequestedConfiguration();
            new ConfigurationReader().Read(configuration, new [] { "enable", "Uranium:50:100" });

            RequestedIngotConfiguration ingot;
            Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ingot/Uranium"), out ingot));
            Assert.That(ingot.StockpileTarget, Is.EqualTo(50));
            Assert.That(ingot.StockpileLimit, Is.EqualTo(100));
        }

        [Test]
        public void StockpileParametersAreUpdatedForDisabledIngots()
        {
            var configuration = new RequestedConfiguration();
            new ConfigurationReader().Read(configuration, new [] { "disable", "Platinum:50:100" });

            RequestedIngotConfiguration ingot;
            Assert.True(configuration.Ingots.TryGetValue(new ItemType("Ingot/Platinum"), out ingot));
            Assert.False(ingot.Enable);
            Assert.That(ingot.StockpileTarget, Is.EqualTo(50));
            Assert.That(ingot.StockpileLimit, Is.EqualTo(100));
        }

        [Test]
        public void UnparseableNumericValueReturnsFalse()
        {
            var configuration = new RequestedConfiguration();
            var result = new ConfigurationReader().Read(configuration, new [] { "enable", "Uranium:5a" });

            Assert.False(result);
        }

        [Test]
        public void CanSpecifyIngotStatusDisplayName()
        {
            var configuration = new RequestedConfiguration();
            new ConfigurationReader().Read(configuration, new [] { "show-ingots", "Display Name with a : in it" });

            Assert.That(configuration.IngotStatusDisplayName, Is.EqualTo("Display Name with a : in it"));
        }

        [Test]
        public void CanSpecifyOreStatusDisplayName()
        {
            var configuration = new RequestedConfiguration();
            new ConfigurationReader().Read(configuration, new [] { "show-ore", "Display Name with a : in it" });

            Assert.That(configuration.OreStatusDisplayName, Is.EqualTo("Display Name with a : in it"));
        }

        [Test]
        public void CanSpecifyContainersToScan()
        {
            var configuration = new RequestedConfiguration();
            new ConfigurationReader().Read(configuration, new [] { "scan", "Ore Containers", "scan", "Ingot Containers" });

            Assert.That(configuration.InventoryBlockNames, Is.EquivalentTo(new [] {
                "Ore Containers",
                "Ingot Containers"
            }));
        }

        [Test]
        public void CanSpecifyAssemblerSpeed()
        {
            var configuration = new RequestedConfiguration();
            new ConfigurationReader().Read(configuration, new [] { "assembler-speed", "4" });

            Assert.That(configuration.AssemblerSpeedFactor, Is.EqualTo(4));
        }

        [Test]
        public void CanSpecifyRefinerySpeed()
        {
            var configuration = new RequestedConfiguration();
            new ConfigurationReader().Read(configuration, new [] { "refinery-speed", "5" });

            Assert.That(configuration.RefinerySpeedFactor, Is.EqualTo(5));
        }
    }
}
