using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Script.RefineryBalance.v2
{
    public partial class Program
    {
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
    }
}
