using System.Collections.Generic;

namespace IngameScript
{
    public class ConfigurationWriter : IConfigurationWriter<RequestedConfiguration>
    {
        public IEnumerable<string> GenerateParts(RequestedConfiguration configuration)
        {
            if (configuration.OreStatusDisplayName != null)
            {
                yield return "show-ore";
                yield return configuration.OreStatusDisplayName;
            }
            if (configuration.IngotStatusDisplayName != null)
            {
                yield return "show-ingots";
                yield return configuration.IngotStatusDisplayName;
            }
            if (configuration.RefinerySpeedFactor != null)
            {
                yield return "refinery-speed";
                yield return configuration.RefinerySpeedFactor.ToString();
            }
            if (configuration.AssemblerSpeedFactor != null)
            {
                yield return "assembler-speed";
                yield return configuration.AssemblerSpeedFactor.ToString();
            }
            foreach (var blockName in configuration.InventoryBlockNames)
            {
                yield return "scan";
                yield return blockName;
            }
            foreach (var ingot in configuration.Ingots)
            {
                yield return ingot.Value.Enable ? "enable" : "disable";
                yield return $"{ingot.Key.ToShortString()}:{ingot.Value.StockpileTarget}:{ingot.Value.StockpileLimit}";
            }
        }
    }
}
