using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public struct RefineryWorkAllocator
    {
        public RefineryWorkAllocator(RefineryWorklist refineryWorklist, Dictionary<ItemType, List<OreDonor>> oreDonors)
        {
            this.refineryWorklist = refineryWorklist;
            this.oreDonors = oreDonors;
        }

        private readonly RefineryWorklist refineryWorklist;
        private readonly Dictionary<ItemType, List<OreDonor>> oreDonors;

        public bool AllocateSingle(IngotWorklist ingotWorklist)
        {
            IngotStockpile preferred;
            while (ingotWorklist.TryGetPreferred(out preferred))
            {
                var type = preferred.Ingot.ItemType;
                IRefineryIterator refineries;
                if (refineryWorklist.TrySelectIngot(type, out refineries))
                {
                    do
                    {
                        if (FillCurrentRefinery(ingotWorklist, refineries)) return true; // Yield as soon as we've allocated work.
                    } while (refineries.CanAllocate()); // Otherwise keep looking for a refinery which can do something.
                }
                ingotWorklist.Skip();
            }
            return false;
        }

        /// <summary>
        /// Provide the specified refinery with enough work to keep it busy until the next iteration.
        /// </summary>
        /// <remarks>
        /// Ore already being processed is not considered when estimating how many ingots will be produced.
        /// This is because we already have at least one interval of lag in adjusting to new requests anyway
        /// and the amount of ore in flight should be insignificant in comparison (approx. one
        /// 'IntervalOverlapSeconds'); it's not worth the hassle to calculate it.
        /// </remarks>
        private bool FillCurrentRefinery(IngotWorklist ingotWorklist, IRefineryIterator worklist)
        {
            var assignedWork = false;
            // How much work does this refinery need to keep it busy until the next iteration, with a safety margin?
            Debug.Assert(worklist.PreferredWorkSeconds > 0, "PreferredWorkSeconds <= 0");

            // Get candidate blueprints in priority order.
            var candidates = worklist.GetCandidateBlueprints().OrderByDescending(ingotWorklist.ScoreBlueprint).ToArray();

            for (var i = 0; i < candidates.Length; i++)
            {
                var blueprint = candidates[i];

                var workProvidedSeconds = TryFillRefinery(worklist.Current, blueprint, worklist.PreferredWorkSeconds);
                if (workProvidedSeconds <= 0)
                {
                    Debug.Write(Debug.Level.All, "Unable to allocate any {0}, moving to next candidate.", blueprint.Input.ItemType.SubtypeId);
                    continue;
                }
                assignedWork = true;
                var isRefinerySatisfied = worklist.AssignedWork(ref workProvidedSeconds);
                if (workProvidedSeconds > 0)
                {
                    Debug.Write(Debug.Level.Debug, "Provided {0} seconds of work using {1}.", workProvidedSeconds, blueprint.Name);
                    // Some of the new work will be processed before next iteration. Update our estimates.
                    ingotWorklist.UpdateStockpileEstimates(worklist.Current, blueprint, workProvidedSeconds);
                }
                // If the refinery's work target is satisfied, it should not run dry before we run again.
                if (isRefinerySatisfied) break;
            }

            // No more ore available for this refinery.
            worklist.Next();
            return assignedWork;
        }

        /// <summary>
        /// Try to use the specified refinery to process the specified blueprint, up to
        /// the refinery's current work target.
        /// </summary>
        /// <param name="refinery"></param>
        /// <param name="blueprint"></param>
        /// <param name="workRequiredSeconds">Amount of work (in seconds) this refinery needs to keep it busy until the next iteration.</param>
        /// <returns>Amount of work (in seconds) provided to this refinery.</returns>
        private double TryFillRefinery(Refinery refinery, Blueprint blueprint, double workRequiredSeconds)
        {
            // How much of this type of ore is required to meet the refinery's work target?
            var oreRate = refinery.OreConsumptionRate * (blueprint.Input.Quantity / blueprint.Duration);
            var oreQuantityRequired = oreRate * workRequiredSeconds;

            var sources = new OreDonorsIterator(oreDonors[blueprint.Input.ItemType]);

            double workProvidedSeconds = 0;
            // Iterate over available stacks until we run out or satisfy the quota.
            while (sources.Next())
            {
                var donor = sources.Current;
                var item = donor.GetItem();
                if (item == null || item.Value.Amount == 0)
                {
                    // Donor stack is empty. Remove it.
                    sources.Remove();
                    continue;
                }

                if (!donor.Inventory.IsConnectedTo(refinery.GetOreInventory()))
                {
                    Debug.Write(Debug.Level.All, "Inventory not connected");
                    // Donor inventory can't reach this refinery. Skip it.
                    continue;
                }

                // Don't try to transfer more ore than the donor stack has.
                var transfer = Math.Min(oreQuantityRequired, (double)item.Value.Amount);
                if (donor.TransferTo(refinery.GetOreInventory(), transfer))
                {
                    // Update our estimates based on the transfer succeeding.
                    // ASSUMPTION: success means the entire requested amount was transferred.
                    oreQuantityRequired -= transfer;
                    workProvidedSeconds += transfer / oreRate;

                    // If we've provided enough work, return.
                    if (workProvidedSeconds >= workRequiredSeconds) break;
                }
            }
            return workProvidedSeconds;
        }
    }
}
