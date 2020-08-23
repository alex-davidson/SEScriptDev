using IngameScript;
using Sandbox.ModAPI.Ingame;

namespace Script.RefineryBalance.v2
{
    public static class Util
    {
        public static Refinery DefaultRefinery = Refinery.Get(Mocks.MockRefinery(), new RefineryType { Efficiency = 1, Speed = 1 }, 1);
        public static Blueprint DefaultBlueprintProducing(string itemTypePath)
        {
            return new Blueprint("BP", 1, new ItemAndQuantity("Ore/Anything", 1), new ItemAndQuantity(itemTypePath, 1));
        }
    }
}
