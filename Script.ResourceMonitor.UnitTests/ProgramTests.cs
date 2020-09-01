using IngameScript.MDK;
using Malware.MDKUtilities;
using Moq;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Script.ResourceMonitor
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
