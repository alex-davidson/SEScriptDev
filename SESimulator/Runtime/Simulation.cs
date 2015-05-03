using System;
using System.Collections.Generic;
using System.Linq;

namespace SESimulator.Runtime
{
    public class Simulation
    {
        class FrameSorter : IComparer<Frame>
        {
            public int Compare(Frame x, Frame y)
            {
                return Comparer<TimeSpan>.Default.Compare(x.Snapshot.NextUpdate, y.Snapshot.NextUpdate);
            }
        }

        struct Frame
        {
            public Frame(Snapshot snapshot, ISimulated simObject)
                : this()
            {
                Snapshot = snapshot;
                SimObject = simObject;
            }

            public ISimulated SimObject { get; private set; }
            public Snapshot Snapshot { get; private set; }
        }

        private readonly SortedSet<Frame> everything = new SortedSet<Frame>(new FrameSorter());
        private TimeSpan now = TimeSpan.Zero;

        public void Add(ISimulated simObject)
        {
            var snapshot = simObject.TryAdvance(new Snapshot(), now);
            AddFrame(snapshot, simObject);
        }

        public void RunUntil(TimeSpan target)
        {
            while (now < target)
            {
                Run(everything.First(), target);
            }
        }

        private void Run(Frame frame, TimeSpan limit)
        {
            everything.Remove(frame);
            try
            {
                var target = frame.Snapshot.NextUpdate > limit ? limit : frame.Snapshot.NextUpdate;
                var snapshot = frame.SimObject.TryAdvance(frame.Snapshot, target);
                AddFrame(snapshot, frame.SimObject);
            }
            catch
            {
                everything.Add(frame);
                throw;
            }
        }

        private void AddFrame(Snapshot snapshot, ISimulated simObject)
        {
            everything.Add(new Frame(snapshot, simObject));
            now = everything.First().Snapshot.Timestamp;
        }

    }
}
