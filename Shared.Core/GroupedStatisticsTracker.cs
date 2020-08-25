using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    /// <summary>
    /// Record samples per grouping key, cleaning up after keys which aren't seen for a while.
    /// </summary>
    class GroupedStatisticsTracker<TKey, TSample>
        where TSample : struct
    {
        private readonly Dictionary<TKey, List<TSample>> sampleGroups;
        private readonly int allocKeyCleanupThreshold;
        private readonly int allocSampleCount;
        private int thisIterationKeys;

        public GroupedStatisticsTracker(int allocKeyCount = Constants.ALLOC_GroupedStatisticsTracker_DEFAULT_KEY_COUNT, int allocSampleCount = Constants.ALLOC_GroupedStatisticsTracker_DEFAULT_SAMPLE_COUNT)
        {
            sampleGroups = new Dictionary<TKey, List<TSample>>(allocKeyCount);
            this.allocKeyCleanupThreshold = allocKeyCount * 2;
            this.allocSampleCount = allocSampleCount;
        }

        public void Reset()
        {
            if (sampleGroups.Count > (allocKeyCleanupThreshold * 2) && thisIterationKeys < sampleGroups.Count / 2)
            {
                // If we're tracking lots of grids but haven't seen half of them in the last
                // iteration, clear out the junk.
                sampleGroups.Clear();
                return;
            }
            foreach (var entry in sampleGroups)
            {
                entry.Value.Clear();
            }
        }

        public void Add(TKey groupKey, TSample sample)
        {
            List<TSample> samples;
            if (!sampleGroups.TryGetValue(groupKey, out samples))
            {
                samples = new List<TSample>(allocSampleCount);
                sampleGroups.Add(groupKey, samples);
            }
            if (samples.Count == 0)
            {
                // Haven't seen this grid before in this iteration.
                thisIterationKeys++;
            }
            samples.Add(sample);
        }

        public IEnumerable<Entry> Enumerate()
        {
            foreach (var kv in sampleGroups)
            {
                if (kv.Value.Count == 0) continue;
                yield return new Entry { Group = kv.Key, Samples = kv.Value };
            }
        }

        public IEnumerable<Entry> Enumerate(TKey thisGroupKey) => Enumerate(k => Equals(k, thisGroupKey));

        public IEnumerable<Entry> Enumerate(Func<TKey, bool> putFirst)
        {
            foreach (var kv in sampleGroups)
            {
                if (kv.Value.Count == 0) continue;
                if (!putFirst(kv.Key)) continue;
                yield return new Entry { Group = kv.Key, Samples = kv.Value };
            }
            foreach (var kv in sampleGroups)
            {
                if (kv.Value.Count == 0) continue;
                if (putFirst(kv.Key)) continue;
                yield return new Entry { Group = kv.Key, Samples = kv.Value };
            }
        }

        public struct Entry
        {
            public TKey Group { get; set; }
            public IEnumerable<TSample> Samples { get; set; }
        }
    }
}
