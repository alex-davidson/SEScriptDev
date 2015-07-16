using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;

namespace Script.RefineryBalance.v2
{
    public partial class Program
    {
        public static readonly string DEFAULT_CONFIGURATION = "";
        public static readonly long TARGET_INTERVAL_SECONDS = 10;
        public static readonly long OVERLAP_INTERVAL_SECONDS = 2;
        public static readonly long DEADLINE_INTERVAL_MILLISECONDS = 4; // 4ms
        /*
         * 'Lean' Refinery Driver Script v2.0
         * Alex Davidson, 08/07/2015.
         * License: Beerware. Feel free to modify and redistribute this script, and if you think it's
         *          worth it, maybe buy me a beer someday.
         * 
         * PURPOSE
         * 
         * Given expected maximum rate of consumption of ingot types, this script scans your stockpile
         * and attempts to determine which type of ingot could run out first in order to prioritise
         * refining of the appropriate ore type.
         * 
         * OPERATING ENVIRONMENT
         * 
         * This script needs:
         *   * Refineries set to NOT use the conveyor system (any that are using it will be ignored)
         *   * Timer trigger loop, to run it on every update.
         *   * Knowledge of the game mode's refinery and assembler speed factors.
         *   
         * COMMENTS
         * 
         * There are a lot of casts from double to float. This is because the structuress used to 
         * configure everything take doubles in order to eliminate the need for the 'f' suffix, but
         * we only need the precision of floats.
         */
        public static readonly long UPDATES_PER_SECOND = 60;
        public static readonly long TARGET_INTERVAL_UPDATES = UPDATES_PER_SECOND * TARGET_INTERVAL_SECONDS;
        public static readonly long OVERLAP_INTERVAL_UPDATES = UPDATES_PER_SECOND * OVERLAP_INTERVAL_SECONDS;
        public static readonly long DEADLINE_INTERVAL_TICKS = DEADLINE_INTERVAL_MILLISECONDS * TimeSpan.TicksPerMillisecond; // 4ms

        public class RefineryDriver
        {
            public RefineryDriver(string configurationString)
            {
                this.configurationString = configurationString;
            }

            private readonly string configurationString;
            
            public bool VerifyConfiguration(string currentConfiguration)
            {
                return currentConfiguration == configurationString;
            }

            private SystemState state;

            // If true, yielding. Run again next cycle.
            public bool Run(long start, IMyGridTerminalSystem gts)
            {
                if (state == null)
                {
                    state = Initialise(configurationString);
                    return true;
                }

                var deadline = start + DEADLINE_INTERVAL_TICKS;

                while (Clock.RealTicks < deadline)
                {
                    if(!state.Current.Run(state, gts)) return false;
                }
                return true;
            }

            private static SystemState Initialise(string configurationString)
            {
                DetectDuplicates(BLUEPRINTS, SelectBlueprintName, "Duplicate blueprint name: {0}");

                var configuration = new ConfigurationParser().Parse(configurationString);

                var state = new SystemState
                {
                    Blueprints = new Blueprints(BLUEPRINTS),
                    RefineryFactory = new RefineryFactory(REFINERY_TYPES)
                };
                state.Current = new DetectRefinerySpeedTask(state);
                
                var ores = System.Linq.Enumerable.ToArray(
                    System.Linq.Enumerable.Distinct(
                        System.Linq.Enumerable.Select(BLUEPRINTS, SelectInputItemType)));

                var ingotTypes = System.Linq.Enumerable.ToArray(PrepareIngotTypes(configuration, state.Blueprints));

                state.OreTypes = new OreTypes(ores, BLUEPRINTS);
                state.IngotTypes = new IngotTypes(ingotTypes);
                state.Ingots = new IngotStockpiles(
                    System.Linq.Enumerable.ToArray(
                        System.Linq.Enumerable.Select(ingotTypes, SelectStockpileFromIngotType)));

                if (configuration.AssemblerSpeedFactor.HasValue) state.AssemblerSpeedFactor = configuration.AssemblerSpeedFactor.Value;
                if (configuration.RefinerySpeedFactor.HasValue)
                {
                    state.RefinerySpeedFactor = configuration.RefinerySpeedFactor.Value;
                    state.Current = new WaitForDeadlineTask();
                }
                state.StatusDisplayName = configuration.StatusDisplayName;
                state.InventoryBlockNames = configuration.InventoryBlockNames;

                return state;
            }

            private static void DetectDuplicates<TItem, TKey>(IEnumerable<TItem> items, Func<TItem, TKey> selectKey, string messageFormat)
            {
                var duplicates = System.Linq.Enumerable.Select(
                    System.Linq.Enumerable.Where(
                        System.Linq.Enumerable.GroupBy(items, selectKey),
                        IsCountMoreThanOne),
                    SelectGroupKey);
                if (!System.Linq.Enumerable.Any(duplicates)) return;
                var anyDuplicate = System.Linq.Enumerable.FirstOrDefault(duplicates);
                throw new Exception(String.Format(messageFormat, anyDuplicate));
            }

            class IngotConfigurer
            {
                private readonly IDictionary<ItemType, RequestedIngotConfiguration> ingotConfigurations;

                public IngotConfigurer(IDictionary<ItemType, RequestedIngotConfiguration> ingotConfigurations)
                {
                    this.ingotConfigurations = ingotConfigurations;
                }

                public IngotType Configure(IngotType type)
                {
                    RequestedIngotConfiguration ingotConfig;
                    if (!ingotConfigurations.TryGetValue(type.ItemType, out ingotConfig)) return type;
                    type.Enabled = true; // Enable it if it's not already enabled.
                    type.StockpileTargetOverride = ingotConfig.StockpileTarget;
                    type.StockpileLimit = ingotConfig.StockpileLimit;
                    return type;
                }
            }

