using System;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public static class Batteries
    {
        public class SummaryByGrid : IDisplayPart
        {
            private readonly GroupedStatisticsTracker<string, BatteryCharge> chargeByGrid = new GroupedStatisticsTracker<string, BatteryCharge>();

            public void Clear() => chargeByGrid.Reset();

            public void Draw(IMyCubeGrid grid, StringBuilder target)
            {
                target.AppendLine("Batteries:");
                foreach (var row in chargeByGrid.Enumerate(grid.CustomName))
                {
                    var totalCapacity = 0f;
                    var totalDischargeCapacity = 0f;
                    var count = 0;
                    foreach (var sample in row.Samples)
                    {
                        totalCapacity += sample.Percentage;
                        if (sample.ChargeMode != ChargeMode.Recharge) totalDischargeCapacity += sample.Percentage;
                        count++;
                    }
                    target.AppendFormat("{0,-20}  {1,3:#0%} / {2,3:#0%}\n", row.Group, totalDischargeCapacity / count, totalCapacity / count);
                }
            }

            public void Visit(IMyTerminalBlock block)
            {
                var battery = block as IMyBatteryBlock;
                if (battery == null) return;
                var charge = new BatteryCharge { Percentage = battery.CurrentStoredPower / battery.MaxStoredPower, ChargeMode = battery.ChargeMode };
                chargeByGrid.Add(block.CubeGrid.CustomName, charge);
            }

            struct BatteryCharge
            {
                public float Percentage { get; set; }
                public ChargeMode ChargeMode { get; set; }
            }
        }
    }
}
