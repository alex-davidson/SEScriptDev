using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public static class SupportUtil
    {
        public static IList<float> ParseModuleBonuses(IMyTerminalBlock block)
        {
            var percentages = new List<float>();
            if (string.IsNullOrEmpty(block.DetailedInfo)) return percentages;

            var lines = block.DetailedInfo.Split('\n');
            // A blank line separates block info from module bonuses.
            var foundBlankLine = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Trim() == "")
                {
                    if (foundBlankLine) break;
                    foundBlankLine = true;
                    continue;
                }
                if (!foundBlankLine) continue;

                float percent = 100;
                var m = rxModuleBonusPercent.Match(line);
                if (m.Success)
                {
                    if (!float.TryParse(m.Groups["p"].Value, out percent)) percent = 100;
                }
                percentages.Add(percent / 100);
            }
            return percentages;
        }
        private static readonly System.Text.RegularExpressions.Regex rxModuleBonusPercent = new System.Text.RegularExpressions.Regex(@":\s*(?<p>\d+)%");
    }
}