            private static IEnumerable<IngotType> PrepareIngotTypes(RequestedConfiguration configuration, Blueprints blueprints)
            {
                return System.Linq.Enumerable.Select(
                    System.Linq.Enumerable.Where(
                        System.Linq.Enumerable.Select(INGOT_TYPES, new IngotConfigurer(configuration.Ingots).Configure),
                        IsIngotTypeEnabled),
                    blueprints.CalculateNormalisationFactor);

            }
        }

        public class ConfigurationParser
        {
            public RequestedConfiguration Parse(string configurationString)
            {
                // TODO

                return new RequestedConfiguration();
            }
        }

        #region Tasks

        public interface IScriptTask
        {
            /// <summary>
            /// Returns false if no more work is immediately available.
            /// </summary>
            /// <param name="state"></param>
            /// <param name="gts"></param>
            /// <returns></returns>
            bool Run(ISystemState state, IMyGridTerminalSystem gts);
        }

        public class DetectRefinerySpeedTask : IScriptTask
        {
            private readonly SystemState writableState;

            public DetectRefinerySpeedTask(SystemState writableState)
            {
                this.writableState = writableState;
            }

            public bool Run(ISystemState state, IMyGridTerminalSystem gts)
            {
                writableState.RefinerySpeedFactor = 1; // TODO
                state.Current = new WaitForDeadlineTask();
                return true;
            }
        }

        public class WaitForDeadlineTask : IScriptTask
        {
            public bool Run(ISystemState state, IMyGridTerminalSystem gts)
            {
                if (state.NextAllocationTimestamp > Clock.GameUpdates) return false;
                state.NextAllocationTimestamp = Clock.GameUpdates + TARGET_INTERVAL_UPDATES;
                state.Current = new CollectBlocksTask();
                return true;
            }
        }

        public class CollectBlocksTask : IScriptTask
        {
            public bool Run(ISystemState state, IMyGridTerminalSystem gts)
            {
                var refineries = GetParticipatingRefineries(state, gts);
                state.TotalAssemblerSpeed = GetTotalParticipatingAssemblerSpeed(gts) * state.AssemblerSpeedFactor;

                var inventoryOwners = new List<IMyTerminalBlock>();
                gts.GetBlocksOfType<IMyInventoryOwner>(inventoryOwners);
                
                state.Current = new ScanContainersTask(state, refineries, System.Linq.Enumerable.Cast<IMyInventoryOwner>(inventoryOwners));
                
                return true;
            }


            /// <summary>
            /// Find all refineries on-grid which are enabled and configured for script management, ie.
            /// not automatically pulling from conveyors.
            /// </summary>
            private IList<Refinery> GetParticipatingRefineries(ISystemState state, IMyGridTerminalSystem gts)
            {
                var blocks = new List<IMyTerminalBlock>();
                gts.GetBlocksOfType<IMyRefinery>(blocks);
                var result = new List<Refinery>();
                for (var i = 0; i < blocks.Count; i++)
                {
                    var block = (IMyRefinery)blocks[i];
                    if (!IsBlockOperational(block)) continue;
                    if (block.UseConveyorSystem) continue;

                    var refinery = state.RefineryFactory.TryResolveRefinery(block, state.RefinerySpeedFactor);
                    if (refinery != null)
                    {
                        result.Add(refinery);
                    }
                    else
                    {
                        Debug.Write("Unrecognised refinery type: {0}", block.BlockDefinition);
                    }
                }
                return result;
            }

            /// <summary>
            /// Find all assemblers on-grid which are enabled and pulling from conveyors.
            /// </summary>
            private float GetTotalParticipatingAssemblerSpeed(IMyGridTerminalSystem gts)
            {
                // ASSUMPTION: Only assembler type in the game has a base speed of 1.
                const float assemblerSpeed = 1;

                var blocks = new List<IMyTerminalBlock>();
                gts.GetBlocksOfType<IMyAssembler>(blocks);
                var totalSpeed = 0f;
                for (var i = 0; i < blocks.Count; i++)
                {
                    var assembler = (IMyAssembler)blocks[i];
                    if (!IsBlockOperational(assembler)) continue;
                    if (!assembler.UseConveyorSystem) continue;

                    totalSpeed += assemblerSpeed;
                    var moduleBonuses = ParseModuleBonuses(assembler);
                    if (moduleBonuses.Count > 0)
                    {
                        var speedModifier = moduleBonuses[0] - 1; // +1 Speed per 100%.
                        totalSpeed += speedModifier;
                    }
                }
                return totalSpeed;
            }
        }

        public class ScanContainersTask : IScriptTask
        {
            private readonly IList<Refinery> refineries;
            private IEnumerator<IMyInventoryOwner> iterator;
            private InventoryScanner scanner;

            public ScanContainersTask(ISystemState state, IList<Refinery> refineries, IEnumerable<IMyInventoryOwner> inventoryOwners)
            {
                this.refineries = refineries;
                inventoryOwners = System.Linq.Enumerable.ToArray(inventoryOwners);
                iterator = inventoryOwners.GetEnumerator();
                scanner = new InventoryScanner(state.IngotTypes.All, state.OreTypes.All);
            }

            public bool Run(ISystemState state, IMyGridTerminalSystem gts)
            {
                if(iterator.MoveNext())
                {
                    scanner.Scan(iterator.Current);
                    return true;
                }

                state.Ingots.UpdateAssemblerSpeed(state.TotalAssemblerSpeed);
                var ingotWorklist = state.Ingots.UpdateQuantities(scanner.Ingots);

                LogToStatusDisplay(state, gts);
                state.Current = new AllocateWorkTask(refineries, ingotWorklist, scanner.Ore);
                return true;
            }

            private void LogToStatusDisplay(ISystemState state, IMyGridTerminalSystem gts)
            {
                state.Ingots.LogToStatusDisplay(Debug.DebugPanel);

                if (String.IsNullOrEmpty(state.StatusDisplayName)) return;
                var statusScreen = (IMyTextPanel)gts.GetBlockWithName(state.StatusDisplayName);
                if (statusScreen == null || statusScreen == Debug.DebugPanel) return;

                // Clear previous state.
                statusScreen.WritePublicText(String.Format("Ingot stockpiles  {0:dd MMM HH:mm}\n", DateTime.Now));

                state.Ingots.LogToStatusDisplay(statusScreen);
            }
        }

