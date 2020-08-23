using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using IngameScript;

namespace Script.RefineryBalance.v2
{
    [TestFixture]
    public class RefineryWorklistTests
    {
        private readonly StaticState staticState = new StaticState(new RequestedConfiguration());

        [Test]
        public void IteratorReturnsOnlyRefineriesAbleToProduceIngotType()
        {
            var worklist = new RefineryWorklist(staticState.OreTypes, staticState.IngotTypes, staticState.RefineryFactory, staticState.Blueprints);
            worklist.Initialise(new List<Refinery> {
                Refinery.Get(Mocks.MockRefinery(), Constants.REFINERY_TYPES.Single(t => t.BlockDefinitionName.EndsWith("LargeRefinery")), 1),
                Refinery.Get(Mocks.MockRefinery(), Constants.REFINERY_TYPES.Single(t => t.BlockDefinitionName.EndsWith("Blast Furnace")), 1),
                Refinery.Get(Mocks.MockRefinery(), Constants.REFINERY_TYPES.Single(t => t.BlockDefinitionName.EndsWith("LargeRefinery")), 1)
            }); 

            IRefineryIterator iterator;
            Assert.True(worklist.TrySelectIngot(new ItemType("Ingot/Gold"), out iterator));

            Assert.True(iterator.CanAllocate());
            Assert.That(iterator.Current.BlockDefinitionString, Does.EndWith("LargeRefinery"));

            iterator.Next();

            Assert.True(iterator.CanAllocate());
            Assert.That(iterator.Current.BlockDefinitionString, Does.EndWith("LargeRefinery"));

            iterator.Next();

            Assert.False(iterator.CanAllocate());
        }

        [Test]
        public void IteratorCannotAllocateWhenNoRefineriesAvailable()
        {
            var worklist = new RefineryWorklist(staticState.OreTypes, staticState.IngotTypes, staticState.RefineryFactory, staticState.Blueprints);
            worklist.Initialise(new List<Refinery>());

            IRefineryIterator iterator;
            Assert.False(worklist.TrySelectIngot(new ItemType("Ingot/Gold"), out iterator));

            Assert.False(iterator.CanAllocate());
        }
    }
}
