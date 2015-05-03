namespace SESimulator.Data
{
    public struct LocalisableString
    {
        public LocalisableString(string value) : this()
        {
            this.RawValue = value;
        }

        public string RawValue {get; private set;}
    }
}