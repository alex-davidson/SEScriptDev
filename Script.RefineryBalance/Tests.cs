using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;

namespace Script.RefineryBalance
{
    [TestFixture]
    public partial class Program
    {
        [Test]
        public void AssignedWorkAffectsQuotaFactor()
        {
            var blueprint = new Blueprint("A", new ItemAndQuantity("Ore/A", 10f), new ItemAndQuantity("Ingot/A", 10f));

            var stockpiles = new Stockpiles(
                new[] { new IngotStockpile { Ingot = new IngotType("Ingot/A", 1, 10), CurrentQuantity = 50, TargetQuantity = 100 } },
                new[] { blueprint });

            var refinery = new Refinery(new Mock<IMyRefinery>().Object, new RefineryType("Refinery") { SupportedBlueprints = { "A" } });

            var initialStockpile = stockpiles.GetStockpiles().Single();
            stockpiles.UpdateStockpileEstimates(refinery, blueprint, 5f);
            var updatedStockpile = stockpiles.GetStockpiles().Single();

            Assert.That(updatedStockpile.QuotaFraction, Is.GreaterThan(initialStockpile.QuotaFraction));
            Assert.That(updatedStockpile.EstimatedProduction, Is.GreaterThan(0));
        }

        [Test]
        public void ToleratesBlueprintsWithUnknownIngotTypes()
        {
            var blueprint = new Blueprint("A", new ItemAndQuantity("Ore/A", 10f), new ItemAndQuantity("Ingot/A", 10f));
            var stockpiles = new Stockpiles(
                new IngotStockpile[0],
                new[] { blueprint });

            var refinery = new Refinery(new Mock<IMyRefinery>().Object, new RefineryType("Refinery") { SupportedBlueprints = { "A" } });

            stockpiles.UpdateStockpileEstimates(refinery, blueprint, 5f);
        }

        [Test]
        public void BlueprintWithAllOutputsInDemandScoresHigherThanBlueprintWithOnlyOneOutputInDemand()
        {
            var blueprintAB = new Blueprint("AB", new ItemAndQuantity("Ore/AB", 10f), new ItemAndQuantity("Ingot/A", 10f), new ItemAndQuantity("Ingot/B", 10f));
            var blueprintBC = new Blueprint("BC", new ItemAndQuantity("Ore/BC", 10f), new ItemAndQuantity("Ingot/B", 10f), new ItemAndQuantity("Ingot/C", 10f));
            var stockpiles = new Stockpiles(
                new[] {
                    new IngotStockpile { Ingot = new IngotType("Ingot/A", 1, 10), CurrentQuantity = 500, TargetQuantity = 100 },    // Not in demand.
                    new IngotStockpile { Ingot = new IngotType("Ingot/B", 1, 10), CurrentQuantity = 50, TargetQuantity = 100 },
                    new IngotStockpile { Ingot = new IngotType("Ingot/C", 1, 10), CurrentQuantity = 50, TargetQuantity = 100 }
                },
                new[] { blueprintAB, blueprintBC });

            var abScore = stockpiles.ScoreBlueprint(blueprintAB);
            var bcScore = stockpiles.ScoreBlueprint(blueprintBC);

            Assert.That(bcScore, Is.GreaterThan(abScore));
        }

        [Test]
        public void HighYieldBlueprintScoresHigherThanLowYieldBlueprint()
        {
            var blueprintALow = new Blueprint("ALow", new ItemAndQuantity("Ore/A", 10f), new ItemAndQuantity("Ingot/A", 10f));
            var blueprintAHigh = new Blueprint("AHigh", new ItemAndQuantity("Ore/A", 10f), new ItemAndQuantity("Ingot/A", 12f));
            var stockpiles = new Stockpiles(
                new[] {
                    new IngotStockpile { Ingot = new IngotType("Ingot/A", 1, 10), CurrentQuantity = 50, TargetQuantity = 100 }
                },
                new[] { blueprintALow, blueprintAHigh });

            var aLowScore = stockpiles.ScoreBlueprint(blueprintALow);
            var aHighScore = stockpiles.ScoreBlueprint(blueprintAHigh);

            Assert.That(aHighScore, Is.GreaterThan(aLowScore));
        }

