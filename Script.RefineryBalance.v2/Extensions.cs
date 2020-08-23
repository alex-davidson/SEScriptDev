using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public static class Extensions
    {
        public static bool IsOperational(this IMyFunctionalBlock block)
        {
            return block.Enabled && block.IsFunctional && block.IsWorking;
        }
    }
}
