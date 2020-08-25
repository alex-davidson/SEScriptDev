using System.Collections.Generic;

namespace IngameScript
{
    public static class Extensions
    {
        public static bool Read<T>(this IConfigurationReader<T> reader, T configuration, IEnumerable<string> parts)
        {
            using (var enumerator = parts.GetEnumerator())
            {
                return reader.Read(configuration, enumerator);
            }
        }
    }
}
