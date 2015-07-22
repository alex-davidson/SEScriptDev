using System;
using System.Collections.Generic;
using System.Text;
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
         * Alex Davidson, 08/08/2015.
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
         * This code uses doubles instead of floats to remove the 'f' suffix from the configuration
         * block at the end. Performance impact should be negligible.
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

            private bool initialised;
            private SystemState state;
            private ITransitions transitions;

            // If true, yielding. Run again next cycle.
            public bool Run(long start, IMyGridTerminalSystem gts)
            {
                if (!initialised)
                {
                    Initialise(configurationString);
                    return true;
                }

                var deadline = start + DEADLINE_INTERVAL_TICKS;

                do
                {
                    if (!transitions.CurrentTask.Run(state, transitions, gts))
                    {
                        transitions.CurrentTask.ReleaseResources();
                        return false;
                    }
                }
                while (Clock.RealTicks < deadline);

                transitions.CurrentTask.ReleaseResources();
                return true;
            }

            private void Initialise(string configurationString)
            {
                DetectDuplicates(BLUEPRINTS, SelectBlueprintName, "Duplicate blueprint name: {0}");

                var configuration = new ConfigurationParser().Parse(configurationString);
                var staticState = new StaticState(configuration);

                state = new SystemState(staticState);
                transitions = new TaskTransitions(staticState);
                if (staticState.RefinerySpeedFactor.HasValue)
                {
                    transitions.WaitForDeadline();
                }
                else
                {
                    transitions.DetectRefinerySpeed();
                }
                initialised = true;
            }

            public static void DetectDuplicates<TItem, TKey>(IEnumerable<TItem> items, Func<TItem, TKey> selectKey, string messageFormat)
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
        }

        public class ConfigurationParser
        {
            class Tokeniser
            {
                private readonly string configurationString;
                private int position;

                public Tokeniser(string configurationString)
                {
                    this.configurationString = configurationString;
                    position = -1;
                }

                public bool Next()
                {
                    if (position < configurationString.Length) position++;
                    return position < configurationString.Length;
                }
                
                public char Current { get { return configurationString[position]; } }

                private string GetToken(int start, int end)
                {
                    return configurationString.Substring(start, end - start);
                }

                public string ReadString()
                {
                    return ReadStringFrom(position);
                }

                private string ReadStringFrom(int start)
                {
                    var i = start;
                    if (IsQuote(configurationString[i]))
                    {
                        i++;
                        while (i < configurationString.Length)
                        {
                            if (IsQuote(configurationString[i]))
                            {
                                position = i + 1;
                                return GetToken(start + 1, i);
                            }
                            i++;
                        }

                        throw Error("Unterminated quoted string at char {0}: \"{1}", start, GetToken(position, i));
                    }
                    while (i < configurationString.Length && !IsTokenSeparator(configurationString[i])) i++;
                    var str = GetToken(start, i);
                    position = i;
                    return str;
                }

                public void RequireNext()
                {
                    if (!Next()) throw Error("Unexpected end of string, after '{0}'", configurationString[configurationString.Length - 1]);
                }

                public bool TryReadParameter(out string parameterValue)
                {
                    parameterValue = null;
                    if (position >= configurationString.Length) return false;
                    if (configurationString[position] != ARG_SEPARATOR) return false;

                    parameterValue = ReadStringFrom(position + 1);
                    return true;
                }

                public bool TryReadNumericParameter(out float? parameterValue)
                {
                    string s;
                    parameterValue = null;
                    if (!TryReadParameter(out s)) return false;
                    if (s.Length > 0)
                    {
                        float f;
                        if (!Single.TryParse(s, out f)) throw Error("Unable to parse '{0}' as a valid number.", s);
                        parameterValue = f;
                    }
                    return true;
                }

                private const char QUOTE = '"';
                private const char ARG_SEPARATOR = ':';

                private static bool IsQuote(char c)
                {
                    return c == QUOTE;
                }

                private static bool IsTokenSeparator(char c)
                {
                    return Char.IsWhiteSpace(c) || c == ARG_SEPARATOR;
                }
            }

            public RequestedConfiguration Parse(string configurationString)
            {
                var configuration = new RequestedConfiguration();
                var tokeniser = new Tokeniser(configurationString);
                while(tokeniser.Next())
                {
                    string parameterValue;
                    switch (tokeniser.Current)
                    {
                        case '+':
                            {
                                tokeniser.RequireNext();
                                ConfigureIngot(configuration, tokeniser, true);
                                break;
                            }

                        case '-':
                            {
                                tokeniser.RequireNext();
                                ConfigureIngot(configuration, tokeniser, false);
                                break;
                            }
                        case '/':
                            {
                                tokeniser.RequireNext();
                                ParseSimpleOption(configuration, tokeniser);
                            }
                            break;

                        default:
                            break;
                    }
                    // Discard any remaining parameters:
                    while (tokeniser.TryReadParameter(out parameterValue)) { }
                }
                return configuration;
            }

            private static void ConfigureIngot(RequestedConfiguration configuration, Tokeniser tokeniser, bool enableIngot)
            {
                var ingotName = tokeniser.ReadString();
                var ingotConfig = ConfigureIngot(configuration, ingotName);
                ingotConfig.Enable = enableIngot;
                float? val;
                if (tokeniser.TryReadNumericParameter(out val))
                {
                    ingotConfig.StockpileTarget = val;
                    if (tokeniser.TryReadNumericParameter(out val))
                    {
                        ingotConfig.StockpileLimit = val;
                    }
                }
            }

            private static void ParseSimpleOption(RequestedConfiguration configuration, Tokeniser tokeniser)
            {
                var optionName = tokeniser.ReadString();
                switch (optionName)
                {
                    case "status":
                        string statusDisplayName;
                        if (!tokeniser.TryReadParameter(out statusDisplayName) || statusDisplayName == "") throw Error("Expected '/status:<block name>'");
                        configuration.StatusDisplayName = statusDisplayName;
                        break;

                    case "scan":
                        string containerName;
                        if (!tokeniser.TryReadParameter(out containerName) || containerName == "") throw Error("Expected '/scan:<block name>'");
                        configuration.InventoryBlockNames.Add(containerName);
                        break;

                    case "assemblerSpeed":
                        float? assemblerSpeed;
                        if (!tokeniser.TryReadNumericParameter(out assemblerSpeed) || !assemblerSpeed.HasValue) throw Error("Expected '/assemblerSpeed:<number>'");
                        configuration.AssemblerSpeedFactor = assemblerSpeed;
                        break;

                    case "refinerySpeed":
                        float? refinerySpeed;
                        if (!tokeniser.TryReadNumericParameter(out refinerySpeed) || !refinerySpeed.HasValue) throw Error("Expected '/refinerySpeed:<number>'");
                        configuration.RefinerySpeedFactor = refinerySpeed;
                        break;

                    default:
                        throw Error("Unrecognised option: '/{0}'", optionName);
                }
            }

            private static Exception Error(string message, params object[] args)
            {
                return new Exception(String.Format(message, args));
            }


            private static RequestedIngotConfiguration ConfigureIngot(RequestedConfiguration configuration, string ingotName)
            {
                var itemType = ingotName.IndexOf('/') >= 0 ? new ItemType(ingotName) : new ItemType("Ingot/" + ingotName);
                RequestedIngotConfiguration ingotConfig;
                if (configuration.Ingots.TryGetValue(itemType, out ingotConfig)) throw new Exception("Duplicate ingot configuration: " + itemType);

                ingotConfig = new RequestedIngotConfiguration();
                configuration.Ingots.Add(itemType, ingotConfig);
                return ingotConfig;
            }
        }

        #region Tasks

        public interface IScriptTask
        {
            /// <summary>
            /// Returns false if no more work is immediately available.
            /// </summary>
            bool Run(ISystemState state, ITransitions transitions, IMyGridTerminalSystem gts);

            /// <summary>
            /// Before returning control to the game, discard references to any temporary
            /// resources which may otherwise be promoted to gen2.
            /// </summary>
            void ReleaseResources();
        }

        public class DetectRefinerySpeedTask : IScriptTask
        {
            public bool Run(ISystemState state, ITransitions transitions, IMyGridTerminalSystem gts)
            {
                state.RefinerySpeedFactor = 1; // TODO
                transitions.WaitForDeadline();
                return true;
            }

            public void ReleaseResources()
            {
            }
        }

        public class WaitForDeadlineTask : IScriptTask
        {
            public bool Run(ISystemState state, ITransitions transitions, IMyGridTerminalSystem gts)
            {
                if (state.NextAllocationTimestamp > Clock.GameUpdates) return false;
                state.NextAllocationTimestamp = Clock.GameUpdates + TARGET_INTERVAL_UPDATES;
                Debug.Clear();
                transitions.CollectBlocks();
                return true;
            }

            public void ReleaseResources()
            {
            }
        }

        public class CollectBlocksTask : IScriptTask
        {
            private readonly IList<string> inventoryBlockNames;

            public CollectBlocksTask(IList<string> inventoryBlockNames)
            {
                this.inventoryBlockNames = inventoryBlockNames;
            }

            public bool Run(ISystemState state, ITransitions transitions, IMyGridTerminalSystem gts)
            {
                var refineries = GetParticipatingRefineries(state, gts);

                var assemblerSpeedFactor = state.Static.AssemblerSpeedFactor ?? state.RefinerySpeedFactor;
                var totalAssemblerSpeed = GetTotalParticipatingAssemblerSpeed(gts) * assemblerSpeedFactor;
                state.Ingots.UpdateAssemblerSpeed(totalAssemblerSpeed);

                var inventoryOwners = CollectUniqueContainers(gts);
                transitions.ScanContainers(refineries, inventoryOwners);
                
                return true;
            }

            private IEnumerable<IMyInventoryOwner> CollectUniqueContainers(IMyGridTerminalSystem gts)
            {
                var blocks = new List<IMyTerminalBlock>();
                if (inventoryBlockNames.Count == 0)
                {
                    gts.GetBlocksOfType<IMyInventoryOwner>(blocks);
                    return System.Linq.Enumerable.OfType<IMyInventoryOwner>(blocks);
                }
                else
                {
                    for (var i = 0; i < inventoryBlockNames.Count; i++)
                    {
                        gts.SearchBlocksOfName(inventoryBlockNames[i], blocks);
                    }
                    return System.Linq.Enumerable.Distinct(System.Linq.Enumerable.OfType<IMyInventoryOwner>(blocks));
                }
            }

            public void ReleaseResources()
            {
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

                    var refinery = state.Static.RefineryFactory.TryResolveRefinery(block, state.RefinerySpeedFactor);
                    if (refinery == null)
                    {
                        Debug.Write("Unrecognised refinery type: {0}", block.BlockDefinition);
                    }
                    else
                    {
                        result.Add(refinery);
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

        /// <summary>
        /// Scan all participating inventories for ore and ingots.
        /// </summary>
        public class ScanContainersTask : IScriptTask
        {
            public ScanContainersTask(InventoryScanner scanner)
            {
                this.scanner = scanner;
                refineries = new List<Refinery>(ALLOC_REFINERY_COUNT);
                inventoryOwners = new List<IMyInventoryOwner>(ALLOC_INVENTORY_OWNER_COUNT);
            }

            public void Initialise(IEnumerable<Refinery> refineries, IEnumerable<IMyInventoryOwner> inventoryOwners)
            {
                this.refineries.Clear();
                this.refineries.AddRange(refineries);

                this.inventoryOwners.Clear();
                this.inventoryOwners.AddRange(inventoryOwners);

                this.scanner.Reset();
            }

            private readonly List<Refinery> refineries;
            private readonly List<IMyInventoryOwner> inventoryOwners;
            private readonly InventoryScanner scanner;

            public bool Run(ISystemState state, ITransitions transitions, IMyGridTerminalSystem gts)
            {
                if (inventoryOwners.Count > 0)
                {
                    scanner.Scan(inventoryOwners[0]);
                    inventoryOwners.RemoveAtFast(0);
                    return true;
                }

                state.Ingots.UpdateQuantities(scanner.Ingots);

                LogToStatusDisplay(state, gts);
                transitions.AllocateWork(refineries, scanner.Ore);
                return true;
            }

            private void LogToStatusDisplay(ISystemState state, IMyGridTerminalSystem gts)
            {
                state.Ingots.LogToStatusDisplay(Debug.DebugPanel);

                if (String.IsNullOrEmpty(state.Static.StatusDisplayName)) return;
                var statusScreen = (IMyTextPanel)gts.GetBlockWithName(state.Static.StatusDisplayName);
                if (statusScreen == null || statusScreen == Debug.DebugPanel) return;

                // Clear previous state.
                statusScreen.WritePublicText(String.Format("Ingot stockpiles  {0:dd MMM HH:mm}\n", DateTime.Now));

                state.Ingots.LogToStatusDisplay(statusScreen);
            }


            public void ReleaseResources()
            {
            }
        }

        public class AllocateWorkTask : IScriptTask
        {
            public AllocateWorkTask(RefineryWorklist refineryWorklist)
            {
                this.refineryWorklist = refineryWorklist;
            }

            public void Initialise(IList<Refinery> refineries, IDictionary<ItemType, List<OreDonor>> oreDonors)
            {
                this.refineryWorklist.Initialise(refineries);

                this.oreDonors.Clear();
                var oreDonorsIterator = oreDonors.GetEnumerator();
                while(oreDonorsIterator.MoveNext()) this.oreDonors.Add(oreDonorsIterator.Current.Key, oreDonorsIterator.Current.Value);
            }

            private readonly Dictionary<ItemType, List<OreDonor>> oreDonors = new Dictionary<ItemType, List<OreDonor>>(ALLOC_ORE_TYPE_COUNT);
            private IngotWorklist ingotWorklist;
            private RefineryWorklist refineryWorklist;
            
            public bool Run(ISystemState state, ITransitions transitions, IMyGridTerminalSystem gts)
            {
                if (ingotWorklist == null) ingotWorklist = state.Ingots.GetWorklist();

                if (AllocateSingle()) return true;
                
                transitions.WaitForDeadline();
                Refinery.ReleaseAll();
                return false;
            }

            public void ReleaseResources()
            {
                // Release sorted ingot list at the end of each iteration to avoid gen2 promotion.
                ingotWorklist = null;
                refineryWorklist.ReleaseResources();
            }

            private bool AllocateSingle()
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
                            if (FillCurrentRefinery(refineries)) return true; // Yield as soon as we've allocated work.
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
            private bool FillCurrentRefinery(IRefineryIterator worklist)
            {
                var assignedWork = false;
                // How much work does this refinery need to keep it busy until the next iteration, with a safety margin?
                Debug.Assert(worklist.PreferredWorkSeconds > 0, "PreferredWorkSeconds <= 0");

                // Get candidate blueprints in priority order.
                var candidates = System.Linq.Enumerable.ToArray(
                        System.Linq.Enumerable.OrderByDescending(worklist.GetCandidateBlueprints(), ingotWorklist.ScoreBlueprint));

                for (var i = 0; i < candidates.Length; i++)
                {
                    var blueprint = candidates[i];

                    var workProvidedSeconds = TryFillRefinery(worklist.Current, blueprint, worklist.PreferredWorkSeconds);
                    if (workProvidedSeconds <= 0)
                    {
                        Debug.Write("Unable to allocate any {0}, moving to next candidate.", blueprint.Input.ItemType.SubtypeId);
                        continue;
                    }
                    assignedWork = true;
                    var isRefinerySatisfied = worklist.AssignedWork(ref workProvidedSeconds);
                    if (workProvidedSeconds > 0)
                    {
                        // Some of the new work will be processed before next iteration. Update our estimates.
                        ingotWorklist.UpdateStockpileEstimates(worklist.Current, blueprint, workProvidedSeconds);
                    }
                    // If the refinery's work target is satisfied, it should not run dry before we run again.
                    if (isRefinerySatisfied) break;
                }

                // No more ore available for this refinery.
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
                var oreRate = refinery.OreConsumptionRate * blueprint.Input.Quantity;
                var oreQuantityRequired = oreRate * workRequiredSeconds;

                var sources = new OreDonorsIterator(oreDonors[blueprint.Input.ItemType]);

                double workProvidedSeconds = 0;
                // Iterate over available stacks until we run out or satisfy the quota.
                while(sources.Next())
                {
                    var donor = sources.Current;
                    var item = donor.GetItem();
                    if (item == null || item.Amount == 0)
                    {
                        // Donor stack is empty. Remove it.
                        sources.Remove();
                        continue;
                    }
                    if (!donor.Inventory.IsConnectedTo(refinery.GetOreInventory()))
                    {
                        // Donor inventory can't reach this refinery. Skip it.
                        continue;
                    }

                    // Don't try to transfer more ore than the donor stack has.
                    var transfer = Math.Min(oreQuantityRequired, (double)item.Amount);
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
            StaticState Static { get; }

            float RefinerySpeedFactor { get; set; }

            // State maintained between iterations
            long NextAllocationTimestamp { get; set; }
            float TotalAssemblerSpeed { get; set; }
            IngotStockpiles Ingots { get; }

        }

        public interface ITransitions
        {
            void DetectRefinerySpeed();
            void WaitForDeadline();
            void CollectBlocks();
            void ScanContainers(IEnumerable<Refinery> refineries, IEnumerable<IMyInventoryOwner> inventoryOwners);
            void AllocateWork(IList<Refinery> refineries, IDictionary<ItemType, List<OreDonor>> oreDonors);

            // Currently-executing task
            IScriptTask CurrentTask { get; }
        }

        public class TaskTransitions : ITransitions
        {
            private readonly DetectRefinerySpeedTask detectRefinerySpeedTask;
            private readonly WaitForDeadlineTask waitForDeadlineTask;
            private readonly CollectBlocksTask collectBlocksTask;
            private readonly ScanContainersTask scanContainersTask;
            private readonly AllocateWorkTask allocateWorkTask;

            public TaskTransitions(StaticState staticState)
            {
                detectRefinerySpeedTask = new DetectRefinerySpeedTask();
                waitForDeadlineTask = new WaitForDeadlineTask();
                collectBlocksTask = new CollectBlocksTask(staticState.InventoryBlockNames);
                scanContainersTask = new ScanContainersTask(new InventoryScanner(staticState.IngotTypes.AllItemTypes, staticState.OreTypes.All));
                allocateWorkTask = new AllocateWorkTask(new RefineryWorklist(staticState.OreTypes, staticState.IngotTypes, staticState.RefineryFactory, staticState.Blueprints));
                CurrentTask = waitForDeadlineTask;
            }

            public void DetectRefinerySpeed()
            {
                CurrentTask.ReleaseResources();
                CurrentTask = detectRefinerySpeedTask;
            }

            public void WaitForDeadline()
            {
                CurrentTask.ReleaseResources();
                CurrentTask = waitForDeadlineTask;
            }

            public void CollectBlocks()
            {
                CurrentTask.ReleaseResources();
                CurrentTask = collectBlocksTask;
            }

            public void ScanContainers(IEnumerable<Refinery> refineries, IEnumerable<IMyInventoryOwner> inventoryOwners)
            {
                scanContainersTask.Initialise(refineries, inventoryOwners);
                CurrentTask.ReleaseResources();
                CurrentTask = scanContainersTask;
            }

            public void AllocateWork(IList<Refinery> refineries, IDictionary<ItemType, List<OreDonor>> oreDonors)
            {
 	            allocateWorkTask.Initialise(refineries, oreDonors);
                CurrentTask.ReleaseResources();
                CurrentTask = allocateWorkTask;
            }

            public IScriptTask CurrentTask { get; private set; }
        }

        public class StaticState
        {
            public StaticState(RequestedConfiguration configuration)
            {
                Blueprints = new Blueprints(BLUEPRINTS);
                RefineryFactory = new RefineryFactory(REFINERY_TYPES);
                
                var ores = System.Linq.Enumerable.ToArray(
                    System.Linq.Enumerable.Distinct(
                        System.Linq.Enumerable.Select(BLUEPRINTS, SelectInputItemType)));

                OreTypes = new OreTypes(ores, BLUEPRINTS);

                var ingotTypes = System.Linq.Enumerable.ToArray(PrepareIngotTypes(configuration, Blueprints, RefineryFactory));
                IngotTypes = new IngotTypes(ingotTypes);
                
                RefinerySpeedFactor = configuration.RefinerySpeedFactor;
                AssemblerSpeedFactor = configuration.AssemblerSpeedFactor;
                StatusDisplayName = configuration.StatusDisplayName;
                InventoryBlockNames = configuration.InventoryBlockNames;
            }

            public RefineryFactory RefineryFactory { get; private set; }
            public Blueprints Blueprints { get; private set; }
            public OreTypes OreTypes { get; private set; }
            public IngotTypes IngotTypes { get; private set; }
            public IList<string> InventoryBlockNames { get; private set; }

            public string StatusDisplayName { get; private set; }
            public float? RefinerySpeedFactor { get; private set; }
            public float? AssemblerSpeedFactor { get; private set; }

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

            private static IEnumerable<IngotType> PrepareIngotTypes(RequestedConfiguration configuration, Blueprints blueprints, RefineryFactory refineryFactory)
            {
                return System.Linq.Enumerable.Select(
                    System.Linq.Enumerable.Where(
                        System.Linq.Enumerable.Select(INGOT_TYPES, new IngotConfigurer(configuration.Ingots).Configure),
                        IsIngotTypeEnabled),
                    blueprints.CalculateNormalisationFactor);

            }

            public bool FilterInventoryBlock(IMyTerminalBlock block)
            {
                if (InventoryBlockNames.Count == 0) return true;
                return InventoryBlockNames.Contains(block.CustomName);
            }
        }

        public class SystemState : ISystemState
        {
            public SystemState(StaticState staticState)
            {
                Static = staticState;
                Ingots = new IngotStockpiles(
                    System.Linq.Enumerable.ToArray(
                        System.Linq.Enumerable.Select(staticState.IngotTypes.All, i => new IngotStockpile(i))));
            }

            public StaticState Static {get; private set;}

            private float? refinerySpeedFactor;
            public float RefinerySpeedFactor
            {
                get { return refinerySpeedFactor ?? 1; } // Assume the worst (1) until we can detect it.
                set { if(value > 0) refinerySpeedFactor = value; }
            }


            // Maintained between iterations, therefore must be SystemState properties:

            public long NextAllocationTimestamp { get; set; } // Timestamps are 'game time', counting update ticks.
            public float TotalAssemblerSpeed { get; set; }
            public IngotStockpiles Ingots { get; private set; }
        }

        public class RefineryWorklist
        {
            private readonly OreTypes oreTypes;
            private readonly HashSet<Refinery> refineries = new HashSet<Refinery>();
            
            private readonly Dictionary<KeyValuePair<ItemType, string>, Blueprint[]> blueprintsByIngotTypeAndBlockDefinition;
            private readonly Dictionary<ItemType, string[]> blockDefinitionsByIngotType;
           
            public RefineryWorklist(OreTypes oreTypes, IngotTypes ingotTypes, RefineryFactory refineryFactory, Blueprints blueprints)
            {
                this.oreTypes = oreTypes;
                iterators = new Dictionary<ItemType, IRefineryIterator>(ingotTypes.All.Count);
                
                blueprintsByIngotTypeAndBlockDefinition = new Dictionary<KeyValuePair<ItemType, string>, Blueprint[]>(refineryFactory.AllTypes.Count * ingotTypes.All.Count);
                blockDefinitionsByIngotType = new Dictionary<ItemType, string[]>(ingotTypes.All.Count);
                foreach (var ingotType in ingotTypes.AllItemTypes)
                {
                    var bps = blueprints.GetBlueprintsProducing(ingotType);
                    var blocks = new List<string>(refineryFactory.AllTypes.Count);
                    foreach (var refineryType in refineryFactory.AllTypes)
                    {
                        var matchingBps = System.Linq.Enumerable.ToArray(
                                System.Linq.Enumerable.Where(bps, refineryType.Supports));
                        var key = new KeyValuePair<ItemType, string>(ingotType, refineryType.BlockDefinitionName);
                        blueprintsByIngotTypeAndBlockDefinition.Add(key, matchingBps);
                        if(matchingBps.Length > 0) blocks.Add(refineryType.BlockDefinitionName);
                    }
                    blockDefinitionsByIngotType.Add(ingotType, blocks.ToArray());
                }

            }
            
            public void Initialise(IList<Refinery> refineries)
            {
                this.refineries.Clear();
                this.refineries.UnionWith(refineries);
                refineriesByBlockDefinition = System.Linq.Enumerable.ToLookup(refineries, r => r.BlockDefinitionString);
            }

            private readonly Dictionary<ItemType, IRefineryIterator> iterators;
            public bool TrySelectIngot(ItemType ingotType, out IRefineryIterator iterator)
            {
                if (!iterators.TryGetValue(ingotType, out iterator))
                {
                    var definitions = blockDefinitionsByIngotType[ingotType];
                    if (definitions.Length == 0) return false;

                    InitResources();

                    var sortedList = System.Linq.Enumerable.ToList(
                            System.Linq.Enumerable.OrderByDescending(
                                System.Linq.Enumerable.SelectMany(definitions, d => refineriesByBlockDefinition[d]),
                                r => r.TheoreticalIngotProductionRate));

                    iterator = new RefineryIterator(this, ingotType, sortedList);
                    iterators.Add(ingotType, iterator);
                }
                return iterator.CanAllocate();
            }

            private System.Linq.ILookup<string, Refinery> refineriesByBlockDefinition;

            private void InitResources()
            {
                refineriesByBlockDefinition = refineriesByBlockDefinition ?? System.Linq.Enumerable.ToLookup(refineries, r => r.BlockDefinitionString);
            }

            public void ReleaseResources()
            {
                iterators.Clear();
                refineriesByBlockDefinition = null;
            }


            class RefineryIterator : IRefineryIterator
            {
                private int index;
                private bool valid;
                private readonly RefineryWorklist worklist;
                private readonly ItemType ingotType;
                private readonly List<Refinery> refineries;
                private Blueprint[] candidateBlueprints;

                public RefineryIterator(RefineryWorklist worklist, ItemType ingotType, List<Refinery> refineries)
                {
                    this.worklist = worklist;
                    this.ingotType = ingotType;
                    this.refineries = refineries;
                    index = 0;
                    valid = false;
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
                        if (TrySelectRefinery(refineries[index])) return true;
                        index++;
                    }
                    return false;
                }

                private bool TrySelectRefinery(Refinery refinery)
                {
                    RequiredWorkSeconds = TARGET_INTERVAL_SECONDS - refinery.GetSecondsToClear(worklist.oreTypes);
                    PreferredWorkSeconds += RequiredWorkSeconds + OVERLAP_INTERVAL_SECONDS;
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
                        worklist.refineries.Remove(Current);
                        Skip();
                        return true;
                    }
                    return false;
                }

                // How much work does this refinery need to keep it busy until the next iteration, with a safety margin?
                public double PreferredWorkSeconds { get; private set; }
                // This is how much of the newly-assigned work can be applied against production targets.
                // Safety margin doesn't apply to contribution to quotas.
                public double RequiredWorkSeconds { get; private set; }

                public void Skip()
                {
                    valid = false;
                    index++;
                }
            }

        }

        public interface IRefineryIterator
        {
            void Skip();
            bool AssignedWork(ref double seconds);
            Refinery Current { get; }
            double PreferredWorkSeconds { get; }
            double RequiredWorkSeconds { get; }
            bool CanAllocate();
            Blueprint[] GetCandidateBlueprints();
        }

        public class InventoryScanner
        {
            public InventoryScanner(IEnumerable<ItemType> ingotTypes, IEnumerable<ItemType> oreTypes)
            {
                Ore = new Dictionary<ItemType, List<OreDonor>>();
                Ingots = new Dictionary<ItemType, double>();

                var oreIterator = oreTypes.GetEnumerator();
                while (oreIterator.MoveNext())
                {
                    List<OreDonor> list;
                    if (!Ore.TryGetValue(oreIterator.Current, out list))
                    {
                        list = new List<OreDonor>(ALLOC_ORE_DONOR_COUNT);
                        Ore.Add(oreIterator.Current, list);
                    }
                }

                var ingotIterator = ingotTypes.GetEnumerator();
                while (ingotIterator.MoveNext())
                {
                    Ingots.Add(ingotIterator.Current, 0);
                }
            }

            public void Reset()
            {
                // Reuse OreDonor lists:
                var oreIterator = Ore.GetEnumerator();
                while (oreIterator.MoveNext())
                {
                    oreIterator.Current.Value.Clear();
                }

                var ingotIterator = System.Linq.Enumerable.ToList(Ingots.Keys).GetEnumerator();
                while (ingotIterator.MoveNext())
                {
                    Ingots[ingotIterator.Current] = 0;
                }
            }

            public IDictionary<ItemType, List<OreDonor>> Ore { get; private set; }
            public IDictionary<ItemType, double> Ingots { get; private set; }

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

            private void AddIngots(ItemType ingot, double quantity)
            {
                double existing;
                if (!Ingots.TryGetValue(ingot, out existing)) return;
                Ingots[ingot] = existing + quantity;
            }
        }

        public class IngotWorklist
        {
            public IngotWorklist(IngotStockpile[] ingotStockpiles)
            {
                this.stockpilesByIngotType = System.Linq.Enumerable.ToDictionary(ingotStockpiles, SelectIngotItemType);
                candidateStockpiles = new List<IngotStockpile>(ingotStockpiles);
                UpdatePreferred();
            }
            
            private double sortThreshold;
            private int? preferredIndex;
            private List<IngotStockpile> candidateStockpiles;
            private readonly Dictionary<ItemType, IngotStockpile> stockpilesByIngotType;
            
            private void UpdatePreferred()
            {
                preferredIndex = null;
                var lowest = Double.MaxValue;
                for(var i = 0; i < candidateStockpiles.Count; i++)
                {
                    var qf = candidateStockpiles[i].QuotaFraction;
                    if(qf < lowest)
                    {
                        sortThreshold = lowest;
                        preferredIndex = i;
                        lowest = qf;
                    }
                    else if(qf < sortThreshold)
                    {
                        sortThreshold = qf;
                    }
                }
            }
            
            public bool Any { get { return candidateStockpiles.Count > 0; } }
            
            public bool TryGetPreferred(out IngotStockpile preferred)
            {
                if (!preferredIndex.HasValue) UpdatePreferred();

                if (preferredIndex == null)
                {
                    preferred = null;
                    return false;
                }

                preferred = candidateStockpiles[preferredIndex.Value];
                return true;
            }

            public void Skip()
            {
                candidateStockpiles.RemoveAtFast(preferredIndex.Value);
                preferredIndex = null;
            }

            public void UpdateStockpileEstimates(Refinery refinery, Blueprint blueprint, double workTowardsDeadline)
            {
                for (var i = 0; i < blueprint.Outputs.Length; i++)
                {
                    var output = blueprint.Outputs[i];

                    IngotStockpile stockpile;
                    if (!stockpilesByIngotType.TryGetValue(output.ItemType, out stockpile)) continue;

                    stockpile.AssignedWork(workTowardsDeadline, refinery.GetActualIngotProductionRate(output, blueprint.Duration));
                }
                if(preferredIndex.HasValue)
                {
                    var s = candidateStockpiles[preferredIndex.Value];
                    if(s.IsSatisfied)
                    {
                        candidateStockpiles.RemoveAtFast(preferredIndex.Value);
                        preferredIndex = null;
                    }
                    else if(s.QuotaFraction > sortThreshold)
                    {
                        preferredIndex = null;
                    }
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

            public void UpdateAssemblerSpeed(double totalAssemblerSpeed)
            {
                for(var i = 0; i < ingotStockpiles.Length; i++)
                {
                    ingotStockpiles[i].UpdateAssemblerSpeed(totalAssemblerSpeed);
                }
            }

            public IngotWorklist GetWorklist()
            {
                return new IngotWorklist(ingotStockpiles);
            }

            public void UpdateQuantities(IDictionary<ItemType, double> currentQuantities)
            {
                for (var i = 0; i < ingotStockpiles.Length; i++)
                {
                    double quantity;
                    currentQuantities.TryGetValue(ingotStockpiles[i].Ingot.ItemType, out quantity);
                    ingotStockpiles[i].UpdateQuantity(quantity);
                }
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

        public class RequestedIngotConfiguration
        {
            public RequestedIngotConfiguration()
            {
                Enable = true;
            }

            public float? StockpileTarget { get; set; }
            public float? StockpileLimit { get; set; }
            public bool Enable { get; set; }
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
        private int yieldCount;
        private long totalTime;
        private RefineryDriver instance;

        void Main(string args)
        {
            Clock.GameUpdates++;
            var now = Clock.RealTicks;
            var ticksSinceLastCall = now - lastCall;
            // Fast bail-out when we're between operations.
            if(yieldCount <= 0 && ticksSinceLastCall < HighPrecisionBailout) return;

            lastCall = now;

            Debug.Initialise(GridTerminalSystem);
            
            if (instance == null || !instance.VerifyConfiguration(args))
            {
                Debug.Write("Configuration updated. Reinitialising...");
                instance = new RefineryDriver(args);
            }

            var yielded = instance.Run(now, GridTerminalSystem);

            var thisUpdate = Clock.RealTicks - now;
            Debug.Write("Runtime this update: {0:0.#}ms", thisUpdate / TimeSpan.TicksPerMillisecond);

            if(yielded)
            {
                yieldCount++;
                totalTime += thisUpdate;
            }
            else if(yieldCount > 0)
            {
                Debug.Write("Completed in {0:0.#}ms over {1} updates.", totalTime / TimeSpan.TicksPerMillisecond, yieldCount);
                yieldCount = 0;
                totalTime = 0;
            }
        }
        
        // Lambdas

        public static ItemType SelectOreType(Blueprint blueprint) { return blueprint.Input.ItemType; }
        public static double SelectOreConsumedPerSecond(Blueprint blueprint) { return blueprint.Input.Quantity / blueprint.Duration; }
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
        public static TKey SelectKey<TKey, TValue>(KeyValuePair<TKey, TValue> pair) { return pair.Key; }
        public static TValue SelectValue<TKey, TValue>(KeyValuePair<TKey, TValue> pair) { return pair.Value; }

        public static bool IsBlockOperational(IMyFunctionalBlock block)
        {
            return block.Enabled && block.IsFunctional && block.IsWorking;
        }

        /// <summary>
        /// Records current quantities and target quantities for a material.
        /// Long-lived object.
        /// </summary>
        public class IngotStockpile
        {
            public IngotStockpile(IngotType ingot)
            {
                if (ingot.ProductionNormalisationFactor <= 0) throw new Exception(String.Format("ProductionNormalisationFactor is not positive, for ingot type {0}", ingot.ItemType));
                Ingot = ingot;
                UpdateAssemblerSpeed(1); // Default assembler speed to 'realistic'. Provided for tests; should be updated prior to use anyway.
            }

            public void AssignedWork(double seconds, double productionRate)
            {
                EstimatedProduction += seconds * productionRate;
            }

            public void UpdateAssemblerSpeed(double totalAssemblerSpeed)
            {
                TargetQuantity = Ingot.StockpileTargetOverride ?? (Ingot.ConsumedPerSecond * TARGET_INTERVAL_SECONDS * totalAssemblerSpeed);

                shortfallUpperLimit = TargetQuantity / 2;
                shortfallLowerLimit = TargetQuantity / 100;
            }

            public void UpdateQuantity(double currentQuantity)
            {
                UpdateShortfall(CurrentQuantity - currentQuantity);
                CurrentQuantity = currentQuantity;
                EstimatedProduction = 0;
            }

            private void UpdateShortfall(double shortfall)
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

            private double shortfallUpperLimit;
            private double shortfallLowerLimit;
            // Adjustment factor for ingot consumption rate.
            private double lastShortfall;

            public double CurrentQuantity { get; private set; }
            /// <summary>
            /// Units (kg) of ingots which we believe newly-allocated refinery work will produce
            /// before the next iteration.
            /// </summary>
            public double EstimatedProduction { get; private set; }
            /// <summary>
            /// Maximum number of units (kg) of ingots which may be consumed by assemblers, assuming
            /// most expensive blueprint.
            /// </summary>
            public double TargetQuantity { get; private set; }

            /// <summary>
            /// Based on newly-allocated work, the estimated number of units (kg) of ingots which
            /// will be produced before the next iteration, minus consumption.
            /// </summary>
            public double EstimatedQuantity { get { return Math.Max(0, CurrentQuantity + EstimatedProduction - lastShortfall); } }
            /// <summary>
            /// Estimated fraction of target quantity (kg) of ingots which should be produced before
            /// the next iteration.
            /// </summary>
            /// <remarks>
            /// Used as a priority indicator. The lower this is, the sooner the given ingot type might
            /// run out. If this is less than 1, it means that if all assemblers are manufacturing the
            /// most demanding blueprint then the ingots may run out.
            /// </remarks>
            public double QuotaFraction { get { return EstimatedQuantity / TargetQuantity; } }

            public bool IsSatisfied { get { return Ingot.StockpileLimit.HasValue && Ingot.StockpileLimit.Value <= EstimatedQuantity; } }
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

        public class Refinery
        {
            private IMyRefinery block;
            private double refineSpeed;
            private double materialEfficiency;

            private Refinery()
            {
            }
            
            public static Refinery Get(IMyRefinery block, RefineryType type, double speedFactor)
            {
                var item = GetOrCreate();
                item.block = block;
                item.refineSpeed = type.Speed;
                item.materialEfficiency = type.Efficiency;
                item.BlockDefinitionString = type.BlockDefinitionName;

                var moduleBonuses = ParseModuleBonuses(block);
                if (moduleBonuses.Count > 0)
                {
                    var speedModifier = moduleBonuses[0] - 1; // +1 Speed per 100%.
                    item.refineSpeed += speedModifier;
                }
                if (moduleBonuses.Count > 1)
                {
                    var efficiencyModifier = moduleBonuses[1];
                    item.materialEfficiency *= efficiencyModifier;
                }
                item.refineSpeed *= speedFactor;
                return item;
            }

            public string BlockDefinitionString { get; private set; }
            public double OreConsumptionRate { get { return refineSpeed; } }
            public IMyInventory GetOreInventory() { return block.GetInventory(0); }
            public bool IsValid { get { return block != null && IsBlockOperational(block); } }

            public double TheoreticalIngotProductionRate { get { return refineSpeed * materialEfficiency; } }

            public double GetActualIngotProductionRate(ItemAndQuantity ingotTypeFromBlueprint, double blueprintDuration)
            {
                // ASSUMPTION: Blueprints are defined such that each output type's quantity should not exceed 1.

                var actualOutputQuantity = Math.Min(ingotTypeFromBlueprint.Quantity * materialEfficiency, 1);
                return (actualOutputQuantity / blueprintDuration) * refineSpeed;
            }
            
            /// <summary>
            /// Calculate how long the specified refinery will take to run dry, taking into account
            /// refinery speed and ore type.
            /// </summary>
            public double GetSecondsToClear(OreTypes oreTypes)
            {
                var items = GetOreInventory().GetItems();
                double time = 0;
                for (var i = 0; i < items.Count; i++)
                {
                    time += oreTypes.GetSecondsToClear(items[i]);
                }
                return time / OreConsumptionRate;
            }

            
            private static readonly List<Refinery> available = new List<Refinery>(ALLOC_REFINERY_COUNT);
            private static readonly List<Refinery> pool = new List<Refinery>(ALLOC_REFINERY_COUNT);

            public static void ReleaseAll()
            {
                available.Clear();
                available.AddRange(pool);
            }

            private static Refinery GetOrCreate()
            {
                if (available.Count == 0)
                {
                    var item = new Refinery();
                    pool.Add(item);
                    return item;    
                }
                var last = available[available.Count - 1];
                available.RemoveAt(available.Count - 1);
                return last;
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
            public double? StockpileTargetOverride { get; set; }
            public double? StockpileLimit { get; set; }
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

        public struct OreDonorsIterator
        {
            private readonly List<OreDonor> donors;
            private int current;
            
            public OreDonorsIterator(List<OreDonor> donors)
            {
                current = -1;
                this.donors = donors;
            }

            public void Remove()
            {
                // Order doesn't matter. Just get the next one really really fast.
                donors.RemoveAtFast(current);
                current--;
            }

            public OreDonor Current { get { return donors[current]; } }

            public bool Next()
            {
                current++;
                return current < donors.Count;
            }
        }

        public struct OreDonor
        {
            public IMyInventory Inventory { get; set; }
            public uint ItemId { get; set; }

            public IMyInventoryItem GetItem() { return Inventory.GetItemByID(ItemId); }

            public bool TransferTo(IMyInventory target, double amount)
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

            public bool Supports(Blueprint bp)
            {
                return SupportedBlueprints.Contains(bp.Name);
            }
        }

        #region 'Bundle' structures containing indexed game info about various things

        public struct Blueprints
        {
            private readonly IDictionary<ItemType, List<Blueprint>> blueprintsByOutputType;

            public Blueprints(IList<Blueprint> blueprints) : this()
            {
                All = blueprints;
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

            public IEnumerable<Blueprint> All { get; private set; }

            public double GetOutputPerSecondForDefaultBlueprint(ItemType singleOutput)
            {
                List<Blueprint> blueprints;
                if (!blueprintsByOutputType.TryGetValue(singleOutput, out blueprints)) return 0;

                double perSecond = 0;
                var ignoreMultiOutput = false;
                var iterator = blueprints.GetEnumerator();
                while (iterator.MoveNext())
                {
                    var hasMultiOutput = iterator.Current.Outputs.Length > 1;
                    // Once we have a single-output blueprint, ignore all subsequent multi-output blueprints.
                    if (ignoreMultiOutput && hasMultiOutput) continue;

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
            private readonly IDictionary<ItemType, double> oreConsumedPerSecond;
            private readonly HashSet<ItemType> ores;

            public OreTypes(ItemType[] ores, Blueprint[] blueprints)
            {
                this.ores = new HashSet<ItemType>(ores);
                oreConsumedPerSecond = System.Linq.Enumerable.ToDictionary(
                    System.Linq.Enumerable.GroupBy(blueprints, SelectOreType, SelectOreConsumedPerSecond),
                    SelectGroupKey,
                    System.Linq.Enumerable.Max);
            }

            public double GetSecondsToClear(IMyInventoryItem item)
            {
                var type = new ItemType(item.Content.TypeId.ToString(), item.Content.SubtypeId.ToString());
                double perSecond;
                if (!oreConsumedPerSecond.TryGetValue(type, out perSecond)) return 0f;
                return ((double)item.Amount) / perSecond;
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

            public ICollection<IngotType> All
            {
                get { return ingotTypes; }
            }
            
            public IEnumerable<ItemType> AllItemTypes
            {
                get { return System.Linq.Enumerable.Select(ingotTypes, SelectIngotTypeItemType); }
            }
        }

        public struct RefineryFactory
        {
            private readonly Dictionary<string, RefineryType> refineryTypesByBlockDefinitionString;
            public RefineryFactory(RefineryType[] refineryTypes)
            {
                refineryTypesByBlockDefinitionString = System.Linq.Enumerable.ToDictionary(refineryTypes, SelectBlockDefinitionString);
            }

            public ICollection<RefineryType> AllTypes
            {
                get { return refineryTypesByBlockDefinitionString.Values; }
            }

            public Refinery TryResolveRefinery(IMyRefinery block, double refinerySpeedFactor)
            {
                RefineryType type;
                if (refineryTypesByBlockDefinitionString.TryGetValue(block.BlockDefinition.ToString(), out type))
                {
                    return Refinery.Get(block, type, refinerySpeedFactor);
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

        /****************************************** MEMORY ALLOCATION TUNING ***************************************/

        private const int ALLOC_REFINERY_COUNT = 20;
        private const int ALLOC_ORE_TYPE_COUNT = 10;
        private const int ALLOC_INVENTORY_OWNER_COUNT = 10;
        private const int ALLOC_ORE_DONOR_COUNT = 10;


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
            new RefineryType("LargeRefinery") {
                SupportedBlueprints = { "StoneOreToIngot", "IronOreToIngot", "ScrapToIronIngot", "NickelOreToIngot", "CobaltOreToIngot",
                    "MagnesiumOreToIngot", "SiliconOreToIngot", "SilverOreToIngot", "GoldOreToIngot", "PlatinumOreToIngot", "UraniumOreToIngot" },
                Efficiency = 0.8, Speed = 1.3
            },
            new RefineryType("Blast Furnace") {
                SupportedBlueprints = { "IronOreToIngot", "ScrapToIronIngot", "NickelOreToIngot", "CobaltOreToIngot" },
                Efficiency = 0.9, Speed = 1.6
            },
            new RefineryType("Big Arc Furnace") {
                SupportedBlueprints = { "IronOreToIngot", "ScrapToIronIngot", "NickelOreToIngot", "CobaltOreToIngot" },
                Efficiency = 0.9, Speed = 16.8
            },
            new RefineryType("BigPreciousFurnace") {
                SupportedBlueprints = { "PlatinumOreToIngot", "SilverOreToIngot", "GoldOreToIngot" },
                Efficiency = 0.9, Speed = 16.1
            },
            new RefineryType("BigSolidsFurnace") {
                SupportedBlueprints = { "StoneOreToIngot", "MagnesiumOreToIngot", "SiliconOreToIngot" },
                Efficiency = 0.8, Speed = 16.2
            },
            new RefineryType("BigGasCentrifugalRefinery") {
                SupportedBlueprints = { "UraniumOreToIngot" },
                Efficiency = 0.95, Speed = 16
            }
        };

    }
}
