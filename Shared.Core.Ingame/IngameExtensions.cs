using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public static class IngameExtensions
    {
        public static bool IsOperational(this IMyCubeBlock block)
        {
            if (!block.IsFunctional) return false;
            if (!block.IsWorking) return false;
            var functionalBlock = block as IMyFunctionalBlock;
            if (functionalBlock != null)
            {
                if (!functionalBlock.Enabled) return false;
            }
            return true;
        }

        private static readonly List<MyItemType> local_AnyInventoryCanContainMatching_acceptedItemTypes = new List<MyItemType>(30);

        public static bool AnyInventoryCanContainMatching(this IMyTerminalBlock block, Func<MyItemType, bool> itemType)
        {
            if (!block.HasInventory) return false;
            local_AnyInventoryCanContainMatching_acceptedItemTypes.Clear();
            for (var i = 0; i < block.InventoryCount; i++)
            {
                block.GetInventory(i).GetAcceptedItems(local_AnyInventoryCanContainMatching_acceptedItemTypes, itemType);
                if (local_AnyInventoryCanContainMatching_acceptedItemTypes.Any()) return true;
            }
            return false;
        }

        public static bool AnyInventoryCanContain(this IMyTerminalBlock block, MyItemType itemType) =>
            AnyInventoryCanContainMatching(block, i => i == itemType);
    }
}
