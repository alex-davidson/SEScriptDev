using System;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        public Program()
        {
            Debug.Initialise(Debug.Level.Info, Echo);
            RefineryDriver.StaticInitialise();

            configuration = new ConfigurationReader().Deserialise(Storage);

            Runtime.UpdateFrequency = Constants.EXPECTED_UPDATE_MODE;
        }

        public void Save()
        {
            Storage = new ConfigurationWriter().Serialise(configuration);
        }

        private int yieldCount;
        private RefineryDriver instance;
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
                        configuration = new RequestedConfiguration();
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
            var staticState = new StaticState(configuration);
            instance = new RefineryDriver(staticState);
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
                    Debug.Write(Debug.Level.Info, new Message("Completed in {0} updates.", yieldCount));
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
