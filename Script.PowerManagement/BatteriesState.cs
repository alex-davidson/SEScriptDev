using System;

namespace IngameScript
{
    public struct BatteriesState
    {
        public float MaxEnergyMWh;
        public float EnergyRemainingMWh;
        public float PowerConsumedMW;
        public float PowerDrawMW;
        public float MaxPowerDrawMW;
        public int Charging;

        public int PowerDrawPercent => (int)(100 * PowerDrawMW / MaxPowerDrawMW);
        public float SecondsRemaining => PowerConsumedMW > 0 ? (3600 * EnergyRemainingMWh / PowerConsumedMW) : float.PositiveInfinity;
    }
}
