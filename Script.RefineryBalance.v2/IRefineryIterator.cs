
using System.Collections.Generic;

namespace IngameScript
{
    public interface IRefineryIterator
    {
        // Internal use, until class nesting is fixed.
        void Initialise(IEnumerable<Refinery> sortedRefineries);

        void Next();
        bool AssignedWork(ref double seconds);
        Refinery Current { get; }
        double PreferredWorkSeconds { get; }
        double RequiredWorkSeconds { get; }
        bool CanAllocate();
        Blueprint[] GetCandidateBlueprints();
    }

}
