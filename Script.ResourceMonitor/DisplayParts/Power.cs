using System.Text;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI.Ingame;
using Shared.Core;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public static class Power
    {
        public class SummaryByGrid : IDisplayPart
        {
            private readonly BlockDetailsParser blockDetailsParser = new BlockDetailsParser();
            private readonly AggregateStatisticsTracker<string, Distribution> distributionByGrid = new AggregateStatisticsTracker<string, Distribution>();

            public void Clear() => distributionByGrid.Reset();

            public void Draw(IMyCubeGrid grid, StringBuilder target)
            {
                target.AppendLine("Power:");

                var totalDistribution = new Distribution();
                foreach (var row in distributionByGrid.Enumerate())
                {
                    totalDistribution = totalDistribution.Accumulate(new Distribution
                    {
                        ProducedMegawatts = row.Total.ProducedMegawatts,
                        ConsumedMegawatts = row.Total.ConsumedMegawatts,
                        // Ignore storage on non-producing grids.
                        StoredMegawattHours = row.Total.ProducedMegawatts > 0 ? row.Total.StoredMegawattHours : 0,
                        CapacityMegawattHours = row.Total.ProducedMegawatts > 0 ? row.Total.CapacityMegawattHours : 0,
                    });
                }

                WriteRow(target, "TOTAL", totalDistribution);
                foreach (var row in distributionByGrid.Enumerate(grid.CustomName))
                {
                    WriteRow(target, row.Key, row.Total);
                }
            }

            private void WriteRow(StringBuilder target, string name, Distribution row)
            {
                target.AppendFormat("{0,-20}  {1} -> [{2} / {3}] -> {4}\n",
                    name,
                    Unit.Power.FormatSI(row.ProducedMegawatts),
                    Unit.Energy.FormatSI(row.StoredMegawattHours),
                    Unit.Energy.FormatSI(row.CapacityMegawattHours),
                    Unit.Power.FormatSI(row.ConsumedMegawatts));
            }

            public void Visit(IMyTerminalBlock block)
            {
                var sample = new Distribution();
                var battery = block as IMyBatteryBlock;
                if (battery != null)
                {
                    sample.StoredMegawattHours += battery.CurrentStoredPower;
                    sample.CapacityMegawattHours += battery.MaxStoredPower;
                }
                else
                {
                    var powerProducer = block as IMyPowerProducer;
                    sample.ProducedMegawatts += powerProducer?.CurrentOutput ?? 0;

                    if (blockDetailsParser.Parse(block.DetailedInfo))
                    {
                        sample.ConsumedMegawatts += GetConsumedMegawatts(block, blockDetailsParser);
                    }
                }

                distributionByGrid.Add(block.CubeGrid.CustomName, sample);
            }

            private static float GetConsumedMegawatts(IMyTerminalBlock block, BlockDetailsParser details)
            {
                var consumedValue = details.Get("Required Input") ?? details.Get("Current Input");
                float consumedMegawatts;
                if (Unit.Power.TryParseSI(consumedValue, out consumedMegawatts))
                {
                    return consumedMegawatts;
                }
                // If it has a power consumption detail but we couldn't parse it, bail out.
                if (consumedValue != null) return 0;

                // Other cases?
                return 0;
            }

            struct Distribution : IAccumulator<Distribution>
            {
                public float ConsumedMegawatts { get; set; }
                public float ProducedMegawatts { get; set; }
                public float StoredMegawattHours { get; set; }
                public float CapacityMegawattHours { get; set; }

                public Distribution Accumulate(Distribution record)
                {
                    return new Distribution
                    {
                        ConsumedMegawatts = ConsumedMegawatts + record.ConsumedMegawatts,
                        ProducedMegawatts = ProducedMegawatts + record.ProducedMegawatts,
                        StoredMegawattHours = StoredMegawattHours + record.StoredMegawattHours,
                        CapacityMegawattHours = CapacityMegawattHours + record.CapacityMegawattHours,
                    };
                }
            }
        }
    }
}
