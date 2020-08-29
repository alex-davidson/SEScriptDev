
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public struct OreTypes
    {
        private readonly IDictionary<ItemType, Blueprint> entries;
        public readonly ICollection<ItemType> All;

        public OreTypes(IEnumerable<ItemType> ores, IEnumerable<Blueprint> blueprints)
        {
            All = new HashSet<ItemType>(ores);
            entries = blueprints.ToDictionary(b => b.Input.ItemType);
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
            Blueprint blueprint;
            if (!entries.TryGetValue(itemType, out blueprint)) return 0;
            return blueprint.Input.Quantity / blueprint.Duration;
        }

        public Blueprint? GetBlueprint(ItemType itemType)
        {
            Blueprint blueprint;
            if (!entries.TryGetValue(itemType, out blueprint)) return null;
            return blueprint;
        }
    }
}
