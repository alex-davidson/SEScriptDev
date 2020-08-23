using System.Collections.Generic;
using System.Linq;
using IngameScript;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;

namespace Script.RefineryBalance.v2
{
    [TestFixture]
    public class IngotWorklistPrioritisationTests
    {
        private readonly IMyRefinery mockRefinery = Mocks.MockRefinery();

        [Test]
        public void AssignedWorkAffectsQuotaFactor()
        {
            var blueprint = new Blueprint("A", 1, new ItemAndQuantity("Ore/A", 10), new ItemAndQuantity("Ingot/A", 10));
            var stockpiles = new IngotStockpiles(new TestIngotDefinitions { { "Ingot/A", 100 } });
            var ingotWorklist = new IngotWorklist(stockpiles);
            var refinery = Refinery.Get(mockRefinery, new RefineryType("Refinery") { SupportedBlueprints = { "A" } }, 1);

            stockpiles.UpdateQuantities(new TestIngotQuantities { { "Ingot/A", 50 } });
            ingotWorklist.Initialise();

            IngotStockpile preferred;
            Assume.That(ingotWorklist.TryGetPreferred(out preferred));
            Assume.That(preferred.EstimatedProduction, Is.EqualTo(0));

            var initialQuotaFraction = preferred.QuotaFraction;
            ingotWorklist.UpdateStockpileEstimates(refinery, blueprint, 5);

            IngotStockpile updatedPreferred;
            Assume.That(ingotWorklist.TryGetPreferred(out updatedPreferred));

            Assert.That(updatedPreferred.QuotaFraction, Is.GreaterThan(initialQuotaFraction));
            Assert.That(updatedPreferred.EstimatedProduction, Is.GreaterThan(0));
        }

        [Test]
        public void ToleratesBlueprintsWithUnknownIngotTypes()
        {
            var blueprint = new Blueprint("A", 1, new ItemAndQuantity("Ore/A", 10), new ItemAndQuantity("Ingot/A", 10));
            var stockpiles = new IngotStockpiles(new TestIngotDefinitions { { "Ingot/B", 100 } });
            var ingotWorklist = new IngotWorklist(stockpiles);
            var refinery = Refinery.Get(mockRefinery, new RefineryType("Refinery") { SupportedBlueprints = { "A" } }, 1);

            stockpiles.UpdateQuantities(new TestIngotQuantities { { "Ingot/B", 20 } });
            ingotWorklist.Initialise();
                
            ingotWorklist.ScoreBlueprint(blueprint);
            ingotWorklist.UpdateStockpileEstimates(refinery, blueprint, 5);
        }

        [Test]
        public void BlueprintWithAllOutputsInDemandScoresHigherThanBlueprintWithOnlyOneOutputInDemand()
        {
            var blueprintAB = new Blueprint("AB", 1, new ItemAndQuantity("Ore/AB", 10), new ItemAndQuantity("Ingot/A", 10), new ItemAndQuantity("Ingot/B", 10));
            var blueprintBC = new Blueprint("BC", 1, new ItemAndQuantity("Ore/BC", 10), new ItemAndQuantity("Ingot/B", 10), new ItemAndQuantity("Ingot/C", 10));
            var stockpiles = new IngotStockpiles(new TestIngotDefinitions {
                { "Ingot/A", 100 },
                { "Ingot/B", 100 },
                { "Ingot/C", 100 }
            });
            var ingotWorklist = new IngotWorklist(stockpiles);

            stockpiles.UpdateQuantities(new TestIngotQuantities {
                { "Ingot/A", 500 },  // Not in demand.
                { "Ingot/B", 50 },
                { "Ingot/C", 50 }
            });
            ingotWorklist.Initialise();

            var abScore = ingotWorklist.ScoreBlueprint(blueprintAB);
            var bcScore = ingotWorklist.ScoreBlueprint(blueprintBC);

            Assert.That(bcScore, Is.GreaterThan(abScore));
        }

