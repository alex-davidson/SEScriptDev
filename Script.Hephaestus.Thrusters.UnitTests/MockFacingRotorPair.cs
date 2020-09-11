using System;
using IngameScript;

namespace Script.Hephaestus.Thrusters.UnitTests
{
    class MockFacingRotorPair : IFacingRotorPair
    {
        private RotorLimits limits = RotorLimits.None;
        private bool locked;

        public bool IsViable => true;
        public float CurrentAngleDegrees { get; set; }

        public float TargetVelocityDegreesPerSecond { get; set; }

        public virtual void ApplyLimits(RotorLimits newLimits)
        {
        }

        public void CheckState(Errors errors, string owningModuleName) { }
        public virtual void ForceLock()
        {
            locked = true;
        }

        public void Unlock()
        {
            locked = false;
        }

        public void Step(TimeSpan elapsed)
        {
            if (TargetVelocityDegreesPerSecond == 0) return;
            if (locked && TargetVelocityDegreesPerSecond < 0.1)
            {
                // The game is actually less fussy than this, but for test purposes we require a closer approximation of 'not moving'.
                TargetVelocityDegreesPerSecond = 0;
                return;
            }
            var angleThisStep = (float)elapsed.TotalSeconds * TargetVelocityDegreesPerSecond;
            CurrentAngleDegrees = limits.ClampDelta(CurrentAngleDegrees, angleThisStep);
        }
    }
}
