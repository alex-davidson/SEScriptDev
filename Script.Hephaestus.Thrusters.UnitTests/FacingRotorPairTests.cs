using IngameScript;
using Moq;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace Script.Hephaestus.Thrusters.UnitTests
{
    [TestFixture]
    public class FacingRotorPairTests
    {
        [Test]
        public void CurrentAngleDegrees_UsesGovernerAngle()
        {
            var angle = 60;
            var governer = Mock.Of<IMyMotorStator>(m => m.Angle == MathHelper.ToRadians(angle));

            var pair = new FacingRotorPair(governer, AnyValidMotorStator);
            Assert.That(pair.CurrentAngleDegrees, Is.EqualTo(angle).Within(0.0001f));
        }

        [Test]
        public void CurrentAngleDegrees_InterpretsOpposedAngle()
        {
            var angle = 300;
            var opposed = Mock.Of<IMyMotorStator>(m => m.Angle == MathHelper.ToRadians(angle));

            var pair = new FacingRotorPair(null, opposed);
            Assert.That(pair.CurrentAngleDegrees, Is.EqualTo(60).Within(0.0001f));
        }

        [Test]
        public void TargetVelocityDegreesPerSecond_UsesGovernerTargetVelocity()
        {
            var governer = Mock.Of<IMyMotorStator>(m => m.TargetVelocityRPM == 1);

            var pair = new FacingRotorPair(governer, AnyValidMotorStator);
            Assert.That(pair.TargetVelocityDegreesPerSecond, Is.EqualTo(6));
        }

        [Test]
        public void TargetVelocityDegreesPerSecond_InterpretsOpposedTargetVelocity()
        {
            var opposed = Mock.Of<IMyMotorStator>(m => m.TargetVelocityRPM == -1);

            var pair = new FacingRotorPair(null, opposed);
            Assert.That(pair.TargetVelocityDegreesPerSecond, Is.EqualTo(6));
        }

        [Test]
        public void GovernerOutsideLimits_AddsSafetyConcern()
        {
            var governer = MakeOperational(MakeAttached(SameGrid, 
                Mock.Of<IMyMotorStator>(m =>
                    m.Angle == MathHelper.ToRadians(60) &&
                    m.UpperLimitDeg == 100 &&
                    m.LowerLimitDeg == 80)));

            var pair = new FacingRotorPair(governer, AnyValidMotorStator);
            var errors = new Errors();
            pair.CheckState(errors, "Test");
            Assert.That(errors.SafetyConcerns, Is.Not.Empty);
        }

        [Test]
        public void OpposedOutsideLimits_AddsSafetyConcern()
        {
            var opposed = MakeOperational(MakeAttached(SameGrid, 
                Mock.Of<IMyMotorStator>(m =>
                    m.Angle == MathHelper.ToRadians(60) &&
                    m.UpperLimitDeg == 100 &&
                    m.LowerLimitDeg == 80)));

            var pair = new FacingRotorPair(AnyValidMotorStator, opposed);
            var errors = new Errors();
            pair.CheckState(errors, "Test");
            Assert.That(errors.SafetyConcerns, Is.Not.Empty);
        }

        [Test]
        public void BothInsideLimits_DoesNotAddSafetyConcern()
        {
            var governer = MakeOperational(MakeAttached(SameGrid, 
                Mock.Of<IMyMotorStator>(m =>
                    m.Angle == MathHelper.ToRadians(60) &&
                    m.UpperLimitDeg == 100 &&
                    m.LowerLimitDeg == 20)));
            var opposed = MakeOperational(MakeAttached(SameGrid, 
                Mock.Of<IMyMotorStator>(m =>
                    m.Angle == MathHelper.ToRadians(300) &&
                    m.UpperLimitDeg == 340 &&
                    m.LowerLimitDeg == 260)));

            var pair = new FacingRotorPair(governer, opposed);
            var errors = new Errors();
            pair.CheckState(errors, "Test");
            Assert.That(errors.SafetyConcerns, Is.Empty);
        }

        [Test]
        public void MismatchedAngles_AddsSanityCheck()
        {
            var governer = MakeOperational(MakeAttached(SameGrid,
                Mock.Of<IMyMotorStator>(m =>
                    m.Angle == MathHelper.ToRadians(60) &&
                    m.UpperLimitDeg == 100 &&
                    m.LowerLimitDeg == 20)));
            var opposed = MakeOperational(MakeAttached(SameGrid, 
                Mock.Of<IMyMotorStator>(m =>
                    m.Angle == MathHelper.ToRadians(298) &&
                    m.UpperLimitDeg == 340 &&
                    m.LowerLimitDeg == 260)));

            var pair = new FacingRotorPair(governer, opposed);
            var errors = new Errors();
            pair.CheckState(errors, "Test");
            Assert.That(errors.SanityChecks, Is.Not.Empty);
        }

        [Test]
        public void CloselyMatchedAngles_DoesNotAddSanityCheck()
        {
            var governer = MakeOperational(MakeAttached(SameGrid,
                Mock.Of<IMyMotorStator>(m =>
                    m.Angle == MathHelper.ToRadians(60) &&
                    m.UpperLimitDeg == 100 &&
                    m.LowerLimitDeg == 20)));
            var opposed = MakeOperational(MakeAttached(SameGrid, 
                Mock.Of<IMyMotorStator>(m =>
                    m.Angle == MathHelper.ToRadians(299.5) &&
                    m.UpperLimitDeg == 340 &&
                    m.LowerLimitDeg == 260)));

            var pair = new FacingRotorPair(governer, opposed);
            var errors = new Errors();
            pair.CheckState(errors, "Test");
            Assert.That(errors.SanityChecks, Is.Empty);
        }

        [Test]
        public void ExactlyMatchedAngles_DoesNotAddSanityCheck()
        {
            var governer = MakeOperational(MakeAttached(SameGrid,
                Mock.Of<IMyMotorStator>(m =>
                    m.Angle == MathHelper.ToRadians(60.5) &&
                    m.UpperLimitDeg == 100 &&
                    m.LowerLimitDeg == 20)));
            var opposed = MakeOperational(MakeAttached(SameGrid, 
                Mock.Of<IMyMotorStator>(m =>
                    m.Angle == MathHelper.ToRadians(299.5) &&
                    m.UpperLimitDeg == 340 &&
                    m.LowerLimitDeg == 260)));

            var pair = new FacingRotorPair(governer, opposed);
            var errors = new Errors();
            pair.CheckState(errors, "Test");
            Assert.That(errors.SanityChecks, Is.Empty);
        }

        private static IMyMotorStator MakeOperational(IMyMotorStator stator)
        {
            Mock.Get(stator).Setup(s => s.Enabled).Returns(true);
            Mock.Get(stator).Setup(s => s.IsWorking).Returns(true);
            Mock.Get(stator).Setup(s => s.IsFunctional).Returns(true);
            return stator;
        }

        private static IMyMotorStator MakeAttached(IMyCubeGrid topGrid, IMyMotorStator stator)
        {
            Mock.Get(stator).Setup(s => s.IsAttached).Returns(true);
            Mock.Get(stator).Setup(s => s.TopGrid).Returns(topGrid);
            return stator;
        }

        public static IMyMotorStator AnyValidMotorStator => MakeOperational(MakeAttached(SameGrid, Mock.Of<IMyMotorStator>()));
        public static IMyCubeGrid SameGrid { get; } = Mock.Of<IMyCubeGrid>(g => g.EntityId == 42);
    }
}