        public class AllocateWorkTask : IScriptTask
        {
            private readonly RefineryWorklist refineries;
            private readonly IDictionary<ItemType, List<OreDonor>> oreDonors;
            private readonly IngotWorklist ingotWorklist;

            public AllocateWorkTask(IList<Refinery> refineries, IngotWorklist ingotWorklist, IDictionary<ItemType, List<OreDonor>> oreDonors)
            {
                this.refineries = new RefineryWorklist(refineries);
                this.ingotWorklist = ingotWorklist;
                this.oreDonors = oreDonors;
            }

            public bool Run(ISystemState state, IMyGridTerminalSystem gts)
            {
                if (AllocateSingle(state)) return true;

                state.Current = new WaitForDeadlineTask();
                return false;
            }

            private bool AllocateSingle(ISystemState state)
            {
                // TODO

                // if no refineries need work, return false
                // if no ore available, return false
                return false;
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
            private void FillRefinery(ISystemState state, Refinery refinery, IngotStockpile stockpile)
            {
                var secondsToClear = GetSecondsToClear(state.OreTypes, refinery);
                // How much work does this refinery need to keep it busy until the next iteration, with a safety margin?
                var workRequiredSeconds = TARGET_INTERVAL_SECONDS + OVERLAP_INTERVAL_SECONDS - secondsToClear;
                if (workRequiredSeconds <= 0) return;

                // This is how much of the newly-assigned work can be applied against production targets.
                // Safety margin doesn't apply to contribution to quotas.
                var assemblerDeadlineSeconds = TARGET_INTERVAL_SECONDS - secondsToClear;

                // Get candidate blueprints in priority order.
                var candidates =
                    System.Linq.Enumerable.ToArray(
                        System.Linq.Enumerable.OrderByDescending(
                            state.Blueprints.GetBlueprintsProducing(stockpile.Ingot.ItemType),
                            ingotWorklist.ScoreBlueprint));

                for (var i = 0; i < candidates.Length; i++)
                {
                    var blueprint = candidates[i];

                    var workProvidedSeconds = TryFillRefinery(refinery, blueprint, workRequiredSeconds);
                    if (workProvidedSeconds <= 0)
                    {
                        Debug.Write("Unable to allocate any {0}, moving to next candidate.", blueprint.Input.ItemType.SubtypeId);
                        continue;
                    }

                    workRequiredSeconds -= workProvidedSeconds;
                    var workTowardsDeadline = Math.Min(assemblerDeadlineSeconds, workProvidedSeconds);
                    if (workTowardsDeadline > 0)
                    {
                        // Some of the new work will be processed before next iteration. Update our estimates.
                        ingotWorklist.UpdateStockpileEstimates(refinery, blueprint, workTowardsDeadline);
                    }
                    assemblerDeadlineSeconds -= workTowardsDeadline;

                    if (workRequiredSeconds <= 0)
                    {
                        // Refinery's work target is satisfied. It should not run dry before we run again.
                        return;
                    }
                }

                // No more ore available for this refinery.
            }

            /// <summary>
            /// Calculate how long the specified refinery will take to run dry, taking into account
            /// refinery speed and ore type.
            /// </summary>
            /// <param name="refinery"></param>
            /// <returns></returns>
            private static float GetSecondsToClear(OreTypes oreTypes, Refinery refinery)
            {
                var items = refinery.GetOreInventory().GetItems();
                float time = 0;
                for (var i = 0; i < items.Count; i++)
                {
                    time += oreTypes.GetSecondsToClear(items[i]);
                }
                return time / refinery.OreConsumptionRate;
            }

            /// <summary>
            /// Try to use the specified refinery to process the specified blueprint, up to
            /// the refinery's current work target.
            /// </summary>
            /// <param name="refinery"></param>
            /// <param name="blueprint"></param>
            /// <param name="workRequiredSeconds">Amount of work (in seconds) this refinery needs to keep it busy until the next iteration.</param>
            /// <returns>Amount of work (in seconds) provided to this refinery.</returns>
            private float TryFillRefinery(Refinery refinery, Blueprint blueprint, float workRequiredSeconds)
            {
                // How much of this type of ore is required to meet the refinery's work target?
                var oreRate = refinery.OreConsumptionRate * (float)blueprint.Input.Quantity;
                var oreQuantityRequired = oreRate * workRequiredSeconds;

                var sources = oreDonors[blueprint.Input.ItemType];

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

        }

        #endregion

        public interface ISystemState
        {
            // Configuration
            RefineryFactory RefineryFactory { get; }
            Blueprints Blueprints { get; }
            OreTypes OreTypes { get; }
            IngotTypes IngotTypes { get; }
            string StatusDisplayName { get; }
            float RefinerySpeedFactor { get; }
            float AssemblerSpeedFactor { get; }
            ICollection<string> InventoryBlockNames { get; }

            // State maintained between iterations
            long NextAllocationTimestamp { get; set; }
            float TotalAssemblerSpeed { get; set; }
            IngotStockpiles Ingots { get; }

            // Currently-executing task
            IScriptTask Current { get; set; }
        }
        
        public class SystemState : ISystemState
        {
            public RefineryFactory RefineryFactory { get; set; }
            public Blueprints Blueprints { get; set; }
            public OreTypes OreTypes { get; set; }
            public IngotTypes IngotTypes { get; set; }
            public ICollection<string> InventoryBlockNames { get; set; }

            public string StatusDisplayName { get; set; }

            private float? refinerySpeedFactor;
            public float RefinerySpeedFactor
            {
                get { return refinerySpeedFactor ?? 1; } // Assume the worst (1) until we can detect it.
                set { if(value > 0) refinerySpeedFactor = value; }
            }
            private float? assemblerSpeedFactor;
            public float AssemblerSpeedFactor
            {
                get { return assemblerSpeedFactor ?? RefinerySpeedFactor; } // Assume same as refineries unless otherwise specified.
                set { if (value > 0) assemblerSpeedFactor = value; }
            }


            // Maintained between iterations, therefore must be SystemState properties:

            public long NextAllocationTimestamp { get; set; } // Timestamps are 'game time', counting update ticks.
            public float TotalAssemblerSpeed { get; set; }
            public IngotStockpiles Ingots { get; set; }

            public IScriptTask Current { get; set; }
        }

        public class RefineryWorklist
        {
            public RefineryWorklist(IEnumerable<Refinery> refineries)
            {

            }

            private Refinery[] refineriesBySpeed;
            private Refinery[] refineriesByEfficiency;
        }

        public class InventoryScanner
        {
            public InventoryScanner(IEnumerable<ItemType> ingotTypes, IEnumerable<ItemType> oreTypes)
            {
                Ore = new Dictionary<ItemType, List<OreDonor>>();
                var oreIterator = oreTypes.GetEnumerator();
                while(oreIterator.MoveNext())
                {
                    Ore.Add(oreIterator.Current, new List<OreDonor>());
                }
                Ingots = new Dictionary<ItemType, float>();
                var ingotIterator = ingotTypes.GetEnumerator();
                while (ingotIterator.MoveNext())
                {
                    Ingots.Add(ingotIterator.Current, 0);
                }
            }

            public IDictionary<ItemType, List<OreDonor>> Ore { get; private set; }
            public IDictionary<ItemType, float> Ingots { get; private set; }

            public void Scan(IMyInventoryOwner inventoryOwner)
            {
                var isRefinery = inventoryOwner is IMyRefinery;

                for (var i = 0; i < inventoryOwner.InventoryCount; i++)
                {
                    var inventory = inventoryOwner.GetInventory(i);
                    var items = inventory.GetItems();
                    for (var j = 0; j < items.Count; j++)
                    {
                        var item = items[j];
                        var itemType = new ItemType(item.Content.TypeId.ToString(), item.Content.SubtypeId.ToString());

                        AddIngots(itemType, item.ItemId);
                        if (!isRefinery) AddOre(itemType, inventory, item.ItemId);
                    }
                }
            }

            private void AddOre(ItemType ore, IMyInventory inventory, uint itemId)
            {
                List<OreDonor> existing;
                if (!Ore.TryGetValue(ore, out existing)) return;
                existing.Add(new OreDonor { Inventory = inventory, ItemId = itemId });
            }

            private void AddIngots(ItemType ingot, float quantity)
            {
                float existing;
                if (!Ingots.TryGetValue(ingot, out existing)) return;
                Ingots[ingot] = existing + quantity;
            }
        }

        public class IngotWorklist
        {
            public IngotWorklist(IngotStockpile[] ingotStockpiles)
            {
                this.stockpilesByIngotType = System.Linq.Enumerable.ToDictionary(ingotStockpiles, SelectIngotItemType);
                orderedStockpiles = SortStockpiles(ingotStockpiles);
            }

            private LinkedList<IngotStockpile> orderedStockpiles;
            private readonly Dictionary<ItemType, IngotStockpile> stockpilesByIngotType;

            private static LinkedList<IngotStockpile> SortStockpiles(IEnumerable<IngotStockpile> stockpiles)
            {
                return new LinkedList<IngotStockpile>(System.Linq.Enumerable.OrderBy(stockpiles, i => i.QuotaFraction));
            }

            public IngotStockpile Preferred { get { return orderedStockpiles.First.Value; } }

            public void Skip()
            {
                orderedStockpiles.RemoveFirst();
            }

            public void UpdateStockpileEstimates(Refinery refinery, Blueprint blueprint, float workTowardsDeadline)
            {
                for (var i = 0; i < blueprint.Outputs.Length; i++)
                {
                    var output = blueprint.Outputs[i];

                    IngotStockpile stockpile;
                    if (!stockpilesByIngotType.TryGetValue(output.ItemType, out stockpile)) continue;

                    stockpile.AssignedWork(workTowardsDeadline, refinery.GetActualIngotProductionRate(output, blueprint.Duration));
                }
            }

            public double ScoreBlueprint(Blueprint blueprint)
            {
                double score = 0;
                for (var i = 0; i < blueprint.Outputs.Length; i++)
                {
                    var output = blueprint.Outputs[i];

                    IngotStockpile stockpile;
                    if (!stockpilesByIngotType.TryGetValue(output.ItemType, out stockpile)) continue;
                    var quantityPerSecond = output.Quantity / blueprint.Duration;
                    score += (quantityPerSecond / stockpile.Ingot.ProductionNormalisationFactor) / stockpile.QuotaFraction;
                }
                return score;
            }
        }

        public struct IngotStockpiles
        {
            public IngotStockpiles(ICollection<IngotStockpile> ingotStockpiles)
            {
                this.ingotStockpiles = System.Linq.Enumerable.ToArray(ingotStockpiles);
            }

            private readonly IngotStockpile[] ingotStockpiles;

            public void UpdateAssemblerSpeed(float totalAssemblerSpeed)
            {
                for(var i = 0; i < ingotStockpiles.Length; i++)
                {
                    ingotStockpiles[i].UpdateAssemblerSpeed(totalAssemblerSpeed);
                }
            }

            public IngotWorklist UpdateQuantities(IDictionary<ItemType, float> currentQuantities)
            {
                for (var i = 0; i < ingotStockpiles.Length; i++)
                {
                    float quantity;
                    currentQuantities.TryGetValue(ingotStockpiles[i].Ingot.ItemType, out quantity);
                    ingotStockpiles[i].UpdateQuantity(quantity);
                }
                return new IngotWorklist(ingotStockpiles);
            }

            public void LogToStatusDisplay(IMyTextPanel display)
            {
                if (display == null) return;
                for (var i = 0; i < ingotStockpiles.Length; i++)
                {
                    var stockpile = ingotStockpiles[i];
                    display.WritePublicText(
                        String.Format("{0}:  {3:#000%}   {1:0.##} / {2:0.##} {4}\n",
                            stockpile.Ingot.ItemType.SubtypeId,
                            stockpile.CurrentQuantity,
                            stockpile.TargetQuantity,
                            stockpile.QuotaFraction,
                            stockpile.QuotaFraction < 1 ? "(!)" : ""),
                        true);
                }
            }
        }

        public static class Clock
        {
            public static long RealTicks { get { return DateTime.UtcNow.Ticks; } }
            public static long GameUpdates;
        }

        #region Requested configuration

        public struct RequestedIngotConfiguration
        {
            public float? StockpileTarget { get; set; }
            public float? StockpileLimit { get; set; }
        }

        public class RequestedConfiguration
        {
            public RequestedConfiguration()
            {
                Ingots = new Dictionary<ItemType, RequestedIngotConfiguration>();
                InventoryBlockNames = new List<string>();
            }

            public float? RefinerySpeedFactor { get; set; }
            public float? AssemblerSpeedFactor { get; set; }
            public IDictionary<ItemType, RequestedIngotConfiguration> Ingots { get; private set; }

            public List<string> InventoryBlockNames { get; private set; }
            public string StatusDisplayName { get; set; }
        }

        #endregion

        // Optimisation. We never need to run more often than once in 100ms, unless the driver yielded last time.
        private static readonly long HighPrecisionBailout = TimeSpan.FromMilliseconds(100).Ticks;

        private long lastCall;
        private bool yielded;
        private RefineryDriver instance;

        void Main(string args)
        {
            Clock.GameUpdates++;
            var now = Clock.RealTicks;
            var ticksSinceLastCall = now - lastCall;
            // Fast bail-out when we're between operations.
            if(!yielded && ticksSinceLastCall < HighPrecisionBailout) return;

            lastCall = now;

            Debug.Initialise(GridTerminalSystem);
            
            if (instance == null || !instance.VerifyConfiguration(args))
            {
                Debug.Write("Configuration updated. Reinitialising...");
                instance = new RefineryDriver(args);
            }

            yielded = instance.Run(now, GridTerminalSystem);

            Debug.Write("Runtime this update: {0:0.#}ms", (Clock.RealTicks - now) / TimeSpan.TicksPerMillisecond);
        }
        
        // Lambdas

        public static ItemType SelectOreType(Blueprint blueprint) { return blueprint.Input.ItemType; }
        public static float SelectOreConsumedPerSecond(Blueprint blueprint) { return (float)(blueprint.Input.Quantity / blueprint.Duration); }
        public string SelectCustomName(IMyTerminalBlock block) { return block.CustomName; }
        public static IMyInventory SelectCargoContainerInventory(IMyCargoContainer container) { return container.GetInventory(0); }
        public static ItemType SelectIngotItemType(IngotStockpile stockpile) { return stockpile.Ingot.ItemType; }
        public static ItemType SelectIngotTypeItemType(IngotType ingotType) { return ingotType.ItemType; }
        public static string SelectBlockDefinitionString(RefineryType type) { return type.BlockDefinitionName; }
        public static ItemType SelectInputItemType(Blueprint blueprint) { return blueprint.Input.ItemType; }
        public static string SelectBlueprintName(Blueprint blueprint) { return blueprint.Name; }
        public static TKey SelectGroupKey<TKey, TElement>(System.Linq.IGrouping<TKey, TElement> group) { return group.Key; }
        public static bool IsCountMoreThanOne<T>(IEnumerable<T> set) { return System.Linq.Enumerable.Count(set) > 1; }
        public static bool IsIngotTypeEnabled(IngotType ingotType) { return ingotType.Enabled; }
        public static IngotStockpile SelectStockpileFromIngotType(IngotType ingotType) { return new IngotStockpile(ingotType); }

        private static bool IsBlockOperational(IMyFunctionalBlock block)
        {
            return block.Enabled && block.IsFunctional && block.IsWorking;
        }

        /// <summary>
        /// Records current quantities and target quantities for a material.
        /// </summary>
        public class IngotStockpile
        {
            public IngotStockpile(IngotType ingot)
            {
                if (ingot.ProductionNormalisationFactor <= 0) throw new Exception(String.Format("ProductionNormalisationFactor is not positive, for ingot type {0}", ingot.ItemType));
                Ingot = ingot;
                UpdateAssemblerSpeed(1); // Default assembler speed to 'realistic'. Provided for tests; should be updated prior to use anyway.
            }

            public void AssignedWork(float seconds, float productionRate)
            {
                EstimatedProduction += seconds * productionRate;
            }

            public void UpdateAssemblerSpeed(float totalAssemblerSpeed)
            {
                TargetQuantity = Ingot.StockpileTargetOverride ?? ((float)Ingot.ConsumedPerSecond * TARGET_INTERVAL_SECONDS * totalAssemblerSpeed);

                shortfallUpperLimit = TargetQuantity / 2;
                shortfallLowerLimit = TargetQuantity / 100;
            }

            public void UpdateQuantity(float currentQuantity)
            {
                UpdateShortfall(CurrentQuantity - currentQuantity);
                CurrentQuantity = currentQuantity;
                EstimatedProduction = 0;
            }

            private void UpdateShortfall(float shortfall)
            {
                if (shortfall <= 0)
                {
                    // Production appears to be keeping up. Reduce shortfall until it falls below a threshold, then forget it.
                    if(lastShortfall <= 0) return;
                    if (lastShortfall < shortfallLowerLimit)
                    {
                        lastShortfall = 0;
                        return;
                    }
                    lastShortfall /= 2;
                }
                else
                {
                    // Produced less than was consumed?
                    lastShortfall = Math.Min(Math.Max(shortfall, lastShortfall), shortfallUpperLimit);
                }
            }

            public IngotType Ingot { get; private set; }

            private float shortfallUpperLimit;
            private float shortfallLowerLimit;
            // Adjustment factor for ingot consumption rate.
            private float lastShortfall;

            public float CurrentQuantity { get; private set; }
            /// <summary>
            /// Units (kg) of ingots which we believe newly-allocated refinery work will produce
            /// before the next iteration.
            /// </summary>
            public float EstimatedProduction { get; private set; }
            /// <summary>
            /// Maximum number of units (kg) of ingots which may be consumed by assemblers, assuming
            /// most expensive blueprint.
            /// </summary>
            public float TargetQuantity { get; set; }

            /// <summary>
            /// Based on newly-allocated work, the estimated number of units (kg) of ingots which
            /// will be produced before the next iteration, minus consumption.
            /// </summary>
            public float EstimatedQuantity { get { return Math.Max(0, CurrentQuantity + EstimatedProduction - lastShortfall); } }
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

            public bool IsSatisfied { get { return Ingot.StockpileLimit.HasValue && Ingot.StockpileLimit.Value <= EstimatedQuantity; } }

            /// <summary>
            /// Used for maintaining the sorted linked list of stockpiles.
            /// </summary>
            public IngotStockpile Next { get; set; }
        }

        public static IList<float> ParseModuleBonuses(IMyTerminalBlock block)
        {
            var percentages = new List<float>();
            if (String.IsNullOrEmpty(block.DetailedInfo)) return percentages;

            var lines = block.DetailedInfo.Split('\n');
            // A blank line separates block info from module bonuses.
            var foundBlankLine = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Trim() == "")
                {
                    if (foundBlankLine) break;
                    foundBlankLine = true;
                    continue;
                }
                if (!foundBlankLine) continue;

                float percent = 100;
                var m = rxModuleBonusPercent.Match(line);
                if (m.Success)
                {
                    if (!Single.TryParse(m.Groups["p"].Value, out percent)) percent = 100;
                }
                percentages.Add(percent / 100);
            }
            return percentages;
        }
        private static readonly System.Text.RegularExpressions.Regex rxModuleBonusPercent = new System.Text.RegularExpressions.Regex(@":\s*(?<p>\d+)%");

