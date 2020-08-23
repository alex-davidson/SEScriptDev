namespace IngameScript
{
    public class RequestedIngotConfiguration
    {
        public RequestedIngotConfiguration()
        {
            Enable = true;
        }

        public float? StockpileTarget { get; set; }
        public float? StockpileLimit { get; set; }
        public bool Enable { get; set; }

        public RequestedIngotConfiguration Copy()
        {
            return new RequestedIngotConfiguration
            {
                StockpileTarget = StockpileTarget,
                StockpileLimit = StockpileLimit,
                Enable = Enable,
            };
        }
    }
}
