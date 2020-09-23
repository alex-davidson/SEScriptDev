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

            yield return null;

            foreach (var display in displays)
            {
                Debug.Write(Debug.Level.Info, new Message("Display {0}: scanning {1} blocks", display.Name, display.BlockCount));
                foreach (var yieldPoint in display.Update())
                {
                    yield return yieldPoint;
                }
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

                foreach (var display in displays)
                {
                    foreach (var yieldPoint in display.RescanBlocks(scanBlocks))
                    {
                        yield return yieldPoint;
                    }
                }
                scanBlocks.Clear();
                rescanBlocks = false;
            }
        }
    }
}
