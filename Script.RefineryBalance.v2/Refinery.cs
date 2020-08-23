using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public class Refinery
    {
        private IMyRefinery block;
        private double refineSpeed;
        private double materialEfficiency;

        private Refinery()
        {
        }

        public static Refinery Get(IMyRefinery block, RefineryType type, double speedFactor)
        {
            var item = GetOrCreate();
            item.block = block;
            item.refineSpeed = type.Speed;
            item.materialEfficiency = type.Efficiency;
            item.BlockDefinitionString = type.BlockDefinitionName;

            var moduleBonuses = SupportUtil.ParseModuleBonuses(block);
            if (moduleBonuses.Count > 0)
            {
                var speedModifier = moduleBonuses[0] - 1; // +1 Speed per 100%.
                item.refineSpeed += speedModifier;
            }
            if (moduleBonuses.Count > 1)
            {
                var efficiencyModifier = moduleBonuses[1];
                item.materialEfficiency *= efficiencyModifier;
            }
            item.refineSpeed *= speedFactor;
            return item;
        }

        public string BlockDefinitionString { get; private set; }
        public double OreConsumptionRate { get { return refineSpeed; } }
        public IMyInventory GetOreInventory() { return block.InputInventory; }

        public double TheoreticalIngotProductionRate { get { return refineSpeed * materialEfficiency; } }

        public double GetActualIngotProductionRate(ItemAndQuantity ingotTypeFromBlueprint, double blueprintDuration)
        {
            // ASSUMPTION: Blueprints are defined such that each output type's quantity should not exceed 1.

            var actualOutputQuantity = Math.Min(ingotTypeFromBlueprint.Quantity * materialEfficiency, 1);
            return actualOutputQuantity / blueprintDuration * refineSpeed;
        }

        /// <summary>
        /// Calculate how long the specified refinery will take to run dry, taking into account
        /// refinery speed and ore type.
        /// </summary>
        public double GetSecondsToClear(OreTypes oreTypes)
        {
            var inventory = GetOreInventory();
            double time = 0;
            for (var i = 0; i < inventory.ItemCount; i++)
            {
                var item = inventory.GetItemAt(i);
                if (item == null) continue;
                time += oreTypes.GetSecondsToClear(item.Value);
            }
            return time / refineSpeed; // OreConsumptionRate;
        }

        private static readonly List<Refinery> available = new List<Refinery>(Constants.ALLOC_REFINERY_COUNT);
        private static readonly List<Refinery> pool = new List<Refinery>(Constants.ALLOC_REFINERY_COUNT);

        public static void ReleaseAll()
        {
            available.Clear();
            available.AddRange(pool);
        }

        private static Refinery GetOrCreate()
        {
            if (available.Count == 0)
            {
                var item = new Refinery();
                pool.Add(item);
                return item;
            }
            var last = available[available.Count - 1];
            available.RemoveAt(available.Count - 1);
            return last;
        }
    }

}
