using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;

namespace Script.RefineryBalance.v2
{
    [TestFixture]
    public partial class Program
    {
        [TestFixture]
        public class IngotWorklistTests
        {
            [Test]
            public void InitialPreferredIngotTypeIsFurthestFromTarget()
            {
                var stockpiles = new IngotStockpiles(new TestIngotDefinitions {
                    { "Ingot/A", 10 },
                    { "Ingot/B", 100 },
                    { "Ingot/C", 20 },
                    { "Ingot/D", 8 }
                });
                stockpiles.UpdateQuantities(new TestIngotQuantities {
                    { "Ingot/A", 5 },   // 50%
                    { "Ingot/B", 10 },  // 10%
                    { "Ingot/C", 8 },   // 40%
                    { "Ingot/D", 12 }   // 150%
                });
                var worklist = stockpiles.GetWorklist(); 

                Assert.That(worklist.Preferred.Ingot.ItemType, Is.EqualTo(new ItemType("Ingot/B")));
            }
        }
    }
}