        [Description("Refs a bug in v1.4 where it would prefer iron over gold even when iron ingots were plentiful.")]
        public class HighYieldLowDemandBlueprintScoresLowerThanLowYieldHighDemandBlueprint
        {
            [Test]
            public void IronAndGold()
            {
                var gold = new Blueprint("GoldOreToIngot", new ItemAndQuantity("Ore/Gold", 2.5f), new ItemAndQuantity("Ingot/Gold", 0.025f));
                var iron = new Blueprint("IronOreToIngot", new ItemAndQuantity("Ore/Iron", 20f), new ItemAndQuantity("Ingot/Iron", 14f));
                var stockpiles = new Stockpiles(
                    new[] {
                        new IngotStockpile { Ingot = new IngotType("Ingot/Gold", 5f, 0.025f), CurrentQuantity = 27, TargetQuantity = 50 },
                        new IngotStockpile { Ingot = new IngotType("Ingot/Iron", 80f, 14f), CurrentQuantity = 15000, TargetQuantity = 800 }
                    },
                    new[] { gold, iron });

                var goldScore = stockpiles.ScoreBlueprint(gold);
                var ironScore = stockpiles.ScoreBlueprint(iron);

                Assert.That(ironScore, Is.LessThan(goldScore));
            }

            [Test]
            public void SilverAndCobalt()
            {
                var cobalt = new Blueprint("CobaltOreToIngot", new ItemAndQuantity("Ore/Cobalt", 0.25f), new ItemAndQuantity("Ingot/Cobalt", 0.075f));
                var silver = new Blueprint("SilverOreToIngot", new ItemAndQuantity("Ore/Silver", 1f), new ItemAndQuantity("Ingot/Silver", 0.1f));
                var stockpiles = new Stockpiles(
                    new[] {
                        new IngotStockpile { Ingot = new IngotType("Ingot/Cobalt", 220f, 0.075f), CurrentQuantity = 275, TargetQuantity = 2200 },
                        new IngotStockpile { Ingot = new IngotType("Ingot/Silver", 10f, 0.1f), CurrentQuantity = 34, TargetQuantity = 100 }
                    },
                    new[] { cobalt, silver });

                var cobaltScore = stockpiles.ScoreBlueprint(cobalt);
                var silverScore = stockpiles.ScoreBlueprint(silver);

                Assert.That(silverScore, Is.LessThan(cobaltScore));
            }

            [Test]
            public void SilverAndCobalt2()
            {
                var cobalt = new Blueprint("CobaltOreToIngot", new ItemAndQuantity("Ore/Cobalt", 0.25f), new ItemAndQuantity("Ingot/Cobalt", 0.075f));
                var silver = new Blueprint("SilverOreToIngot", new ItemAndQuantity("Ore/Silver", 1f), new ItemAndQuantity("Ingot/Silver", 0.1f));
                var stockpiles = new Stockpiles(
                    new[] {
                        new IngotStockpile { Ingot = new IngotType("Ingot/Cobalt", 220f, 0.075f), CurrentQuantity = 220, TargetQuantity = 2200 },
                        new IngotStockpile { Ingot = new IngotType("Ingot/Silver", 10f, 0.1f), CurrentQuantity = 10, TargetQuantity = 100 }
                    },
                    new[] { cobalt, silver });

                var cobaltScore = stockpiles.ScoreBlueprint(cobalt);
                var silverScore = stockpiles.ScoreBlueprint(silver);

                Assert.That(silverScore, Is.EqualTo(cobaltScore).Within(0.01).Percent);
            }
        }

        private IMyGridTerminalSystem GridTerminalSystem { get; set; }
        public static int Main(string[] args)
        {
            return 0;
        }

    }
}
