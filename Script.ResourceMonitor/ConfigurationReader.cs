using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public class ConfigurationReader : IConfigurationReader<RequestedConfiguration>
    {
        public bool Read(RequestedConfiguration configuration, IEnumerable<string> parts)
        {
            using (var enumerator = parts.GetEnumerator())
            {
                return Read(configuration, enumerator);
            }
        }

        public bool Read(RequestedConfiguration configuration, IEnumerator<string> parts)
        {
            while (parts.MoveNext())
            {
                switch (parts.Current?.ToLower())
                {
                    case "include":
                        if (!ExpectNext(parts, "Expected a block name to follow 'include'.")) return false;
                        ResetBlockRule(configuration, parts.Current);
                        configuration.BlockRules.Add(new BlockRule { BlockName = parts.Current, Include = true });
                        break;
                    case "exclude":
                        if (!ExpectNext(parts, "Expected a block name to follow 'exclude'.")) return false;
                        ResetBlockRule(configuration, parts.Current);
                        configuration.BlockRules.Add(new BlockRule { BlockName = parts.Current, Include = false });
                        break;
                    case "forget":
                        if (!ExpectNext(parts, "Expected a block name to follow 'forget'.")) return false;
                        ResetBlockRule(configuration, parts.Current);
                        break;

                    case "display":
                        if (!ExpectNext(parts, "Expected a display name to follow 'display'.")) return false;
                        var displayIndex = configuration.Displays.FindIndex(d => d.DisplayName == parts.Current);
                        var displayConfiguration = displayIndex < 0 ? new RequestedDisplayConfiguration { DisplayName = parts.Current } : configuration.Displays[displayIndex];

                        if (!ParseDisplayConfiguration(displayConfiguration, parts)) return false;

                        if (!displayConfiguration.IncludeCategories.Any())
                        {
                            // Empty display.
                            if (displayIndex >= 0) configuration.Displays.RemoveAtFast(displayIndex);
                        }
                        else if (displayIndex < 0)
                        {
                            // New display.
                            configuration.Displays.Add(displayConfiguration);
                        }
                        else
                        {
                            // Already updated in-place.
                        }
                        if (configuration.Displays.Count(d => d.DisplayName == displayConfiguration.DisplayName) > 1)
                        {
                            Debug.Write(Debug.Level.Error, new Message("Duplicate display name: {0}", displayConfiguration.DisplayName));
                            return false;
                        }
                        break;

                    default:
                        Debug.Write(Debug.Level.Error, new Message("Unrecognised parameter: {0}", parts.Current));
                        return false;
                }
            }
            return true;
        }

        private bool ParseDisplayConfiguration(RequestedDisplayConfiguration displayConfiguration, IEnumerator<string> parts)
        {
            if (parts.MoveNext())
            {
                if (parts.Current == "add")
                {
                    if (!ExpectNextToken(parts, "{", "Expected '{' to follow 'display <name> add'.")) return false;
                    while (parts.MoveNext())
                    {
                        if (parts.Current == "}") return true;
                        displayConfiguration.IncludeCategories.Add(parts.Current);
                    }
                    Debug.Write(Debug.Level.Error, "Expected display configuration to end with '}'.");
                    return false;
                }
                if (parts.Current == "clear")
                {
                    displayConfiguration.IncludeCategories.Clear();
                    return true;
                }
                if (parts.Current == "rename-to")
                {
                    if (!ExpectNext(parts, "Expected a block name to follow 'display <name> rename-to'.")) return false;
                    displayConfiguration.DisplayName = parts.Current;
                    return true;
                }
            }
            Debug.Write(Debug.Level.Error, "Expected 'add', 'clear', or 'rename-to' to follow 'display <name>'.");
            return false;
        }

        private void ResetBlockRule(RequestedConfiguration configuration, string blockName)
        {
            var index = configuration.BlockRules.FindIndex(b => b.BlockName == blockName);
            if (index < 0) return;  // Not present. Ignore.
            if (index == configuration.BlockRules.Count - 1)
            {
                // Last rule. Remove by truncating the list.
                configuration.BlockRules.RemoveAt(index);
                return;
            }
            // In the middle somewhere. Null it out. It will be dropped during serialisation to storage.
            configuration.BlockRules[index] = new BlockRule();
        }

        private static bool ExpectNext(IEnumerator<string> parts, string error)
        {
            if (parts.MoveNext()) return true;
            Debug.Write(Debug.Level.Error, error);
            return false;
        }

        private static bool ExpectNextToken(IEnumerator<string> parts, string token, string error)
        {
            if (parts.MoveNext() && parts.Current == token) return true;
            Debug.Write(Debug.Level.Error, error);
            return false;
        }
    }
}
