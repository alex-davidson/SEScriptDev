using System.Collections.Generic;

namespace IngameScript
{
    public struct OreDonorsIterator
    {
        private readonly List<OreDonor> donors;
        private int current;
            
        public OreDonorsIterator(List<OreDonor> donors)
        {
            current = -1;
            this.donors = donors;
            Current = default(OreDonor);
        }

        // ASSUMPTION: Next() is always called after this.
        public void Remove()
        {
            // Order doesn't matter. Just get the next one really really fast.
            donors.RemoveAtFast(current);
            current--;
        }

        public OreDonor Current;

        // ASSUMPTION: Nothing reads Current after this returns false.
        public bool Next()
        {
            current++;
            if (current < donors.Count)
            {
                Current = donors[current];
                return true;
            }
            return false;
        }
    }

}
