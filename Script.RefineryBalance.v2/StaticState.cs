using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public class StaticState
    {
        public StaticState(RequestedConfiguration configuration)
        {
            Blueprints = new Blueprints(Constants.BLUEPRINTS);
            RefineryFactory = new RefineryFactory(Constants.REFINERY_TYPES);

            var ores = Constants.BLUEPRINTS.Select(b => b.Input.ItemType).Distinct();

            OreTypes = new OreTypes(ores, Constants.BLUEPRINTS);

            var ingotTypes = PrepareIngotTypes(configuration, Blueprints).ToArray();
            IngotTypes = new IngotTypes(ingotTypes);

            RefinerySpeedFactor = configuration.RefinerySpeedFactor;
            AssemblerSpeedFactor = configuration.AssemblerSpeedFactor;
            IngotStatusDisplayName = configuration.IngotStatusDisplayName;
            OreStatusDisplayName = configuration.OreStatusDisplayName;
            InventoryBlockNames = configuration.InventoryBlockNames;
        }

        public readonly RefineryFactory RefineryFactory;
        public readonly Blueprints Blueprints;
        public readonly OreTypes OreTypes;
        public readonly IngotTypes IngotTypes;
        public readonly IList<string> InventoryBlockNames;
        public readonly string IngotStatusDisplayName;
        public readonly string OreStatusDisplayName;

        public readonly float? RefinerySpeedFactor;
        public readonly float? AssemblerSpeedFactor;

        class IngotConfigurer
        {
            private readonly IDictionary<ItemType, RequestedIngotConfiguration> ingotConfigurations;

            public IngotConfigurer(IDictionary<ItemType, RequestedIngotConfiguration> ingotConfigurations)
            {
                this.ingotConfigurations = ingotConfigurations;
            }

            public IngotType Configure(IngotType type)
            {
                RequestedIngotConfiguration ingotConfig;
                if (!ingotConfigurations.TryGetValue(type.ItemType, out ingotConfig)) return type;
                type.Enabled = true; // Enable it if it's not already enabled.
                type.StockpileTargetOverride = ingotConfig.StockpileTarget;
                type.StockpileLimit = ingotConfig.StockpileLimit;
                return type;
            }
        }

        private static IEnumerable<IngotType> PrepareIngotTypes(RequestedConfiguration configuration, Blueprints blueprints)
        {
            return Constants.INGOT_TYPES
                .Select(new IngotConfigurer(configuration.Ingots).Configure)
                .Where(i => i.Enabled)
                .Select(blueprints.CalculateNormalisationFactor);
        }
    }

}
