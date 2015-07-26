using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;

namespace Script.RefineryBalance
{
    public partial class Program
    {
        /*
         * 'Lean' Refinery Driver Script v1.6
         * Alex Davidson, 26/07/2015.
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
         *   * Names of containers holding ore (default: 'Unrefined Ore')
         *   * Names of containers holding ingots (default: 'Raw Materials')
         *   * Refineries set to NOT use the conveyor system (any that are using it will be ignored)
         *   * Regular runs. Every ten seconds or so is good.
         *   * Knowledge of the game mode's refinery and assembler speed factors.
         *   
         * Most of this stuff is configurable using the properties of the 'Configuration' struct below.
         * 
         * I suggest using a timer block because this script relies on running regularly.
         * You can use it in high-precision mode or legacy mode:
         *   * Legacy mode just involves setting up a timer block to run the script every ten seconds or
         *     so. Sometimes the timer blocks lag a bit and the script might be delayed, causing refineries
         *     to run dry for a moment.
         *   * High-precision mode requires setting a timer block to run the script *and* trigger itself
         *     immediately. This will cause the script to run on every update (60 times a second) and allow
         *     it to hit its target interval more reliably.
         * Ore distribution and other heavy computation will not happen on every call when running in
         * high-precision mode. The script attempts to determine which mode is intended based on the
         * time since it was last called.
         * Modify TargetIntervalSeconds to make it run less (or more) often, but it may be CPU-intensive
         * in some configurations so don't lower that value too much.
         * 
         * Changelog, v1.6
         * ----------------
         *   * SE v1.092 introduces a breaking API change. The Blocks property is no longer available.
         * 
         * Changelog, v1.5x
         * ----------------
         *   * HOTFIX, v1.52: Correctly apply world modifier for Assembler efficiency.
         *   * HOTFIX, v1.52: Ignore incomplete or unpowered Refineries.
         *   * HOTFIX, v1.52: Account for Assembler upgrade modules.
         *   * HOTFIX, v1.51: Reimplemented MinimumStockpileOverride, which went missing in v1.4.
         *   * Fixed a bug in scoring of blueprints which caused Iron to be prioritised over just about
         *     everything, due to its relatively rapid rate of production.
         *   * The above fix required a small internal API change. IngotType now demands a normalisation
         *     factor for production rate. This usually comes from the 'default' blueprint for that ingot.
         *   * KNOWN BUG: Material Efficiency (Effectiveness) upgrades are not properly accounted for
         *     when dealing with high-efficiency materials like Iron and Stone. The game caps their
         *     conversion ratio at 1:1 but the script does not, causing it to overestimate the quantities
         *     produced in each cycle. Impact: believed to be low since Iron has a high target quota.
         * 
         * Changelog, v1.4
         * ---------------
         *   * Allow an arbitrary number of possible Refinery types.
         *   * Add support for v1.086 new Refinery types.
         *   * Refactor the algorithm to work with blueprints and item types, rather than assuming a
         *     one-to-one mapping from ore type to ingot type. Adds direct support for refining scrap,
         *     and allows modded materials to be handled properly. Note that handling of multiple input
         *     types is still not supported because I don't know how refineries would even handle that.
         * 
         * Changelog, v1.3
         * ---------------
         *   * Fix detection of Arc Furnaces in non-English locales. Status screen is still English-only.
         *   * Add changelog to script docs.
         *   
         * Changelog, v1.2
         * ---------------
         *   * Added support for SE v1.081 refinery upgrade modules. Material efficiency and production
         *     speed upgrades should now be accounted for. 
         *   * Added a 'high precision mode' for the script to try to fix timer lag. This is enabled
         *     simply by running the script often enough, eg. using a timer in a 'Trigger Now' loop. I
         *     have tried to ensure that the script only does minimal computation on each call until it's
         *     time to update the refineries again so this shouldn't cause a performance problem. 
         *   * Script is backwards-compatible with setups which trigger it on eg. a ten-second delay,
         *     but no longer needs to be told which timer block is running it. 
         *   * Default debug panel is now 'Debug:LeanRefinery' rather than 'Debug'. 
         *   * Added documentation and license to the top of the script.
         *  
         * Changelog, v1.1
         * ---------------
         *   * Fixed Uranium material definition to track Uranium ingots instead of Platinum ingots. 
         *   * Fixed a bug in prioritisation which caused the script to only sort ingot types once, then
         *     cycle through them. 
         *   * Made it simpler to enable Uranium management. 
         *   * Reduced Iron bias to match the requirements of the second-most-demanding blueprint (Medical
         *     Components, 80 ingots/sec) because the highest (Gravity Generator Components, 600 ingots/sec)
         *     is MUCH higher and probably is not produced as often as steel plate (22 ingots/sec). 
         *   * Added a configuration option for a status display so you can see the quota percentages of
         *     each ingot type, with regard to number of assemblers and maximum consumption rate. This
         *     defaults to the debug screen (block name: Debug).
         *     
         * v1.0, initial release.
         */
        public struct Configuration
        {
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
            /// <summary>
            /// Whether or not this script will attempt to manage Uranium stocks too.
            /// Defaults to false.
            /// </summary>
            public bool EnableUraniumManagement { get { return false; } }
            /// <summary>
            /// Aim to wait only this long between each run.
            /// </summary>
            public int TargetIntervalSeconds { get { return 10; } }

