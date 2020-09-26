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

namespace Script.PowerManagement.UnitTests
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
                ProgrammableBlock = Mock.Of<IMyProgrammableBlock>(),
            };
            TestBootstrapper.Run(config);
        }
    }
}
