using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Shared.LinearSolver
{
    /// <summary>
    /// Yields pivots in ascending order of score, per Bland's Rule.
    /// </summary>
    /// <remarks>
    /// Not threadsafe. May not be modified during enumeration.
    /// </remarks>
    public class ReadSortedPivotList : IEnumerable<Pivot>
    {
        private readonly List<Node> nodes;

        public ReadSortedPivotList(int capacity)
        {
            nodes = new List<Node>(capacity);
        }

        struct Node
        {
            public float score;
            public Pivot value;
        }

        public void Clear()
        {
            nodes.Clear();
        }

        public void Add(Pivot pivot, float score)
        {
            nodes.Add(new Node { value = pivot, score = score });
        }

        public IEnumerator<Pivot> GetEnumerator()
        {
            nodes.Sort((x, y) => x.score.CompareTo(y.score));
            return nodes.Select(x => x.value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
