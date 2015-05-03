using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SESimulator.Data;

namespace SESimulator.Runtime
{
    public class Refinery : ISimulated
    {
        private Stack<ItemStack> inputInventory = new Stack<ItemStack>();
        private Stack<ItemStack> outputInventory = new Stack<ItemStack>();

        public Refinery(RefineryBlock block)
        {

        }

    
        public Snapshot TryAdvance(Snapshot lastSnapshot, TimeSpan target)
        {
            var duration = target - lastSnapshot.Timestamp;
            while (duration > TimeSpan.Zero)
            {
                duration = ProcessOre(duration);
            }
            var now = target - duration;
            return new Snapshot(now, now + GetTimeToClearNextStack());
        }

        private TimeSpan GetTimeToClearNextStack()
        {
            throw new NotImplementedException();
        }

        private TimeSpan ProcessOre(TimeSpan duration)
        {
            return TimeSpan.Zero;
        }
    }

    public static class RateCalculation
    {
        public static decimal RefinedPerSecond(RefineryBlock refinery, PhysicalItem ore, Blueprint blueprint)
        {
            Debug.Assert(Equals(blueprint.Inputs.Single().ItemId, ore.Id));
            var producedPerSecond = 1 / blueprint.BaseProductionTimeInSeconds;
            return 0;
        }
    }
}