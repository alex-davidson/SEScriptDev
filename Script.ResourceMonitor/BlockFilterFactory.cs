using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript
{
    public class BlockFilterFactory
    {
        public BlockRuleFilter CreateRuleFilter(IList<BlockRule> rules)
        {
            // Currently only name rules can be specified, so this is simple.

            var names = new List<string>(Constants.ALLOC_SCAN_RULE_COUNT);
            var onlyIncluded = false;
            foreach (var rule in rules)
            {
                if (rule.Include == null) continue;
                if (string.IsNullOrWhiteSpace(rule.BlockName)) continue;
                if (rule.Include == true)
                {
                    // As soon as we see an include, we can ignore all excludes because our include-all rule is dropped.
                    if (!onlyIncluded) names.Clear();
                    onlyIncluded = true;
                    names.Add(rule.BlockName);
                }
                else if (!onlyIncluded)
                {
                    names.Add(rule.BlockName);
                }
            }

            // If no rules, include everything.
            if (names.Count == 0) return new BlockRuleFilter();

            return new BlockRuleFilter(names, onlyIncluded);;
        }

        public struct BlockRuleFilter
        {
            private readonly ICollection<string> names;
            private readonly bool onlyIncluded;

            public BlockRuleFilter(ICollection<string> names, bool onlyIncluded)
            {
                this.names = names;
                this.onlyIncluded = onlyIncluded;
            }

            public bool Filter(IMyTerminalBlock block)
            {
                if (!block.IsFunctional) return false;
                if (names == null) return true;
                if (onlyIncluded) return names.Contains(block.CustomName);
                return !names.Contains(block.CustomName);
            }
        }
    }
}
