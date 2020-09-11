using System;
using System.Collections.Generic;
using System.Linq;
using IngameScript;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace Script.Hephaestus.Thrusters.UnitTests
{
    [TestFixture]
    public class EngineTierTests
    {
        [Test]
        public void RotatesAllModulesUntilComplete()
        {
            var rotorPairs = new []
            {
                new MockFacingRotorPair { CurrentAngleDegrees = 20 },
                new MockFacingRotorPair { CurrentAngleDegrees = 20 },
                new MockFacingRotorPair { CurrentAngleDegrees = 20 },
            };
            var modules = rotorPairs.Select((r, i) => new EngineModule(i.ToString(), r, new RotorLimits(0, 130), new IMyThrust[0])).ToArray();

            var tier = new EngineTier(modules, new [] { new StaticState.EnginePresetDef("Test", 90) }, 0);

            // 10% frame jitter.
            const float jitter = RotationSpeed.TimeStepSeconds * 0.1f;
            var random = new Random();

            Assert.That(tier.ActivatePreset("Test"), Is.True);

            var iterations = 0;
            while (tier.Run())
            {
                var step = TimeSpan.FromSeconds(MathHelper.Lerp(-jitter, jitter, random.NextDouble()) + RotationSpeed.TimeStepSeconds);
                foreach (var rotorPair in rotorPairs) rotorPair.Step(step);
                Clock.AddTime(step);
                iterations++;
            }

            TestContext.WriteLine("Iterations: {0} ({1} sec)", iterations, iterations / 6);
            Assert.That(rotorPairs.Select(r => r.CurrentAngleDegrees), Has.All.EqualTo(90).Within(0.1f));
            Assert.That(iterations, Is.LessThanOrEqualTo(60));
        }

        [Test]
        public void RotatesAllModulesFasterWhenTriggeredTwice()
        {
            var rotorPairs = new[]
            {
                new MockFacingRotorPair { CurrentAngleDegrees = 20 },
                new MockFacingRotorPair { CurrentAngleDegrees = 20 },
                new MockFacingRotorPair { CurrentAngleDegrees = 20 },
            };
            var modules = rotorPairs.Select((r, i) => new EngineModule(i.ToString(), r, new RotorLimits(0, 130), new IMyThrust[0])).ToArray();

            var tier = new EngineTier(modules, new[] { new StaticState.EnginePresetDef("Test", 90) }, 0);

            // 10% frame jitter.
            const float jitter = RotationSpeed.TimeStepSeconds * 0.1f;
            var random = new Random();

            Assert.That(tier.ActivatePreset("Test"), Is.True);

            var iterations = 0;
            while (tier.Run())
            {
                if (iterations == 6)
                {
                    // After 1 second, activate the preset again.
                    Assert.That(tier.ActivatePreset("Test"), Is.True);
                }
                var step = TimeSpan.FromSeconds(MathHelper.Lerp(-jitter, jitter, random.NextDouble()) + RotationSpeed.TimeStepSeconds);
                foreach (var rotorPair in rotorPairs) rotorPair.Step(step);
                Clock.AddTime(step);
                iterations++;
            }

            TestContext.WriteLine("Iterations: {0} ({1} sec)", iterations, iterations / 6);
            Assert.That(rotorPairs.Select(r => r.CurrentAngleDegrees), Has.All.EqualTo(90).Within(0.1f));
            Assert.That(iterations, Is.LessThanOrEqualTo(30));
        }
    }
}
