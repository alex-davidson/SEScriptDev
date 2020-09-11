using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public struct EngineTierBuilder
    {
        private readonly IMyGridTerminalSystem gridTerminalSystem;
        private static readonly List<IMyThrust> local_static_thrusters = new List<IMyThrust>(100);
        private readonly ILookup<long, IMyThrust> thrustersByGrid;
        private static readonly IMyThrust[] NoThrusters = new IMyThrust[0];

        public EngineTierBuilder(IMyGridTerminalSystem gridTerminalSystem)
        {
            this.gridTerminalSystem = gridTerminalSystem;
            gridTerminalSystem.GetBlocksOfType(local_static_thrusters);
            thrustersByGrid = local_static_thrusters.ToLookup(t => t.CubeGrid.EntityId);
        }

        public EngineTier Build(StaticState.EngineTierDef definition)
        {
            var hash = 0L;
            var modules = new EngineModule[definition.Modules.Length];

            var limits = new RotorLimits(definition.Presets.Min(p => p.Angle), definition.Presets.Max(p => p.Angle));
            AddToHash(ref hash, limits.GetHashCode());

            for (var i = 0; i < definition.Modules.Length; i++)
            {
                modules[i] = BuildModule(definition.Modules[i], limits, ref hash);
            }
            return new EngineTier(modules, definition.Presets, hash);
        }

        private EngineModule BuildModule(StaticState.EngineModuleDef definition, RotorLimits limits, ref long hash)
        {
            var governing = gridTerminalSystem.GetBlockWithName(definition.GoverningRotorName) as IMyMotorStator;
            var opposing = gridTerminalSystem.GetBlockWithName(definition.OpposingRotorName) as IMyMotorStator;

            AddToHash(ref hash, governing?.EntityId ?? 0);
            AddToHash(ref hash, opposing?.EntityId ?? 0);

            var thrusters = GetThrusters(governing, opposing);

            return new EngineModule(definition.Name, new FacingRotorPair(governing, opposing), limits, thrusters);
        }

        private IMyThrust[] GetThrusters(IMyMotorStator governing, IMyMotorStator opposing)
        {
            var governingGridId = governing?.TopGrid?.EntityId;
            var opposingGridId = opposing?.TopGrid?.EntityId;

            if (governingGridId == opposingGridId)
            {
                if (governingGridId != null) return thrustersByGrid[governingGridId.Value].ToArray();
                return NoThrusters;
            }
            if (governingGridId == null) return thrustersByGrid[opposingGridId.Value].ToArray();
            if (opposingGridId == null) return thrustersByGrid[governingGridId.Value].ToArray();
            // Attached to different grids.
            return NoThrusters;
        }

        private static void AddToHash(ref long hash, long component) => hash = (hash  * 397) ^ component;
    }
}
