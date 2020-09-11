namespace IngameScript
{
    public class Stopwatch
    {
        private double referenceSeconds;
        public Stopwatch()
        {
            Reset();
        }

        public void Reset()
        {
            referenceSeconds = Clock.Now.TotalSeconds;
        }

        public double GetElapsedSeconds()
        {
            return Clock.Now.TotalSeconds - referenceSeconds;
        }
    }
}
