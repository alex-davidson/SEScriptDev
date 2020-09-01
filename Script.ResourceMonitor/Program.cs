using System;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public Program()
        {
            Debug.Initialise(Debug.Level.Info, Echo);
            ResourceMonitorDriver.StaticInitialise();

            configuration = new ConfigurationReader().Deserialise(RequestedConfiguration.GetDefault(), Storage);

            Runtime.UpdateFrequency = Constants.EXPECTED_UPDATE_MODE;
        }

        public void Save()
        {
            Storage = new ConfigurationWriter().Serialise(configuration);
        }

        private int yieldCount;
        private ResourceMonitorDriver instance;
        private RequestedConfiguration configuration;
        private IEnumerator<object> current;
        private TimeSpan lastRescanRequested;
        private readonly MyCommandLine commandLine = new MyCommandLine();

        public void Main(string argument, UpdateType updateSource)
        {
            Clock.AddTime(Runtime.TimeSinceLastRun);

            if (current == null)
            {
                Debug.ClearBuffer();
            }
            else
            {
                Debug.RestoreBuffer();
            }

            if (commandLine.TryParse(argument))
            {
                var command = commandLine.Argument(0);
                switch (command?.ToLower())
                {
                    case "configure":
                        configuration = new ConfigurationReader().UpdateFromCommandLine(configuration, commandLine.Items.Skip(1));
                        instance = null;
                        break;

                    case "reset":
                        configuration = RequestedConfiguration.GetDefault();
                        instance = null;
                        break;

                    case "rescan":
                        Rescan();
                        break;
                }
            }

            EnsureInitialised();

            if ((updateSource & (UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100)) != 0)
            {
                HandlePeriodicUpdate();
            }
        }

        private void EnsureInitialised()
        {
            if (instance != null) return;

            Debug.Write(Debug.Level.Info, "Configuration updated. Reinitialising...");
            instance = new ResourceMonitorDriver(configuration);
            Rescan();
            current = null;
            yieldCount = 0;
        }

        private void HandlePeriodicUpdate()
        {
            var yielded = Step();

            if (yielded)
            {
                Debug.Assert(current != null, "No current iterator.");
                Debug.Write(Debug.Level.Debug, "Yielding");
                Runtime.UpdateFrequency |= UpdateFrequency.Update1;
                yieldCount++;
            }
            else
            {
                Debug.Assert(current == null, "Current iterator still exists.");
                Runtime.UpdateFrequency &= ~UpdateFrequency.Update1;
                if (yieldCount > 0)
                {
                    Debug.Write(Debug.Level.Info, "Completed in {0} updates.", yieldCount);
                    yieldCount = 0;
                }
                else
                {
                    Debug.Write(Debug.Level.Info, "Completed.");
                }
            }
        }

        private void Rescan()
        {
            lastRescanRequested = Clock.Now;
            instance?.RescanAll();
        }

        private bool Step()
        {
            if (Clock.Now - lastRescanRequested > Constants.BLOCK_RESCAN_INTERVAL)
            {
                Rescan();
            }

            if (current == null)
            {
                current = instance.Run(Runtime.TimeSinceLastRun, GridTerminalSystem);
            }

            var i = Constants.ACTIONS_BEFORE_YIELD;
            while (current.MoveNext())
            {
                i--;
                if (i <= 0) return true;
            }

            current = null;
            return false;
        }
    }
}
