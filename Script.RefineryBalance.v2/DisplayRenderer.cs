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

        public void UpdateOreDisplay(Dictionary<ItemType, List<OreDonor>> ore, OreTypes oreTypes)
        {
            if (oreStatusScreen == null) return;

            // Clear previous state.
            oreStatusScreen.WriteText(string.Format("Ore stockpiles  {0:dd MMM HH:mm}\n", DateTime.Now));

            foreach (var slot in ore)
            {
                var perSecond = oreTypes.GetAmountConsumedPerSecond(slot.Key);
                var total = slot.Value.Sum(v => v.GetAmountAvailable());
                oreStatusScreen.WriteText(
                    String.Format("{0}:  {1:0.##}     ({2:0.##} refined/sec)\n",
                        slot.Key.SubtypeId,
                        total,
                        perSecond),
                    true);
            }
        }
    }
}