            /// <summary>
            /// Names of containers used to store unrefined ore available for processing.
            /// </summary>
            public string[] OreStorageContainerNames { get { return new[] { "Unrefined Ore" }; } }
            /// <summary>
            /// Names of containers used to store ingots available for manufacturing.
            /// </summary>
            public string[] IngotStorageContainerNames { get { return new[] { "Raw Materials" }; } }
            /// <summary>
            /// Name of text or LCD panel to use to display ingot quotas.
            /// Defaults to the debug screen.
            /// </summary>
            public string StatusDisplayName { get { return DEBUG_SCREEN; } }

            /// <summary>
            /// Maximum expected timer lag. Timer blocks do not fire exactly on time and may be
            /// delayed by a small amount.
            /// Commonly-observed values fall between 0.5 and 1 second, but may vary with update rate.
            /// I have seen it exceed five seconds before.
            /// </summary>
            public float MaxTimerLagSeconds { get { return 2f; } }
            /// <summary>
            /// Duration of extra work to allocate to refineries on each iteration.
            /// </summary>
            /// <remarks>
            /// If this is too low, refineries may occasionally run out of work when the timer is
            /// delayed more than normal.
            /// If this is too high, it may take a while for the script to adjust to changes in ingot
            /// consumption. A couple of seconds seems to be about right.
            /// </remarks>
            public float IntervalOverlapSeconds { get { return 2f; } }
            /// <summary>
            /// Maximum interval to assume when running in legacy mode.
            /// If the game is paused for a long period of time the script may assume that's the period
            /// of the timer and allocate that much work to the refineries. This will delay adjustment
            /// if the assemblers' needs change in the time it takes for that backlog to clear.
            /// The default is two minutes. If you are using legacy mode and your timer interval is larger
            /// than this, increase this value accordingly.
            /// </summary>
            public int MaxIntervalSeconds { get { return 120; } }
        }

        public const string DEBUG_SCREEN = "Debug:LeanRefinery";

