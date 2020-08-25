using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public class RequestedConfiguration : IDeepCopyable<RequestedConfiguration>
    {
        public RequestedConfiguration()
        {
            Ingots = new Dictionary<ItemType, RequestedIngotConfiguration>();
            InventoryBlockNames = new List<string>();
        }
 
        public IDictionary<ItemType, RequestedIngotConfiguration> Ingots { get; private set; }
        public List<string> InventoryBlockNames { get; private set; }

        public float? RefinerySpeedFactor;
        public float? AssemblerSpeedFactor;
        public string IngotStatusDisplayName;
        public string OreStatusDisplayName;

        public RequestedConfiguration Copy()
        {
            return new RequestedConfiguration
            {
                Ingots = Ingots.ToDictionary(i => i.Key, i => i.Value.Copy()),
                InventoryBlockNames = InventoryBlockNames.ToList(),
                RefinerySpeedFactor = RefinerySpeedFactor,
                AssemblerSpeedFactor = AssemblerSpeedFactor,
                IngotStatusDisplayName = IngotStatusDisplayName,
                OreStatusDisplayName = OreStatusDisplayName,
            };
        }
    }
}
