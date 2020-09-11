using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Malware.MDKUtilities;
using Moq;
using Sandbox.ModAPI.Ingame;
using IngameScript.MDK;

namespace Script.Hephaestus.Thrusters.UnitTests
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

        [Test]
        public void TestCommand_Terminates()
        {
            var config = new MDKFactory.ProgramConfig
            {
                GridTerminalSystem = Mock.Of<IMyGridTerminalSystem>(),
            };
            TestBootstrapper.Run(config, "test");
        }
    }
}
