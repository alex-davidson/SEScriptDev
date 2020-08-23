namespace IngameScript
{
    public struct IngotType
    {
        public readonly ItemType ItemType;
        public readonly double ConsumedPerSecond;

        public IngotType(string typePath, double consumedPerSecond)
            : this()
        {
            ItemType = new ItemType(typePath);
            ConsumedPerSecond = consumedPerSecond;
            Enabled = true;
        }

        public double ProductionNormalisationFactor;
        public bool Enabled;
        public double? StockpileTargetOverride;
        public double? StockpileLimit;
    }

}
