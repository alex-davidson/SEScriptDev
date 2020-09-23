using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public Program()
        {
            Debug.Initialise(Debug.Level.Info, Echo);

            Runtime.UpdateFrequency = UpdateFrequency.Update10 | UpdateFrequency.Update100;
        }

        public void Save()
        {
        }

        private readonly MyCommandLine commandLine = new MyCommandLine();

        private readonly State state = new State();

        public void Main(string argument, UpdateType updateSource)
        {
            Clock.AddTime(Runtime.TimeSinceLastRun);

            EnsureInitialised();

            if (commandLine.TryParse(argument))
            {
                var command = commandLine.Argument(0);
                var towerDesignation = commandLine.Argument(1);
                if (StringComparer.OrdinalIgnoreCase.Equals(towerDesignation, "all"))
                {
                    foreach (var tower in state.Towers.Values) ApplyOperation(command, tower);
                }
                else
                {
                    var tower = state.GetDrillTower(towerDesignation);
                    ApplyOperation(command, tower);
                }
            }

            HandlePeriodicUpdate();
        }

        private void ApplyOperation(string command, DrillTower tower)
        {
            switch (command?.ToLower())
            {
                case "lights-on":
                    tower?.LightsOn();
                    break;

                case "lights-off":
                    tower?.LightsOff();
                    break;

                case "lights":
                    if (tower == null) return;
                    if (tower.LightsAreOn)
                    {
                        tower.LightsOff();
                    }
                    else
                    {
                        tower.LightsOn();
                    }
                    break;

                case "start":
                    tower?.LightsOn();
                    tower?.StartDrilling();
                    break;

                case "stop":
                    tower?.StopDrilling();
                    break;

                case "fail":
                    tower?.EmergencyStop();
                    break;

                case "rescan":
                    state.PendingRescan = true;
                    break;
            }
        }

        private void EnsureInitialised()
        {
            if (state.PendingRescan == false) return;

            Debug.Write(Debug.Level.Info, "Configuration updated. Reinitialising...");
            var builder = new DrillTowerBuilder(GridTerminalSystem);
            state.Towers.Clear();
            foreach (var towerDef in StaticState.Instance.DrillTowers)
            {
                state.Towers.Add(towerDef.Letter, builder.Build(towerDef));
            }
            state.PendingRescan = false;
        }

        private static readonly StringBuilder local_display_builder = new StringBuilder(1000);
        private void HandlePeriodicUpdate()
        {
            var anyRan = false;
            foreach (var kv in state.Towers)
            {
                if (kv.Value.Run()) anyRan = true;
                if (kv.Value.Display != null)
                {
                    local_display_builder.Clear();
                    local_display_builder.AppendFormat("Drill Tower {0}  {1}\n", kv.Key, Datestamp.Minutes);
                    kv.Value.Describe(local_display_builder);
                    kv.Value.Display.WriteText(local_display_builder);
                }
            }

            if (anyRan)
            {
                Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                Debug.Write(Debug.Level.Debug, "Yielding");
            }
            else
            {
                Runtime.UpdateFrequency &= ~UpdateFrequency.Update10;
                Debug.Write(Debug.Level.Info, "Completed.");
            }
        }

        class State
        {
            public Dictionary<char, DrillTower> Towers { get; } = new Dictionary<char, DrillTower>();
            public bool PendingRescan { get; set; } = true;

            public DrillTower GetDrillTower(string designation)
            {
                if (designation?.Length != 1) return null;
                DrillTower tower;
                Towers.TryGetValue(designation[0], out tower);
                return tower;
            }
        }
    }
}
