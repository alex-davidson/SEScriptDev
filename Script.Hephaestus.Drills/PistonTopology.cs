using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public struct PistonTopology
    {
        /// <summary>
        /// Given a set of pistons, group them into stacks.
        /// </summary>
        /// <remarks>
        /// Performs many allocations. Do not use frequently.
        /// </remarks>
        public PistonStack[] GetPistonStacks(ICollection<IMyPistonBase> pistons)
        {
            // A 'stack' consists of a chain of single pistons.
            // Four stacks might be configured as:
            // * 3 stacks supporting a grid supporting a single stack.
            // * 4 stacks supporting one grid.
            // * 2 stacks supporting a grid supporting 2 more stacks.
            // etc etc
            // Therefore:
            // * a base on the same grid as multiple tops may be the start of a stack.
            // * a base on the same grid as other bases may be the start of a stack.
            // * a base on a grid with no tops may be the start of a stack.

            var pistonsByBaseGrid = pistons.ToLookup(p => p.CubeGrid);
            var pistonsByTopGrid = pistons.ToLookup(p => p.TopGrid);
            var allGrids = pistons.Select(g => g.CubeGrid)
                .Union(pistons.Select(g => g.TopGrid))
                .ToArray();
            var midStackGrids = new HashSet<IMyCubeGrid>(
                allGrids
                    .Where(g => pistonsByBaseGrid[g].Count() == 1)
                    .Where(g => pistonsByTopGrid[g].Count() == 1));

            var stackBasePistons = allGrids.Except(midStackGrids).SelectMany(g => pistonsByBaseGrid[g]);

            var collector = new List<IMyPistonBase>();
            var stacks = new List<PistonStack>();
            foreach (var basePiston in stackBasePistons)
            {
                collector.Add(basePiston);
                var piston = basePiston;
                while (midStackGrids.Contains(piston.TopGrid))
                {
                    piston = pistonsByBaseGrid[piston.TopGrid].Single();
                    collector.Add(piston);
                }

                stacks.Add(new PistonStack(collector.ToArray()));
                collector.Clear();
            }

            return stacks.ToArray();
        }
    }
}
