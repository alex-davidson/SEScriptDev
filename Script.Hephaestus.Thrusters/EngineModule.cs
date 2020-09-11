using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public struct EngineModule
    {
        private readonly string moduleName;
        private readonly IFacingRotorPair rotorPair;
        private readonly RotorLimits limits;
        private readonly IMyThrust[] thrusters;

        public EngineModule(string moduleName, IFacingRotorPair rotorPair, RotorLimits limits, IMyThrust[] thrusters)
        {
            this.moduleName = moduleName;
            this.rotorPair = rotorPair;
            this.limits = limits;
            this.thrusters = thrusters;
        }

        /// <summary>
        /// Rotates module smoothly to the specified angle.
        /// </summary>
        /// <remarks>
        /// The iterator yields true while rotation is ongoing, then yields false thereafter.
        /// It is non-terminating; stepping after completion merely checks invariants and enforces
        /// rotor lock.
        /// </remarks>
        public IEnumerator<bool> RotateTo(float targetAngleDegrees, RotationSpeed speed)
        {
            if (IsModuleViable())
            {
                var stopwatch = new Stopwatch();

                var initialAngleDegrees = rotorPair.CurrentAngleDegrees;

                var thisRotationLimits = limits.ClampRotation(initialAngleDegrees, targetAngleDegrees, 1);
                UnlockAndPrepareForRotate(thisRotationLimits);

                var sqrtSpringConstant = Math.Sqrt(speed.SpringConstant);

                // Indicate that we're ready to start.
                yield return true;

                while (IsModuleViable())
                {
                    var remainingDegrees = targetAngleDegrees - rotorPair.CurrentAngleDegrees;
                    var currentVelocityDps = rotorPair.TargetVelocityDegreesPerSecond;

                    if (IsRotationComplete(remainingDegrees, currentVelocityDps)) break;

                    var elapsed = stopwatch.GetElapsedSeconds();

                    // Critically-damped spring:
                    var acceleratingForce = remainingDegrees * speed.SpringConstant;
                    var deceleratingForce = -currentVelocityDps * 2 * sqrtSpringConstant;
                    var force = acceleratingForce + deceleratingForce;
                    // Err on the side of smaller time steps.
                    var expectTimeStep = Math.Min(RotationSpeed.TimeStepSeconds, elapsed);

                    var accelerationNextSecond = force;// MathHelper.Clamp(force, -specification.MaximumAcceleration, specification.MaximumAcceleration);
                    rotorPair.TargetVelocityDegreesPerSecond += (float)(accelerationNextSecond * expectTimeStep);

                    stopwatch.Reset();
                    yield return true;
                }
            }

            // Complete. Enforce limits and lock.
            while (true)
            {
                ForceLock();
                yield return false;
            }
        }

        private bool IsRotationComplete(float remainingAngleDegrees, float currentVelocityDps)
        {
            if (Math.Abs(currentVelocityDps) > 0.1) return false;
            if (Math.Abs(remainingAngleDegrees) > 0.1) return false;
            return true;
        }

        /// <summary>
        /// Rotates module a limited distance in the specified direction from its current angle.
        /// </summary>
        /// <remarks>
        /// The iterator yields true while rotation is ongoing, then yields false thereafter.
        /// It is non-terminating; stepping after completion merely checks invariants and enforces
        /// rotor lock.
        /// </remarks>
        public IEnumerator<bool> TestRotate(bool clockwise)
        {
            var initialAngleDegrees = rotorPair.CurrentAngleDegrees;
            var targetAngleDegrees = clockwise ? initialAngleDegrees + Constants.MODULE_TEST_DEFLECTION_DEGREES : initialAngleDegrees - Constants.MODULE_TEST_DEFLECTION_DEGREES;
            return RotateTo(targetAngleDegrees, Constants.MODULE_TEST_ROTATION_SPEED);
        }

        public void CheckState(Errors errors)
        {
            rotorPair.CheckState(errors, moduleName);
        }

        private bool IsModuleViable() => rotorPair.IsViable;
        public void ForceLock()
        {
            rotorPair.ForceLock();
            rotorPair.ApplyLimits(limits);
            foreach (var thruster in thrusters) thruster.Enabled = true;
        }

        private void UnlockAndPrepareForRotate(RotorLimits thisRotationLimits)
        {
            foreach (var thruster in thrusters) thruster.Enabled = false;
            rotorPair.ApplyLimits(thisRotationLimits);
            rotorPair.Unlock();
        }
    }
}
