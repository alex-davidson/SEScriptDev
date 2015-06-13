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
                new[] { new IngotStockpile { Ingot = new IngotType("Ingot/A", 1), CurrentQuantity = 50, TargetQuantity = 100 } },
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
                    new IngotStockpile { Ingot = new IngotType("Ingot/A", 1), CurrentQuantity = 500, TargetQuantity = 100 },    // Not in demand.
                    new IngotStockpile { Ingot = new IngotType("Ingot/B", 1), CurrentQuantity = 50, TargetQuantity = 100 },
                    new IngotStockpile { Ingot = new IngotType("Ingot/C", 1), CurrentQuantity = 50, TargetQuantity = 100 }
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
                    new IngotStockpile { Ingot = new IngotType("Ingot/A", 1), CurrentQuantity = 50, TargetQuantity = 100 }
                },
                new[] { blueprintALow, blueprintAHigh });

            var aLowScore = stockpiles.ScoreBlueprint(blueprintALow);
            var aHighScore = stockpiles.ScoreBlueprint(blueprintAHigh);

            Assert.That(aHighScore, Is.GreaterThan(aLowScore));
        }

        private IMyGridTerminalSystem GridTerminalSystem { get; set; }
        public static int Main(string[] args)
        {
            return 0;
        }

    }

}
