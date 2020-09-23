using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class ResourceMonitorDriver
    {
        public ResourceMonitorDriver(RequestedConfiguration configuration)
        {
            this.configuration = configuration;
            blockRuleFilter = new BlockFilterFactory().CreateRuleFilter(configuration.BlockRules);
        }

        private readonly List<DisplayRenderer> displays = new List<DisplayRenderer>(Constants.ALLOC_DISPLAY_COUNT);
        private readonly List<IMyTerminalBlock> scanBlocks = new List<IMyTerminalBlock>(Constants.ALLOC_SCAN_BLOCK_COUNT);
        private readonly RequestedConfiguration configuration;
        private readonly BlockFilterFactory.BlockRuleFilter blockRuleFilter;

        public static void StaticInitialise()
        {
        }

        public void RescanAll()
        {
            rescanBlocks = true;
        }

        private bool rescanBlocks = true;

        public IEnumerator<object> Run(TimeSpan timeSinceLastRun, IMyGridTerminalSystem gts)
        {
            Debug.Write(Debug.Level.Debug, new Message("Begin: {0}", Datestamp.Seconds));

            // Collect blocks:
            foreach (var yieldPoint in ScanBlocksIfNecessary(gts))
            {
                yield return yieldPoint;
            }
            Debug.Write(Debug.Level.Info, new Message("Using {0} display groups", displays.Count));
            Debug.Write(Debug.Level.Info, new Message("Scanning {0} blocks", scanBlocks.Count));

            yield return null;

            var i = 0;
            foreach (var display in displays)
            {
                var collectors = display.BeginDraw();
                foreach (var collector in collectors)
                {
                    foreach (var block in scanBlocks)
                    {
                        i++;
                        collector.Visit(block);
                        // Inventory scanning is probably not very intensive, but we still need to yield
                        // periodically, and the main loop is yielding to the game every X (default 20)
                        // of our yields...
                        if (i % 3 == 0) yield return null;
                    }
                }
                display.EndDraw();
            }

            Debug.Write(Debug.Level.Debug, new Message("End: {0}", Datestamp.Seconds));
        }

        private IEnumerable<object> ScanBlocksIfNecessary(IMyGridTerminalSystem gts)
        {
            if (rescanBlocks)
            {
                Debug.Write(Debug.Level.Debug, "Scanning blocks on grid...");
                // get stuff
                displays.Clear();
                scanBlocks.Clear();

                var displayBlocks = new List<IMyTextPanel>(10);
                var displayRendererFactory = new DisplayRendererFactory();
                foreach (var displayConfiguration in configuration.Displays)
                {
                    gts.GetBlocksOfType(displayBlocks, b => b.CustomName == displayConfiguration.DisplayName);
                    if (!displayBlocks.Any())
                    {
                        Debug.Write(Debug.Level.Warning, new Message("Display does not exist: {0}", displayConfiguration.DisplayName));
                        continue;
                    }
                    if (!displayBlocks.Any(d => d.IsOperational()))
                    {
                        Debug.Write(Debug.Level.Info, new Message("Display is not operational: {0}", displayConfiguration.DisplayName));
                        continue;
                    }
                    displays.Add(displayRendererFactory.Create(displayBlocks, displayConfiguration));
                }

                gts.GetBlocksOfType(scanBlocks, blockRuleFilter.Filter);

                var i = 0;
                var j = 0;
                var displayFilter = DisplayRenderer.GetBlockFilter(displays);
                while (j < scanBlocks.Count)
                {
                    i++;
                    if (!displayFilter.Filter(scanBlocks[j]))
                    {
                        scanBlocks.RemoveAtFast(j);
                    }
                    else
                    {
                        j++;
                    }
                    // Block filtering is probably not very intensive, but we still need to yield
                    // periodically, and the main loop is yielding to the game every X (default 20)
                    // of our yields...
                    if (i % 3 == 0) yield return null;
                }
                rescanBlocks = false;
            }
        }
    }
}
