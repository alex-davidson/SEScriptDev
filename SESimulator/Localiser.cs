using System.Collections.Generic;
using SESimulator.Data;

namespace SESimulator
{
    public class Localiser
    {
        private readonly Dictionary<string, string> mappings;

        public Localiser(Dictionary<string, string> mappings)
        {
            this.mappings = mappings;
        }

        public string ToString(LocalisableString localisable)
        {
            string value;
            if (mappings.TryGetValue(localisable.RawValue, out value)) return value;
            return localisable.RawValue;
        }
    }
}