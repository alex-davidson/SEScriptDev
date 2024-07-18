using System;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const string ValveBlockGroup = "Valves";
        const string ValveNamePart = "(Valve)";
        const int RescanIntervalSeconds = 180;
        const int UpdateIntervalSeconds = 5;


        public Program()
        {
            Debug.Initialise(Debug.Level.Info, Echo);

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            state = new State { PendingRescan = true };
        }

        public void Main(string argument, UpdateType updateSource)
        {
            HandlePeriodicUpdate();
            // Run every tick if we're processing an action, otherwise every 100 ticks.
            Runtime.UpdateFrequency = state.CurrentAction != null ? UpdateFrequency.Update1 : UpdateFrequency.Update100;
        }

        private readonly State state;

        private void HandlePeriodicUpdate()
        {
            Clock.AddTime(Runtime.TimeSinceLastRun);

            if (state.CurrentAction == null)
            {
                // No current action.
                if (state.PendingRescan)
                {
                    Debug.Write(Debug.Level.Info, "Scanning for valves");
                    state.CurrentAction = ScanValves();
                }
                else if (state.LastUpdate.GetElapsedSeconds() > UpdateIntervalSeconds)
                {
                    Debug.Write(Debug.Level.Info, new Message("Updating {0} valves", state.Valves.Count));
                    state.CurrentAction = UpdateValves();
                }
                else
                {
                    Debug.Write(Debug.Level.Debug, new Message("Waiting for update: {0}", state.LastUpdate.GetElapsedSeconds()));
                }
            }
            if (state.CurrentAction != null)
            {
                // If we're processing an action, pump it once.
                if (!state.CurrentAction.MoveNext())
                {
                    // Completed. Clear it.
                    state.CurrentAction = null;
                    Debug.Write(Debug.Level.Info, "Completed.");
                }
                else
                {
                    Debug.Write(Debug.Level.Debug, "Yielding");
                }
            }
        }

        private IEnumerator<object> ScanValves()
        {
            // Gather candidate blocks.
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(ValveNamePart, blocks, block => block is IMyShipConnector);
            GridTerminalSystem.GetBlockGroupWithName(ValveBlockGroup)?.GetBlocks(blocks, block => block is IMyShipConnector);

            state.Valves.Clear();
            Debug.Write(Debug.Level.Debug, new Message("Found blocks: {0}", blocks.Count));

            yield return null;

            var byName = blocks.GroupBy(x => x.CustomName).ToArray();

            var i = 4;

            var connectors = new List<IMyShipConnector>(5);
            foreach (var group in byName)
            {
                connectors.Clear();
                connectors.AddRange(group.OfType<IMyShipConnector>());
                if (connectors.Count != 4)
                {
                    Debug.Write(Debug.Level.Debug, new Message("Incorrect number of connectors ({1}) in valve: {0}", group.Key, connectors.Count));
                    continue;
                }

                Pair firstPair, secondPair;
                if (!TryExtractPair(group.Key, connectors, out firstPair)) continue;
                if (!TryExtractPair(group.Key, connectors, out secondPair)) continue;
                if (connectors.Count != 0)
                {
                    // Should be impossible.
                    Debug.Write(Debug.Level.Debug, new Message("Incorrect number of connectors ({1}) remaining in valve after pairs identified: {0}", group.Key, connectors.Count));
                    continue;
                }

                state.Valves.Add(new Valve { Name = group.Key, First = firstPair, Second = secondPair });

                // Yield periodically.
                i++;
                if (i % 8 == 0) yield return null;
            }

            state.LastRescan.Reset();
            state.PendingRescan = false;
        }

        private static bool TryExtractPair(string name, List<IMyShipConnector> connectors, out Pair pair)
        {
            if (connectors.Count < 2) throw new ArgumentException("Need at least two connectors in order to identify a pair.", nameof(connectors));

            pair = default(Pair);
            if (connectors.Any(x => x.Status == MyShipConnectorStatus.Unconnected))
            {
                Debug.Write(Debug.Level.Debug, new Message("Unable to identify connector pairs for valve: {0}", name));
                return false;
            }
            if (connectors.Any(x => x.CollectAll))
            {
                Debug.Write(Debug.Level.Debug, new Message("One or more connectors is misconfigured to Collect All, for valve: {0}", name));
            }

            var first = connectors[0];
            var otherConnector = first.OtherConnector;
            if (otherConnector == null)
            {
                first.Connect();
                otherConnector = first.OtherConnector;
                // Undo any changes we make.
                first.Disconnect();
                if (otherConnector == null)
                {
                    Debug.Write(Debug.Level.Debug, new Message("Unable to identify connector pairs for valve: {0}", name));
                    return false;
                }
            }
            if (!connectors.Contains(otherConnector))
            {
                Debug.Write(Debug.Level.Debug, new Message("Connector did not connect to another member of the valve: {0}", name));
                return false;
            }
            pair = new Pair { A = first, B = otherConnector };
            connectors.Remove(otherConnector);
            connectors.Remove(first);
            return true;
        }

        private IEnumerator<object> UpdateValves()
        {
            var i = 0;
            foreach (var valve in state.Valves)
            {
                if (!valve.Update())
                {
                    // On any error, rescan.
                    state.PendingRescan = true;
                }
                // Yield periodically.
                i++;
                if (i % 8 == 0) yield return null;
            }
            state.LastUpdate.Reset();
            // Rescan every few minutes.
            if (state.LastRescan.GetElapsedSeconds() > RescanIntervalSeconds)
            {
                state.PendingRescan = true;
            }
        }

        struct Pair
        {
            public IMyShipConnector A;
            public IMyShipConnector B;
        }

        struct Valve
        {
            public string Name;
            public Pair First;
            public Pair Second;

            public bool Update()
            {
                if (First.A.IsConnected)
                {
                    if (!Disconnect(First)) return false;
                    if (!Connect(Second)) return false;
                    return true;
                }
                if (Second.A.IsConnected)
                {
                    if (!Disconnect(Second)) return false;
                    if (!Connect(First)) return false;
                    return true;
                }
                return Connect(First);
            }

            private bool Connect(Pair pair)
            {
                if (pair.A.IsConnected) return CheckConnection(pair);

                pair.A.Connect();

                if (!CheckConnection(pair))
                {
                    // Undo the change.
                    pair.A.Disconnect();
                    return false;
                }
                return true;
            }

            private bool Disconnect(Pair pair)
            {
                if (!CheckConnection(pair)) return false;

                pair.A.Disconnect();

                return true;
            }

            private bool CheckConnection(Pair pair)
            {
                if (!pair.A.IsConnected) return true;
                if (pair.A.OtherConnector != pair.B)
                {
                    Debug.Write(Debug.Level.Debug, new Message("Valve connected to unexpected block: {0}", Name));
                    return false;
                }
                return true;
            }
        }

        class State
        {
            public IEnumerator<object> CurrentAction;
            public readonly List<Valve> Valves = new List<Valve>();
            public readonly Stopwatch LastRescan = new Stopwatch();
            public readonly Stopwatch LastUpdate = new Stopwatch();
            public bool PendingRescan;
        }
    }
}
