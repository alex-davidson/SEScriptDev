using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public class RequestedConfiguration : IDeepCopyable<RequestedConfiguration>
    {
        public List<BlockRule> BlockRules { get; private set; } = new List<BlockRule>();
        public List<RequestedDisplayConfiguration> Displays { get; private set; } = new List<RequestedDisplayConfiguration>();

        public RequestedConfiguration Copy()
        {
            return new RequestedConfiguration
            {
                BlockRules = BlockRules.ToList(),
                Displays = Displays.Select(d => d.Copy()).ToList(),
            };
        }

        public static RequestedConfiguration GetDefault()
        {
            return new RequestedConfiguration
            {
                Displays =
                {
                    new RequestedDisplayConfiguration
                    {
                        DisplayName = "Display.Monitor.Batteries",
                        IncludeCategories = { "Batteries.SummaryByGrid" }
                    },
                    new RequestedDisplayConfiguration
                    {
                        DisplayName = "Display.Monitor.Power",
                        IncludeCategories = { "Power.SummaryByGrid" }
                    },
                    new RequestedDisplayConfiguration
                    {
                        DisplayName = "Display.Monitor.Gas",
                        IncludeCategories = { "Oxygen.SummaryByGrid", "Hydrogen.SummaryByGrid", "Ice.StocksAndProcessing" }
                    },
                },
            };
        }
    }

    public struct BlockRule
    {
        public string BlockName { get; set; }
        public bool? Include { get; set; }
    }

    public class RequestedDisplayConfiguration
    {
        public string DisplayName { get; set; }
        public List<string> IncludeCategories { get; private set; } = new List<string>();

        public RequestedDisplayConfiguration Copy()
        {
            return new RequestedDisplayConfiguration
            {
                DisplayName = DisplayName,
                IncludeCategories = IncludeCategories.ToList(),
            };
        }
    }
}
