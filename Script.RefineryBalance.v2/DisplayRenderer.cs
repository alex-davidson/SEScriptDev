using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Core;

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

        private readonly StringBuilder local_UpdateIngotDisplay_builder = new StringBuilder(1000);
        public void UpdateIngotDisplay(IngotStockpiles ingotStockpiles)
        {
            if (ingotStatusScreen == null) return;

            // Clear previous state.
            local_UpdateIngotDisplay_builder.Clear();

            // Clear previous state.
            local_UpdateIngotDisplay_builder.AppendFormat("Ingot stockpiles  {0:dd MMM HH:mm}\n", DateTime.Now);

            foreach (var stockpile in ingotStockpiles)
            {
                local_UpdateIngotDisplay_builder.Append(stockpile.Ingot.ItemType.SubtypeId);
                local_UpdateIngotDisplay_builder.Append(":  ");

                if (stockpile.QuotaFraction >= 20)
                {
                    local_UpdateIngotDisplay_builder.AppendFormat("{0:0}x", stockpile.QuotaFraction);
                }
                else if (stockpile.QuotaFraction >= 2)
                {
                    local_UpdateIngotDisplay_builder.AppendFormat("{0:0.#}x", stockpile.QuotaFraction);
                }
                else
                {
                    local_UpdateIngotDisplay_builder.AppendFormat("{0:#000%}", stockpile.QuotaFraction);
                }

                local_UpdateIngotDisplay_builder.AppendFormat("   {0} / {1}",
                    Unit.Mass.FormatSI((float)stockpile.CurrentQuantity),
                    Unit.Mass.FormatSI((float)stockpile.TargetQuantity));

                if (stockpile.QuotaFraction < 1)
                {
                    local_UpdateIngotDisplay_builder.Append(" (!)");
                }
                local_UpdateIngotDisplay_builder.AppendLine();
            }
            ingotStatusScreen.WriteText(local_UpdateIngotDisplay_builder.ToString());
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

            var totalMass = 0.0;
            var totalVolume = 0.0;
            var secondsRemaining = 0.0;
            foreach (var slot in ore)
            {
                var perSecond = oreTypes.GetAmountConsumedPerSecond(slot.Key) * totalConsumeSpeed;
                var total = slot.Value.Sum(v => v.GetAmountAvailable());
                secondsRemaining += total / perSecond;
                totalMass += total;
                totalVolume += total * 0.37;    // All ore is currently 370 ml/kg

                local_UpdateOreDisplay_builder.AppendFormat("  {0}:  {1} ({2}/s)", slot.Key.SubtypeId, Unit.Mass.FormatSI((float)total),  Unit.Mass.FormatSI((float)perSecond));

                var blueprint = oreTypes.GetBlueprint(slot.Key);
                if (blueprint != null)
                {
                    local_UpdateOreDisplay_builder.AppendFormat(" -> ");
                    if (blueprint.Value.Outputs.Length == 1)
                    {
                        var output = blueprint.Value.Outputs[0];
                        var outputPerSecond = output.Quantity * totalProduceSpeed / blueprint.Value.Duration;
                        local_UpdateOreDisplay_builder.AppendFormat("{0}/s", Unit.Mass.FormatSI((float)outputPerSecond));
                    }
                    else
                    {
                        foreach (var output in blueprint.Value.Outputs)
                        {
                            var outputPerSecond = output.Quantity * totalProduceSpeed / blueprint.Value.Duration;
                            local_UpdateOreDisplay_builder.AppendFormat(" {0}: {1}/s ", output.ItemType.SubtypeId.Substring(0, 2), Unit.Mass.FormatSI((float)outputPerSecond));
                        }
                    }
                }

                local_UpdateOreDisplay_builder.Append("\n");
            }
            local_UpdateOreDisplay_builder.AppendFormat("Total: {0} ({1})       Time to clear:  {2}",
                Unit.Mass.FormatSI((float)totalMass),
                Unit.Volume.FormatSI((float)totalVolume),
                TimeSpan.FromSeconds((int)secondsRemaining));
            oreStatusScreen.WriteText(local_UpdateOreDisplay_builder.ToString());
        }
    }
}
