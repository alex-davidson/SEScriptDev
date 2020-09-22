using System;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public interface IFacingRotorPair
    {
        void CheckState(Errors errors, string owningModuleName);
        bool IsViable { get; }
        float CurrentAngleDegrees { get; }
        float TargetVelocityDegreesPerSecond { get; set; }
        void ForceLock();
        void ApplyLimits(RotorLimits newLimits);
        void Unlock();
    }

    public class FacingRotorPair : IFacingRotorPair
    {
        private readonly IMyMotorStator governing;
        private readonly IMyMotorStator opposing;

        public FacingRotorPair(IMyMotorStator governing, IMyMotorStator opposing)
        {
            this.governing = governing;
            this.opposing = opposing;
        }

        public void CheckState(Errors errors, string owningModuleName)
        {
            if (governing == null) errors.Warnings.Add(new Message("Governing rotor of module {0} is missing.", owningModuleName));
            else if (!governing.IsOperational()) errors.Warnings.Add(new Message("Governing rotor {0} of module {1} is not operational.", governing.CustomName, owningModuleName));
            else if (!governing.IsAttached) errors.Warnings.Add(new Message("Governing rotor {0} of module {1} is not attached.", governing.CustomName, owningModuleName));

            if (opposing == null) errors.Warnings.Add(new Message("Opposing rotor of module {0} is missing.", owningModuleName));
            else if (!opposing.IsOperational()) errors.Warnings.Add(new Message("Opposing rotor {0} of module {1} is not operational.", opposing.CustomName, owningModuleName));
            else if (!opposing.IsAttached) errors.Warnings.Add(new Message("Opposing rotor {0} of module {1} is not attached.", opposing.CustomName, owningModuleName));

            if (!IsViable) return;

            if (governing?.IsAttached == true && opposing?.IsAttached == true)
            {
                if (governing.TopGrid != opposing.TopGrid)
                {
                    errors.SanityChecks.Add(new Message("Rotors of module {0} connect to different subgrids ({1} and {2}).", owningModuleName, governing.TopGrid.CustomName, opposing.TopGrid.CustomName));
                }
                else
                {
                    var governingDegrees = MathHelper.ToDegrees(governing.Angle);
                    var opposedDegrees = RotorLimits.OpposedAngleDegrees(MathHelper.ToDegrees(opposing.Angle));
                    var rotorOffset = RotorLimits.DifferenceDegrees(governingDegrees, opposedDegrees);
                    if (rotorOffset > 1) errors.SanityChecks.Add(new Message("Rotors of module {0} are not properly synchronised: {1}.", owningModuleName, rotorOffset));
                }
            }

            if (!ValidateLimits(CurrentAngleDegrees))
            {
                errors.SafetyConcerns.Add(new Message("Rotors of module {0} are outside of set limits.", owningModuleName));
            }
        }

        public bool IsViable
        {
            get
            {
                var governingIsAvailable = governing != null && governing.IsOperational() && governing.IsAttached;
                var opposingIsAvailable = opposing != null && opposing.IsOperational() && opposing.IsAttached;
                return governingIsAvailable || opposingIsAvailable;
            }
        }

        public float CurrentAngleDegrees
        {
            get
            {
                if (governing != null) return MathHelper.ToDegrees(governing.Angle);
                if (opposing != null) return RotorLimits.OpposedAngleDegrees(MathHelper.ToDegrees(opposing.Angle));
                return float.NaN;
            }
        }

        public float TargetVelocityDegreesPerSecond
        {
            get
            {
                if (governing != null) return governing.TargetVelocityRPM * 6;
                if (opposing != null) return -opposing.TargetVelocityRPM * 6;
                return float.NaN;
            }
            set
            {
                var rpm = value / 6;
                if (governing != null) governing.TargetVelocityRPM = rpm;
                if (opposing != null) opposing.TargetVelocityRPM = -rpm;
            }
        }

        public void ForceLock()
        {
            if (governing != null)
            {
                governing.RotorLock = true;
                governing.TargetVelocityRPM = 0;
            }
            if (opposing != null)
            {
                opposing.RotorLock = true;
                opposing.TargetVelocityRPM = 0;
            }
        }

        private bool ValidateLimits(float angleDegrees)
        {
            if (governing != null)
            {
                if (angleDegrees > governing.UpperLimitDeg) return false;
                if (angleDegrees < governing.LowerLimitDeg) return false;
            }

            if (opposing != null)
            {
                var opposingAngleDegrees = RotorLimits.OpposedAngleDegrees(angleDegrees);
                if (opposingAngleDegrees > opposing.UpperLimitDeg) return false;
                if (opposingAngleDegrees < opposing.LowerLimitDeg) return false;
            }

            return true;
        }

        public void ApplyLimits(RotorLimits newLimits)
        {
            // Set lower limits first, since upper limits might be clamped.
            if (governing != null)
            {
                governing.Displacement = Constants.FACING_ROTOR_DISPLACEMENT;
                governing.LowerLimitDeg = newLimits.Minimum;
                governing.UpperLimitDeg = newLimits.Maximum;
            }
            
            if (opposing != null)
            {
                var newOpposingLimits = newLimits.Opposing();
                opposing.Displacement = Constants.FACING_ROTOR_DISPLACEMENT;
                opposing.LowerLimitDeg = newOpposingLimits.Minimum;
                opposing.UpperLimitDeg = newOpposingLimits.Maximum;
            }
        }

        public void Unlock()
        {
            if (governing != null) governing.RotorLock = false;
            if (opposing != null) opposing.RotorLock = false;
        }
    }
}
