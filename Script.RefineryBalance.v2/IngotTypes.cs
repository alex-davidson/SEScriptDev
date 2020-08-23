
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public struct IngotTypes
    {
        public readonly ICollection<IngotType> All;

        public IngotTypes(IngotType[] ingotTypes)
        {
            All = ingotTypes;
        }

        public IEnumerable<ItemType> AllIngotItemTypes
        {
            get { return All.Select(i => i.ItemType); }
        }
    }
}
