namespace IngameScript
{
    public struct RotationSpeed
    {
        /// <summary>
        /// Target time step is 10 ticks, or 1/6th of a second.
        /// </summary>
        public const float TimeStepSeconds = 1f/6f;
        /// <summary>
        /// Affects actual angular acceleration, depending on remaining rotation distance.
        /// </summary>
        public float SpringConstant { get; set; }
        /// <summary>
        /// Intended limit on total rotation time.
        /// </summary>
        public float TimeTargetSeconds { get; set; }

        public override string ToString() => $"Spring: {SpringConstant}   Time: {TimeTargetSeconds} sec";
    }
}
