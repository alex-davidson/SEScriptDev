using System;

namespace IngameScript
{
    /// <summary>
    /// Describes the type of a PhysicalItem or Component in a form which can be used as a dictionary key.
    /// </summary>
    public struct ItemType
    {
        public readonly string TypeId;
        public readonly string SubtypeId;

        /// <summary>
        /// Creates an item type from a raw TypeId and SubtypeId, eg. MyObjectBuilder_Ore, Iron
        /// </summary>
        public ItemType(string typeId, string subtypeId)
            : this()
        {
            TypeId = String.Intern(typeId);
            SubtypeId = String.Intern(subtypeId);
        }

        /// <summary>
        /// Creates an item type from a human-readable item 'path', eg. Ore/Iron
        /// </summary>
        public ItemType(string typePath)
            : this()
        {
            var pathParts = typePath.Split('/');
            if (pathParts.Length != 2) throw new Exception("Path is not of the format TypeId/SubtypeId: " + typePath);
            var typeId = pathParts[0];
            SubtypeId = String.Intern(pathParts[1]);
            if (typeId == "" || SubtypeId == "") throw new Exception("Not a valid path: " + typePath);
            TypeId = GetActualTypeId(typeId);
        }

        private const string prefix = "MyObjectBuilder_";

        private string GetActualTypeId(string abbreviated)
        {
            return String.Intern(prefix + abbreviated);
        }

        public string ToShortString()
        {
            if (!TypeId.StartsWith(prefix)) return ToString();
            return TypeId.Substring(prefix.Length) + "/" + SubtypeId;
        }

        public override string ToString()
        {
            return TypeId + "/" + SubtypeId;
        }
    }

}
