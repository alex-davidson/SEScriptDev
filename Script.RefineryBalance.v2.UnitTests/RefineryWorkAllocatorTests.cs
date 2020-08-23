using IngameScript;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using VRage;

namespace Script.RefineryBalance.v2
{
    [TestFixture]
    public class RefineryWorkAllocatorTests
    {
        private readonly StaticState staticState = new StaticState(new RequestedConfiguration());

        [Test]
        public void AllocatesWork()
        {
            var state = new SystemState(staticState);

            var worklist = new RefineryWorklist(staticState.OreTypes, staticState.IngotTypes, staticState.RefineryFactory, staticState.Blueprints);
            worklist.Initialise(new List<Refinery> {
                Refinery.Get(Mocks.MockRefinery(), Constants.REFINERY_TYPES.Single(t => t.BlockDefinitionName.EndsWith("LargeRefinery")), 1),
            });
            var ingotWorklist = new IngotWorklist(state.Ingots);

            var inventoryScanner = new InventoryScanner(staticState.IngotTypes.AllIngotItemTypes, staticState.OreTypes.All);

            inventoryScanner.Ore[new ItemType("Ore/Iron")] = new List<OreDonor> { CreateOreDonor(new ItemType("Ore/Iron"), 4000) };

            state.Ingots.UpdateQuantities(new Dictionary<ItemType, double>());  // No existing ingots.
            ingotWorklist.Initialise();

            var refineryWorkAllocator = new RefineryWorkAllocator(worklist, inventoryScanner.Ore);

            var allocated = refineryWorkAllocator.AllocateSingle(ingotWorklist);

            Assert.True(allocated);
        }

        private OreDonor CreateOreDonor(ItemType itemType, double quantity)
        {
            var inventory = Mock.Of<MockInventoryBase>();
            inventory.Add(itemType, (MyFixedPoint)quantity);
            return new OreDonor(inventory, inventory.GetItemAt(0).Value.ItemId);
        }
    }
}
