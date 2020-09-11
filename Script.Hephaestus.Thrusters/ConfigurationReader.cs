using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public class ConfigurationReader : IConfigurationReader<RequestedConfiguration>
    {
        public bool Read(RequestedConfiguration configuration, IEnumerable<string> parts)
        {
            using (var enumerator = parts.GetEnumerator())
            {
                return Read(configuration, enumerator);
            }
        }

        public bool Read(RequestedConfiguration configuration, IEnumerator<string> parts)
        {
            while (parts.MoveNext())
            {
                switch (parts.Current?.ToLower())
                {
                    case "tested":
                        if (!ExpectNext(parts, "Expected a hash to follow 'tested'.")) return false;
                        configuration.TestedHash = parts.Current;
                        break;

                    default:
                        Debug.Write(Debug.Level.Error, "Unrecognised parameter: {0}", parts.Current);
                        return false;
                }
            }
            return true;
        }

        private static bool ExpectNext(IEnumerator<string> parts, string error)
        {
            if (parts.MoveNext()) return true;
            Debug.Write(Debug.Level.Error, error);
            return false;
        }
    }
}
