using System;

namespace IngameScript
{
    public static class Clock
    {
        public static TimeSpan Now { get; private set; }

        public static void AddTime(TimeSpan timeSpan)
        {
            Now += timeSpan;
        }
    }
}