        /// <summary>
        /// Define all compute-once constants used by the script.
        /// </summary>
        /// <returns></returns>
        Everything DefineEverything(Configuration config)
        {
            var ingots = System.Linq.Enumerable.ToList(new[] {
                new IngotType("Ingot/Cobalt", 220f, 0.075f), 
                new IngotType("Ingot/Gold", 5f, 0.025f),
                // TWEAK: Reduced Iron bias.
                // Highest consumption rate: 600 ingots/sec (gravity generator components).
                // Next highest: 80 ingots/sec (medical components).
                // If you produce a lot of gravity generator components, consider increasing this.
                new IngotType("Ingot/Iron", 80f, 14f),
                new IngotType("Ingot/Nickel", 70f, 0.2f),
                new IngotType("Ingot/Magnesium", 0.35f, 0.007f),
                new IngotType("Ingot/Silicon", 15f, 1.1667f),
                new IngotType("Ingot/Silver", 10f, 0.1f),
                new IngotType("Ingot/Platinum", 0.4f, 0.0013f)
            });
            var blueprints = System.Linq.Enumerable.ToList(new[] { 
                // Default blueprints for refining ore to ingots:
                new Blueprint("CobaltOreToIngot", new ItemAndQuantity("Ore/Cobalt", 0.25f), new ItemAndQuantity("Ingot/Cobalt", 0.075f)),
                new Blueprint("GoldOreToIngot", new ItemAndQuantity("Ore/Gold", 2.5f), new ItemAndQuantity("Ingot/Gold", 0.025f)),
                new Blueprint("IronOreToIngot", new ItemAndQuantity("Ore/Iron", 20f), new ItemAndQuantity("Ingot/Iron", 14f)),
                new Blueprint("NickelOreToIngot", new ItemAndQuantity("Ore/Nickel", 0.5f), new ItemAndQuantity("Ingot/Nickel", 0.2f)),
                new Blueprint("MagnesiumOreToIngot", new ItemAndQuantity("Ore/Magnesium", 1f), new ItemAndQuantity("Ingot/Magnesium", 0.007f)),
                new Blueprint("SiliconOreToIngot", new ItemAndQuantity("Ore/Silicon", 1.6667f), new ItemAndQuantity("Ingot/Silicon", 1.1667f)),
                new Blueprint("SilverOreToIngot", new ItemAndQuantity("Ore/Silver", 1f), new ItemAndQuantity("Ingot/Silver", 0.1f)),
                new Blueprint("PlatinumOreToIngot", new ItemAndQuantity("Ore/Platinum", 0.25f), new ItemAndQuantity("Ingot/Platinum", 0.0013f)),
                new Blueprint("ScrapToIronIngot", new ItemAndQuantity("Ore/Scrap", 25f), new ItemAndQuantity("Ingot/Iron", 20f))
            });
            if(config.EnableUraniumManagement)
            {
                ingots.Add(new IngotType("Ingot/Uranium", 0.01f, 0.0018f) {
                    // Increase this to maintain a uranium stockpile:
                    MinimumStockpileOverride = 0f
                });
                blueprints.Add(new Blueprint("UraniumOreToIngot", new ItemAndQuantity("Ore/Uranium", 0.25f), new ItemAndQuantity("Ingot/Uranium", 0.0018f)));
            };

            var refineryTypes = System.Linq.Enumerable.ToList(new[] { 
                new RefineryType("LargeRefinery")
                {
                    SupportedBlueprints = { "StoneOreToIngot", "IronOreToIngot", "ScrapToIronIngot", "NickelOreToIngot", "CobaltOreToIngot",
                        "MagnesiumOreToIngot", "SiliconOreToIngot", "SilverOreToIngot", "GoldOreToIngot", "PlatinumOreToIngot", "UraniumOreToIngot" },
                    Efficiency = 0.8f,
                    Speed = 1.3f
                },
                new RefineryType("Blast Furnace")
                {
                    SupportedBlueprints = { "IronOreToIngot", "ScrapToIronIngot", "NickelOreToIngot", "CobaltOreToIngot" },
                    Efficiency = 0.9f,
                    Speed = 1.6f
                },
                new RefineryType("Big Arc Furnace")
                {
                    SupportedBlueprints = { "IronOreToIngot", "ScrapToIronIngot", "NickelOreToIngot", "CobaltOreToIngot" },
                    Efficiency = 0.9f,
                    Speed = 16.8f
                },
                new RefineryType("BigPreciousFurnace")
                {
                    SupportedBlueprints = { "PlatinumOreToIngot", "SilverOreToIngot", "GoldOreToIngot" },
                    Efficiency = 0.9f,
                    Speed = 16.1f
                },
                new RefineryType("BigSolidsFurnace")
                {
                    SupportedBlueprints = { "StoneOreToIngot", "MagnesiumOreToIngot", "SiliconOreToIngot" },
                    Efficiency = 0.8f,
                    Speed = 16.2f
                },
                new RefineryType("BigGasCentrifugalRefinery")
                {
                    SupportedBlueprints = { "UraniumOreToIngot" },
                    Efficiency = 0.95f,
                    Speed = 16f
                }
            });

            var ores = System.Linq.Enumerable.ToList(
                System.Linq.Enumerable.Distinct(
                    System.Linq.Enumerable.Select(blueprints, SelectInputItemType)));

            DetectDuplicates(blueprints, SelectBlueprintName, "Duplicate blueprint name: {0}");

            return new Everything(ores, ingots, blueprints, refineryTypes);
        }
        
