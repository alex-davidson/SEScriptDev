
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public class RefineryWorklist
    {
        private readonly OreTypes oreTypes;
        private readonly Dictionary<KeyValuePair<ItemType, string>, Blueprint[]> blueprintsByIngotTypeAndBlockDefinition;
        private readonly Dictionary<ItemType, string[]> blockDefinitionsByIngotType;
        private readonly Dictionary<ItemType, IRefineryIterator> iterators;

        public RefineryWorklist(OreTypes oreTypes, IngotTypes ingotTypes, RefineryFactory refineryFactory, Blueprints blueprints)
        {
            this.oreTypes = oreTypes;
            iterators = new Dictionary<ItemType, IRefineryIterator>(ingotTypes.All.Count);

            blueprintsByIngotTypeAndBlockDefinition = new Dictionary<KeyValuePair<ItemType, string>, Blueprint[]>(refineryFactory.AllTypes.Count * ingotTypes.All.Count);
            blockDefinitionsByIngotType = new Dictionary<ItemType, string[]>(ingotTypes.All.Count);
            var blocks = new List<string>(refineryFactory.AllTypes.Count);
            foreach (var ingotType in ingotTypes.AllIngotItemTypes)
            {
                var bps = blueprints.GetBlueprintsProducing(ingotType);
                blocks.Clear();
                foreach (var refineryType in refineryFactory.AllTypes)
                {
                    var matchingBps = bps.Where(refineryType.Supports).ToArray();
                    var key = new KeyValuePair<ItemType, string>(ingotType, refineryType.BlockDefinitionName);
                    blueprintsByIngotTypeAndBlockDefinition.Add(key, matchingBps);
                    if (matchingBps.Length > 0) blocks.Add(refineryType.BlockDefinitionName);
                }

                blockDefinitionsByIngotType.Add(ingotType, blocks.ToArray());
                iterators[ingotType] = new RefineryIterator(this, ingotType);
            }
        }

        public void Initialise(IList<Refinery> allRefineries)
        {
            var refineriesByBlockDefinition = allRefineries.ToLookup(r => r.BlockDefinitionString);

            foreach (var iterator in iterators)
            {
                var definitions = blockDefinitionsByIngotType[iterator.Key];
                if (definitions.Length == 0)
                {
                    iterator.Value.Initialise(Enumerable.Empty<Refinery>());
                }
                else
                {
                    var sortedList = definitions.SelectMany(d => refineriesByBlockDefinition[d]).OrderByDescending(r => r.TheoreticalIngotProductionRate);
                    iterator.Value.Initialise(sortedList);
                }
            }
        }

        public bool TrySelectIngot(ItemType ingotType, out IRefineryIterator iterator)
        {
            if (!iterators.TryGetValue(ingotType, out iterator)) return false;
            return iterator.CanAllocate();
        }

        class RefineryIterator : IRefineryIterator
        {
            private int index;
            private bool valid;
            private readonly RefineryWorklist worklist;
            private readonly ItemType ingotType;
            private readonly List<Refinery> refineries = new List<Refinery>(Constants.ALLOC_REFINERY_COUNT);
            private Blueprint[] candidateBlueprints;

            public RefineryIterator(RefineryWorklist worklist, ItemType ingotType)
            {
                this.worklist = worklist;
                this.ingotType = ingotType;
            }

            public void Initialise(IEnumerable<Refinery> sortedRefineries)
            {
                candidateBlueprints = null;
                valid = false;
                index = 0;
                refineries.Clear();
                refineries.AddRange(sortedRefineries);
            }

            public Blueprint[] GetCandidateBlueprints()
            {
                candidateBlueprints = candidateBlueprints ?? worklist.blueprintsByIngotTypeAndBlockDefinition[new KeyValuePair<ItemType, string>(ingotType, Current.BlockDefinitionString)];
                return candidateBlueprints;
            }

            public Refinery Current
            {
                get { return refineries[index]; }
            }

            public bool CanAllocate()
            {
                if (valid) return true;
                candidateBlueprints = null;
                while (index < refineries.Count)
                {
                    if (TrySelectRefinery(refineries[index]))
                    {
                        valid = true;
                        return true;
                    }
                    index++;
                }
                return false;
            }

            private bool TrySelectRefinery(Refinery refinery)
            {
                var secondsToClear = refinery.GetSecondsToClear(worklist.oreTypes);
                RequiredWorkSeconds = Constants.TARGET_INTERVAL_SECONDS - secondsToClear;
                PreferredWorkSeconds = RequiredWorkSeconds + Constants.OVERLAP_INTERVAL_SECONDS;
                return PreferredWorkSeconds > 0;
            }

            /// <summary>
            /// Returns true when the refinery is 'full'.
            /// Parameter is updated to the amount of work counted against 'required'.
            /// </summary>
            /// <param name="seconds"></param>
            /// <returns></returns>
            public bool AssignedWork(ref double seconds)
            {
                Debug.Assert(valid, "Assigned work while valid == false.");
                PreferredWorkSeconds -= seconds;
                if (RequiredWorkSeconds < seconds)
                {
                    seconds = RequiredWorkSeconds;
                    RequiredWorkSeconds = 0;
                }
                else
                {
                    RequiredWorkSeconds -= seconds;
                }
                if (PreferredWorkSeconds <= 0)
                {
                    PreferredWorkSeconds = 0;
                    return true;
                }
                return false;
            }

            // How much work does this refinery need to keep it busy until the next iteration, with a safety margin?
            public double PreferredWorkSeconds { get; private set; }
            // This is how much of the newly-assigned work can be applied against production targets.
            // Safety margin doesn't apply to contribution to quotas.
            public double RequiredWorkSeconds { get; private set; }

            public void Next()
            {
                valid = false;
                index++;
            }
        }

    }

}
