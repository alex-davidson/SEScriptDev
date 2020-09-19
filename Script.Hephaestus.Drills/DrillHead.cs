using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class DrillHead
    {
        private readonly IMyShipDrill[] drills;

        public DrillHead(IMyShipDrill[] drills)
        {
            this.drills = drills;
            UpdateState();
        }

        public int Total => drills.Length;
        public int Operable { get; private set; }
        public int Drilling { get; private set; }
        public bool IsViable => Total > 0 && Operable == Total;
        public bool IsDrilling => Drilling == Operable;

        public void UpdateState()
        {
            BeginUpdate();
            foreach (var drill in drills)
            {
                UpdateState(drill);
            }
        }

        public void Start()
        {
            BeginUpdate();
            foreach (var drill in drills)
            {
                drill.Enabled = true;
                UpdateState(drill);
            }
        }

        public void Stop()
        {
            BeginUpdate();
            foreach (var drill in drills)
            {
                drill.Enabled = false;
                UpdateState(drill);
            }
        }

        private void BeginUpdate()
        {
            Operable = 0;
            Drilling = 0;
        }

        private void UpdateState(IMyShipDrill drill)
        {
            if (!drill.IsFunctional) return;
            // Can't tell if the drill is powered unless it's also enabled.
            if (drill.Enabled)
            {
                if (!drill.IsWorking) return;
                Operable++;
                Drilling++;
            }
            else
            {
                Operable++;
            }
        }
    }
}
