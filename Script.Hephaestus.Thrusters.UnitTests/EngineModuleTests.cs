using System;
using System.Collections.Generic;
using IngameScript;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace Script.Hephaestus.Thrusters.UnitTests
{
    [TestFixture]
    public class EngineModuleTests
    {
        public static RotationSpeed[] Speeds =
        {
            Constants.MODULE_TEST_ROTATION_SPEED,
            Constants.MODULE_NORMAL_ROTATION_SPEED,
            Constants.MODULE_EMERGENCY_ROTATION_SPEED,
        };

        [TestCaseSource(nameof(Speeds))]
        public void RotateTo_DoesNotOvershoot(RotationSpeed speed)
        {
            var rotorPair = new MockFacingRotorPair { CurrentAngleDegrees = 20 };

            var module = new EngineModule("Test", rotorPair, new RotorLimits(0, 130), new IMyThrust[0]);

            // 10% frame jitter.
            const float jitter = RotationSpeed.TimeStepSeconds * 0.1f;
            var random = new Random();

            var operation = module.RotateTo(90, speed);
            var rotorVelocities = new List<float>();
            // First iteration performs setup, so the clock doesn't need to tick until the next.
            while (operation.MoveNext())
            {
                rotorVelocities.Add(rotorPair.TargetVelocityDegreesPerSecond);
                // Accuracy ranges:
                if (rotorVelocities.Count < speed.TimeTargetSeconds * 6)
                {
                    // No target within deadline.
                }
                else if (rotorVelocities.Count < speed.TimeTargetSeconds * 12)
                {
                    // After deadline, expect to be within 5 degrees.
                    Assert.That(rotorPair.CurrentAngleDegrees, Is.EqualTo(90).Within(5f));
                }
                else 
                {
                    // After double deadline, expect to be within 0.1 degrees.
                    Assert.That(rotorPair.CurrentAngleDegrees, Is.EqualTo(90).Within(0.1f));
                }
                if (!operation.Current) break;  // Completed.
                var step = TimeSpan.FromSeconds(MathHelper.Lerp(-jitter, jitter, random.NextDouble()) + RotationSpeed.TimeStepSeconds);
                rotorPair.Step(step);
                Clock.AddTime(step);
            }

            TestContext.WriteLine("Iterations: {0} ({1} sec)", rotorVelocities.Count, rotorVelocities.Count / 6);
            Assert.That(rotorPair.CurrentAngleDegrees, Is.EqualTo(90).Within(0.1f));
            Assert.That(rotorVelocities, Has.None.Negative);
            Assert.That(rotorVelocities, Has.Count.LessThanOrEqualTo(60));
        }
    }
}
