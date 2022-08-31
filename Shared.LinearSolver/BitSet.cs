using System.Collections;
using System.Collections.Generic;

namespace Shared.LinearSolver
{
    public struct BitSet : IEnumerable<int>
    {
        private ulong head;
        private readonly ulong[] list;
        private readonly int capacity;

        public BitSet(int capacity)
        {
            this.capacity = capacity;
            list = capacity > 64 ? new ulong[(capacity - 1) / 64] : null;
            head = 0;
        }

        public void Add(int value)
        {
            if (value < 64)
            {
                head |= 1UL << value;
                return;
            }
            var word = value / 64 - 1;
            var bit = value % 64;
            list[word] |= 1UL << bit;
        }

        public void Remove(int value)
        {
            var mod = value % 64;
            if (value < 64)
            {
                head &= ~(1UL << mod);
                return;
            }
            var word = value / 64 - 1;
            var bit = value % 64;
            list[word] &= ~(1UL << bit);
        }

        public BitSetIterator GetEnumerator() => new BitSetIterator(head, list, capacity);
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
