using System;

namespace IngameScript
{
    public struct ReactorsState
    {
        public float FuelAvailableKg;
        public float EnergyRemainingMWh => (float)FuelAvailableKg / Constants.URANIUM_KG_PER_MWH;
        public float PowerConsumedMW;
        public float MaxPowerDrawMW;
        public int Enabled;

        public int PowerDrawPercent => (int)(100 * PowerConsumedMW / MaxPowerDrawMW);
        public float SecondsRemaining => PowerConsumedMW > 0 ? (3600 * EnergyRemainingMWh / PowerConsumedMW) : float.PositiveInfinity;
    }
}
