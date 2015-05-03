namespace SESimulator.Data
{
    /// <summary>
    /// A single type of item and an amount.
    /// </summary>
    public class ItemStack
    {
        public Id ItemId { get; set; }
        public decimal Amount { get; set; }
    }
}