        public struct RefineryState
        {
            public long Timestamp;
            public float SecondsOfWork;
        }

        public class Refinery
        {
            private readonly IMyRefinery block;
            private readonly float refineSpeed;
            private readonly float materialEfficiency;

            public Refinery(IMyRefinery block, RefineryType type, float speedFactor)
            {
                this.block = block;
                refineSpeed = (float)type.Speed;
                materialEfficiency = (float)type.Efficiency;

                var moduleBonuses = ParseModuleBonuses(block);
                if (moduleBonuses.Count > 0)
                {
                    var speedModifier = moduleBonuses[0] - 1; // +1 Speed per 100%.
                    refineSpeed += speedModifier;
                }
                if (moduleBonuses.Count > 1)
                {
                    var efficiencyModifier = moduleBonuses[1];
                    materialEfficiency *= efficiencyModifier;
                }
                refineSpeed *= speedFactor;
            }

            public string Type { get { return block.BlockDefinition.ToString(); } }
            public float OreConsumptionRate { get { return refineSpeed; } }
            public IMyInventory GetOreInventory() { return block.GetInventory(0); }
            public bool IsValid { get { return IsBlockOperational(block); } }

            public float TheoreticalIngotProductionRate { get { return refineSpeed * materialEfficiency; } }

            public float GetActualIngotProductionRate(ItemAndQuantity ingotTypeFromBlueprint, double blueprintDuration)
            {
                // ASSUMPTION: Blueprints are defined such that each output type's quantity should not exceed 1.

                var actualOutputQuantity = Math.Min(ingotTypeFromBlueprint.Quantity * materialEfficiency, 1);
                return (float)(actualOutputQuantity / blueprintDuration) * refineSpeed;
            }
        }

