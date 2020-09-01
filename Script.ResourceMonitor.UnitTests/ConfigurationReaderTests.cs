using IngameScript;
using NUnit.Framework;

namespace Script.ResourceMonitor
{
    [TestFixture]
    public class ConfigurationReaderTests
    {
        [Test]
        public void CanIncludeBlockName()
        {
            var configuration = new RequestedConfiguration();
            new ConfigurationReader().Read(configuration, new [] { "include", "Ore" });

            Assert.That(configuration.BlockRules, Is.EqualTo(new [] { new BlockRule { BlockName = "Ore", Include = true } }));
        }

        [Test]
        public void CanExcludeBlockName()
        {
            var configuration = new RequestedConfiguration();
            new ConfigurationReader().Read(configuration, new [] { "exclude", "Ore" });

            Assert.That(configuration.BlockRules, Is.EqualTo(new [] { new BlockRule { BlockName = "Ore", Include = false } }));
        }

        [Test]
        public void CanForgetBlockName()
        {
            var configuration = new RequestedConfiguration
            {
                BlockRules =
                {
                    new BlockRule { BlockName = "A", Include = true },
                    new BlockRule { BlockName = "B", Include = false },
                    new BlockRule { BlockName = "C", Include = true },
                }
            };
            new ConfigurationReader().Read(configuration, new [] { "forget", "B" });

            Assert.That(configuration.BlockRules,
                Is.EqualTo(new []
                {
                    new BlockRule { BlockName = "A", Include = true },
                    new BlockRule(),
                    new BlockRule { BlockName = "C", Include = true },
                }));
        }

        [Test]
        public void ChangingBlockInclusionMovesItToTheEndOfTheRules()
        {
            var configuration = new RequestedConfiguration
            {
                BlockRules =
                {
                    new BlockRule { BlockName = "A", Include = true },
                    new BlockRule { BlockName = "B", Include = false },
                    new BlockRule { BlockName = "C", Include = true },
                }
            };
            new ConfigurationReader().Read(configuration, new [] { "exclude", "B" });

            Assert.That(configuration.BlockRules,
                Is.EqualTo(new []
                {
                    new BlockRule { BlockName = "A", Include = true },
                    new BlockRule(),
                    new BlockRule { BlockName = "C", Include = true },
                    new BlockRule { BlockName = "B", Include = false },
                }));
        }
    }
}
