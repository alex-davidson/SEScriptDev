using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript
{
    public class BlockFilterFactory
    {
        public Func<IMyTerminalBlock, bool> CreateFilter(IList<BlockRule> rules)
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

            // No rules; include everything.
            if (names.Count == 0) return b => b.IsOperational();

            if (onlyIncluded) return b => b.IsOperational() && names.Contains(b.CustomName);
            return b => b.IsOperational() && !names.Contains(b.CustomName);
        }
    }
}
