using SpaceEngineers.Game.ModAPI.Ingame;

namespace IngameScript
{
    public class ClampGroup
    {
        private readonly IMyLandingGear[] clamps;

        public ClampGroup(IMyLandingGear[] clamps)
        {
            this.clamps = clamps;
        }

        public int Total => clamps.Length;
        public int Operable { get; private set; }
        public int Locked { get; private set; }
        public bool AnyLocked => Locked > 0;
        public bool IsViable => Total > 0 && Operable == Total;

        public void UpdateState()
        {
            BeginUpdate();
            foreach (var clamp in clamps)
            {
                UpdateState(clamp);
            }
        }

        public void Lock()
        {
            BeginUpdate();
            foreach (var clamp in clamps)
            {
                clamp.Lock();
                UpdateState(clamp);
            }
        }

        public void Unlock()
        {
            BeginUpdate();
            foreach (var clamp in clamps)
            {
                clamp.Unlock();
                UpdateState(clamp);
            }
        }

        private void BeginUpdate()
        {
            Operable = 0;
            Locked = 0;
        }

        private void UpdateState(IMyLandingGear clamp)
        {
            clamp.AutoLock = false;
            if (!clamp.IsOperational()) return;
            Operable++;
            if (clamp.IsLocked) Locked++;
        }
    }
}
