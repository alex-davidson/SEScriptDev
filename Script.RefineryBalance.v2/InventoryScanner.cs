using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;

using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public class InventoryScanner
    {
        public readonly Dictionary<ItemType, List<OreDonor>> Ore;
        public readonly Dictionary<ItemType, double> Ingots;
        private readonly IEnumerable<ItemType> ingotTypes;

        public InventoryScanner(IEnumerable<ItemType> ingotTypes, IEnumerable<ItemType> oreTypes)
        {
            Ore = new Dictionary<ItemType, List<OreDonor>>(Constants.ALLOC_ORE_TYPE_COUNT);
            Ingots = new Dictionary<ItemType, double>(Constants.ALLOC_INGOT_TYPE_COUNT);

            foreach (var oreType in oreTypes)
            {
                if (Ore.ContainsKey(oreType)) continue;
                var list = new List<OreDonor>(Constants.ALLOC_ORE_DONOR_COUNT);
                Ore.Add(oreType, list);
            }

            this.ingotTypes = ingotTypes;
        }

        public void Reset()
        {
            // Reuse OreDonor lists:
            foreach (var slot in Ore.Values)
            {
                slot.Clear();
            }

            Ingots.Clear();
            foreach (var ingotType in ingotTypes)
            {
                Ingots.Add(ingotType, 0);
            }
        }

        public void Scan(IMyEntity inventoryOwner)
        {
            Debug.Write(Debug.Level.All, new Message("Scanning: '{0}'", inventoryOwner.DisplayName));
            var isRefinery = inventoryOwner is IMyRefinery;

            for (var i = 0; i < inventoryOwner.InventoryCount; i++)
            {
                var inventory = inventoryOwner.GetInventory(i);
                for (var j = 0; j < inventory.ItemCount; j++)
                {
                    var item = inventory.GetItemAt(j);
                    if (item == null) continue;
                    var itemType = new ItemType(item.Value.Type.TypeId, item.Value.Type.SubtypeId);
                    AddIngots(itemType, (double)item.Value.Amount);
                    if (!isRefinery) AddOre(itemType, inventory, item.Value.ItemId);
                }
            }
        }

        private void AddOre(ItemType ore, IMyInventory inventory, uint itemId)
        {
            List<OreDonor> existing;
            if (!Ore.TryGetValue(ore, out existing)) return;
            existing.Add(new OreDonor(inventory, itemId));
        }

        private void AddIngots(ItemType ingot, double quantity)
        {
            double existing;
            if (!Ingots.TryGetValue(ingot, out existing)) return;
            Ingots[ingot] = existing + quantity;
        }
    }

}
