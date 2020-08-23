using System.Collections.Generic;
using IngameScript;
using NUnit.Framework;

namespace Script.RefineryBalance.v2
{
    [TestFixture]
    public class ConfigurationSerialiserTests
    {
        [Test]
        public void CanRoundtrip()
        {
            var original = new RequestedConfiguration
            {
                Ingots =
                {
                    [new ItemType("Ingot/Iron")] = new RequestedIngotConfiguration { Enable = true, StockpileTarget = 500, StockpileLimit = 1000 },
                    [new ItemType("Ingot/Platinum")] = new RequestedIngotConfiguration { Enable = false, StockpileLimit = 200 },
                    [new ItemType("Ore/Test")] = new RequestedIngotConfiguration { Enable = true, StockpileTarget = 700 },
                },
                InventoryBlockNames =
                {
                    "Storage",
                    "Buffer",
                },
                OreStatusDisplayName = "Status: \"Ore\"",
                IngotStatusDisplayName = "IngotStatus",
                RefinerySpeedFactor = 5,
                AssemblerSpeedFactor = 4,
            };

            var serialised = new ConfigurationSerialiser().Serialise(original);
            var roundtripped = new ConfigurationSerialiser().Deserialise(serialised);

            Assert.Multiple(() =>
            {
                Assert.That(roundtripped.Ingots, Is.EquivalentTo(original.Ingots).Using(new RequestedIngotConfigurationEqualityComparer()));
                Assert.That(roundtripped.InventoryBlockNames, Is.EquivalentTo(original.InventoryBlockNames));
                Assert.That(roundtripped.OreStatusDisplayName, Is.EqualTo(original.OreStatusDisplayName));
                Assert.That(roundtripped.IngotStatusDisplayName, Is.EqualTo(original.IngotStatusDisplayName));
                Assert.That(roundtripped.RefinerySpeedFactor, Is.EqualTo(original.RefinerySpeedFactor));
                Assert.That(roundtripped.AssemblerSpeedFactor, Is.EqualTo(original.AssemblerSpeedFactor));
            });
        }

        class RequestedIngotConfigurationEqualityComparer : IEqualityComparer<RequestedIngotConfiguration>
        {
            public bool Equals(RequestedIngotConfiguration x, RequestedIngotConfiguration y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.Enable != y.Enable) return false;
                if (x.StockpileTarget != y.StockpileTarget) return false;
                if (x.StockpileLimit != y.StockpileLimit) return false;
                return true;
            }

            public int GetHashCode(RequestedIngotConfiguration obj)
            {
                return 0;
            }
        }
    }
}
