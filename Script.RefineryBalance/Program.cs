using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;

namespace Script.RefineryBalance
{
    class Program
    {
        private IMyGridTerminalSystem GridTerminalSystem { get; set; }

        public struct Configuration
        {
            /// <summary>
            /// Unique name of the timer block running this script.
            /// </summary>
            public string TimerBlockName { get { return "Timer (Status Monitoring)"; } }
            /// <summary>
            /// Names of containers used to store unrefined ore available for processing.
            /// </summary>
            public string[] OreStorageContainerNames { get { return new[] { "Unrefined Ore" }; } }
            /// <summary>
            /// Names of containers used to store ingots available for manufacturing.
            /// </summary>
            public string[] IngotStorageContainerNames { get { return new[] { "Raw Materials" }; } }
            /// <summary>
            /// Maximum expected timer lag. Timer blocks do not fire exactly on time and may be delayed by a small amount.
            /// Observed values fall between 0.5 and 1 second, but may vary with update rate.
            /// </summary>
            public float MaxTimerLagSeconds { get { return 1f; } }
            /// <summary>
            /// Duration of extra work to allocate to refineries on each iteration.
            /// </summary>
            /// <remarks>
            /// If this is too low, refineries may occasionally run out of work when the timer is delayed.
            /// If this is too high, it may take a while for the script to adjust to changes in ingot
            /// consumption. A couple of seconds seems to be about right.
            /// </remarks>
            public float IntervalOverlapSeconds { get { return 2f; } }

            /// <summary>
            /// Refinery speed, defined for the map. Usually one of 1, 3 or 10.
            /// Survival Realistic is 1, Creative is 10.
            /// </summary>
            public float RefinerySpeedFactor { get { return 1; } }
            /// <summary>
            /// Assembler efficiency, defined for the map. Usually one of 1, 3 or 10.
            /// Survival Realistic is 1, Creative is 10.
            /// </summary>
            public float AssemblerEfficiencyFactor { get { return 1; } }
        }

        /// <summary>
        /// Define all compute-once constants used by the script.
        /// </summary>
        /// <returns></returns>
        Everything DefineEverything()
        {
            var materials = new[] { 
                new Material { 
                    OreType = new ItemType("Ore/Cobalt"), 
                    OreConsumedPerSecond = 0.25f, 
                    IngotType = new ItemType("Ingot/Cobalt"), 
                    IngotsProducedPerSecond = 0.075f, 
                    IngotsConsumedPerSecond = 220f, 
                    IsCommonMetal = true 
                }, 
                new Material { 
                    OreType = new ItemType("Ore/Gold"), 
                    OreConsumedPerSecond = 2.5f, 
                    IngotType = new ItemType("Ingot/Gold"), 
                    IngotsProducedPerSecond = 0.025f, 
                    IngotsConsumedPerSecond = 5f, 
                }, 
                new Material { 
                    OreType = new ItemType("Ore/Iron"), 
                    OreConsumedPerSecond = 20f, 
                    IngotType = new ItemType("Ingot/Iron"), 
                    IngotsProducedPerSecond = 14f, 
                    IngotsConsumedPerSecond = 600f,
                    IsCommonMetal = true 
                }, 
                new Material { 
                    OreType = new ItemType("Ore/Nickel"), 
                    OreConsumedPerSecond = 0.5f, 
                    IngotType = new ItemType("Ingot/Nickel"), 
                    IngotsProducedPerSecond = 0.2f, 
                    IngotsConsumedPerSecond = 70f,
                    IsCommonMetal = true 
                }, 
                new Material { 
                    OreType = new ItemType("Ore/Magnesium"), 
                    OreConsumedPerSecond = 1f, 
                    IngotType = new ItemType("Ingot/Magnesium"), 
                    IngotsProducedPerSecond = 0.007f, 
                    IngotsConsumedPerSecond = 0.35f,
                }, 
                new Material { 
                    OreType = new ItemType("Ore/Silicon"), 
                    OreConsumedPerSecond = 1.6667f, 
                    IngotType = new ItemType("Ingot/Silicon"), 
                    IngotsProducedPerSecond = 1.1667f, 
                    IngotsConsumedPerSecond = 15f, 
                }, 
                new Material { 
                    OreType = new ItemType("Ore/Silver"), 
                    OreConsumedPerSecond = 1f, 
                    IngotType = new ItemType("Ingot/Silver"), 
                    IngotsProducedPerSecond = 0.1f, 
                    IngotsConsumedPerSecond = 10f,
                }, 
                new Material { 
                    OreType = new ItemType("Ore/Platinum"), 
                    OreConsumedPerSecond = 0.25f, 
                    IngotType = new ItemType("Ingot/Platinum"), 
                    IngotsProducedPerSecond = 0.0013f, 
                    IngotsConsumedPerSecond = 0.4f, 
                },
                // Remove the /* and */ to enable Uranium management:
                /*
                new Material { 
                    OreType = new ItemType("Ore/Uranium"), 
                    OreConsumedPerSecond = 0.25f, 
                    IngotType = new ItemType("Ingot/Platinum"), 
                    IngotsProducedPerSecond = 0.0018f, 
                    IngotsConsumedPerSecond = 0.01f,
                    // Increase this to maintain a uranium stockpile:
                    MinimumStockpileOverride = 0f
                }
                 */
            };

            return new Everything(materials);
        }
        
