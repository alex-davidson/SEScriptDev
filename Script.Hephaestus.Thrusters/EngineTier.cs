using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public class EngineTier
    {
        public long Hash { get; }
        private readonly EngineModule[] modules;
        private readonly StaticState.EnginePresetDef[] presets;
        private readonly IEnumerator<bool>[] enumeratorPerModule;

        public EngineTier(EngineModule[] modules, StaticState.EnginePresetDef[] presets, long hash)
        {
            Hash = hash;
            this.modules = modules;
            this.presets = presets;
            enumeratorPerModule = new IEnumerator<bool>[modules.Length];
        }

        private string activePreset;
        private IEnumerator<bool> current;

        public bool ActivatePreset(string name)
        {
            var preset = presets.FirstOrDefault(p => StringComparer.OrdinalIgnoreCase.Equals(p.Name, name));
            if (default(StaticState.EnginePresetDef).Equals(preset)) return false;

            var speed = activePreset == name ? Constants.MODULE_EMERGENCY_ROTATION_SPEED : Constants.MODULE_NORMAL_ROTATION_SPEED;
            Cancel();
            activePreset = name;
            current = RotateAllTo(preset.Angle, speed);
            return true;
        }

        private IEnumerator<bool> RotateAllTo(float angle, RotationSpeed speed)
        {
            Begin(m => m.RotateTo(angle, speed));
            yield return true;
            while (RunModules(enumeratorPerModule)) yield return true;
        }

        public bool BeginTest()
        {
            Cancel();
            current = RunTest();
            return true;
        }

        private IEnumerator<bool> RunTest()
        {
            while (true)
            {
                Begin(m => m.TestRotate(true));
                // The 'while' loops will not yield if modules aren't viable, so make sure we yield unconditionally at least once.
                yield return true;
                while (RunModules(enumeratorPerModule)) yield return true;
                Begin(m => m.TestRotate(false));
                yield return true;
                while (RunModules(enumeratorPerModule)) yield return true;
            }
        }

        public void Stop()
        {
            Cancel();
            foreach (var module in modules) module.ForceLock();
        }

        public void CheckState(Errors errors)
        {
            foreach (var module in modules) module.CheckState(errors);
        }

        public bool Run()
        {
            if (current == null) return false;
            if (!current.MoveNext())
            {
                Cancel();
                return false;
            }
            return true;
        }

        private void Cancel()
        {
            if (current == null) return;
            current.Dispose();
            current = null;
            activePreset = null;
        }

        private void Begin(Func<EngineModule, IEnumerator<bool>> begin)
        {
            for (var i = 0; i < modules.Length; i++)
            {
                enumeratorPerModule[i] = begin(modules[i]);
            }
        }

        private static bool RunModules(IEnumerator<bool>[] running)
        {
            var pending = 0;
            foreach (var operation in running)
            {
                if (operation.MoveNext() && operation.Current) pending++;
            }
            return pending > 0;
        }
    }
}
