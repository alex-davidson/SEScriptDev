using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;
using Moq;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;

namespace Script.RefineryBalance.v2
{
    public partial class Program
    {
        [TestFixture]
        public class RefineryWorklistTests
        {
            private readonly StaticState staticState = new StaticState(new RequestedConfiguration());
         
            private IMyRefinery MockRefinery()
            {
                var mockInventory = new Mock<Sandbox.ModAPI.Interfaces.IMyInventory>();
                mockInventory.Setup(i => i.GetItems()).Returns(new List<IMyInventoryItem>());
                var mockRefinery = new Mock<IMyRefinery>();
                mockRefinery.As<IMyInventoryOwner>().Setup(r => r.GetInventory(It.IsAny<int>())).Returns(mockInventory.Object);
                return mockRefinery.Object;
            }
               
            [Test]
            public void IteratorReturnsOnlyRefineriesAbleToProduceIngotType()
            {
                var worklist = new RefineryWorklist(staticState.OreTypes, staticState.IngotTypes, staticState.RefineryFactory, staticState.Blueprints);
                worklist.Initialise(new List<Refinery> {
                    Refinery.Get(MockRefinery(), REFINERY_TYPES.Single(t => t.BlockDefinitionName.EndsWith("LargeRefinery")), 1),
                    Refinery.Get(MockRefinery(), REFINERY_TYPES.Single(t => t.BlockDefinitionName.EndsWith("Blast Furnace")), 1),
                    Refinery.Get(MockRefinery(), REFINERY_TYPES.Single(t => t.BlockDefinitionName.EndsWith("LargeRefinery")), 1)
                });

                IRefineryIterator iterator;
                Assert.True(worklist.TrySelectIngot(new ItemType("Ingot/Gold"), out iterator));

                Assert.True(iterator.CanAllocate());
                Assert.That(iterator.Current.BlockDefinitionString, Is.StringEnding("LargeRefinery"));

                iterator.Skip();

                Assert.True(iterator.CanAllocate());
                Assert.That(iterator.Current.BlockDefinitionString, Is.StringEnding("LargeRefinery"));

                iterator.Skip();

                Assert.False(iterator.CanAllocate());
            }
        }
    }
}