        Everything precomputed;
        public static Configuration configuration = new Configuration();

        // Recomputed on each run: 
        private System.Linq.ILookup<string, IMyCargoContainer> containers;
        private IList<IMyAssembler> assemblers;
        private IList<Refinery> refineriesNeedingWork;
        private IDictionary<ItemType, IList<OreDonor>> ore;
        private IDictionary<ItemType, float> ingots;
        private float interval;

        private IMyTextPanel debugScreen;
        private DateTime lastRun;

        public void InitDebug()
        {
            debugScreen = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("Debug");
            if (debugScreen == null) return;
            debugScreen.WritePublicText(String.Format("{0:dd MMM HH:mm}\n", DateTime.Now));
        }

        public void WriteDebug(string message, params object[] args)
        {
            if (debugScreen == null) return;
            debugScreen.WritePublicText(String.Format(message, args), true);
            debugScreen.WritePublicText("\n", true);
        }

        void Main()
        {
            if (!precomputed.IsInitialised) precomputed = DefineEverything(); // One-off precomputation. 
            InitDebug();

            var now = DateTime.Now;
            WriteDebug("Time since last run: {0:0.#}ms", (now - lastRun).TotalMilliseconds);
            lastRun = now;

            var timerBlock = (IMyTimerBlock)GridTerminalSystem.GetBlockWithName(configuration.TimerBlockName);
            if (timerBlock == null) throw new Exception(String.Format("Timer block not found: '{0}'", configuration.TimerBlockName));
            // A timer's interval is not precise. There's a certain amount of lag in it, so add a second or two to account for that.
            interval = timerBlock.TriggerDelay + configuration.MaxTimerLagSeconds;

            // Get refineries which are expected to run dry before this script runs again:
            refineriesNeedingWork =
                System.Linq.Enumerable.ToList(
                    System.Linq.Enumerable.OrderByDescending(
                        System.Linq.Enumerable.Where(
                            GetParticipatingRefineries(),
                            NeedsWork),
                        SelectIngotProductionRate));
            if (!System.Linq.Enumerable.Any(refineriesNeedingWork)) return;

            containers = AllCargoContainersByName();
            assemblers = GetParticipatingAssemblers();

            ore = CollectOreStorage(
                System.Linq.Enumerable.ToList(
                    System.Linq.Enumerable.Select(
                        System.Linq.Enumerable.SelectMany(configuration.OreStorageContainerNames, SelectCargoContainersFromName),
                        SelectCargoContainerInventory)));
            ingots = CollectIngotAmounts(
                System.Linq.Enumerable.ToList(
                    System.Linq.Enumerable.SelectMany(configuration.IngotStorageContainerNames, SelectCargoContainersFromName)));

            // Don't bother considering materials for which we have no ore: 
            var ingotStockpiles =
                System.Linq.Enumerable.ToList(
                    System.Linq.Enumerable.Where(
                        System.Linq.Enumerable.Select(precomputed.Materials, AnalyseStockpile),
                        IsOreAvailable));

            WriteDebug("{0} refineries needing work, {1} ore types available", refineriesNeedingWork.Count, ingotStockpiles.Count);

            // Log any stockpiles which are in danger of running out:
            for (var i = 0; i < ingotStockpiles.Count; i++)
            {
                var item = ingotStockpiles[i];
                if (item.QuotaFraction < 1)
                {
                    WriteDebug("{0}: {1:0.##} / {2:0.##}", item.Material.IngotType.SubtypeId, item.CurrentQuantity, item.TargetQuantity);
                }
            }

            // Attempt to queue enough ore to keep refineries busy until we run again.
            // Prioritise materials which are 'closest' to running out and adjust those estimates
            // as work is assigned.

            var stockpiles = new Stockpiles(ingotStockpiles);
            var iterator = refineriesNeedingWork.GetEnumerator();
            while (iterator.MoveNext())
            {
                FillRefinery(iterator.Current, stockpiles);
            }
        }

