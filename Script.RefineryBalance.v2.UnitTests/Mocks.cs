using Moq;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace Script.RefineryBalance.v2
{
    public class Mocks
    {
        public static IMyRefinery MockRefinery()
        {
            var inventory = Mock.Of<IMyInventory>();
            return Mock.Of<IMyRefinery>(r => r.InputInventory == inventory);
        }
    }
}
