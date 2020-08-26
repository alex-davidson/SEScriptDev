﻿using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public class BlockCollector
    {
        private readonly IList<string> inventoryBlockNames;

        public BlockCollector(IList<string> inventoryBlockNames)
        {
            this.inventoryBlockNames = inventoryBlockNames;
        }

        public void CollectUniqueContainers(List<IMyEntity> containers, IMyGridTerminalSystem gts)
        {
            if (inventoryBlockNames.Count == 0)
            {
                gts.GetBlocksOfType(containers, b => b.HasInventory);
            }
            else
            {
                var blocks = new List<IMyTerminalBlock>();
                for (var i = 0; i < inventoryBlockNames.Count; i++)
                {
                    gts.SearchBlocksOfName(inventoryBlockNames[i], blocks);
                }
                containers.AddRange(blocks.OfType<IMyEntity>().Distinct());
            }
        }

        /// <summary>
        /// Find all refineries on-grid which are enabled and configured for script management, ie.
        /// not automatically pulling from conveyors.
        /// </summary>
        public void CollectParticipatingRefineries(SystemState state, IMyGridTerminalSystem gts, List<Refinery> refineries)
        {
            var blocks = new List<IMyRefinery>();
            gts.GetBlocksOfType(blocks);
            var refinerySpeedFactor = state.RefinerySpeedFactor;
            for (var i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (!block.IsOperational()) continue;
                if (block.UseConveyorSystem) continue;

                var refinery = state.Static.RefineryFactory.TryResolveRefinery(block, refinerySpeedFactor);
                if (refinery == null)
                {
                    Debug.Write(Debug.Level.Warning, "Unrecognised refinery type: {0}", block.BlockDefinition);
                }
                else
                {
                    refineries.Add(refinery);
                }
            }
        }

        /// <summary>
        /// Find all assemblers on-grid which are enabled and pulling from conveyors.
        /// </summary>
        public float GetTotalParticipatingAssemblerSpeed(IMyGridTerminalSystem gts)
        {
            // ASSUMPTION: Only assembler type in the game has a base speed of 1.
            const float assemblerSpeed = 1;

            var blocks = new List<IMyAssembler>();
            gts.GetBlocksOfType(blocks);
            var totalSpeed = 0f;
            for (var i = 0; i < blocks.Count; i++)
            {
                var assembler = blocks[i];
                if (!assembler.IsOperational()) continue;
                if (!assembler.UseConveyorSystem) continue;

                totalSpeed += assemblerSpeed;
                var moduleBonuses = SupportUtil.ParseModuleBonuses(assembler);
                if (moduleBonuses.Count > 0)
                {
                    var speedModifier = moduleBonuses[0] - 1; // +1 Speed per 100%.
                    totalSpeed += speedModifier;
                }
            }
            return totalSpeed;
        }
    }
}