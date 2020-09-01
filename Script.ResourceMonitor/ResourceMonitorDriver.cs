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
            blockFilter = new BlockFilterFactory().CreateFilter(configuration.BlockRules);
        }

        private readonly List<DisplayRenderer> displays = new List<DisplayRenderer>(Constants.ALLOC_DISPLAY_COUNT);
        private readonly List<IMyTerminalBlock> scanBlocks = new List<IMyTerminalBlock>(Constants.ALLOC_SCAN_BLOCK_COUNT);
        private readonly RequestedConfiguration configuration;
        private readonly Func<IMyTerminalBlock, bool> blockFilter;

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
            Debug.Write(Debug.Level.Debug, "Begin: {0}", DateTime.Now);

            // Collect blocks:
            ScanBlocksIfNecessary(gts);
            Debug.Write(Debug.Level.Info, "Using {0} display groups", displays.Count);
            Debug.Write(Debug.Level.Info, "Scanning {0} blocks", scanBlocks.Count);

            yield return null;

            foreach (var display in displays)
            {
                var collectors = display.BeginDraw();
                foreach (var collector in collectors)
                {
                    foreach (var block in scanBlocks)
                    {
                        collector.Visit(block);
                    }
                    // One display 'part' per action.
                    yield return null;
                }
                display.EndDraw();
            }

            Debug.Write(Debug.Level.Debug, "End: {0}", DateTime.Now);
        }

        private void ScanBlocksIfNecessary(IMyGridTerminalSystem gts)
        {
            if (rescanBlocks)
            {
                Debug.Write(Debug.Level.Debug, "Scanning blocks on grid...");
                // get stuff
                displays.Clear();
                scanBlocks.Clear();
                gts.GetBlocksOfType(scanBlocks, blockFilter);

                var displayBlocks = new List<IMyTextPanel>(10);
                var displayRendererFactory = new DisplayRendererFactory();
                foreach (var displayConfiguration in configuration.Displays)
                {
                    gts.GetBlocksOfType(displayBlocks, b => b.CustomName == displayConfiguration.DisplayName);
                    if (!displayBlocks.Any())
                    {
                        Debug.Write(Debug.Level.Warning, "Display does not exist: {0}", displayConfiguration.DisplayName);
                        continue;
                    }
                    if (!displayBlocks.Any(d => d.IsOperational()))
                    {
                        Debug.Write(Debug.Level.Info, "Display is not operational: {0}", displayConfiguration.DisplayName);
                        continue;
                    }
                    displays.Add(displayRendererFactory.Create(displayBlocks, displayConfiguration));
                }

                rescanBlocks = false;
            }
        }
    }
}
