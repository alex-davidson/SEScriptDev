using System.Collections.Generic;
using System.Text;

namespace IngameScript
{
    /// <summary>
    /// Record aggregate per key.
    /// </summary>
    class AggregateStatisticsTracker<TKey, TSample>
        where TSample : struct, IAccumulator<TSample>
    {
        private readonly Dictionary<TKey, TSample> aggregates;

        public AggregateStatisticsTracker(int allocKeyCount = Constants.ALLOC_AggregateStatisticsTracker_DEFAULT_KEY_COUNT)
        {
            aggregates = new Dictionary<TKey, TSample>(allocKeyCount);
        }

        public void Reset()
        {
            aggregates.Clear();
        }

        public void Add(TKey key, TSample sample)
        {
            if (default(TSample).Equals(sample)) return;
            TSample accumulator;
            aggregates.TryGetValue(key, out accumulator);
            aggregates[key] = accumulator.Accumulate(sample);
        }

        public IEnumerable<Entry> Enumerate()
        {
            foreach (var kv in aggregates)
            {
                yield return new Entry { Key = kv.Key, Total = kv.Value };
            }
        }

        public IEnumerable<Entry> Enumerate(TKey thisGroupKey)
        {
            if (thisGroupKey != null)
            {
                TSample accumulator;
                aggregates.TryGetValue(thisGroupKey, out accumulator);
                yield return new Entry { Key = thisGroupKey, Total = accumulator };
            }
            foreach (var kv in aggregates)
            {
                if (Equals(kv.Key, thisGroupKey)) continue;
                yield return new Entry { Key = kv.Key, Total = kv.Value };
            }
        }

        public struct Entry
        {
            public TKey Key { get; set; }
            public TSample Total { get; set; }
        }
    }
}
