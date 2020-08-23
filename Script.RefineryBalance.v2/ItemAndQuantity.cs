namespace IngameScript
{
    public struct ItemAndQuantity
    {
        public readonly ItemType ItemType;
        public readonly double Quantity;

        public ItemAndQuantity(string typePath, double consumed) : this()
        {
            ItemType = new ItemType(typePath);
            Quantity = consumed;
        }
    }

}
