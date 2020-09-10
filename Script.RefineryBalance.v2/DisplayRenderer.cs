using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    public class DisplayRenderer
    {
        private IMyTextPanel oreStatusScreen;
        private IMyTextPanel ingotStatusScreen;

        public void Rescan(SystemState state, IMyGridTerminalSystem gts)
        {
            if (!string.IsNullOrEmpty(state.Static.OreStatusDisplayName))
            {
                oreStatusScreen = (IMyTextPanel)gts.GetBlockWithName(state.Static.OreStatusDisplayName);
            }
            if (!string.IsNullOrEmpty(state.Static.IngotStatusDisplayName))
            {
                ingotStatusScreen = (IMyTextPanel)gts.GetBlockWithName(state.Static.IngotStatusDisplayName);
            }
        }

        public void UpdateIngotDisplay(IngotStockpiles ingotStockpiles)
        {
            if (ingotStatusScreen == null) return;

            // Clear previous state.
            ingotStatusScreen.WriteText(string.Format("Ingot stockpiles  {0:dd MMM HH:mm}\n", DateTime.Now));
            
            foreach (var stockpile in ingotStockpiles)
            {
                ingotStatusScreen.WriteText(
                    String.Format("{0}:  {3:#000%}   {1:0.##} / {2:0.##} {4}\n",
                        stockpile.Ingot.ItemType.SubtypeId,
                        stockpile.CurrentQuantity,
                        stockpile.TargetQuantity,
                        stockpile.QuotaFraction,
                        stockpile.QuotaFraction < 1 ? "(!)" : ""),
                    true);
            }
        }

        private readonly StringBuilder local_UpdateOreDisplay_builder = new StringBuilder(1000);
        public void UpdateOreDisplay(Dictionary<ItemType, List<OreDonor>> ore, OreTypes oreTypes, List<Refinery> refineries)
        {
            if (oreStatusScreen == null) return;

            // Clear previous state.
            local_UpdateOreDisplay_builder.Clear();
            
            local_UpdateOreDisplay_builder.AppendFormat("Ore stockpiles  {0:dd MMM HH:mm}\n", DateTime.Now);

            var totalConsumeSpeed = 0.0;
            var totalProduceSpeed = 0.0;
            foreach (var refinery in refineries)
            {
                totalConsumeSpeed += refinery.OreConsumptionRate;
                totalProduceSpeed += refinery.TheoreticalIngotProductionRate;
            }
            local_UpdateOreDisplay_builder.AppendFormat("Refineries: {0}  Speed: {1:0.#}  Efficiency: {2:0.#}\n", refineries.Count, totalConsumeSpeed, totalProduceSpeed / totalConsumeSpeed);

            var secondsRemaining = 0.0;
            foreach (var slot in ore)
            {
                var perSecond = oreTypes.GetAmountConsumedPerSecond(slot.Key) * totalConsumeSpeed;
                var total = slot.Value.Sum(v => v.GetAmountAvailable());
                secondsRemaining += total / perSecond;

                local_UpdateOreDisplay_builder.AppendFormat("  {0}:  {1:0.#} ({2:0.#}/s)", slot.Key.SubtypeId, total, perSecond);

                var blueprint = oreTypes.GetBlueprint(slot.Key);
                if (blueprint != null)
                {
                    local_UpdateOreDisplay_builder.AppendFormat(" -> ");
                    if (blueprint.Value.Outputs.Length == 1)
                    {
                        var output = blueprint.Value.Outputs[0];
                        local_UpdateOreDisplay_builder.AppendFormat("{0:0.#}/s", output.Quantity * totalProduceSpeed / blueprint.Value.Duration);
                    }
                    else
                    {
                        foreach (var output in blueprint.Value.Outputs)
                        {
                            local_UpdateOreDisplay_builder.AppendFormat(" {0}: {1:0.#}/s ", output.ItemType.SubtypeId.Substring(0, 2), output.Quantity * totalProduceSpeed / blueprint.Value.Duration);
                        }
                    }
                }

                local_UpdateOreDisplay_builder.Append("\n");
            }
            local_UpdateOreDisplay_builder.AppendFormat("Time to clear:  {0}", TimeSpan.FromSeconds((int)secondsRemaining));
            oreStatusScreen.WriteText(local_UpdateOreDisplay_builder.ToString());
        }
    }
}
