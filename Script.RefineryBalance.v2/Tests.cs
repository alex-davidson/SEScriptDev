using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;

namespace Script.RefineryBalance.v2
{
    [TestFixture]
    public partial class Program
    {
        private readonly IMyRefinery mockRefinery = new Mock<IMyRefinery>().Object;

        [Test]
        public void AssignedWorkAffectsQuotaFactor()
        {
            var blueprint = new Blueprint("A", 1, new ItemAndQuantity("Ore/A", 10), new ItemAndQuantity("Ingot/A", 10));
            var stockpiles = new IngotStockpiles(new TestIngotDefinitions { { "Ingot/A", 100 } });
            var refinery = new Refinery(mockRefinery, new RefineryType("Refinery") { SupportedBlueprints = { "A" } }, 1);

            var worklist = stockpiles.UpdateQuantities(new TestIngotQuantities { { "Ingot/A", 50 } });

            Assume.That(worklist.Preferred.EstimatedProduction, Is.EqualTo(0));
            var initialQuotaFraction = worklist.Preferred.QuotaFraction;
            worklist.UpdateStockpileEstimates(refinery, blueprint, 5);

            Assert.That(worklist.Preferred.QuotaFraction, Is.GreaterThan(initialQuotaFraction));
            Assert.That(worklist.Preferred.EstimatedProduction, Is.GreaterThan(0));
        }

        [Test]
        public void ToleratesBlueprintsWithUnknownIngotTypes()
        {
            var blueprint = new Blueprint("A", 1, new ItemAndQuantity("Ore/A", 10), new ItemAndQuantity("Ingot/A", 10));
            var stockpiles = new IngotStockpiles(new TestIngotDefinitions { { "Ingot/B", 100 } });
            var refinery = new Refinery(mockRefinery, new RefineryType("Refinery") { SupportedBlueprints = { "A" } }, 1);

            var worklist = stockpiles.UpdateQuantities(new TestIngotQuantities { { "Ingot/B", 20 } });

            worklist.ScoreBlueprint(blueprint);
            worklist.UpdateStockpileEstimates(refinery, blueprint, 5);
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

            var worklist = stockpiles.UpdateQuantities(new TestIngotQuantities {
                { "Ingot/A", 500 },  // Not in demand.
                { "Ingot/B", 50 },
                { "Ingot/C", 50 }
            });

            var abScore = worklist.ScoreBlueprint(blueprintAB);
            var bcScore = worklist.ScoreBlueprint(blueprintBC);

            Assert.That(bcScore, Is.GreaterThan(abScore));
        }

        [Test]
        public void HighYieldBlueprintScoresHigherThanLowYieldBlueprint()
        {
            var blueprintALow = new Blueprint("ALow", 1, new ItemAndQuantity("Ore/A", 10), new ItemAndQuantity("Ingot/A", 10));
            var blueprintAHigh = new Blueprint("AHigh", 1, new ItemAndQuantity("Ore/A", 10), new ItemAndQuantity("Ingot/A", 12));
            var stockpiles = new IngotStockpiles(new TestIngotDefinitions { { "Ingot/A", 100 } });
            
            var worklist = stockpiles.UpdateQuantities(new TestIngotQuantities { { "Ingot/A", 50 } });

            var aLowScore = worklist.ScoreBlueprint(blueprintALow);
            var aHighScore = worklist.ScoreBlueprint(blueprintAHigh);

            Assert.That(aHighScore, Is.GreaterThan(aLowScore));
        }

        [Description("Refs a bug in v1.4 where it would prefer iron over gold even when iron ingots were plentiful. Now technically redundant since we always prioritise by demand first.")]
        public class LowYieldHighDemandBlueprint_IsPreferredOver_HighYieldLowDemandBlueprint
        {
            [Test]
            public void IronAndGold()
            {
                var stockpiles = new IngotStockpiles(new [] {
                    new IngotStockpile(new IngotType("Ingot/Gold", 5) { ProductionNormalisationFactor = 0.025 }),
                    new IngotStockpile(new IngotType("Ingot/Iron", 80) { ProductionNormalisationFactor = 14 })
                });
                stockpiles.UpdateAssemblerSpeed(10);
                
                var worklist = stockpiles.UpdateQuantities(new TestIngotQuantities {
                    { "Ingot/Gold", 27 },
                    { "Ingot/Iron", 15000 }
                });

                Assert.That(worklist.Preferred.Ingot.ItemType, Is.EqualTo(new ItemType("Ingot/Gold")));
            }

            [Test]
            public void SilverAndCobalt()
            {
                var stockpiles = new IngotStockpiles(new [] {
                    new IngotStockpile(new IngotType("Ingot/Cobalt", 220) { ProductionNormalisationFactor = 0.075 }),
                    new IngotStockpile(new IngotType("Ingot/Silver", 10) { ProductionNormalisationFactor = 0.1 })
                });
                stockpiles.UpdateAssemblerSpeed(10);
                
                var worklist = stockpiles.UpdateQuantities(new TestIngotQuantities {
                    { "Ingot/Cobalt", 275 },
                    { "Ingot/Silver", 34 }
                });

                Assert.That(worklist.Preferred.Ingot.ItemType, Is.EqualTo(new ItemType("Ingot/Cobalt")));
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
                stockpiles.UpdateAssemblerSpeed(10);
                
                var worklist = stockpiles.UpdateQuantities(new TestIngotQuantities {
                    { "Ingot/Cobalt", 220 },
                    { "Ingot/Silver", 10 }
                });

                var cobaltScore = worklist.ScoreBlueprint(cobalt);
                var silverScore = worklist.ScoreBlueprint(silver);

                Assert.That(silverScore, Is.EqualTo(cobaltScore).Within(0.01).Percent);
            }
        }

        public class IngotWorklistTests
        {
            [Test]
            public void InitialPreferredIngotTypeIsFurthestFromTarget()
            {
                var stockpiles = new IngotStockpiles(new TestIngotDefinitions {
                    { "Ingot/A", 10 },
                    { "Ingot/B", 100 },
                    { "Ingot/C", 20 },
                    { "Ingot/D", 8 }
                });
                var worklist = stockpiles.UpdateQuantities(new TestIngotQuantities {
                    { "Ingot/A", 5 },   // 50%
                    { "Ingot/B", 10 },  // 10%
                    { "Ingot/C", 8 },   // 40%
                    { "Ingot/D", 12 }   // 150%
                });
                Assert.That(worklist.Preferred.Ingot.ItemType, Is.EqualTo(new ItemType("Ingot/B")));
            }
        }

        #region Test helper classes

        class TestIngotQuantities : Dictionary<ItemType, float>
        {
            public void Add(string itemType, float quantity)
            {
                Add(new ItemType(itemType), quantity);
            }
        }

        class TestIngotDefinitions : List<IngotStockpile>
        {
            public void Add(string itemType, float consumedPerSecond)
            {
                Add(new IngotStockpile(new IngotType(itemType, consumedPerSecond) { ProductionNormalisationFactor = 1 }));
            }
        }

        #endregion
    }
}
