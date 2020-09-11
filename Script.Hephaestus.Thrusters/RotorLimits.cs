using System;
using System.Xml.Schema;

namespace IngameScript
{
    public struct RotorLimits
    {
        public float Minimum { get; private set; }
        public float Maximum { get; private set; }

        public static readonly RotorLimits None = new RotorLimits { Minimum = float.MinValue, Maximum = float.MaxValue };

        public RotorLimits(float a, float b)
        {
            var aNorm = NormaliseDegrees(a);
            var bNorm = NormaliseDegrees(b);
            var min = Math.Min(aNorm, bNorm);
            var max = Math.Max(aNorm, bNorm);
            Minimum = IsUnbounded(min) ? float.MinValue : min;
            Maximum = IsUnbounded(max) ? float.MaxValue : max;
        }

        public RotorLimits Opposing()
        {
            return new RotorLimits(OpposedAngleDegrees(Maximum), OpposedAngleDegrees(Minimum));
        }

        public RotorLimits ClampRotation(float currentAngleDegrees, float targetAngleDegrees, float currentAngleSlackDegrees)
        {
            // Current angle may need some slack, if possible, if the rotor is already rotating away from the target angle.
            if (currentAngleDegrees > targetAngleDegrees)
            {
                return new RotorLimits(targetAngleDegrees, currentAngleDegrees + currentAngleSlackDegrees);
            }
            return new RotorLimits(currentAngleDegrees - currentAngleSlackDegrees, targetAngleDegrees);
        }

        public float Clamp(float value)
        {
            value = NormaliseDegrees(value);
            if (!IsUnbounded(Minimum)) value = Math.Max(value, Minimum);
            if (!IsUnbounded(Maximum)) value = Math.Min(value, Maximum);
            return value;
        }

        public float ClampDelta(float value, float delta)
        {
            if (delta == 0) return value;
            if (delta > 0)
            {
                // Clockwise.
                var total = value + delta;
                if (value > Maximum)
                {
                    // Already out of bounds and didn't wrap around.
                    if (total < 360) return total;
                    total -= 360;
                    // If it wraps twice, it hits the upper limit.
                }
                if (total > Maximum) return Maximum;
                return NormaliseDegrees(total);
            }
            else
            {
                // Anticlockwise.
                var total = value + delta;
                if (value < Minimum)
                {
                    // Already out of bounds and didn't wrap around.
                    if (total >= 0) return total;
                    total += 360;
                    // If it wraps twice, it hits the lower limit.
                }
                if (total < Minimum) return Minimum;
                return NormaliseDegrees(total);
            }
        }

        public static float NormaliseDegrees(float angleDegrees)
        {
            if (IsUnbounded(angleDegrees)) return angleDegrees;
            var remainder = angleDegrees % 360;
            if (remainder < 0) remainder += 360;
            return remainder;
        }

        public static float OpposedAngleDegrees(float angleDegrees)
        {
            if (IsUnbounded(angleDegrees)) return angleDegrees;
            return NormaliseDegrees(360 - angleDegrees);
        }

        private static bool IsUnbounded(float angleDegrees)
        {
            if (angleDegrees >= float.MaxValue - float.Epsilon) return true;
            if (angleDegrees <= float.MinValue + float.Epsilon) return true;
            return false;
        }

        public override string ToString() => $"[{Minimum}, {Maximum}]";
    }
}
