using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public class ConfigurationReader : IConfigurationReader<RequestedConfiguration>
    {
        public bool Read(RequestedConfiguration configuration, IEnumerator<string> parts)
        {
            while (parts.MoveNext())
            {
                switch (parts.Current?.ToLower())
                {
                    case "enable":
                        if (!ExpectNext(parts, "Expected a parameter to follow 'enable'.")) return false;
                        if (!Enable(configuration, parts.Current)) return false;
                        break;
                    case "disable":
                        if (!ExpectNext(parts, "Expected a parameter to follow 'disable'.")) return false;
                        if (!Disable(configuration, parts.Current)) return false;
                        break;

                    case "scan":
                        if (!ExpectNext(parts, "Expected a parameter to follow 'scan'.")) return false;
                        configuration.InventoryBlockNames.Add(parts.Current);
                        break;

                    case "scan-all":
                        configuration.InventoryBlockNames.Clear();
                        break;

                    case "show-ingots":
                        if (!ExpectNext(parts, "Expected a display block name or empty string to follow 'show-ingots'.")) return false;
                        configuration.IngotStatusDisplayName = String.IsNullOrWhiteSpace(parts.Current) ? null : parts.Current;
                        break;

                    case "show-ore":
                        if (!ExpectNext(parts, "Expected a display block name or empty string to follow 'show-ore'.")) return false;
                        configuration.OreStatusDisplayName = String.IsNullOrWhiteSpace(parts.Current) ? null : parts.Current;
                        break;

                    case "refinery-speed":
                        if (!ExpectNext(parts, "Expected a numeric value or '?' to follow 'refinery-speed'.")) return false;
                        float? refinerySpeed;
                        if (!TryReadNumberOrNull(parts.Current, out refinerySpeed)) return false;
                        configuration.RefinerySpeedFactor = refinerySpeed;
                        break;

                    case "assembler-speed":
                        if (!ExpectNext(parts, "Expected a numeric value or '?' to follow 'assembler-speed'.")) return false;
                        float? assemblerSpeed;
                        if (!TryReadNumberOrNull(parts.Current, out assemblerSpeed)) return false;
                        configuration.AssemblerSpeedFactor = assemblerSpeed;
                        break;

                    default:
                        Debug.Write(Debug.Level.Error, "Unrecognised parameter: {0}", parts.Current);
                        return false;
                }
            }
            return true;
        }

        private bool Enable(RequestedConfiguration configuration, string current)
        {
            var segments = current.Split(':');
            var itemType = ReadIngotItemType(segments.First());
            RequestedIngotConfiguration ingotConfiguration;
            if (!configuration.Ingots.TryGetValue(itemType, out ingotConfiguration))
            {
                ingotConfiguration = new RequestedIngotConfiguration();
                configuration.Ingots[itemType] = ingotConfiguration;
            }
            ingotConfiguration.Enable = true;
            if (!TryApplyIngotConfiguration(ingotConfiguration, segments)) return false;
            return true;
        }

        private bool Disable(RequestedConfiguration configuration, string current)
        {
            var segments = current.Split(':');
            var itemType = ReadIngotItemType(segments.First());
            RequestedIngotConfiguration ingotConfiguration;
            if (!configuration.Ingots.TryGetValue(itemType, out ingotConfiguration))
            {
                ingotConfiguration = new RequestedIngotConfiguration();
                configuration.Ingots[itemType] = ingotConfiguration;
            }
            ingotConfiguration.Enable = false;
            if (!TryApplyIngotConfiguration(ingotConfiguration, segments)) return false;
            return true;
        }

        private static ItemType ReadIngotItemType(string segment)
        {
            if (segment.Contains("/")) return new ItemType(segment);
            return new ItemType("Ingot/" + segment);
        }

        private static bool TryApplyIngotConfiguration(RequestedIngotConfiguration ingotConfiguration, string[] segments)
        {
            var targetParam = segments.ElementAtOrDefault(1)?.Trim();
            if (!String.IsNullOrWhiteSpace(targetParam))
            {
                float? targetValue;
                if (!TryReadNumberOrNull(targetParam, out targetValue)) return false;
                ingotConfiguration.StockpileTarget = targetValue;
            }

            var limitParam = segments.ElementAtOrDefault(2)?.Trim();
            if (!String.IsNullOrWhiteSpace(limitParam))
            {
                float? limitValue;
                if (!TryReadNumberOrNull(limitParam, out limitValue)) return false;
                ingotConfiguration.StockpileLimit = limitValue;
            }
            return true;
        }

        private static bool ExpectNext(IEnumerator<string> parts, string error)
        {
            if (parts.MoveNext()) return true;
            Debug.Write(Debug.Level.Error, error);
            return false;
        }

        private static bool TryReadNumberOrNull(string part, out float? numberOrNull)
        {
            numberOrNull = null;
            if (part == "?") return true;
            float number;
            if (float.TryParse(part, out number))
            {
                numberOrNull = number;
                return true;
            }
            Debug.Write(Debug.Level.Error,  "Expected a numeric value or '?', but found '{0}'.", part);
            return false;
        }
    }
}
