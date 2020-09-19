using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace IngameScript
{
    public class DrillTowerBuilder
    {
        private readonly IMyGridTerminalSystem gridTerminalSystem;
        private static readonly List<IMyPistonBase> local_static_pistons = new List<IMyPistonBase>(50);
        private static readonly List<IMyLandingGear> local_static_clamps = new List<IMyLandingGear>(10);
        private static readonly List<IMyShipDrill> local_static_drills = new List<IMyShipDrill>(30);
        private static readonly List<IMyLightingBlock> local_static_floodlights = new List<IMyLightingBlock>(10);

        public DrillTowerBuilder(IMyGridTerminalSystem gridTerminalSystem)
        {
            this.gridTerminalSystem = gridTerminalSystem;
        }

        public DrillTower Build(StaticState.DrillTowerDef definition)
        {
            gridTerminalSystem.GetBlockGroupWithName(definition.PistonGroupName)?.GetBlocksOfType(local_static_pistons);
            gridTerminalSystem.GetBlockGroupWithName(definition.DrillGroupName)?.GetBlocksOfType(local_static_drills);
            gridTerminalSystem.GetBlockGroupWithName(definition.DrillClampsGroupName)?.GetBlocksOfType(local_static_clamps);
            gridTerminalSystem.GetBlocksOfType(local_static_floodlights, b => b.CustomName == definition.FloodlightsName);
            var rotor = gridTerminalSystem.GetBlockWithName(definition.RotorName) as IMyMotorStator;
            var display = gridTerminalSystem.GetBlockWithName(definition.DisplayName) as IMyTextPanel;

            var stacks = new PistonTopology().GetPistonStacks(local_static_pistons);
            var drills = new DrillHead(local_static_drills.ToArray());
            var clamps = new ClampGroup(local_static_clamps.ToArray());
            var lights = new LightGroup(local_static_floodlights.ToArray());
            return new DrillTower(stacks, drills, clamps, rotor, lights, display);
        }
    }
}