        public struct ItemAndQuantity
        {
            public ItemAndQuantity(string typePath, double consumed) : this()
            {
                ItemType = new ItemType(typePath);
                Quantity = consumed;
            }
            public ItemType ItemType { get; private set; }
            public double Quantity { get; private set; }
        }

        public struct IngotType
        {
            public IngotType(string typePath, double consumedPerSecond)
                : this()
            {
                ItemType = new ItemType(typePath);
                ConsumedPerSecond = consumedPerSecond;
                Enabled = true;
            }

            public ItemType ItemType { get; private set; }
            public double ConsumedPerSecond { get; private set; }
            public double ProductionNormalisationFactor { get; set; }

            public bool Enabled { get; set; }
            public float? StockpileTargetOverride { get; set; }
            public float? StockpileLimit { get; set; }
        }

        public struct Blueprint
        {
            public Blueprint(string name, double duration, ItemAndQuantity input, params ItemAndQuantity[] outputs) : this()
            {
                Name = name;
                Duration = duration;
                Input = input;
                Outputs = outputs;
            }

            public string Name { get; set; }
            public double Duration { get; set; }
            public ItemAndQuantity Input { get; private set; }
            public ItemAndQuantity[] Outputs { get; private set; }
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
                TypeId = String.Intern(typeId);
                SubtypeId = String.Intern(subtypeId);
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
                SubtypeId = String.Intern(pathParts[1]);
                if (typeId == "" || SubtypeId == "") throw new Exception("Not a valid path: " + typePath);
                TypeId = GetActualTypeId(typeId);
            }