        [Test]
        public void HighYieldBlueprintScoresHigherThanLowYieldBlueprint()
        {
            var blueprintALow = new Blueprint("ALow", 1, new ItemAndQuantity("Ore/A", 10), new ItemAndQuantity("Ingot/A", 10));
            var blueprintAHigh = new Blueprint("AHigh", 1, new ItemAndQuantity("Ore/A", 10), new ItemAndQuantity("Ingot/A", 12));
            var stockpiles = new IngotStockpiles(new TestIngotDefinitions { { "Ingot/A", 100 } });
            var ingotWorklist = new IngotWorklist(stockpiles);

            stockpiles.UpdateQuantities(new TestIngotQuantities { { "Ingot/A", 50 } });
            ingotWorklist.Initialise();

            var aLowScore = ingotWorklist.ScoreBlueprint(blueprintALow);
            var aHighScore = ingotWorklist.ScoreBlueprint(blueprintAHigh);

            Assert.That(aHighScore, Is.GreaterThan(aLowScore));
        }

        [Description("Refs a bug in v1.4 where it would prefer iron over gold even when iron ingots were plentiful. Now technically redundant since we always prioritise by demand first.")]
        public class LowYieldHighDemandBlueprint_IsPreferredOver_HighYieldLowDemandBlueprint
        {
            [Test]
            public void IronAndGold()
            {
                var stockpiles = new IngotStockpiles(new[] {
                    new IngotStockpile(new IngotType("Ingot/Gold", 5) { ProductionNormalisationFactor = 0.025 }),
                    new IngotStockpile(new IngotType("Ingot/Iron", 80) { ProductionNormalisationFactor = 14 })
                });
                var ingotWorklist = new IngotWorklist(stockpiles);
                stockpiles.UpdateAssemblerSpeed(10);

                stockpiles.UpdateQuantities(new TestIngotQuantities {
                    { "Ingot/Gold", 27 },
                    { "Ingot/Iron", 15000 }
                });
                ingotWorklist.Initialise();

                IngotStockpile preferred;
                Assert.That(ingotWorklist.TryGetPreferred(out preferred));
                Assert.That(preferred.Ingot.ItemType, Is.EqualTo(new ItemType("Ingot/Gold")));
            }

            [Test]
            public void SilverAndCobalt()
            {
                var stockpiles = new IngotStockpiles(new[] {
                    new IngotStockpile(new IngotType("Ingot/Cobalt", 220) { ProductionNormalisationFactor = 0.075 }),
                    new IngotStockpile(new IngotType("Ingot/Silver", 10) { ProductionNormalisationFactor = 0.1 })
                });
                var ingotWorklist = new IngotWorklist(stockpiles);
                stockpiles.UpdateAssemblerSpeed(10);

                stockpiles.UpdateQuantities(new TestIngotQuantities {
                    { "Ingot/Cobalt", 275 },
                    { "Ingot/Silver", 34 }
                });
                ingotWorklist.Initialise();

                IngotStockpile preferred;
                Assert.That(ingotWorklist.TryGetPreferred(out preferred));
                Assert.That(preferred.Ingot.ItemType, Is.EqualTo(new ItemType("Ingot/Cobalt")));
            }

            [Test]
            public void SilverAndCobalt2()
            {
                var cobalt = new Blueprint("CobaltOreToIngot", 1, new ItemAndQuantity("Ore/Cobalt", 0.25), new ItemAndQuantity("Ingot/Cobalt", 0.075));
                var silver = new Blueprint("SilverOreToIngot", 1, new ItemAndQuantity("Ore/Silver", 1), new ItemAndQuantity("Ingot/Silver", 0.1));
                var stockpiles = new IngotStockpiles(new[] {
                    new IngotStockpile(new IngotType("Ingot/Cobalt", 220) { ProductionNormalisationFactor = 0.075 }),
                    new IngotStockpile(new IngotType("Ingot/Silver", 10) { ProductionNormalisationFactor = 0.1 })
                });
                var ingotWorklist = new IngotWorklist(stockpiles);
                stockpiles.UpdateAssemblerSpeed(10);

                stockpiles.UpdateQuantities(new TestIngotQuantities {
                    { "Ingot/Cobalt", 220 },
                    { "Ingot/Silver", 10 }
                });
                ingotWorklist.Initialise();

                var cobaltScore = ingotWorklist.ScoreBlueprint(cobalt);
                var silverScore = ingotWorklist.ScoreBlueprint(silver);

                Assert.That(silverScore, Is.EqualTo(cobaltScore).Within(0.01).Percent);
            }
        }
    }
}
