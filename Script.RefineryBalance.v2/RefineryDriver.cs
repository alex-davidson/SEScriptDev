using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public class RefineryDriver
    {
        public RefineryDriver(StaticState configuredStaticState)
        {
            state = new SystemState(configuredStaticState);
            blockCollector = new BlockCollector(configuredStaticState.InventoryBlockNames);
            inventoryScanner = new InventoryScanner(configuredStaticState.IngotTypes.AllIngotItemTypes, configuredStaticState.OreTypes.All);
            refineryWorklist = new RefineryWorklist(configuredStaticState.OreTypes, configuredStaticState.IngotTypes, configuredStaticState.RefineryFactory, configuredStaticState.Blueprints);
            ingotWorklist = new IngotWorklist(state.Ingots);
            displayRenderer = new DisplayRenderer();
        }

        private readonly SystemState state;
        private readonly BlockCollector blockCollector;
        private readonly InventoryScanner inventoryScanner;
        private readonly RefineryWorklist refineryWorklist;
        private readonly DisplayRenderer displayRenderer;
        private readonly IngotWorklist ingotWorklist;
        private float totalAssemblerSpeed;
        private readonly List<IMyEntity> inventoryOwners = new List<IMyEntity>(Constants.ALLOC_INVENTORY_OWNER_COUNT);
        private readonly List<Refinery> refineries = new List<Refinery>(Constants.ALLOC_REFINERY_COUNT);

        private bool rescanRefineries = true;
        private bool rescanAssemblers = true;
        private bool rescanContainers = true;
        private bool rescanResources = true;
        private int rescanResourcesCountdown;

        // Iterate until complete. Yields at each possible break point.
        public IEnumerator<object> Run(TimeSpan timeSinceLastRun, IMyGridTerminalSystem gts)
        {
            Debug.Write(Debug.Level.Debug, "Begin: {0}", DateTime.Now);

            // Collect blocks:
            ScanRefineriesIfNecessary(gts);
            Debug.Write(Debug.Level.Info, "Managing {0} refineries at global speed {1}x", refineries.Count, state.RefinerySpeedFactor);

            ScanAssemblersIfNecessary(gts);
            Debug.Write(Debug.Level.Info, "Total assembler speed: {0}", totalAssemblerSpeed);

            ScanContainersIfNecessary(gts);
            Debug.Write(Debug.Level.Info, "Using {0} containers", inventoryOwners.Count);

            yield return null;

            ScanResourcesIfNecessary(gts);

            displayRenderer.UpdateIngotDisplay(state.Ingots);
            displayRenderer.UpdateOreDisplay(inventoryScanner.Ore, state.Static.OreTypes, refineries);

            yield return null;

            refineryWorklist.Initialise(refineries);
            ingotWorklist.Initialise();

            var refineryWorkAllocator = new RefineryWorkAllocator(refineryWorklist, inventoryScanner.Ore);
            while (refineryWorkAllocator.AllocateSingle(ingotWorklist))
            {
                yield return null;
            }

            Debug.Write(Debug.Level.Debug, "End: {0}", DateTime.Now);
        }

        public void RescanAll()
        {
            rescanRefineries = true;
            rescanAssemblers = true;
            rescanContainers = true;
        }

        private void ScanRefineriesIfNecessary(IMyGridTerminalSystem gts)
        {
            if (rescanRefineries)
            {
                Debug.Write(Debug.Level.Debug, "Scanning for refineries...");
                refineries.Clear();
                Refinery.ReleaseAll();
                blockCollector.CollectParticipatingRefineries(state, gts, refineries);
                rescanRefineries = false;
            }
        }

        private void ScanAssemblersIfNecessary(IMyGridTerminalSystem gts)
        {
            if (rescanAssemblers)
            {
                Debug.Write(Debug.Level.Debug, "Scanning for assemblers...");
                var assemblerSpeedFactor = state.Static.AssemblerSpeedFactor ?? state.RefinerySpeedFactor;
                totalAssemblerSpeed = blockCollector.GetTotalParticipatingAssemblerSpeed(gts) * assemblerSpeedFactor;
                state.Ingots.UpdateAssemblerSpeed(totalAssemblerSpeed);
                rescanAssemblers = false;
            }
        }

        private void ScanContainersIfNecessary(IMyGridTerminalSystem gts)
        {
            if (rescanContainers)
            {
                Debug.Write(Debug.Level.Debug, "Scanning for containers...");
                inventoryOwners.Clear();
                blockCollector.CollectUniqueContainers(inventoryOwners, gts);
                rescanResources = true;
                rescanContainers = false;
            }
        }

        private void ScanResourcesIfNecessary(IMyGridTerminalSystem gts)
        {
            rescanResourcesCountdown--;
            if (rescanResourcesCountdown <= 0) rescanResources = true;

            if (rescanResources)
            {
                Debug.Write(Debug.Level.Debug, "Scanning container contents...");
                inventoryScanner.Reset();
                foreach (var inventoryOwner in inventoryOwners)
                {
                    inventoryScanner.Scan(inventoryOwner);
                }

                state.Ingots.UpdateQuantities(inventoryScanner.Ingots);

                rescanResources = false;
                rescanResourcesCountdown = Constants.RESOURCE_RESCAN_INTERVAL_UPDATES;

                displayRenderer.Rescan(state, gts);
            }
        }


        public static void StaticInitialise()
        {
            DetectDuplicates(Constants.BLUEPRINTS, b => b.Name, "Duplicate blueprint name: {0}");
        }

        private static void DetectDuplicates<TItem, TKey>(IEnumerable<TItem> items, Func<TItem, TKey> selectKey, string messageFormat)
        {
            var duplicates = items
                .GroupBy(selectKey)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicates.Count == 0) return;
            throw new Exception(string.Format(messageFormat, duplicates[0]));
        }
    }

}
