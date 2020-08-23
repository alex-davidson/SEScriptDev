
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public struct OreTypes
    {
        private readonly IDictionary<ItemType, double> oreConsumedPerSecond;
        public readonly ICollection<ItemType> All;

        public OreTypes(IEnumerable<ItemType> ores, IEnumerable<Blueprint> blueprints)
        {
            All = new HashSet<ItemType>(ores);
            oreConsumedPerSecond = blueprints
                .GroupBy(b => b.Input.ItemType, b => b.Input.Quantity / b.Duration)
                .ToDictionary(g => g.Key, g => g.Max());
        }

        public double GetSecondsToClear(MyInventoryItem item)
        {
            var type = new ItemType(item.Type.TypeId, item.Type.SubtypeId);
            var perSecond = GetAmountConsumedPerSecond(type);
            if (perSecond <= 0) return 0;
            return ((double)item.Amount) / perSecond;
        }

        public double GetAmountConsumedPerSecond(ItemType itemType)
        {
            double perSecond;
            if (!oreConsumedPerSecond.TryGetValue(itemType, out perSecond)) return 0f;
            return perSecond;
        }
    }
}
