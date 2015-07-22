using Moq;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Script.RefineryBalance.v2
{
    public partial class Program
    {
        class TestIngotQuantities : Dictionary<ItemType, double>
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

        private static Refinery DefaultRefinery = Refinery.Get(new Mock<IMyRefinery>().Object, new RefineryType { Efficiency = 1, Speed = 1 }, 1);
        private static Blueprint DefaultBlueprintProducing(string itemTypePath)
        {
            return new Blueprint("BP", 1, new ItemAndQuantity("Ore/Anything", 1), new ItemAndQuantity(itemTypePath, 1));
        }
    }
}
