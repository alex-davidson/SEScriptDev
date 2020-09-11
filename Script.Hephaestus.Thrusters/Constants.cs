namespace IngameScript
{
    public static partial class Constants
    {
        public const int MODULE_TEST_DEFLECTION_DEGREES = 2;
        public static readonly RotationSpeed MODULE_TEST_ROTATION_SPEED = new RotationSpeed { SpringConstant = 10f, TimeTargetSeconds = 3f };
        public static readonly RotationSpeed MODULE_NORMAL_ROTATION_SPEED = new RotationSpeed { SpringConstant = 1.2f, TimeTargetSeconds = 5f };
        public static readonly RotationSpeed MODULE_EMERGENCY_ROTATION_SPEED = new RotationSpeed { SpringConstant = 15f, TimeTargetSeconds = 2f, };

        public const float FACING_ROTOR_DISPLACEMENT = -0.2f;
    }
}
