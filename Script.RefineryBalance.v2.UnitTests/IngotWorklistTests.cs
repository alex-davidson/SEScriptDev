using System.Collections.Generic;
using System.Linq;
using IngameScript;
using NUnit.Framework;

namespace Script.RefineryBalance.v2
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
            var ingotWorklist = new IngotWorklist(stockpiles);
            stockpiles.UpdateQuantities(new TestIngotQuantities {
                { "Ingot/A", 5 },   // 50%
                { "Ingot/B", 10 },  // 10%
                { "Ingot/C", 8 },   // 40%
                { "Ingot/D", 12 }   // 150%
            });
            ingotWorklist.Initialise();

            IngotStockpile preferred;
            Assume.That(ingotWorklist.TryGetPreferred(out preferred));
            Assert.That(preferred.Ingot.ItemType, Is.EqualTo(new ItemType("Ingot/B")));
        }

        [Test]
        public void UpdatingStockpilesUpdatesPreferredIngotType()
        {
            var stockpiles = new IngotStockpiles(new TestIngotDefinitions {
                { "Ingot/A", 10 },
                { "Ingot/B", 100 }
            });
            var ingotWorklist = new IngotWorklist(stockpiles);
            stockpiles.UpdateQuantities(new TestIngotQuantities {
                { "Ingot/A", 5 },   // 50%
                { "Ingot/B", 10 }   // 10%
            });
            ingotWorklist.Initialise();

            ingotWorklist.UpdateStockpileEstimates(Util.DefaultRefinery, Util.DefaultBlueprintProducing("Ingot/B"), 100);

            IngotStockpile preferred;
            Assert.That(ingotWorklist.TryGetPreferred(out preferred));
            Assert.That(preferred.Ingot.ItemType, Is.EqualTo(new ItemType("Ingot/A")));
        }

        [Test]
        public void SkippingLastStockpileLeavesEmptyWorklist()
        {
            var stockpiles = new IngotStockpiles(new TestIngotDefinitions {
                { "Ingot/A", 10 },
                { "Ingot/B", 100 }
            });
            var ingotWorklist = new IngotWorklist(stockpiles);
            stockpiles.UpdateQuantities(new TestIngotQuantities {
                { "Ingot/A", 5 },   // 50%
                { "Ingot/B", 10 }   // 10%
            });
            ingotWorklist.Initialise();

            ingotWorklist.UpdateStockpileEstimates(Util.DefaultRefinery, Util.DefaultBlueprintProducing("Ingot/B"), 100);

            IngotStockpile preferred;
            Assume.That(ingotWorklist.TryGetPreferred(out preferred));
            ingotWorklist.Skip();
            Assume.That(ingotWorklist.TryGetPreferred(out preferred));
            ingotWorklist.Skip();
            Assert.That(ingotWorklist.TryGetPreferred(out preferred), Is.False);
        }
    }
}
