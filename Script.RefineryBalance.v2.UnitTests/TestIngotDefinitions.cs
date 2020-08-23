using IngameScript;
using System.Collections.Generic;

namespace Script.RefineryBalance.v2
{
    class TestIngotDefinitions : List<IngotStockpile>
    {
        public void Add(string itemType, float consumedPerSecond)
        {
            Add(new IngotStockpile(new IngotType(itemType, consumedPerSecond) { ProductionNormalisationFactor = 1 }));
        }
    }
}
