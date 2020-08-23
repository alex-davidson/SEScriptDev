using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IngameScript;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Script.RefineryBalance.v2;

[assembly: AssemblyFixture]

namespace Script.RefineryBalance.v2
{
    public class AssemblyFixture : TestActionAttribute
    {
        public override ActionTargets Targets => ActionTargets.Suite;

        public override void BeforeTest(ITest test)
        {
            Debug.Initialise(Debug.Level.All, s => Console.WriteLine(s));
        }
    }
}
