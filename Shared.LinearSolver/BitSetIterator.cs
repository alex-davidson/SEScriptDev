using System;
using System.Collections;
using System.Collections.Generic;

namespace Shared.LinearSolver
{
    public struct BitSetIterator : IEnumerator<int>
    {
        private ulong current;
        private readonly ulong[] list;
        private readonly int limit;
        private int index;

        /// <summary>
        /// ASSUMPTION: if list is null, limit &lt; 64;
        /// </summary>
        public BitSetIterator(ulong head, ulong[] list, int limit)
        {
            this.current = head;
            this.list = list;
            this.limit = limit;
            index = -1;
        }

        public void Dispose() { }

        public bool MoveNext()
        {
            var bit = 0;
            while (index < 63)
            {
                index++;
                if (index >= limit) return false;
                bit = index % 64;
                if ((current & (1UL << bit)) != 0) return true;
            }

            do
            {
                index++;
                if (index >= limit) return false;
                bit = index % 64;
                if (bit == 0)
                {
                    current = list[index / 64 - 1];
                    if (current == 0) index += 63;
                }
            }
            while ((current & (1UL << bit)) == 0);
            return true;
        }

        public void Reset() => throw new NotSupportedException();

        public int Current => index;

        object IEnumerator.Current => Current;
    }
}
