using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace Script.RefineryBalance.v2
{
    public partial class Program
    {
        private IMyGridTerminalSystem GridTerminalSystem { get; set; }
        public static int Main(string[] args)
        {
            new Program().Main(String.Join(" ", args));
            return 0;
        }
    }
}