        private Everything precomputed;
        public static Configuration configuration = new Configuration();

        // Recomputed on each run: 
        private System.Linq.ILookup<string, IMyCargoContainer> containers;
        private float totalAssemblerSpeed;
        private IList<Refinery> refineriesNeedingWork;
        private IDictionary<ItemType, IList<OreDonor>> ore;
        private IDictionary<ItemType, float> ingots;
        private float interval;

        private IMyTextPanel debugScreen;
        private long lastAllocation;
        private long lastCall;
        
        public void InitDebug()
        {
            var needsClearing = debugScreen == null;
            debugScreen = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(DEBUG_SCREEN);
            if (debugScreen == null) return;
            if (needsClearing) ClearDebug();
        }

        public void ClearDebug()
        {
            if (debugScreen == null) return;
            debugScreen.WritePublicText(String.Format("{0:dd MMM HH:mm}\n", DateTime.Now));
        }

        public void WriteDebug(string message, params object[] args)
        {
            if (debugScreen == null) return;
            debugScreen.WritePublicText(String.Format(message, args), true);
            debugScreen.WritePublicText("\n", true);
        }

        // If we're called more frequently than every 3 seconds, assume that the timer is in high-precision mode.
        private static readonly long HighPrecisionThreshold = TimeSpan.FromMilliseconds(3000).Ticks;
        // Optimisation. We never need to run more often than once in 100ms.
        private static readonly long HighPrecisionBailout = TimeSpan.FromMilliseconds(100).Ticks;

        void Main()
        {
            var now = DateTime.Now.Ticks;
            var ticksSinceLastCall = now - lastCall;
            if (ticksSinceLastCall < HighPrecisionBailout) return;
            lastCall = now;
            if (ticksSinceLastCall < HighPrecisionThreshold)
            {
                // High-precision mode. Assume that the timer is calling us regularly enough that we can
                // hit the target to a reasonable degree of accuracy.
                if ((now - lastAllocation) / TimeSpan.TicksPerSecond < configuration.TargetIntervalSeconds) return;

                interval = configuration.TargetIntervalSeconds;
                ClearDebug();
                WriteDebug("High-precision mode. Target interval: {0}s", configuration.TargetIntervalSeconds);
            }
            else
            {
                // Low-precision mode? We're probably being called every X seconds by a timer block. That can
                // be expected to vary.
                if(lastAllocation == 0) 
                {
                    // First run. Assume TargetIntervalSeconds between calls. This will sort itself out
                    // next time around.
                    interval = configuration.TargetIntervalSeconds + configuration.MaxTimerLagSeconds;
                }
                else
                {
                    var actualSecondsSinceLastAllocation = (float)(now - lastAllocation) / TimeSpan.TicksPerSecond;
                    // If the game is paused for half an hour, don't allocate half an hour of work to the refineries.
                    var accountForDelaysDueToPausing = Math.Min(actualSecondsSinceLastAllocation, configuration.MaxIntervalSeconds);
                    // If the game lags briefly the threshold might be hit even in HP mode. Don't assume an 
                    // interval shorter than that configured. Note that this takes precedence over MaxIntervalSeconds.
                    var accountForGameLagInHighPrecisionMode = Math.Max(accountForDelaysDueToPausing, configuration.TargetIntervalSeconds);
                    // Add a bit to account for timer lag variation.
                    interval = accountForGameLagInHighPrecisionMode + configuration.MaxTimerLagSeconds;
                }
                ClearDebug();
                WriteDebug("Legacy mode. Assumed interval: {0}s", interval);
            }

            if (!precomputed.IsInitialised)
            {
                precomputed = DefineEverything(configuration); // One-off precomputation. 
                // Return immediately after precomputing, since we don't know how long it took
                // and attempting to allocate resources too might overrun our instruction quota.
                return;
            }
            InitDebug();
            
            WriteDebug("Time since last allocation: {0:0.#}ms", (now - lastAllocation) / TimeSpan.TicksPerMillisecond);
            lastAllocation = now;

            RunAllocation();
        }

