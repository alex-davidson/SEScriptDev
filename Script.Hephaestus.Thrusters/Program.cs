using System.Runtime.Remoting.Messaging;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public Program()
        {
            Debug.Initialise(Debug.Level.Info, Echo);

            configuration = new ConfigurationReader().Deserialise(RequestedConfiguration.GetDefault(), Storage);

            Runtime.UpdateFrequency = UpdateFrequency.Update10 | UpdateFrequency.Update100;
        }

        public void Save()
        {
            Storage = new ConfigurationWriter().Serialise(configuration);
        }

        private RequestedConfiguration configuration;
        private readonly MyCommandLine commandLine = new MyCommandLine();

        private State state;
        private readonly DisplayRenderer renderer = new DisplayRenderer();
        private Message commandState = default(Message);
        private readonly Errors commandErrors = new Errors();
        private readonly Errors stateErrors = new Errors();

        public void Main(string argument, UpdateType updateSource)
        {
            Clock.AddTime(Runtime.TimeSinceLastRun);

            EnsureInitialised();

            if (commandLine.TryParse(argument))
            {
                var command = commandLine.Argument(0);
                switch (command?.ToLower())
                {
                    case "test":
                        configuration.TestedHash = state.GetCurrentHash();
                        state.BowEngines.BeginTest();
                        state.MidshipsEngines.BeginTest();
                        commandState = new Message("Running test pattern.");
                        break;

                    case "set":
                        var tier = commandLine.Argument(1);
                        var preset = commandLine.Argument(2) ?? "";
                        switch (tier.ToLower())
                        {
                            case "bow":
                                if (state.BowEngines.ActivatePreset(preset) == false)
                                {
                                    commandErrors.Warnings.Add(new Message("Bow engine preset not recognised: {0}", preset));
                                }
                                else
                                {
                                    commandState = new Message("Move Bow engine to preset: {0}", preset);
                                }
                                break;

                            case "midships":
                                if (state.MidshipsEngines.ActivatePreset(preset) == false)
                                {
                                    commandErrors.Warnings.Add(new Message("Midships engine preset not recognised: {0}", preset));
                                }
                                else
                                {
                                    commandState = new Message("Move Midships engine to preset: {0}", preset);
                                }
                                break;
                        }
                        break;


                    case "stop":
                        state.BowEngines.Stop();
                        state.MidshipsEngines.Stop();
                        commandState = new Message("Stopped.");
                        break;

                    case "rescan":
                        renderer.Rescan(GridTerminalSystem);
                        state.PendingRescan = true;
                        break;

                    case "reset":
                        state.BowEngines.Stop();
                        state.MidshipsEngines.Stop();
                        configuration = RequestedConfiguration.GetDefault();
                        state = null;
                        return;
                }
            }

            stateErrors.Clear();
            if (state.GetCurrentHash() != configuration.TestedHash)
            {
                stateErrors.SafetyConcerns.Add(new Message("Configuration has changed since the last test."));
            }
            state.BowEngines.CheckState(stateErrors);
            state.MidshipsEngines.CheckState(stateErrors);

            HandlePeriodicUpdate();

            renderer.Render(stateErrors, commandErrors, commandState);
        }

        private void EnsureInitialised()
        {
            if (state != null) return;

            Debug.Write(Debug.Level.Info, "Configuration updated. Reinitialising...");
            commandErrors.Clear();
            var builder = new EngineTierBuilder(GridTerminalSystem);
            state = new State
            {
                BowEngines = builder.Build(StaticState.Instance.Bow),
                MidshipsEngines = builder.Build(StaticState.Instance.Midships),
            };
            renderer.Rescan(GridTerminalSystem);
        }

        private void HandlePeriodicUpdate()
        {
            var bowRan = state.BowEngines.Run();
            var midshipsRan = state.MidshipsEngines.Run();

            if (bowRan || midshipsRan)
            {
                Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                Debug.Write(Debug.Level.Debug, "Yielding");
            }
            else
            {
                Runtime.UpdateFrequency &= ~UpdateFrequency.Update10;
                Debug.Write(Debug.Level.Info, "Completed.");
                commandState = new Message("Idle.");
                if (state.PendingRescan)
                {
                    state = null;
                }
            }
        }

        class State
        {
            public EngineTier BowEngines { get; set; }
            public EngineTier MidshipsEngines { get; set; }
            public bool PendingRescan { get; set; }

            public string GetCurrentHash()
            {
                return $"{BowEngines.Hash:X16}{MidshipsEngines.Hash:X16}";
            }
        }
    }
}
