using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public struct RefineryFactory
    {
        private readonly Dictionary<string, RefineryType> refineryTypesByBlockDefinitionString;
        public readonly ICollection<RefineryType> AllTypes;

        public RefineryFactory(IEnumerable<RefineryType> refineryTypes)
        {
            refineryTypesByBlockDefinitionString = refineryTypes.ToDictionary(t => t.BlockDefinitionName);
            AllTypes = refineryTypesByBlockDefinitionString.Values;
        }

        public Refinery TryResolveRefinery(IMyRefinery block, double refinerySpeedFactor)
        {
            RefineryType type;
            if (refineryTypesByBlockDefinitionString.TryGetValue(block.BlockDefinition.ToString(), out type))
            {
                return Refinery.Get(block, type, refinerySpeedFactor);
            }
            return null;
        }
    }
}
