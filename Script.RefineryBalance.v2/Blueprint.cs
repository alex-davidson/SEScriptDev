namespace IngameScript
{
    public struct Blueprint
    {
        public readonly string Name;
        public readonly float Duration;
        public readonly ItemAndQuantity Input;
        public readonly ItemAndQuantity[] Outputs;

        public Blueprint(string name, float duration, ItemAndQuantity input, params ItemAndQuantity[] outputs) : this()
        {
            Name = name;
            Duration = duration;
            Input = input;
            Outputs = outputs;
        }
    }

}