        // Lambdas

        public static ItemType SelectOreType(Material material) { return material.OreType; }
        public static float SelectOreConsumedPerSecond(Material material) { return material.OreConsumedPerSecond; }
        public string SelectCustomName(IMyTerminalBlock block) { return block.CustomName; }
        public bool NeedsWork(Refinery refinery) { return GetSecondsToClear(refinery) < interval; }
        public float SelectIngotProductionRate(Refinery refinery) { return refinery.IngotProductionRate; }
        public bool IsOreAvailable(IngotStockpile stockpile) { return ore.ContainsKey(stockpile.Material.OreType); }
        public IEnumerable<IMyCargoContainer> SelectCargoContainersFromName(string containerName) { return containers[containerName]; }
        public static IMyInventory SelectCargoContainerInventory(IMyCargoContainer container) { return container.GetInventory(0); }
        public static bool IsBelowTarget(IngotStockpile stockpile) { return stockpile.QuotaFraction < 1f; }
        public static float SelectQuotaFraction(IngotStockpile stockpile) { return stockpile.QuotaFraction; }

        /// <summary>
        /// Calculate how long the specified refinery will take to run dry, taking into account
        /// refinery speed and ore type.
        /// </summary>
        /// <param name="refinery"></param>
        /// <returns></returns>
        private float GetSecondsToClear(Refinery refinery)
        {
            var items = refinery.GetOreInventory().GetItems();
            float time = 0;
            for (var i = 0; i < items.Count; i++)
            {
                time += precomputed.GetSecondsToClear(items[i]);
            }
            return time / refinery.OreConsumptionRate;
        }

        /// <summary>
        /// Provide the specified refinery with enough work to keep it busy until the next iteration.
        /// Prefer ore types which yield ingots which are in heavy demand.
        /// </summary>
        /// <remarks>
        /// Ore already being processed is not considered when estimating how many ingots will be produced.
        /// This is because we already have at least one interval of lag in adjusting to new requests anyway
        /// and the amount of ore in flight should be insignificant in comparison (approx. one
        /// 'IntervalOverlapSeconds'); it's not worth the hassle to calculate it.
        /// </remarks>
        /// <param name="refinery"></param>
        /// <param name="stockpiles"></param>
        private void FillRefinery(Refinery refinery, Stockpiles stockpiles)
        {
            var secondsToClear = GetSecondsToClear(refinery);
            // How much work does this refinery need to keep it busy until the next iteration, with a safety margin?
            var workRequiredSeconds = interval + configuration.IntervalOverlapSeconds - secondsToClear;
            if (workRequiredSeconds <= 0) return;

            // This is how much of the newly-assigned work can be applied against production targets.
            // Safety margin doesn't apply to contribution to quotas.
            var assemblerDeadlineSeconds = interval - secondsToClear;

            // Get candidate material types in priority order.
            var candidates = stockpiles.GetCandidates(refinery);

            for (var i = 0; i < candidates.Count; i++)
            {
                var stockpile = candidates[i];

                var workProvidedSeconds = TryFillRefinery(refinery, stockpile, workRequiredSeconds);
                if (workProvidedSeconds <= 0) continue;

                workRequiredSeconds -= workProvidedSeconds;
                var workTowardsDeadline = Math.Min(assemblerDeadlineSeconds, workProvidedSeconds);
                if (workTowardsDeadline > 0)
                {
                    // Some of the new work will be processed before next iteration. Update our estimates.
                    stockpile.EstimatedProduction += workTowardsDeadline * refinery.IngotProductionRate * stockpile.Material.IngotsProducedPerSecond;
                    stockpiles.Update(stockpile);
                }
                assemblerDeadlineSeconds -= workTowardsDeadline;

                if (workRequiredSeconds <= 0)
                {
                    // Refinery's work target is satisfied. It should not run dry before we run again.
                    return;
                }
            }

            // No more ore available for this refinery.
            WriteDebug("{0}: No more work for this refinery.", refinery.RefineryName);
        }