        /// <summary>
        /// Analyses ingot stockpiles and allocates work to refineries.
        /// </summary>
        private void RunAllocation()
        {
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
            totalAssemblerSpeed = GetTotalParticipatingAssemblerSpeed();

            ore = CollectOreStorage(
                System.Linq.Enumerable.ToList(
                    System.Linq.Enumerable.Select(
                        System.Linq.Enumerable.SelectMany(configuration.OreStorageContainerNames, SelectCargoContainersFromName),
                        SelectCargoContainerInventory)));
            ingots = CollectIngotAmounts(
                System.Linq.Enumerable.ToList(
                    System.Linq.Enumerable.SelectMany(configuration.IngotStorageContainerNames, SelectCargoContainersFromName)));

            var allIngotStockpiles =
                System.Linq.Enumerable.ToList(
                    System.Linq.Enumerable.Select(precomputed.Ingots, AnalyseStockpile));

            LogToStatusDisplay(allIngotStockpiles);

            // Don't bother considering materials for which we have no ore: 
            var availableBlueprints =
                System.Linq.Enumerable.ToList(
                    System.Linq.Enumerable.Where(precomputed.Blueprints, IsOreAvailable));

            WriteDebug("{0} refineries needing work, {1} ore types available", refineriesNeedingWork.Count, ore.Count);

            // Attempt to queue enough ore to keep refineries busy until we run again.
            // Prioritise materials which are 'closest' to running out and adjust those estimates
            // as work is assigned.

            var stockpiles = new Stockpiles(allIngotStockpiles, availableBlueprints);
            var iterator = refineriesNeedingWork.GetEnumerator();
            while (iterator.MoveNext())
            {
                FillRefinery(iterator.Current, stockpiles);
            }
            // Log stockpile estimates:
            for (var i = 0; i < allIngotStockpiles.Count; i++)
            {
                var item = allIngotStockpiles[i];
                WriteDebug("{0}:  {3:#000%}   {1:0.##} / {2:0.##} {4}", item.Ingot.ItemType.SubtypeId, item.EstimatedQuantity, item.TargetQuantity, item.QuotaFraction, item.QuotaFraction < 1 ? "(!)" : "");
            }
            WriteDebug("Total runtime this iteration: {0:0.#}ms", (DateTime.Now.Ticks - lastAllocation) / TimeSpan.TicksPerMillisecond);
        }

        private void LogToStatusDisplay(IList<IngotStockpile> allIngotStockpiles)
        {
            if (String.IsNullOrEmpty(configuration.StatusDisplayName)) return;
            var statusScreen = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(configuration.StatusDisplayName);
            if (statusScreen == null) return;
            if (statusScreen != debugScreen)
            {
                // Clear previous state.
                statusScreen.WritePublicText(String.Format("Ingot stockpiles  {0:dd MMM HH:mm}\n", DateTime.Now));
            }
            // Log stockpiles:
            for (var i = 0; i < allIngotStockpiles.Count; i++)
            {
                var item = allIngotStockpiles[i];
                statusScreen.WritePublicText(
                    String.Format("{0}:  {3:#000%}   {1:0.##} / {2:0.##} {4}\n", item.Ingot.ItemType.SubtypeId, item.CurrentQuantity, item.TargetQuantity, item.QuotaFraction, item.QuotaFraction < 1 ? "(!)" : ""),
                    true);
            }

        }

        // Lambdas

        public static ItemType SelectOreType(Blueprint blueprint) { return blueprint.Input.ItemType; }
        public static float SelectOreConsumedPerSecond(Blueprint blueprint) { return blueprint.Input.QuantityPerSecond; }
        public string SelectCustomName(IMyTerminalBlock block) { return block.CustomName; }
        public bool NeedsWork(Refinery refinery) { return GetSecondsToClear(refinery) < interval; }
        public float SelectIngotProductionRate(Refinery refinery) { return refinery.IngotProductionRate; }
        public bool IsOreAvailable(Blueprint blueprint) { return ore.ContainsKey(blueprint.Input.ItemType); }
        public IEnumerable<IMyCargoContainer> SelectCargoContainersFromName(string containerName) { return containers[containerName]; }
        public static IMyInventory SelectCargoContainerInventory(IMyCargoContainer container) { return container.GetInventory(0); }
        public static ItemType SelectIngotItemType(IngotStockpile stockpile) { return stockpile.Ingot.ItemType; }
        public static string SelectBlockDefinitionString(RefineryType type) { return type.BlockDefinitionName; }
        public static ItemType SelectInputItemType(Blueprint blueprint) { return blueprint.Input.ItemType; }
        public static string SelectBlueprintName(Blueprint blueprint) { return blueprint.Name; }
        public static TKey SelectGroupKey<TKey, TElement>(System.Linq.IGrouping<TKey, TElement> group) { return group.Key; }
        public static bool IsCountMoreThanOne<T>(IEnumerable<T> set) { return System.Linq.Enumerable.Count(set) > 1; }

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

