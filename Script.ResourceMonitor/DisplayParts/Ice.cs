using System;
using System.Text;
using Sandbox.ModAPI.Ingame;
using Shared.Core;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public static class Ice
    {
        public class StocksAndProcessing : IDisplayPart
        {
            private readonly AggregateStatisticsTracker<string, IceDetails> iceDetailsByGrid = new AggregateStatisticsTracker<string, IceDetails>();

            public void Clear() => iceDetailsByGrid.Reset();

            public void Draw(IMyCubeGrid grid, StringBuilder target)
            {
                target.AppendLine("Ice:");
                var total = new IceDetails();
                foreach (var row in iceDetailsByGrid.Enumerate())
                {
                    total = total.Accumulate(row.Total);
                }

                WriteRow(target, "TOTAL", total);
                foreach (var row in iceDetailsByGrid.Enumerate(grid.CustomName))
                {
                    WriteRow(target, row.Key, row.Total);
                }
            }

            private void WriteRow(StringBuilder target, string name, IceDetails row)
            {
                target.AppendFormat("{0,-20}    {1,4} ice,   {2,2} generator(s)\n",
                    name,
                    Unit.Mass.FormatSI(row.AmountOfIce),
                    row.NumberOfH2O2Generators);
            }

            public void Visit(IMyTerminalBlock block)
            {
                for (var i = 0; i < block.InventoryCount; i++)
                {
                    var inventory = block.GetInventory(i);
                    var amountOfIce = inventory.GetItemAmount(MyItemType.MakeOre("Ice"));
                    if (amountOfIce == 0) continue;

                    iceDetailsByGrid.Add(block.CubeGrid.CustomName, new IceDetails { AmountOfIce = (float)amountOfIce });
                }
                if (block is IMyGasGenerator)
                {
                    iceDetailsByGrid.Add(block.CubeGrid.CustomName, new IceDetails { NumberOfH2O2Generators = 1 });
                }
            }

            struct IceDetails : IAccumulator<IceDetails>
            {
                public float AmountOfIce { get; set; }
                public int NumberOfH2O2Generators { get; set; }

                public IceDetails Accumulate(IceDetails record)
                {
                    return new IceDetails
                    {
                        AmountOfIce = AmountOfIce + record.AmountOfIce,
                        NumberOfH2O2Generators = NumberOfH2O2Generators + record.NumberOfH2O2Generators,
                    };
                }
            }
        }
    }
}