            private string GetActualTypeId(string abbreviated)
            {
                return String.Intern("MyObjectBuilder_" + abbreviated);
            }

            public string TypeId { get; private set; }
            public string SubtypeId { get; private set; }

            public override string ToString()
            {
                return TypeId + "/" + SubtypeId;
            }
        }

        public struct OreDonor
        {
            public IMyInventory Inventory { get; set; }
            public uint ItemId { get; set; }

            public IMyInventoryItem GetItem() { return Inventory.GetItemByID(ItemId); }

            public bool TransferTo(IMyInventory target, float amount)
            {
                var index = Inventory.GetItems().IndexOf(GetItem());
                return Inventory.TransferItemTo(target, index, null, true, (VRage.MyFixedPoint)amount);
            }
        }

        public struct RefineryType
        {
            public string BlockDefinitionName { get; private set; }
            public ICollection<string> SupportedBlueprints { get; private set; }
            public double Efficiency { get; set; }
            public double Speed { get; set; }

            public RefineryType(string subTypeName) : this()
            {
                BlockDefinitionName = String.Intern("MyObjectBuilder_Refinery/" + subTypeName);
                Efficiency = 1;
                Speed = 1;
                SupportedBlueprints = new HashSet<string>();
            }
        }

        #region 'Bundle' structures containing indexed game info about various things