            // Get candidate blueprints in priority order.
            var candidates = stockpiles.GetCandidateBlueprints(refinery);

            for (var i = 0; i < candidates.Count; i++)
            {
                var blueprint = candidates[i];

                var workProvidedSeconds = TryFillRefinery(refinery, blueprint, workRequiredSeconds);
                if (workProvidedSeconds <= 0)
                {
                    WriteDebug("Unable to allocate any {0}, moving to next candidate.", blueprint.Input.ItemType.SubtypeId);
                    continue;
                }

                workRequiredSeconds -= workProvidedSeconds;
                var workTowardsDeadline = Math.Min(assemblerDeadlineSeconds, workProvidedSeconds);
                if (workTowardsDeadline > 0)
                {
                    // Some of the new work will be processed before next iteration. Update our estimates.
                    stockpiles.UpdateStockpileEstimates(refinery, blueprint, workTowardsDeadline);
                }
                assemblerDeadlineSeconds -= workTowardsDeadline;

                if (workRequiredSeconds <= 0)
                {
                    // Refinery's work target is satisfied. It should not run dry before we run again.
                    return;
                }
                // WriteDebug("Allocated {0}s/{1}s of {2}.", workProvidedSeconds, origWorkRequired, stockpile.Material.OreType.SubtypeId);
            }

            // No more ore available for this refinery.
            // WriteDebug("{0}: No more work for this refinery.", refinery.RefineryName);
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
            var oreRate = refinery.OreConsumptionRate * blueprint.Input.QuantityPerSecond;
            var oreQuantityRequired = oreRate * workRequiredSeconds;