        /// <summary>
        /// Try to use the specified refinery to satisfy the given ingot stockpile's quota, up to
        /// the refinery's current work target.
        /// </summary>
        /// <param name="refinery"></param>
        /// <param name="stockpile"></param>
        /// <param name="workRequiredSeconds">Amount of work (in seconds) this refinery needs to keep it busy until the next iteration.</param>
        /// <returns>Amount of work (in seconds) provided to this refinery.</returns>
        private float TryFillRefinery(Refinery refinery, IngotStockpile stockpile, float workRequiredSeconds)
        {
            // How much of this type of ore is required to meet the refinery's work target?
            var oreRate = refinery.OreConsumptionRate * stockpile.Material.OreConsumedPerSecond;
            var oreQuantityRequired = oreRate * workRequiredSeconds;

            var sources = ore[stockpile.Material.OreType];

            var workProvidedSeconds = 0f;
            // Iterate over available stacks until we run out or satisfy the quota.
            for (var j = 0; j < sources.Count; j++)
            {
                var donor = sources[j];
                var item = donor.GetItem();
                if (item == null || item.Amount == 0)
                {
                    // Donor stack is empty. Remove it.
                    sources.RemoveAt(j);
                    j--;
                    continue;
                }
                if (!donor.Inventory.IsConnectedTo(refinery.GetOreInventory()))
                {
                    // Donor inventory can't reach this refinery. Skip it.
                    continue;
                }

                // Don't try to transfer more ore than the donor stack has.
                var transfer = Math.Min(oreQuantityRequired, (float)item.Amount);
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

        /// <summary>
        /// For the given material, determine how many kg of ingots are required to keep all the 
        /// assemblers fed assuming the most demanding blueprint.
        /// </summary>
        private IngotStockpile AnalyseStockpile(Material material)
        {
            var maxConsumption = material.IngotsConsumedPerSecond * interval * assemblers.Count / configuration.AssemblerEfficiencyFactor;
            return new IngotStockpile
            {
                Material = material,
                CurrentQuantity = ingots.ContainsKey(material.IngotType) ? ingots[material.IngotType] : 0,
                TargetQuantity = maxConsumption
            };
        }

        /*********************************************** SCAN INGOT STORAGE ********************************************/

        /// <summary>
        /// Scan all ingot containers and total their ingot contents by type.
        /// </summary>
        /// <remarks>
        /// Counts the entire current stockpile of ingots in a single scan of the containers.
        /// </remarks>
        private IDictionary<ItemType, float> CollectIngotAmounts(IList<IMyCargoContainer> containers)
        {
            var amounts = new Dictionary<ItemType, float>();
            WriteDebug("Iterating {0} containers for ingots", containers.Count);
            for (var i = 0; i < containers.Count; i++)
            {
                CollectItemAmounts(amounts, containers[i], "MyObjectBuilder_Ingot");
            }
            return amounts;
        }

        private void CollectItemAmounts(IDictionary<ItemType, float> amounts, IMyCargoContainer container, string typeId)
        {
            for (var i = 0; i < container.GetInventoryCount(); i++)
            {
                var items = container.GetInventory(i).GetItems();
                for (var j = 0; j < items.Count; j++)
                {
                    var item = items[j];
                    if (item.Content.TypeId.ToString() != typeId) continue;
                    AddItem(amounts, item);
                }
            }
        }

        private void AddItem(IDictionary<ItemType, float> amounts, IMyInventoryItem item)
        {
            if (item.Amount == (VRage.MyFixedPoint)0f) return;
            var type = new ItemType(item.Content.TypeId.ToString(), item.Content.SubtypeId.ToString());
            float soFar;
            amounts.TryGetValue(type, out soFar);
            amounts[type] = soFar + (float)item.Amount;
        }

        /************************************************ SCAN ORE STORAGE *********************************************/

        /// <summary>
        /// Scan all ore containers and index their ore stacks by type.
        /// </summary>
        /// <remarks>
        /// Enables ore stacks of a given type to be found quickly, with only a single scan of the containers.
        /// (Trades RAM for CPU time)
        /// </remarks>
        private IDictionary<ItemType, IList<OreDonor>> CollectOreStorage(IList<IMyInventory> inventories)
        {
            var sets = new Dictionary<ItemType, IList<OreDonor>>();
            WriteDebug("Iterating {0} containers for ore", inventories.Count);
            for (var i = 0; i < inventories.Count; i++)
            {
                CollectInventoriesByContents(sets, inventories[i], "MyObjectBuilder_Ore");
            }
            return sets;
        }

        private void CollectInventoriesByContents(IDictionary<ItemType, IList<OreDonor>> inventories, IMyInventory inventory, string typeId)
        {
            var items = inventory.GetItems();
            for (var j = 0; j < items.Count; j++)
            {
                var item = items[j];
                if (item.Content.TypeId.ToString() != typeId) continue;
                AddInventory(inventories, item, inventory);
            }
        }

        private void AddInventory(IDictionary<ItemType, IList<OreDonor>> inventories, IMyInventoryItem item, IMyInventory inventory)
        {
            if (item.Amount == (VRage.MyFixedPoint)0f) return;
            var type = new ItemType(item.Content.TypeId.ToString(), item.Content.SubtypeId.ToString());
            IList<OreDonor> soFar;
            if (!inventories.TryGetValue(type, out soFar))
            {
                soFar = new List<OreDonor>();
                inventories[type] = soFar;
            }
            soFar.Add(new OreDonor { Inventory = inventory, ItemId = item.ItemId });
        }

        /*************************************************** SCAN GRID ************************************************/

        /// <summary>
        /// Find all assemblers on-grid which are enabled and pulling from conveyors.
        /// </summary>
        private IList<IMyAssembler> GetParticipatingAssemblers()
        {
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(blocks);
            var result = new List<IMyAssembler>();
            for (var i = 0; i < blocks.Count; i++)
            {
                var assembler = (IMyAssembler)blocks[i];
                if (!assembler.Enabled) continue;
                if (!assembler.UseConveyorSystem) continue;
                result.Add(assembler);
            }
            return result;
        }

        /// <summary>
        /// Find all refineries on-grid which are enabled and configured for script management, ie.
        /// not automatically pulling from conveyors.
        /// </summary>
        private IList<Refinery> GetParticipatingRefineries()
        {
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(blocks);
            var result = new List<Refinery>();
            for (var i = 0; i < blocks.Count; i++)
            {
                var refinery = (IMyRefinery)blocks[i];
                if (!refinery.Enabled) continue;
                if (refinery.UseConveyorSystem) continue;
                result.Add(new Refinery(refinery));
            }
            return result;
        }

        /// <summary>
        /// Find all cargo containers on-grid and group them by name for efficient lookup.
        /// </summary>
        private System.Linq.ILookup<string, IMyCargoContainer> AllCargoContainersByName() 
        { 
            return System.Linq.Enumerable.ToLookup(
                System.Linq.Enumerable.OfType<IMyCargoContainer>(GridTerminalSystem.Blocks),
                SelectCustomName);
        }


        /************************************************** DATA STRUCTURES ***********************************************/

        /// <summary>
        /// Maintains a list of ingot types, sorted in ascending order of estimated quantity as a
        /// fraction of target quantity. Or to put it another way, in descending order of priority.
        /// The ingot type which might run out fastest should be first in the list.
        /// </summary>
        /// <remarks>
        /// Ingot types for which we have no ore should already have been filtered out.
        /// </remarks>
        public struct Stockpiles
        {
            private List<IngotStockpile> stockpiles;
            public Stockpiles(IEnumerable<IngotStockpile> stockpiles)
            {
                this.stockpiles = System.Linq.Enumerable.ToList(
                    System.Linq.Enumerable.OrderBy(stockpiles, SelectQuotaFraction));
            }

            public bool HasAny { get { return stockpiles.Count > 0; } }

            /// <summary> 
            /// Fetch all stockpiles whose quotas the specified refinery can contribute towards, in
            /// descending priority order.
            /// </summary> 
            public IList<IngotStockpile> GetCandidates(Refinery refinery)
            {
                var candidates = new List<IngotStockpile>();
                for (var i = 0; i < stockpiles.Count; i++)
                {
                    var stockpile = stockpiles[i];
                    if (refinery.CanRefine(stockpile.Material)) candidates.Add(stockpile);
                }
                return candidates;
            }

            /// <summary>
            /// Replace an entry with updated estimates.
            /// </summary>
            public void Update(IngotStockpile updated)
            {
                if (stockpiles.Count == 0)
                {
                    stockpiles.Add(updated);
                    return;
                }
                var remove = System.Linq.Enumerable.FirstOrDefault(stockpiles, updated.IsSameMaterial);
                stockpiles.Remove(remove);
                stockpiles.Add(updated);
                stockpiles = System.Linq.Enumerable.ToList(
                    System.Linq.Enumerable.OrderBy(stockpiles, SelectQuotaFraction));
            }
        }

        /// <summary>
        /// Records current quantities and target quantities for a material.
        /// </summary>
        public struct IngotStockpile
        {
            public Material Material { get; set; }
            /// <summary>
            /// Units (kg) of ingots currently known to be in storage.
            /// </summary>
            public float CurrentQuantity { get; set; }
            /// <summary>
            /// Units (kg) of ingots which we believe newly-allocated refinery work will produce
            /// before the next iteration.
            /// </summary>
            public float EstimatedProduction { get; set; }
            /// <summary>
            /// Maximum number of units (kg) of ingots which may be consumed by assemblers, assuming
            /// most expensive blueprint.
            /// </summary>
            public float TargetQuantity { get; set; }

            /// <summary>
            /// Based on newly-allocated work, the estimated number of units (kg) of ingots which
            /// will be produced before the next iteration.
            /// </summary>
            public float EstimatedQuantity { get { return CurrentQuantity + EstimatedProduction; } }
            /// <summary>
            /// Estimated fraction of target quantity (kg) of ingots which should be produced before
            /// the next iteration.
            /// </summary>
            /// <remarks>
            /// Used as a priority indicator. The lower this is, the sooner the given ingot type might
            /// run out. If this is less than 1, it means that if all assemblers are manufacturing the
            /// most demanding blueprint then the ingots may run out.
            /// </remarks>
            public float QuotaFraction { get { return EstimatedQuantity / TargetQuantity; } }

            public bool IsSameMaterial(IngotStockpile other)
            {
                return Equals(other.Material, Material);
            }

            public bool CanBeRefinedBy(Refinery refinery)
            {
                return refinery.CanRefine(Material);
            }
        }

        /// <summary>
        /// Wraps a Refinery block. Exposes processing rate information
        /// based on refinery type (Arc Furnace vs. Refinery).
        /// </summary>
        public struct Refinery
        {
            private readonly IMyRefinery block;
            private readonly bool isArcFurnace;
            private readonly float refineSpeed;
            private readonly float materialEfficiency;

            private const float REFINESPEED_ARC_FURNACE = 1.6f;
            private const float REFINESPEED_REFINERY = 1.3f;
            private const float MATERIALEFFICIENCY_ARC_FURNACE = 0.9f;
            private const float MATERIALEFFICIENCY_REFINERY = 0.8f;


            public Refinery(IMyRefinery block)
                : this()
            {
                this.block = block;
                isArcFurnace = block.DefinitionDisplayNameText == "Arc furnace";
                refineSpeed = isArcFurnace ? REFINESPEED_ARC_FURNACE : REFINESPEED_REFINERY;
                materialEfficiency = isArcFurnace ? MATERIALEFFICIENCY_ARC_FURNACE : MATERIALEFFICIENCY_REFINERY;
                IsValid = true;
            }

            public string RefineryName { get { return block.CustomName; } }
            /// <summary>
            /// Multiplied by blueprint input and processing time to determine ore kg/sec.
            /// </summary>
            /// <remarks>
            /// ASSUMPTION: Material efficiency applies to output rather than input.
            /// </remarks>
            public float OreConsumptionRate
            {
                get
                {
                    return refineSpeed
                        * configuration.RefinerySpeedFactor;
                }
            }
            /// <summary>
            /// Multiplied by blueprint output and processing time to determine ingot kg/sec.
            /// </summary>
            /// <remarks>
            /// ASSUMPTION: Material efficiency applies to output rather than input.
            /// </remarks>
            public float IngotProductionRate
            {
                get
                {
                    return refineSpeed * materialEfficiency
                        * configuration.RefinerySpeedFactor;
                }
            }

            /// <summary>
            /// True if this object is not default(Refinery).
            /// </summary>
            public bool IsValid { get; private set; }

            /// <summary>
            /// Optimisation. Skip ores that this refinery can't handle instead of iterating
            /// all their stacks.
            /// </summary>
            public bool CanRefine(Material material)
            {
                if (isArcFurnace) return material.IsCommonMetal;
                return true;
            }

            /// <summary>
            /// Get the inventory of this refinery responsible for storing ore.
            /// </summary>
            public IMyInventory GetOreInventory()
            {
                return block.GetInventory(0);
            }
        }

        public struct Material
        {
            public ItemType OreType { get; set; }
            /// <summary> 
            /// Units (kg) of ore consumed per second by one refinery, on Realistic settings, assuming
            /// speed and efficiency of 1. 
            /// Calculated as blueprint input amount divided by processing time. 
            /// </summary> 
            public float OreConsumedPerSecond { get; set; }
            public ItemType IngotType { get; set; }
            /// <summary> 
            /// Units (kg) of ingots produced per second by one refinery, on Realistic settings, assuming
            /// speed and efficiency of 1. 
            /// Calculated as blueprint output amount divided by processing time. 
            /// </summary> 
            public float IngotsProducedPerSecond { get; set; }
            /// <summary> 
            /// Maximum number of units (kg) of ingots consumed per second by a single assembler, on
            /// Realistic settings. 
            /// Calculated as the largest input amount of any supported blueprint, divided by processing time. 
            /// </summary> 
            public float IngotsConsumedPerSecond { get; set; }
            /// <summary> 
            /// Minimum stockpile of ingots to maintain, irrespective of consumption rate or number
            /// of assemblers.
            /// </summary> 
            public float MinimumStockpileOverride { get; set; }

            /// <summary> 
            /// If true, Arc Furnaces can refine this ore. 
            /// </summary> 
            public bool IsCommonMetal { get; set; }
        }

        /// <summary>
        /// Describes the type of a PhysicalItem or Component in a form which can be used as a dictionary key.
        /// </summary>
        public struct ItemType
        {
            /// <summary>
            /// Creates an item type from a raw TypeId and SubtypeId, eg. MyObjectBuilder_Ore, Iron
            /// </summary>
            public ItemType(string typeId, string subtypeId)
                : this()
            {
                TypeId = typeId;
                SubtypeId = subtypeId;
            }

            /// <summary>
            /// Creates an item type from a human-readable item 'path', eg. Ore/Iron
            /// </summary>
            public ItemType(string typePath)
                : this()
            {
                var pathParts = typePath.Split('/');
                if (pathParts.Length != 2) throw new Exception("Path is not of the format TypeId/SubtypeId: " + typePath);
                var typeId = pathParts[0];
                SubtypeId = pathParts[1];
                if (typeId == "" || SubtypeId == "") throw new Exception("Not a valid path: " + typePath);
                TypeId = GetActualTypeId(typeId);
            }

            private string GetActualTypeId(string abbreviated)
            {
                return "MyObjectBuilder_" + abbreviated;
            }

            public string TypeId { get; private set; }
            public string SubtypeId { get; private set; }
        }

        /// <summary>
        /// Bookmarks an item stack in an inventory.
        /// </summary>
        /// <remarks>
        /// ASSUMPTION: An item's 
        /// </remarks>
        public struct OreDonor
        {
            public IMyInventory Inventory { get; set; }
            public uint ItemId { get; set; }

            public IMyInventoryItem GetItem()
            {
                return Inventory.GetItemByID(ItemId);
            }

            public bool TransferTo(IMyInventory target, float amount)
            {
                var index = Inventory.GetItems().IndexOf(GetItem());
                return Inventory.TransferItemTo(target, index, null, true, (VRage.MyFixedPoint)amount);
            }
        }

        /// <summary>
        /// Store some precomputed information, lookup tables, etc.
        /// </summary>
        public struct Everything
        {
            public bool IsInitialised { get; private set; }
            public IList<Material> Materials { get; private set; }

            private readonly IDictionary<ItemType, float> oreConsumedPerSecond;


            public float GetSecondsToClear(IMyInventoryItem item)
            {
                var type = new ItemType(item.Content.TypeId.ToString(), item.Content.SubtypeId.ToString());
                float perSecond;
                if (!oreConsumedPerSecond.TryGetValue(type, out perSecond)) return 0f;
                return ((float)item.Amount) / perSecond;
            }

            public Everything(IList<Material> materials)
                : this()
            {
                Materials = materials;
                IsInitialised = true;

                oreConsumedPerSecond = System.Linq.Enumerable.ToDictionary(materials, SelectOreType, SelectOreConsumedPerSecond);
            }
        }
    }
}