        public struct Blueprints
        {
            private readonly IDictionary<ItemType, List<Blueprint>> blueprintsByOutputType;

            public Blueprints(IList<Blueprint> blueprints)
            {
                blueprintsByOutputType = new Dictionary<ItemType, List<Blueprint>>();
                var iterator = blueprints.GetEnumerator();
                while (iterator.MoveNext())
                {
                    var outputs = iterator.Current.Outputs;
                    for (var i = 0; i < outputs.Length; i++)
                    {
                        if (outputs[i].Quantity <= 0) continue;

                        List<Blueprint> existing;
                        if(!blueprintsByOutputType.TryGetValue(outputs[i].ItemType, out existing))
                        {
                            existing = new List<Blueprint>();
                            blueprintsByOutputType.Add(outputs[i].ItemType, existing);
                        }
                        existing.Add(iterator.Current);
                    }
                }
            }

            public double GetOutputPerSecondForDefaultBlueprint(ItemType singleOutput)
            {
                List<Blueprint> blueprints;
                if (!blueprintsByOutputType.TryGetValue(singleOutput, out blueprints)) return 0;

                double perSecond = 0;
                var ignoreMultiOutput = false;
                var iterator = blueprints.GetEnumerator();
                while (iterator.MoveNext())
                {
                    // Once we have a single-output blueprint, ignore all subsequent multi-output blueprints.
                    if (ignoreMultiOutput && iterator.Current.Outputs.Length > 0) continue;

                    var quantity = QuantityProduced(iterator.Current, singleOutput);
                    if (quantity <= 0) continue; // Blueprint doesn't produce this item type.
                    var thisPerSecond = quantity / iterator.Current.Duration;

                    var isBetterMatch = (!ignoreMultiOutput && iterator.Current.Outputs.Length == 1)
                        || (thisPerSecond > perSecond);

                    if (!isBetterMatch) continue;

                    perSecond = thisPerSecond;
                    ignoreMultiOutput = iterator.Current.Outputs.Length == 1;
                }
                return perSecond;
            }

            private static double QuantityProduced(Blueprint blueprint, ItemType itemType)
            {
                for (var i = 0; i < blueprint.Outputs.Length; i++)
                {
                    if (Equals(blueprint.Outputs[i].ItemType, itemType))
                    {
                        return blueprint.Outputs[i].Quantity;
                    }
                }
                return 0;
            }

            public IngotType CalculateNormalisationFactor(IngotType type)
            {
                if (type.ProductionNormalisationFactor > 0) return type;
                type.ProductionNormalisationFactor = GetOutputPerSecondForDefaultBlueprint(type.ItemType);
                return type;
            }

            public IList<Blueprint> GetBlueprintsProducing(ItemType singleOutput)
            {
                List<Blueprint> blueprints;
                if (!blueprintsByOutputType.TryGetValue(singleOutput, out blueprints)) return new List<Blueprint>();
                return blueprints;
            }
        }

        public struct OreTypes
        {
            private readonly IDictionary<ItemType, float> oreConsumedPerSecond;
            private readonly HashSet<ItemType> ores;

            public OreTypes(ItemType[] ores, Blueprint[] blueprints)
            {
                this.ores = new HashSet<ItemType>(ores);
                oreConsumedPerSecond = System.Linq.Enumerable.ToDictionary(
                    System.Linq.Enumerable.GroupBy(blueprints, SelectOreType, SelectOreConsumedPerSecond),
                    SelectGroupKey,
                    System.Linq.Enumerable.Max);
            }

            public float GetSecondsToClear(IMyInventoryItem item)
            {
                var type = new ItemType(item.Content.TypeId.ToString(), item.Content.SubtypeId.ToString());
                float perSecond;
                if (!oreConsumedPerSecond.TryGetValue(type, out perSecond)) return 0f;
                return ((float)item.Amount) / perSecond;
            }

            public ICollection<ItemType> All
            {
                get { return ores;}
            }
        }

        public struct IngotTypes
        {
            private readonly IngotType[] ingotTypes;

            public IngotTypes(IngotType[] ingotTypes)
            {
                this.ingotTypes = ingotTypes;
            }

            public IEnumerable<ItemType> All
            {
                get { return System.Linq.Enumerable.Select(ingotTypes, SelectIngotTypeItemType); }
            }
        }

        public struct RefineryFactory
        {
            private Dictionary<string, RefineryType> refineryTypesByBlockDefinitionString;
            public RefineryFactory(IEnumerable<RefineryType> refineryTypes)
            {
                refineryTypesByBlockDefinitionString = System.Linq.Enumerable.ToDictionary(refineryTypes, SelectBlockDefinitionString);
            }

            public Refinery TryResolveRefinery(IMyRefinery block, float refinerySpeedFactor)
            {
                RefineryType type;
                if (refineryTypesByBlockDefinitionString.TryGetValue(block.BlockDefinition.ToString(), out type))
                {
                    return new Refinery(block, type, refinerySpeedFactor);
                }
                return null;
            }
        }

        #endregion

        public static class Debug
        {
            public const string DEFAULT_SCREEN = "Debug:LeanRefinery";

            private static IMyTextPanel debugScreen;

