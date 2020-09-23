using System;
using System.Text;

namespace IngameScript
{
    /// <summary>
    /// Minimal-allocation message structure, supporting up to 3 arguments.
    /// </summary>
    /// <remarks>
    /// Allocates one array statically, amortised over all uses.
    /// </remarks>
    public struct Message
    {
        private readonly string format;
        private readonly object arg0;
        private readonly object arg1;
        private readonly object arg2;

        public Message(string format, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            this.format = format;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        private static readonly object[] args = new object[3];
        public void WriteTo(StringBuilder builder)
        {
            if (format == null) return;
            if (format == "{0}")
            {
                builder.Append(arg0);
                return;
            }
            args[0] = arg0;
            args[1] = arg1;
            args[2] = arg2;
            builder.AppendFormat(format, args);
        }

        public override string ToString() => format == null ? "" : string.Format(format, arg0, arg1, arg2);

        public static implicit operator Message(string str) => new Message("{0}", str);
    }
}
