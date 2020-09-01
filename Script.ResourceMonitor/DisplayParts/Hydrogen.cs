using System.Text;
using Sandbox.ModAPI.Ingame;
using Shared.Core;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public static class Hydrogen
    {
        public class SummaryByGrid : IDisplayPart
        {
            private readonly AggregateStatisticsTracker<string, TankCapacity> capacityByGrid = new AggregateStatisticsTracker<string, TankCapacity>();

            public void Clear() => capacityByGrid.Reset();

            public void Draw(IMyCubeGrid grid, StringBuilder target)
            {
                target.AppendLine("Hydrogen:");
                foreach (var row in capacityByGrid.Enumerate(grid.CustomName))
                {
                    WriteRow(target, row.Key, row.Total);
                }
            }

            private void WriteRow(StringBuilder target, string name, TankCapacity row)
            {
                target.AppendFormat("{0,-20}", name);
                if (row.TankCount > 1) target.AppendFormat(" x{0,-2}", row.TankCount); else target.Append("    ");
                target.AppendFormat("  {0,3:#0%}", row.AvailableContents / row.Capacity);
                target.AppendFormat("  {0,4} / {1,4}", Unit.Volume.FormatSI(row.AvailableContents), Unit.Volume.FormatSI(row.Capacity));
                if (row.IsStockpiling) target.AppendFormat("  ({0,4})", Unit.Volume.FormatSI(row.Contents));
                target.AppendLine();
            }

            public void Visit(IMyTerminalBlock block)
            {
                var tank = block as IMyGasTank;
                if (tank == null) return;
                if (!tank.BlockDefinition.SubtypeId.Contains("Hydrogen")) return;

                var contents = tank.Capacity * (float)tank.FilledRatio;
                capacityByGrid.Add(tank.CubeGrid?.CustomName,
                    new TankCapacity
                    {
                        Contents = contents,
                        Capacity = tank.Capacity,
                        AvailableContents = tank.Stockpile ? 0 : contents,
                        IsStockpiling = contents > 0.01 && tank.Stockpile,
                        TankCount = 1,
                    });
            }

            struct TankCapacity : IAccumulator<TankCapacity>
            {
                public float Contents { get; set; }
                public float Capacity { get; set; }
                public float AvailableContents { get; set; }
                public bool IsStockpiling { get; set; }
                public int TankCount { get; set; }

                public TankCapacity Accumulate(TankCapacity record)
                {
                    return new TankCapacity
                    {
                        Contents = Contents + record.Contents,
                        Capacity = Capacity + record.Capacity,
                        AvailableContents = AvailableContents + record.AvailableContents,
                        IsStockpiling = IsStockpiling || record.IsStockpiling,
                        TankCount = TankCount + record.TankCount,
                    };
                }
            }
        }
    }
}
