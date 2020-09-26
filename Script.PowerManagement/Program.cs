using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Text;
using System;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public Program()
        {
            Debug.Initialise(Debug.Level.Info, Echo);

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
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

            HandlePeriodicUpdate();
        }

        private IEnumerable<object> MaybeRescan()
        {
            if (!state.PendingRescan) yield break;

            state.Batteries.Clear();
            GridTerminalSystem.GetBlocksOfType(state.Batteries, Me.IsSameConstructAs);
            yield return null;
            GridTerminalSystem.GetBlocksOfType(state.Reactors, Me.IsSameConstructAs);
            yield return null;
            state.Display = GridTerminalSystem.GetBlockWithName(Constants.DISPLAY_NAME) as IMyTextPanel;
        }

        private void HandlePeriodicUpdate()
        {
            var yielded = Step();
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

        private bool Step()
        {
            if (current == null)
            {
                current = Run();
            }

            if (current.MoveNext()) return true;

            current = null;
            return false;
        }


        private IEnumerator<object> Run()
        {
            foreach (var yieldPoint in MaybeRescan()) 
            {
                yield return yieldPoint;
            }

            var batteriesState = new BatteriesState();
            ScanBatteries(ref batteriesState);
            yield return null;

            var reactorsState = new ReactorsState();
            ScanReactors(ref reactorsState);
            yield return null;

            if (UpdateState(batteriesState, reactorsState) && state.Display != null)
            {
                // Yield, then rescan before drawing display.
                yield return null;
                ScanBatteries(ref batteriesState);
                yield return null;
                ScanReactors(ref reactorsState);
            }

            if (state.Display != null)
            {
                yield return null;
                new DisplayRenderer().Draw(state.Display, batteriesState, reactorsState);
            }
        }

        private bool UpdateState(BatteriesState batteriesState, ReactorsState reactorsState)
        {
            /*
             * If battery power draw is near the limit, or remaining battery power is below the threshold, switch to reactors.
             * Otherwise if reactor power draw is low enough, switch to batteries.
             * Returns true if anything changed.
             */

            var isBatteryTimeBelowThreshold = batteriesState.SecondsRemaining < Constants.ENABLE_REACTOR_THRESHOLD_BATTERY_POWER_SECONDS;
            var isBatteryPowerDrawNearLimit = batteriesState.PowerDrawPercent > Constants.POWER_DRAW_THRESHOLD_PERCENT;

            if (isBatteryTimeBelowThreshold || isBatteryPowerDrawNearLimit)
            {
                if (batteriesState.Charging > 0 || reactorsState.Enabled < state.Reactors.Count)
                {
                    SwitchToReactors();
                    return true;
                }
            }
            else if (reactorsState.PowerConsumedMW < Constants.DISABLE_REACTOR_THRESHOLD_POWER_DRAW_MW)
            {
                if (batteriesState.Charging < state.Batteries.Count || reactorsState.Enabled > 0)
                {
                    SwitchToBatteries();
                    return true;
                }
            }
            return false;
        }

        private void ScanBatteries(ref BatteriesState batteriesState)
        {
            foreach (var battery in state.Batteries)
            {
                batteriesState.MaxEnergyMWh += battery.MaxStoredPower;
                batteriesState.EnergyRemainingMWh += battery.CurrentStoredPower;
                batteriesState.PowerConsumedMW += battery.CurrentOutput - battery.CurrentInput;
                batteriesState.PowerDrawMW += battery.CurrentOutput;
                batteriesState.MaxPowerDrawMW += battery.MaxOutput;
                if (battery.Enabled && battery.ChargeMode != ChargeMode.Discharge) batteriesState.Charging++;
            }
        }

        private void ScanReactors(ref ReactorsState reactorsState)
        {
            foreach (var reactor in state.Reactors)
            {
                for (var i = 0; i < reactor.InventoryCount; i++)
                {
                    var inventory = reactor.GetInventory(i);
                    reactorsState.FuelAvailableKg += (float)inventory.GetItemAmount(MyItemType.MakeIngot("Uranium"));
                }
                reactorsState.PowerConsumedMW += reactor.CurrentOutput;
                reactorsState.MaxPowerDrawMW += reactor.MaxOutput;
                if (reactor.Enabled) reactorsState.Enabled++;
            }
        }

        private void SwitchToBatteries()
        {
            foreach (var battery in state.Batteries)
            {
                battery.ChargeMode = ChargeMode.Auto;
            }
            foreach (var reactor in state.Reactors)
            {
                reactor.Enabled = false;
            }
        }

        private void SwitchToReactors()
        {
            foreach (var battery in state.Batteries)
            {
                battery.ChargeMode = ChargeMode.Discharge;
            }
            foreach (var reactor in state.Reactors)
            {
                reactor.Enabled = true;
            }
        }

        class State
        {
            public List<IMyBatteryBlock> Batteries { get; } = new List<IMyBatteryBlock>(100);
            public List<IMyReactor> Reactors { get; } = new List<IMyReactor>(8);
            public IMyTextPanel Display { get; set; }
            public bool PendingRescan { get; set; } = true;
        }
    }
}
