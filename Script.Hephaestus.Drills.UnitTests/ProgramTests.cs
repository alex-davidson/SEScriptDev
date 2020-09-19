using IngameScript.MDK;
using Malware.MDKUtilities;
using Moq;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;

namespace Script.Hephaestus.Drills.UnitTests
{
    [TestFixture]
    public class ProgramTests
    {
        [Test]
        public void Runs()
        {
            var config = new MDKFactory.ProgramConfig
            {
                GridTerminalSystem = Mock.Of<IMyGridTerminalSystem>(),
            };
            TestBootstrapper.Run(config);
        }
    }
}
