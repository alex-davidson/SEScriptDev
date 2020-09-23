using System;

namespace IngameScript
{
    /// <summary>
    /// Lazy-allocation encapsulation of the current date and time.
    /// </summary>
    public class Datestamp
    {
        public static Datestamp Minutes { get; } = new Datestamp("dd MMM HH:mm");
        public static Datestamp Seconds { get; } = new Datestamp("dd MMM HH:mm:ss");

        private readonly string format;

        private Datestamp(string format)
        {
            this.format = format;
        }

        public override string ToString() => DateTime.Now.ToString(format);
    }
}
