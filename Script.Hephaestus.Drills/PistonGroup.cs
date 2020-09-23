using System;

namespace IngameScript
{
    public class PistonGroup
    {
        private readonly PistonStack[] pistonStacks;
        private readonly float extendVelocityMetresPerSecond;
        private readonly float retractVelocityMetresPerSecond;

        public PistonGroup(PistonStack[] pistonStacks, float extendVelocityMetresPerSecond, float retractVelocityMetresPerSecond)
        {
            this.pistonStacks = pistonStacks;
            this.extendVelocityMetresPerSecond = extendVelocityMetresPerSecond;
            this.retractVelocityMetresPerSecond = retractVelocityMetresPerSecond;
            UpdateState();
        }

        private bool hasMismatchedExtensions;
        private bool hasMismatchedStacks;
        private PistonStack anyOperablePistonStack;
        public int Total => pistonStacks.Length;
        public int Operable { get; private set; }
        public bool IsViable => Total > 0 && Operable == Total && !hasMismatchedStacks && !hasMismatchedExtensions;

        public float Extension => anyOperablePistonStack?.Extension ?? float.NaN;
        public float ExtensionPercentage => anyOperablePistonStack?.ExtensionPercentage ?? float.NaN;

        public float Velocity => anyOperablePistonStack?.Velocity ?? float.NaN;

        public bool IsExtending => anyOperablePistonStack?.IsExtending ?? false;
        public bool IsRetracting => anyOperablePistonStack?.IsRetracting ?? false;

        public void UpdateState()
        {
            BeginUpdate();
            foreach (var pistonStack in pistonStacks)
            {
                UpdateState(pistonStack);
            }
        }

        private static bool AreMatchingStacks(PistonStack a, PistonStack b)
        {
            if (a.Total != b.Total) return false;
            if (a.BaseGrid != b.BaseGrid) return false;
            if (a.TopGrid != b.TopGrid) return false;
            return true;
        }

        public void Extend()
        {
            BeginUpdate();
            foreach (var pistonStack in pistonStacks)
            {
                pistonStack.Extend(extendVelocityMetresPerSecond);
                UpdateState(pistonStack);
            }
        }

        public void Retract()
        {
            BeginUpdate();
            foreach (var pistonStack in pistonStacks)
            {
                pistonStack.Retract(retractVelocityMetresPerSecond);
                UpdateState(pistonStack);
            }
        }

        public void Stop()
        {
            BeginUpdate();
            foreach (var pistonStack in pistonStacks)
            {
                pistonStack.Stop();
                UpdateState(pistonStack);
            }
        }

        private void BeginUpdate()
        {
            Operable = 0;
            anyOperablePistonStack = null;
            hasMismatchedStacks = false;
            hasMismatchedExtensions = false;
        }

        private void UpdateState(PistonStack pistonStack)
        {
            if (pistonStack.Operable < pistonStack.Total) return;
            Operable++;
            anyOperablePistonStack = anyOperablePistonStack ?? pistonStack;
            if (!AreMatchingStacks(anyOperablePistonStack, pistonStack)) hasMismatchedStacks = true;
            if (Math.Abs(anyOperablePistonStack.Extension - pistonStack.Extension) > 0.1f) hasMismatchedExtensions = true;
        }

        public void CheckState(Errors errors)
        {
            if (hasMismatchedStacks) errors.SafetyConcerns.Add("Mismatched piston stacks.");
            if (hasMismatchedExtensions) errors.SafetyConcerns.Add("Mismatched piston stack extensions.");
        }
    }
}