            public static void Initialise(IMyGridTerminalSystem gts)
            {
                var needsClearing = debugScreen == null;
                debugScreen = (IMyTextPanel)gts.GetBlockWithName(DEFAULT_SCREEN);
                if (debugScreen == null) return;
                if (needsClearing) Clear();
            }

            public static void Clear()
            {
                if (debugScreen == null) return;
                debugScreen.WritePublicText(String.Format("{0:dd MMM HH:mm}\n", DateTime.Now));
            }

            public static void Write(string message, params object[] args)
            {
                if (debugScreen == null) return;
                debugScreen.WritePublicText(String.Format(message, args), true);
                debugScreen.WritePublicText("\n", true);
            }

            public static void Assert(bool condition, string failureText)
            {
                if (condition) return;
                Write("[ASSERT] {0}", failureText);
                throw new Exception(failureText);
            }

            public static void Fail(string failureText)
            {
                Write("[FAIL] {0}", failureText);
                throw new Exception(failureText);
            }

            public static void Warn(string failureText)
            {
                Write("[WARN] {0}", failureText);
            }

            public static IMyTextPanel DebugPanel { get { return debugScreen; } }
        }


        /*********************************************** GAME CONSTANTS ********************************************/

        public static IngotType[] INGOT_TYPES = new[] {
            new IngotType("Ingot/Cobalt", 220), 
            new IngotType("Ingot/Gold", 5),
            new IngotType("Ingot/Iron", 80),
            new IngotType("Ingot/Nickel", 70),
            new IngotType("Ingot/Magnesium", 0.35),
            new IngotType("Ingot/Silicon", 15),
            new IngotType("Ingot/Silver", 10),
            new IngotType("Ingot/Platinum", 0.4),
            new IngotType("Ingot/Uranium", 0.01) { Enabled = false },
            new IngotType("Ingot/Stone", 0.01) { Enabled = false }
        };
        public static Blueprint[] BLUEPRINTS = new[] {
            // Default blueprints for refining ore to ingots:
            new Blueprint("CobaltOreToIngot", 1, new ItemAndQuantity("Ore/Cobalt", 0.25), new ItemAndQuantity("Ingot/Cobalt", 0.075)),
            new Blueprint("GoldOreToIngot", 1, new ItemAndQuantity("Ore/Gold", 2.5), new ItemAndQuantity("Ingot/Gold", 0.025)),
            new Blueprint("IronOreToIngot", 1, new ItemAndQuantity("Ore/Iron", 20), new ItemAndQuantity("Ingot/Iron", 14)),
            new Blueprint("NickelOreToIngot", 1, new ItemAndQuantity("Ore/Nickel", 0.5), new ItemAndQuantity("Ingot/Nickel", 0.2)),
            new Blueprint("MagnesiumOreToIngot", 1, new ItemAndQuantity("Ore/Magnesium", 1), new ItemAndQuantity("Ingot/Magnesium", 0.007)),
            new Blueprint("SiliconOreToIngot", 1, new ItemAndQuantity("Ore/Silicon", 1.6667), new ItemAndQuantity("Ingot/Silicon", 1.1667)),
            new Blueprint("SilverOreToIngot", 1, new ItemAndQuantity("Ore/Silver", 1), new ItemAndQuantity("Ingot/Silver", 0.1)),
            new Blueprint("PlatinumOreToIngot", 1, new ItemAndQuantity("Ore/Platinum", 0.25), new ItemAndQuantity("Ingot/Platinum", 0.0013)),
            new Blueprint("ScrapToIronIngot", 1, new ItemAndQuantity("Ore/Scrap", 25), new ItemAndQuantity("Ingot/Iron", 20)),
            new Blueprint("ScrapIngotToIronIngot", 1, new ItemAndQuantity("Ingot/Scrap", 25), new ItemAndQuantity("Ingot/Iron", 20)),
            new Blueprint("UraniumOreToIngot", 1, new ItemAndQuantity("Ore/Uranium", 0.25), new ItemAndQuantity("Ingot/Uranium", 0.0018)),
            new Blueprint("StoneOreToIngot", 1, new ItemAndQuantity("Ore/Stone", 0.25), new ItemAndQuantity("Ingot/Stone", 0.0018))
        };
        public static RefineryType[] REFINERY_TYPES = new[] {
            new RefineryType("LargeRefinery")
            {
                SupportedBlueprints = { "StoneOreToIngot", "IronOreToIngot", "ScrapToIronIngot", "NickelOreToIngot", "CobaltOreToIngot",
                    "MagnesiumOreToIngot", "SiliconOreToIngot", "SilverOreToIngot", "GoldOreToIngot", "PlatinumOreToIngot", "UraniumOreToIngot" },
                Efficiency = 0.8,
                Speed = 1.3
            },
            new RefineryType("Blast Furnace")
            {
                SupportedBlueprints = { "IronOreToIngot", "ScrapToIronIngot", "NickelOreToIngot", "CobaltOreToIngot" },
                Efficiency = 0.9,
                Speed = 1.6
            },
            new RefineryType("Big Arc Furnace")
            {
                SupportedBlueprints = { "IronOreToIngot", "ScrapToIronIngot", "NickelOreToIngot", "CobaltOreToIngot" },
                Efficiency = 0.9,
                Speed = 16.8
            },
            new RefineryType("BigPreciousFurnace")
            {
                SupportedBlueprints = { "PlatinumOreToIngot", "SilverOreToIngot", "GoldOreToIngot" },
                Efficiency = 0.9,
                Speed = 16.1
            },
            new RefineryType("BigSolidsFurnace")
            {
                SupportedBlueprints = { "StoneOreToIngot", "MagnesiumOreToIngot", "SiliconOreToIngot" },
                Efficiency = 0.8,
                Speed = 16.2
            },
            new RefineryType("BigGasCentrifugalRefinery")
            {
                SupportedBlueprints = { "UraniumOreToIngot" },
                Efficiency = 0.95,
                Speed = 16
            }
        };

    }
}
