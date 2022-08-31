using System;
using System.Text;

namespace Shared.LinearSolver.UnitTests.Debug
{
    public class DebugWriter : IDebugWriter
    {
        private readonly Action<string> debug;
        private readonly StringBuilder buffer = new StringBuilder();

        public DebugWriter(Action<string> debug)
        {
            this.debug = debug;
        }

        public void Write(string message)
        {
            debug.Invoke(message);
            buffer.AppendLine(message);
        }

        internal string Buffer => buffer.ToString();
    }
}
