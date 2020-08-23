using System.Linq;

namespace IngameScript
{
    public class SystemState
    {
        public SystemState(StaticState staticState)
        {
            Static = staticState;
            Ingots = new IngotStockpiles(staticState.IngotTypes.All.Select(i => new IngotStockpile(i)));
        }

        public readonly StaticState Static;

        private float refinerySpeedFactor = 1;  // Assume the worst (1) until we can detect it.
        public float RefinerySpeedFactor
        {
            get { return Static.RefinerySpeedFactor ?? refinerySpeedFactor; }
            set { if (value > 0) refinerySpeedFactor = value; }
        }

        // Maintained between iterations, therefore must be SystemState properties:

        public float TotalAssemblerSpeed;
        public readonly IngotStockpiles Ingots;

        public void Configure()
        {

        }
    }
}
