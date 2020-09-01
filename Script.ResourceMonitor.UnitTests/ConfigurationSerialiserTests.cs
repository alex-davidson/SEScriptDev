using System.Collections.Generic;
using System.Linq;
using IngameScript;
using NUnit.Framework;

namespace Script.ResourceMonitor
{
    [TestFixture]
    public class ConfigurationSerialiserTests
    {
        [Test]
        public void CanRoundtrip()
        {
            var original = new RequestedConfiguration
            {
                BlockRules =
                {
                    new BlockRule { BlockName = "Ore", Include = false },
                },
                Displays =
                {
                    new RequestedDisplayConfiguration
                    {
                        DisplayName = "Components",
                        IncludeCategories = { "Components" },
                    },
                    new RequestedDisplayConfiguration
                    {
                        DisplayName = "Hydrogen",
                        IncludeCategories = { "Hydrogen" },
                    },
                },
            };

            var serialised = new ConfigurationWriter().Serialise(original);
            var roundtripped = new ConfigurationReader().Deserialise(serialised);

            Assert.Multiple(() =>
            {
                Assert.That(roundtripped.BlockRules, Is.EquivalentTo(original.BlockRules));
                Assert.That(roundtripped.Displays, Is.EquivalentTo(original.Displays).Using(new RequestedDisplayConfigurationEqualityComparer()));
            });
        }

        class RequestedDisplayConfigurationEqualityComparer : IEqualityComparer<RequestedDisplayConfiguration>
        {
            public bool Equals(RequestedDisplayConfiguration x, RequestedDisplayConfiguration y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.DisplayName != y.DisplayName) return false;
                if (!x.IncludeCategories.SequenceEqual(y.IncludeCategories)) return false;
                return true;
            }

            public int GetHashCode(RequestedDisplayConfiguration obj)
            {
                return obj.DisplayName.GetHashCode();
            }
        }
    }
}
