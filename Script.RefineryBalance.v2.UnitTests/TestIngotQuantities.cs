using IngameScript;
using System.Collections.Generic;

namespace Script.RefineryBalance.v2
{
    class TestIngotQuantities : Dictionary<ItemType, double>
    {
        public void Add(string itemType, float quantity)
        {
            Add(new ItemType(itemType), quantity);
        }
    }
}
