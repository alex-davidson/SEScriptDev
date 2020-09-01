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
        private readonly IGrouping<IMyCubeGrid, IMyTextPanel>[] displayGroups;
        private readonly IDisplayPart[] parts;

        public DisplayRenderer(IEnumerable<IMyTextPanel> displays, IDisplayPart[] parts)
        {
            this.displayGroups = displays.GroupBy(d => d.CubeGrid).ToArray();
            this.parts = parts;
        }

        public IEnumerable<IDisplayCollector> BeginDraw()
        {
            foreach (var part in parts) part.Clear();
            return parts;
        }

        private readonly StringBuilder local_EndDraw_stringBuilder = new StringBuilder(Constants.ALLOC_DISPLAY_BUFFER_SIZE);
        public void EndDraw()
        {
            foreach (var group in displayGroups)
            {
                local_EndDraw_stringBuilder.Clear();
                local_EndDraw_stringBuilder.AppendLine(DateTime.Now.ToString("dd MMM HH:mm"));
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
        void Clear();
        void Draw(IMyCubeGrid cubeGrid, StringBuilder target);
    }
}
