using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// Represents a stack in an inventory.
    /// </summary>
    public struct OreDonor
    {
        public OreDonor(IMyInventory inventory, uint itemId) : this()
        {
            Inventory = inventory;
            ItemId = itemId;
        }

        public readonly IMyInventory Inventory;
        public readonly uint ItemId;

        public MyInventoryItem? GetItem() { return Inventory.GetItemByID(ItemId); }

        public double GetAmountAvailable()
        {
            var item = GetItem();
            if (item == null) return 0;
            return (double) item.Value.Amount;
        }

        public bool TransferTo(IMyInventory target, double amount)
        {
            var item = GetItem();
            if (item == null) return false;
            return Inventory.TransferItemTo(target, item.Value, (VRage.MyFixedPoint)amount);
        }
    }

}
