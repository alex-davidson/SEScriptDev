using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class LightGroup
    {
        private readonly IMyLightingBlock[] lights;

        public LightGroup(IMyLightingBlock[] lights)
        {
            this.lights = lights;
        }

        public int Total => lights.Length;
        public int Operable { get; private set; }
        public int SwitchedOn { get; private set; }

        public void UpdateState()
        {
            BeginUpdate();
            foreach (var light in lights)
            {
                UpdateState(light);
            }
        }

        public void SwitchOn()
        {
            BeginUpdate();
            foreach (var light in lights)
            {
                light.Enabled = true;
                UpdateState(light);
            }
        }

        public void SwitchOff()
        {
            BeginUpdate();
            foreach (var light in lights)
            {
                light.Enabled = false;
                UpdateState(light);
            }
        }

        private void BeginUpdate()
        {
            Operable = 0;
            SwitchedOn = 0;
        }

        private void UpdateState(IMyLightingBlock light)
        {
            if (!light.IsFunctional) return;
            if (!light.IsWorking) return;
            Operable++;
            if (light.Enabled) SwitchedOn++;
        }
    }
}
