namespace SESimulator.Data
{
    /// <summary>
    /// A conversion from a set of input stacks to an output stack.
    /// </summary>
    public class Blueprint : Thing
    {
        public ItemStack[] Inputs { get; set; }
        public ItemStack Output { get; set; }
        public decimal BaseProductionTimeInSeconds {get;set;}
    }
}
