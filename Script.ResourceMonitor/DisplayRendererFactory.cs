using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class DisplayRendererFactory
    {
        private static readonly List<IDisplayPart> local_Create_partsList = new List<IDisplayPart>(Constants.ALLOC_PARTS_PER_DISPLAY_COUNT);
        public DisplayRenderer Create(IEnumerable<IMyTextPanel> displays, RequestedDisplayConfiguration configuration)
        {
            local_Create_partsList.Clear();
            foreach (var categoryDescriptor in configuration.IncludeCategories)
            {
                var part = GenerateDisplayPart(categoryDescriptor);
                if (part == null)
                {
                    Debug.Write(Debug.Level.Warning, new Message("Display category {0} was not recognised and will be ignored.", categoryDescriptor));
                    continue;
                }
                local_Create_partsList.Add(part);
            }
            return new DisplayRenderer(displays, local_Create_partsList.ToArray());
        }

        private IDisplayPart GenerateDisplayPart(string categoryDescriptor)
        {
            switch (categoryDescriptor)
            {
                case "Batteries.SummaryByGrid":
                    return new Batteries.SummaryByGrid();
                case "Power.SummaryByGrid":
                    return new Power.SummaryByGrid();
                case "Hydrogen.SummaryByGrid":
                    return new Hydrogen.SummaryByGrid();
                case "Oxygen.SummaryByGrid":
                    return new Oxygen.SummaryByGrid();
                case "Ice.StocksAndProcessing":
                    return new Ice.StocksAndProcessing();
            }
            return null;
        }
    }
}
