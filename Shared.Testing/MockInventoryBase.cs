using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game.ModAPI.Ingame;

namespace SharedTesting
{
    public abstract class MockInventoryBase : IMyInventory
    {
        private readonly List<MyInventoryItem?> slots = new List<MyInventoryItem?>();

        public void Add(MyItemType myItemType, MyFixedPoint amount)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null) continue;
                if (!Equals(slots[i].Value.Type, myItemType)) continue;

                slots[i] = new MyInventoryItem(myItemType, slots[i].Value.ItemId, slots[i].Value.Amount + amount);
                return;
            }

            slots.Add(new MyInventoryItem(myItemType, (uint) slots.Count, amount));
        }

        public void Remove(MyItemType myItemType, MyFixedPoint amount)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null) continue;
                if (!Equals(slots[i].Value.Type, myItemType)) continue;

                var resultAmount = slots[i].Value.Amount - amount;
                slots[i] = new MyInventoryItem(myItemType, slots[i].Value.ItemId,
                    MyFixedPoint.Max(resultAmount, MyFixedPoint.Zero));
                return;
            }

            slots.Add(new MyInventoryItem(myItemType, (uint) slots.Count, amount));
        }

        public abstract IMyEntity Owner { get; }
        public abstract bool IsFull { get; }
        public abstract MyFixedPoint CurrentMass { get; }
        public abstract MyFixedPoint MaxVolume { get; }
        public abstract MyFixedPoint CurrentVolume { get; }

        public int ItemCount => slots.Count(s => s?.Amount > 0);
        public float VolumeFillFactor { get; }

        public abstract bool CanItemsBeAdded(MyFixedPoint amount, MyItemType itemType);
        public abstract bool CanTransferItemTo(IMyInventory otherInventory, MyItemType itemType);

        public bool ContainItems(MyFixedPoint amount, MyItemType itemType) => FindItem(itemType)?.Amount >= amount;
        public MyInventoryItem? FindItem(MyItemType itemType) => slots.FirstOrDefault(s => Equals(s?.Type, itemType));

        public abstract void GetAcceptedItems(List<MyItemType> itemsTypes, Func<MyItemType, bool> filter = null);

        public MyFixedPoint GetItemAmount(MyItemType itemType) => FindItem(itemType)?.Amount ?? MyFixedPoint.Zero;

        public MyInventoryItem? GetItemAt(int index) => slots.Where(s => s != null).ElementAtOrDefault(index);
        public MyInventoryItem? GetItemByID(uint id) => slots.FirstOrDefault(s => Equals(s?.ItemId, id));

        public void GetItems(List<MyInventoryItem> items, Func<MyInventoryItem, bool> filter = null) => throw new NotImplementedException();

        public bool IsConnectedTo(IMyInventory otherInventory) => true;
        public bool IsItemAt(int position) => GetItemAt(position) != null;

        public bool TransferItemFrom(IMyInventory sourceInventory, MyInventoryItem item, MyFixedPoint? amount = null) => throw new NotImplementedException();
        public bool TransferItemFrom(IMyInventory sourceInventory, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, MyFixedPoint? amount = null) => throw new NotImplementedException();

        public bool TransferItemTo(IMyInventory dstInventory, MyInventoryItem item, MyFixedPoint? amount = null)
        {
            var myItem = GetItemByID(item.ItemId);
            if (myItem == null) return false;
            var existingAmount = GetItemAmount(item.Type);
            Remove(item.Type, amount ?? existingAmount);
            return existingAmount > 0;
        }

        public bool TransferItemTo(IMyInventory dst, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, MyFixedPoint? amount = null) => throw new NotImplementedException();
    }
}
