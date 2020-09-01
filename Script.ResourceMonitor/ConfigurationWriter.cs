using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public class ConfigurationWriter : IConfigurationWriter<RequestedConfiguration>
    {
        public IEnumerable<string> GenerateParts(RequestedConfiguration configuration)
        {
            foreach (var rule in configuration.BlockRules)
            {
                if (rule.Include == null) continue;
                if (string.IsNullOrWhiteSpace(rule.BlockName)) continue;

                yield return rule.Include.Value ? "include" : "exclude";
                yield return rule.BlockName;
            }
            foreach (var display in configuration.Displays)
            {
                if (!display.IncludeCategories.Any()) continue;
                if (string.IsNullOrWhiteSpace(display.DisplayName)) continue;

                yield return "display";
                yield return display.DisplayName;
                yield return "add";
                yield return "{";
                foreach (var category in display.IncludeCategories)
                {
                    yield return category;
                }
                yield return "}";
            }
        }
    }
}
