using IngameScript;
using SharedTesting;
using VRage;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Script.RefineryBalance.v2
{
    public static class MockExtensions
    {
        public static void Add(this MockInventoryBase inventory, ItemType itemType, MyFixedPoint amount) => inventory.Add(AsMyItemType(itemType), amount);

        public static void Remove(this MockInventoryBase inventory, ItemType itemType, MyFixedPoint amount) => inventory.Remove(AsMyItemType(itemType), amount);

        /// <summary>
        /// Generate a dummy MyItemType from an ItemType. Equality, etc apply as usual but
        /// ID values will be weird.
        /// </summary>
        public static MyItemType AsMyItemType(this ItemType itemType) =>
            new MyItemType(new MyObjectBuilderType(typeof(Dummy)), MyStringHash.GetOrCompute(itemType.ToString()));

        class Dummy
        {
        }
    }
}
