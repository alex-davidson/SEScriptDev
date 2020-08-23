
using System;
using System.Collections.Generic;

namespace IngameScript
{
    public struct RefineryType
    {
        public readonly string BlockDefinitionName;
        public readonly ICollection<string> SupportedBlueprints;

        public double Efficiency;
        public double Speed;

        public RefineryType(string subTypeName) : this()
        {
            BlockDefinitionName = String.Intern("MyObjectBuilder_Refinery/" + subTypeName);
            Efficiency = 1;
            Speed = 1;
            SupportedBlueprints = new HashSet<string>();
        }

        public bool Supports(Blueprint bp)
        {
            return SupportedBlueprints.Contains(bp.Name);
        }
    }

}
