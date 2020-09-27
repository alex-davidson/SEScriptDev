using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public Program()
        {
            Debug.Initialise(Debug.Level.Info, Echo);

            Runtime.UpdateFrequency = UpdateFrequency.Update100 | UpdateFrequency.Update10;
        }

        private readonly State state = new State();
        private IEnumerator<object> current;
        private readonly MyCommandLine commandLine = new MyCommandLine();

        public void Main(string argument, UpdateType updateSource)
        {
            Clock.AddTime(Runtime.TimeSinceLastRun);

            if (commandLine.TryParse(argument))
            {
                var command = commandLine.Argument(0);
                switch (command?.ToLower())
                {
                    case "rescan":
                        state.PendingRescan = true;
                        break;
                }
            }

            HandlePeriodicUpdate(updateSource != UpdateType.Update10 && updateSource != UpdateType.Update1);
        }

        private void HandlePeriodicUpdate(bool forceSyncOff)
        {
            var yielded = Step(forceSyncOff);
            if (yielded)
            {
                Debug.Assert(current != null, "No current iterator.");
                Runtime.UpdateFrequency |= UpdateFrequency.Update1;
                Debug.Write(Debug.Level.Debug, "Yielding");
            }
            else
            {
                Debug.Assert(current == null, "Current iterator still exists.");
                Runtime.UpdateFrequency &= ~UpdateFrequency.Update1;
                Debug.Write(Debug.Level.Info, "Completed.");
            }
        }

        private bool Step(bool forceSyncOff)
        {
            if (current == null)
            {
                if (state.PendingRescan)
                {
                    // Also synchronises state.
                    current = Rescan();
                }
                else
                {
                    current = Run(forceSyncOff);
                }
            }

            if (current.MoveNext()) return true;

            current?.Dispose();
            current = null;
            return false;
        }

        private IEnumerator<object> Run(bool forceSync)
        {
            var anyPrimaryThruster = GetAnyPrimaryThruster();
            var primaryThrusterStateChanged = anyPrimaryThruster.Enabled != state.EnableThrusters;

            // Periodic sync: disable thrusters which should be enabled, but not vice versa.
            var primaryThrustersDisabledNeedSync = forceSync && !anyPrimaryThruster.Enabled;
            var primaryThrustersEnabledNeedsCockpitSync = forceSync && anyPrimaryThruster.Enabled;

            if (primaryThrusterStateChanged)
            {
                state.EnableThrusters = anyPrimaryThruster.Enabled;
            }
            if (primaryThrusterStateChanged || primaryThrustersDisabledNeedSync)
            {
                SyncAllThrusters(state.EnableThrusters);
            }
            if (primaryThrusterStateChanged || primaryThrustersEnabledNeedsCockpitSync)
            {
                DisableAttachedGridInertiaDampeners();
            }
            yield break;
        }

        private void SyncAllThrusters(bool enable)
        {
            foreach (var thruster in state.PrimaryThrusters)
            {
                thruster.Enabled = enable;
            }
            foreach (var thruster in state.OtherThrusters)
            {
                thruster.Enabled = enable;
            }
            if (state.EnableThrusters)
            {
                DisableAttachedGridInertiaDampeners();
            }
        }

        private void DisableAttachedGridInertiaDampeners()
        {
            foreach (var cockpit in state.ConnectedGridCockpits)
            {
                cockpit.DampenersOverride = false;
            }
        }

        private IMyThrust GetAnyPrimaryThruster()
        {
            if (state.AnyPrimaryThruster?.IsFunctional == true) return state.AnyPrimaryThruster;
            state.AnyPrimaryThruster = null;
            foreach (var thruster in state.PrimaryThrusters)
            {
                if (thruster.IsFunctional)
                {
                    state.AnyPrimaryThruster = thruster;
                    break;
                }
            }
            return state.AnyPrimaryThruster;
        }

        private IEnumerator<object> Rescan()
        {
            state.AnyPrimaryThruster = null;
            state.PrimaryThrusters.Clear();
            state.OtherThrusters.Clear();
            state.ConnectedGridCockpits.Clear();
            state.EnableThrusters = false;

            Debug.Write(Debug.Level.Info, "Scanning primary thrusters");
            var primaryThrusterGroup = GridTerminalSystem.GetBlockGroupWithName(StaticState.PrimaryThrustersBlockGroupName);
            primaryThrusterGroup.GetBlocksOfType(state.OtherThrusters, Me.IsSameConstructAs);
            foreach (var thruster in state.OtherThrusters)
            {
                if (thruster.Enabled) state.EnableThrusters = true;
                state.PrimaryThrusters.Add(thruster);
                if (thruster.IsFunctional && state.AnyPrimaryThruster == null)
                {
                    state.AnyPrimaryThruster = thruster;
                }
            }

            yield return null;

            Debug.Write(Debug.Level.Info, "Scanning all thrusters");
            GridTerminalSystem.GetBlocksOfType(state.OtherThrusters, Me.IsSameConstructAs);

            var i = 0;
            while (i < state.OtherThrusters.Count)
            {
                if (state.PrimaryThrusters.Contains(state.OtherThrusters[i]))
                {
                    state.OtherThrusters.RemoveAtFast(i);
                }
                else
                {
                    state.OtherThrusters[i].Enabled = state.EnableThrusters;
                }
                i++;
            }

            Debug.Write(Debug.Level.Info, "Scanning all connected grids' cockpits");
            GridTerminalSystem.GetBlocksOfType(state.ConnectedGridCockpits, c => !Me.IsSameConstructAs(c));

            if (state.EnableThrusters)
            {
                DisableAttachedGridInertiaDampeners();
            }

            state.PendingRescan = false;
        }

        class State
        {
            public IMyThrust AnyPrimaryThruster { get; set; }
            public HashSet<IMyThrust> PrimaryThrusters { get; } = new HashSet<IMyThrust>();
            public List<IMyThrust> OtherThrusters { get; } = new List<IMyThrust>(100);
            public List<IMyCockpit> ConnectedGridCockpits { get; } = new List<IMyCockpit>(100);

            public bool EnableThrusters { get; set; }

            public bool PendingRescan { get; set; } = true;
        }

        struct StaticState
        {
            public const string PrimaryThrustersBlockGroupName = "Thrusters (Primary, Rear)";
        }
    }
}
