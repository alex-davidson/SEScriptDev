using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Script.RefineryBalance.v2
{
    [TestFixture]
    public partial class Program
    {
        [TestFixture]
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
                stockpiles.UpdateQuantities(new TestIngotQuantities {
                    { "Ingot/A", 5 },   // 50%
                    { "Ingot/B", 10 },  // 10%
                    { "Ingot/C", 8 },   // 40%
                    { "Ingot/D", 12 }   // 150%
                });
                var worklist = stockpiles.GetWorklist();

                IngotStockpile preferred;
                Assume.That(worklist.TryGetPreferred(out preferred));
                Assert.That(preferred.Ingot.ItemType, Is.EqualTo(new ItemType("Ingot/B")));
            }

            [Test]
            public void UpdatingStockpilesUpdatesPreferredIngotType()
            {
                var stockpiles = new IngotStockpiles(new TestIngotDefinitions {
                    { "Ingot/A", 10 },
                    { "Ingot/B", 100 }
                });
                stockpiles.UpdateQuantities(new TestIngotQuantities {
                    { "Ingot/A", 5 },   // 50%
                    { "Ingot/B", 10 }   // 10%
                });
                var worklist = stockpiles.GetWorklist();

                worklist.UpdateStockpileEstimates(DefaultRefinery, DefaultBlueprintProducing("Ingot/B"), 100);

                IngotStockpile preferred;
                Assert.That(worklist.TryGetPreferred(out preferred));
                Assert.That(preferred.Ingot.ItemType, Is.EqualTo(new ItemType("Ingot/A")));
            }

            [Test]
            public void SkippingLastStockpileLeavesEmptyWorklist()
            {
                var stockpiles = new IngotStockpiles(new TestIngotDefinitions {
                    { "Ingot/A", 10 },
                    { "Ingot/B", 100 }
                });
                stockpiles.UpdateQuantities(new TestIngotQuantities {
                    { "Ingot/A", 5 },   // 50%
                    { "Ingot/B", 10 }   // 10%
                });
                var worklist = stockpiles.GetWorklist();

                worklist.UpdateStockpileEstimates(DefaultRefinery, DefaultBlueprintProducing("Ingot/B"), 100);

                IngotStockpile preferred;
                Assume.That(worklist.TryGetPreferred(out preferred));
                worklist.Skip();
                Assume.That(worklist.TryGetPreferred(out preferred));
                worklist.Skip();
                Assert.That(worklist.TryGetPreferred(out preferred), Is.False);
            }
        }
    }
}
