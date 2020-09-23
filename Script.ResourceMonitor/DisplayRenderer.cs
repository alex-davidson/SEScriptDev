using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public class DisplayRenderer
    {
        public string Name { get; }
        private readonly IGrouping<IMyCubeGrid, IMyTextPanel>[] displayGroups;
        private readonly IDisplayPart[] parts;
        private readonly List<IMyTerminalBlock> scanBlocks = new List<IMyTerminalBlock>(Constants.ALLOC_SCAN_BLOCK_COUNT);

        public DisplayRenderer(string name, IEnumerable<IMyTextPanel> displays, IDisplayPart[] parts)
        {
            Name = name;
            this.displayGroups = displays.GroupBy(d => d.CubeGrid).ToArray();
            this.parts = parts;
        }

        public IEnumerable<object> RescanBlocks(List<IMyTerminalBlock> candidateBlocks)
        {
            scanBlocks.Clear();
            for (var i = 0; i < candidateBlocks.Count; i++)
            {
                foreach (var part in parts)
                {
                    if (part.Filter(candidateBlocks[i]))
                    {
                        scanBlocks.Add(candidateBlocks[i]);
                    }
                }

                // Block filtering is probably not very intensive, but we still need to yield
                // periodically, and the main loop is yielding to the game every X (default 20)
                // of our yields...
                if (i % 3 == 0) yield return null;
            }
        }

        public int BlockCount => scanBlocks.Count;

        public IEnumerable<object> Update()
        {
            foreach (var part in parts) part.Clear();

            var i = 0;
            foreach (var block in scanBlocks)
            {
                i++;
                foreach (var part in parts) part.Visit(block);

                // Inventory scanning is probably not very intensive, but we still need to yield
                // periodically, and the main loop is yielding to the game every X (default 20)
                // of our yields...
                if (i % 3 == 0) yield return null;
            }
            yield return null;
            DrawDisplays();
        }

        private readonly StringBuilder local_EndDraw_stringBuilder = new StringBuilder(Constants.ALLOC_DISPLAY_BUFFER_SIZE);
        private void DrawDisplays()
        {
            foreach (var group in displayGroups)
            {
                local_EndDraw_stringBuilder.Clear();
                local_EndDraw_stringBuilder.AppendLine(Datestamp.Minutes.ToString());
                if (parts.Any())
                {
                    parts[0].Draw(group.Key, local_EndDraw_stringBuilder);
                    for (var i = 1; i < parts.Length; i++)
                    {
                        local_EndDraw_stringBuilder.AppendLine();
                        parts[i].Draw(group.Key, local_EndDraw_stringBuilder);
                    }
                }
                foreach (var display in group)
                {
                    display.WriteText(local_EndDraw_stringBuilder);
                }
            }
        }
    }

    public interface IDisplayCollector
    {
        void Visit(IMyTerminalBlock block);
    }

    public interface IDisplayPart : IDisplayCollector
    {
        bool Filter(IMyTerminalBlock block);
        void Clear();
        void Draw(IMyCubeGrid cubeGrid, StringBuilder target);
    }
}
