using System;
using System.Collections.Generic;
using System.Linq;
using IngameScript;
using Moq;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace Script.Hephaestus.Drills.UnitTests
{
    [TestFixture]
    public class PistonTopologyTests
    {
        [Test]
        public void CollectsSingleStack()
        {
            var baseGrid = MockGrid("Base");
            var topGrid = MockGrid("Top");
            var pistons = MockPistonsBetween(baseGrid, topGrid, 10).ToList();
            var stacks = new PistonTopology().GetPistonStacks(pistons);

            Assert.That(stacks, Has.Length.EqualTo(1));
            var stack = stacks.Single();

            Assert.That(stack.Total, Is.EqualTo(10));
            Assert.That(stack.BaseGrid, Is.EqualTo(baseGrid));
            Assert.That(stack.TopGrid, Is.EqualTo(topGrid));
        }

        [Test]
        public void CollectsTwoSeparateStacks()
        {
            var baseGrid = MockGrid("Base");
            var topGrid = MockGrid("Top");
            var pistons = MockPistonsBetween(baseGrid, topGrid, 10)
                .Concat(MockPistonsBetween(baseGrid, topGrid, 8))
                .ToList();
            var stacks = new PistonTopology().GetPistonStacks(pistons);

            Assert.That(stacks, Has.Length.EqualTo(2));
            Assert.That(stacks.Select(s => s.Total), Is.EquivalentTo(new [] { 10, 8 }));
        }

        [Test]
        public void CollectsThreeStacksSupportingSingleStack()
        {
            var baseGrid = MockGrid("Base");
            var middleGrid = MockGrid("Middle");
            var topGrid = MockGrid("Top");
            var pistons = MockPistonsBetween(baseGrid, middleGrid, 10)
                .Union(MockPistonsBetween(baseGrid, middleGrid, 10))
                .Union(MockPistonsBetween(baseGrid, middleGrid, 10))
                .Union(MockPistonsBetween(middleGrid, topGrid, 10))
                .ToList();
            var stacks = new PistonTopology().GetPistonStacks(pistons);

            Assert.That(stacks, Has.Length.EqualTo(4));
            Assert.That(stacks.Select(s => s.Total), Is.EquivalentTo(new[] { 10, 10, 10, 10 }));

            Assert.That(stacks.Where(s => s.BaseGrid == baseGrid).ToArray(), Has.Length.EqualTo(3));
            Assert.That(stacks.Where(s => s.BaseGrid == middleGrid).ToArray(), Has.Length.EqualTo(1));
        }

        private IMyCubeGrid MockGrid(string name) => Mock.Of<IMyCubeGrid>(g => g.CustomName == name);

        private IMyPistonBase MockPistonBetween(IMyCubeGrid baseGrid, IMyCubeGrid topGrid) =>
            Mock.Of<IMyPistonBase>(p => p.CubeGrid == baseGrid && p.TopGrid == topGrid);

        private IMyPistonBase MockPistonBetween(IMyCubeGrid baseGrid, IMyPistonBase piston) =>
            Mock.Of<IMyPistonBase>(p => p.CubeGrid == baseGrid && p.TopGrid == piston.CubeGrid);

        private IMyPistonBase MockPistonBetween(IMyPistonBase piston, IMyCubeGrid topGrid) =>
            Mock.Of<IMyPistonBase>(p => p.CubeGrid == piston.TopGrid && p.TopGrid == topGrid);

        private IEnumerable<IMyPistonBase> MockPistonsBetween(IMyCubeGrid baseGrid, IMyCubeGrid topGrid, int count)
        {
            if (count == 0) throw new ArgumentOutOfRangeException();
            if (count == 1)
            {
                yield return MockPistonBetween(baseGrid, topGrid);
                yield break;
            }
            var intermediateGrid = Mock.Of<IMyCubeGrid>();
            yield return MockPistonBetween(baseGrid, intermediateGrid);
            foreach (var piston in MockPistonsBetween(intermediateGrid, topGrid, count - 1))
            {
                yield return piston;
            }
        }
    }
}