            var sources = ore[blueprint.Input.ItemType];

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
        private IngotStockpile AnalyseStockpile(IngotType ingotType)
        {
            var maxConsumption = ingotType.ConsumedPerSecond * interval * totalAssemblerSpeed * configuration.AssemblerEfficiencyFactor;
            return new IngotStockpile
            {
                Ingot = ingotType,
                CurrentQuantity = ingots.ContainsKey(ingotType.ItemType) ? ingots[ingotType.ItemType] : 0,
                TargetQuantity = Math.Max(maxConsumption, ingotType.MinimumStockpileOverride)
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
        private float GetTotalParticipatingAssemblerSpeed()
        {
            // ASSUMPTION: Only assembler type in the game has a base speed of 1.
            const float assemblerSpeed = 1;

            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(blocks);
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

        private bool IsBlockOperational(IMyFunctionalBlock block)
        {
            return block.Enabled && block.IsFunctional && block.IsWorking;
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
                var block = (IMyRefinery)blocks[i];
                if (!IsBlockOperational(block)) continue;
                if (block.UseConveyorSystem) continue;

                var refinery = precomputed.TryResolveRefinery(block);
                if (refinery.HasValue)
                {
                    result.Add(refinery.Value);
                }
                else
                {
                    WriteDebug("Unrecognised refinery type: {0}", block.BlockDefinition);
                }
            }
            return result;
        }

        /// <summary>
        /// Find all cargo containers on-grid and group them by name for efficient lookup.
        /// </summary>
        private System.Linq.ILookup<string, IMyCargoContainer> AllCargoContainersByName() 
        {
            var list = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(list);
            return System.Linq.Enumerable.ToLookup(
                System.Linq.Enumerable.OfType<IMyCargoContainer>(list),
                SelectCustomName);
        }


        /************************************************** DATA STRUCTURES ***********************************************/

        /// <summary>
        /// Prioritises refinery work based on ingot quantities. 
        /// The ingot type which might run out fastest should be produced first.
        /// </summary>
        /// <remarks>
        /// Blueprints for which we have no ore should already have been filtered out.
        /// </remarks>
        public class Stockpiles
        {
            private readonly IEnumerable<Blueprint> blueprints;
            private Dictionary<ItemType, IngotStockpile> stockpiles;
            public Stockpiles(IEnumerable<IngotStockpile> stockpiles, IEnumerable<Blueprint> blueprints)
            {
                this.blueprints = blueprints;
                this.stockpiles = System.Linq.Enumerable.ToDictionary(stockpiles, SelectIngotItemType);
            }

            public bool HasAny { get { return stockpiles.Count > 0; } }

            // UNIT TESTING
            public IEnumerable<IngotStockpile> GetStockpiles()
            {
                return stockpiles.Values;
            }

            /// <summary> 
            /// Fetch blueprints for all stockpiles whose quotas the specified refinery can contribute towards, in
            /// descending priority order.
            /// </summary> 
            public IList<Blueprint> GetCandidateBlueprints(Refinery refinery)
            {
                return System.Linq.Enumerable.ToList(
                    System.Linq.Enumerable.OrderByDescending(
                        System.Linq.Enumerable.Where(blueprints, refinery.Supports),
                        ScoreBlueprint));
            }

            public float ScoreBlueprint(Blueprint blueprint)
            {
                float score = 0;
                for (var i = 0; i < blueprint.Outputs.Length; i++)
                {
                    var output = blueprint.Outputs[i];

                    IngotStockpile stockpile;
                    if (!stockpiles.TryGetValue(output.ItemType, out stockpile)) continue;
                    score += (output.QuantityPerSecond / stockpile.Ingot.ProductionNormalisationFactor) / stockpile.QuotaFraction;
                }
                return score;
            }

            public void UpdateStockpileEstimates(Refinery refinery, Blueprint blueprint, float workTowardsDeadline)
            {
                for (var i = 0; i < blueprint.Outputs.Length; i++)
                {
                    var output = blueprint.Outputs[i];

                    IngotStockpile stockpile;
                    if (!stockpiles.TryGetValue(output.ItemType, out stockpile)) continue;

                    stockpile.EstimatedProduction += workTowardsDeadline * refinery.IngotProductionRate * output.QuantityPerSecond;

                    stockpiles[stockpile.Ingot.ItemType] = stockpile;
                }
            }
        }
        
        /// <summary>
        /// Records current quantities and target quantities for a material.
        /// </summary>
        public struct IngotStockpile
        {
            public IngotType Ingot { get; set; }
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

        /// <summary>
        /// Wraps a Refinery block. Exposes processing rate information
        /// based on refinery type (Arc Furnace vs. Refinery).
        /// </summary>
        public struct Refinery
        {
            private readonly IMyRefinery block;
            private readonly ICollection<string> supportedBlueprints;
            private readonly float refineSpeed;
            private readonly float materialEfficiency;


            public Refinery(IMyRefinery block, RefineryType type)
                : this()
            {
                this.block = block;
                this.supportedBlueprints = type.SupportedBlueprints;
                refineSpeed = type.Speed;
                materialEfficiency = type.Efficiency;

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
            /// BUG: This assumes that there is no 'sanity cap' on the quantity of ingots produced from a unit of ore!
            /// Iron refines at 70% efficiency, but with the right refinery upgrades this can be 140%.
            /// The game caps this at 100% but this script doesn't currently have the info to do this.
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
            /// Skip ores that this refinery can't handle instead of iterating
            /// all their stacks.
            /// </summary>
            public bool Supports(Blueprint blueprint)
            {
                return supportedBlueprints.Contains(blueprint.Name);
            }

            /// <summary>
            /// Get the inventory of this refinery responsible for storing ore.
            /// </summary>
            public IMyInventory GetOreInventory()
            {
                return block.GetInventory(0);
            }
        }

        public struct ItemAndQuantity
        {
            public ItemAndQuantity(string typePath, float consumedPerSecond) : this()
            {
                ItemType = new ItemType(typePath);
                QuantityPerSecond = consumedPerSecond;
            }
            public ItemType ItemType { get; set; }
            /// <summary> 
            /// Units (kg) of the item consumed or produced per second by one refinery, on Realistic settings,
            /// assuming speed and efficiency of 1. 
            /// Calculated as blueprint input/output amount divided by processing time. 
            /// </summary> 
            public float QuantityPerSecond { get; set; }
        }

        public struct IngotType
        {
            public IngotType(string typePath, float consumedPerSecond, float productionNormalisationFactor)
                : this()
            {
                ItemType = new ItemType(typePath);
                ConsumedPerSecond = consumedPerSecond;
                ProductionNormalisationFactor = productionNormalisationFactor;
            }

            public ItemType ItemType { get; set; }
            /// <summary> 
            /// Maximum number of units (kg) of ingots consumed per second by a single assembler, on
            /// Realistic settings. 
            /// Calculated as the largest input amount of any supported assembler blueprint, divided by processing
            /// time. 
            /// </summary> 
            public float ConsumedPerSecond { get; set; }
            /// <summary> 
            /// Minimum stockpile of ingots to maintain, irrespective of consumption rate or number
            /// of assemblers.
            /// </summary> 
            public float MinimumStockpileOverride { get; set; }
            /// <summary>
            /// Estimated 'standard' production rate for blueprints producing this ingot.
            /// Used to normalise the QuantityPerSecond of a blueprint's outputs so that they can be
            /// compared sanely. Typically taken from the 'default' blueprint for a given ingot.
            /// </summary>
            public float ProductionNormalisationFactor { get; set; }
        }

        public struct Blueprint
        {
            public Blueprint(string name, ItemAndQuantity input, params ItemAndQuantity[] outputs) : this()
            {
                Name = name;
                Input = input;
                Outputs = outputs;
            }

            /// <summary>
            /// Name or identifier of this blueprint. Must be unique.
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// Type of ore consumed by this blueprint.
            /// </summary>
            public ItemAndQuantity Input { get; set; }
            /// <summary>
            /// Types of ingots produced by this blueprint.
            /// </summary>
            public ItemAndQuantity[] Outputs { get; set; }
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
        /// ASSUMPTION: An item's ItemId remains the same for that stack.
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
        /// Defines a class of Refinery block.
        /// </summary>
        public struct RefineryType
        {
            public string BlockDefinitionName { get; private set; }
            public ICollection<string> SupportedBlueprints { get; private set; }
            public float Efficiency { get; set; }
            public float Speed { get; set; }

            public RefineryType(string subTypeName) : this()
            {
                BlockDefinitionName = "MyObjectBuilder_Refinery/" + subTypeName;
                Efficiency = 1;
                Speed = 1;
                SupportedBlueprints = new HashSet<string>();
            }
        }

        /// <summary>
        /// Store some precomputed information, lookup tables, etc.
        /// </summary>
        public struct Everything
        {
            public bool IsInitialised { get; private set; }
            public IList<Blueprint> Blueprints { get; private set; }
            public IList<IngotType> Ingots { get; private set; }
            public ICollection<ItemType> Ores { get; private set; }

            private readonly IDictionary<ItemType, float> oreConsumedPerSecond;
            private Dictionary<string, RefineryType> refineryTypesByBlockDefinitionString;


            public float GetSecondsToClear(IMyInventoryItem item)
            {
                var type = new ItemType(item.Content.TypeId.ToString(), item.Content.SubtypeId.ToString());
                float perSecond;
                if (!oreConsumedPerSecond.TryGetValue(type, out perSecond)) return 0f;
                return ((float)item.Amount) / perSecond;
            }

            public Everything(IList<ItemType> ores, IList<IngotType> ingots, IList<Blueprint> blueprints, IEnumerable<RefineryType> refineryTypes)
                : this()
            {
                Blueprints = blueprints;
                Ingots = ingots;
                Ores = new HashSet<ItemType>(ores);

                refineryTypesByBlockDefinitionString = System.Linq.Enumerable.ToDictionary(refineryTypes, SelectBlockDefinitionString);
                oreConsumedPerSecond = System.Linq.Enumerable.ToDictionary(
                    System.Linq.Enumerable.GroupBy(blueprints, SelectOreType, SelectOreConsumedPerSecond),
                    SelectGroupKey,
                    System.Linq.Enumerable.Max);

                IsInitialised = true;
            }

            public Refinery? TryResolveRefinery(IMyRefinery block)
            {
                RefineryType type;
                if(refineryTypesByBlockDefinitionString.TryGetValue(block.BlockDefinition.ToString(), out type))
                {
                    return new Refinery(block, type);
                }
                return null;
            }
        }
    }
}
