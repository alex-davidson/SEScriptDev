using System;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class DrillTower
    {
        private readonly Errors errors = new Errors();
        private readonly PistonGroup pistonGroup;
        private readonly DrillHead drillHead;
        private readonly ClampGroup clampGroup;
        private readonly IMyMotorStator rotor;
        private readonly LightGroup lights;
        public IMyTextPanel Display { get; }

        private DrillTowerState state;

        public DrillTower(PistonStack[] pistonStacks, DrillHead drillHead, ClampGroup clampGroup, IMyMotorStator rotor, LightGroup lights, IMyTextPanel display)
        {
            this.pistonGroup = new PistonGroup(pistonStacks, Constants.DRILL_EXTENSION_SPEED_METRES_PER_SECOND, Constants.DRILL_RETRACTION_SPEED_METRES_PER_SECOND);
            this.drillHead = drillHead;
            this.clampGroup = clampGroup;
            this.rotor = rotor;
            this.lights = lights;
            this.Display = display;
        }

        private void EnsureInitialised()
        {
            if (state != DrillTowerState.None) return;

            // Initial state after eg. game load.
            // Infer current operation.
            if (pistonGroup.IsExtending)
            {
                // Probably already drilling. Do start-up checks again.
                state = DrillTowerState.Starting;
            }
            else if (pistonGroup.IsRetracting || pistonGroup.ExtensionPercentage > 0)
            {
                // Probably retrieving. Do retrieval checks again.
                state = DrillTowerState.Retrieving;
            }
            else
            {
                // Fallback. Do retrieval checks again, although we're probably already clamped.
                state = DrillTowerState.Retrieving;
            }
        }

        /// <summary>
        /// Returns true if an operation is in progress and should regularly be updated.
        /// </summary>
        /// <returns></returns>
        public bool Run()
        {
            EnsureInitialised();

            if (!IsDrillViable())
            {
                EmergencyStop();
                return false;
            }

            pistonGroup.UpdateState();
            drillHead.UpdateState();
            clampGroup.UpdateState();
            lights.UpdateState();

            switch (state)
            {
                case DrillTowerState.Starting:
                    if (!clampGroup.IsViable)
                    {
                        EmergencyStop();
                        return false;
                    }
                    // If possible, lock the drill head while we spin up.
                    pistonGroup.Stop();
                    TryLockClamps();

                    if (IsRotorRunning() && drillHead.IsDrilling)
                    {
                        // Drill is operating. Prepare to extend.
                        state = DrillTowerState.Drilling;
                    }
                    StartRotor();
                    drillHead.Start();
                    return true;

                case DrillTowerState.Drilling:
                    if (!IsRotorRunning() || !drillHead.IsDrilling)
                    {
                        // Should not be in this state.
                        EmergencyStop();
                        return false;
                    }
                    if (!TryUnlockClamps())
                    {
                        // Unable to release the clamps.
                        EmergencyStop();
                        return false;
                    }
                    pistonGroup.Extend();
                    return true;

                case DrillTowerState.Retrieving:
                    if (TryLockClamps())
                    {
                        pistonGroup.Stop();
                        state = DrillTowerState.Stopping;
                        return true;
                    }
                    pistonGroup.Retract();
                    return true;

                case DrillTowerState.Stopping:
                    pistonGroup.Stop();
                    if (!TryLockClamps()) return true;

                    StopRotor();
                    drillHead.Stop();
                    if (!IsRotorRunning())
                    {
                        state = DrillTowerState.Stopped;
                        return true;
                    }
                    return true;

                case DrillTowerState.Stopped:
                    pistonGroup.Stop();
                    StopRotor();
                    drillHead.Stop();
                    return false;

                case DrillTowerState.EmergencyStopped:
                default:
                    EmergencyStop();
                    return false;
            }
        }

        public void StartDrilling()
        {
            state = DrillTowerState.Starting;
        }

        public void StopDrilling()
        {
            state = DrillTowerState.Retrieving;
        }

        public void LightsOn()
        {
            lights.SwitchOn();
        }

        public void LightsOff()
        {
            lights.SwitchOff();
        }

        public bool LightsAreOn => lights.SwitchedOn > 0;

        public void EmergencyStop()
        {
            pistonGroup.Stop();
            StopRotor();
            drillHead.Stop();
            TryLockClamps();
            state = DrillTowerState.EmergencyStopped;
        }

        public void Describe(StringBuilder builder)
        {
            builder.AppendLine(DescribeState(state));

            // Pistons:
            builder.AppendFormat("Pistons: {0} / {1}   ", pistonGroup.Operable, pistonGroup.Total);
            if (pistonGroup.IsViable)
            {
                builder.Append(pistonGroup.IsExtending ? $"Extending, {pistonGroup.ExtensionPercentage:P}"
                    : pistonGroup.IsRetracting ? $"Retracting, {pistonGroup.ExtensionPercentage:P}"
                    : "Stopped");
            }
            else
            {
                builder.Append("DAMAGED");
            }
            builder.AppendLine();

            // Rotor:
            builder.Append("Rotor: ");
            if (rotor == null)
            {
                builder.Append("MISSING");
            }
            else if (rotor.IsOperational())
            {
                builder.Append(IsRotorRunning() ? "Running" : "Stopped");
            }
            else
            {
                builder.Append("DAMAGED");
            }
            builder.AppendLine();

            // Drills:
            builder.AppendFormat("Drills: {0} / {1}   ", drillHead.Operable, drillHead.Total);
            if (drillHead.IsViable)
            {
                builder.Append(drillHead.IsDrilling ? "Drilling" : "Stopped");
            }
            else
            {
                builder.Append("DAMAGED");
            }
            builder.AppendLine();

            // Clamps:
            builder.AppendFormat("Clamps: {0} / {1}   ", clampGroup.Operable, clampGroup.Total);
            if (clampGroup.IsViable)
            {
                builder.Append(clampGroup.AnyLocked ? $"Locked, {clampGroup.Locked}" : "Unlocked");
            }
            else
            {
                builder.Append("DAMAGED");
            }
            builder.AppendLine();

            // Lights:
            if (lights.Total > 0)
            {
                builder.AppendFormat("Lights: {0} / {1}   ", lights.Operable, lights.Total);
                builder.Append(lights.SwitchedOn > 0 ? $"On, {lights.SwitchedOn}" : "Off");
                builder.AppendLine();
            }

            errors.Clear();
            pistonGroup.CheckState(errors);
            errors.WriteTo(builder);
        }

        private static string DescribeState(DrillTowerState state)
        {
            switch (state)
            {
                case DrillTowerState.None: return "Initialising";
                case DrillTowerState.Stopped: return "Idle";
                case DrillTowerState.Starting: return "Starting";
                case DrillTowerState.Drilling: return "Drilling";
                case DrillTowerState.Retrieving: return "Retrieving";
                case DrillTowerState.Stopping: return "Stopping";
                case DrillTowerState.EmergencyStopped: return "EMERGENCY STOP";
            }
            return "";
        }

        private bool IsRotorRunning()
        {
            if (rotor == null) return false;
            if (rotor.RotorLock) return false;
            if (Math.Abs(rotor.TargetVelocityRPM) < 0.001f) return false;
            return true;
        }

        private void StartRotor()
        {
            if (rotor == null) return;
            rotor.RotorLock = false;
            rotor.TargetVelocityRPM = Constants.DRILL_ROTATION_SPEED_RPM;
        }

        private void StopRotor()
        {
            if (rotor == null) return;
            rotor.TargetVelocityRPM = 0;
            rotor.RotorLock = true;
        }

        private bool TryLockClamps()
        {
            if (pistonGroup.Extension > 0.01f) return false;
            // Try to lock the clamps regardless of viability. Best effort.
            clampGroup.Lock();
            return clampGroup.AnyLocked;
        }

        private bool TryUnlockClamps()
        {
            // If clamps are not viable, we cannot be sure that they're all unlocked and dare
            // not try to extend the drill.
            if (!clampGroup.IsViable) return false;
            clampGroup.Unlock();
            return !clampGroup.AnyLocked;
        }

        private bool IsDrillViable()
        {
            if (!pistonGroup.IsViable) return false;
            if (!drillHead.IsViable) return false;
            if (rotor?.IsOperational() != true) return false;
            return true;
        }
    }
}
