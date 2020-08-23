using System;

namespace SESimulator.Data
{
    public struct Id
    {
        public Id(string typeId, string subTypeId) : this()
        {
            if (typeId == null) throw new ArgumentNullException("typeId");
            if (subTypeId == null) throw new ArgumentNullException("subTypeId");
            TypeId = typeId;
            SubTypeId = subTypeId;
        }

        public string TypeId { get; private set; }
        public string SubTypeId { get; private set; }

        public override string ToString() => $"{TypeId}/{SubTypeId}";
    }
}
