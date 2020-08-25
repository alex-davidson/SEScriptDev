using System.Collections.Generic;
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
    }
}
