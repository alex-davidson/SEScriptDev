using System;

namespace SESimulator.Runtime
{
    public struct Snapshot
    {
        public Snapshot(TimeSpan now, TimeSpan nextUpdate) : this()
        {
            Timestamp = now;
            NextUpdate = nextUpdate;
        }

        public TimeSpan Timestamp {get; private set;}
        public TimeSpan NextUpdate {get; private set;}
    }
}