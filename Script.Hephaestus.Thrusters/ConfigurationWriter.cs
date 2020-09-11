using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public class ConfigurationWriter : IConfigurationWriter<RequestedConfiguration>
    {
        public IEnumerable<string> GenerateParts(RequestedConfiguration configuration)
        {
            if (!string.IsNullOrWhiteSpace(configuration.TestedHash))
            {
                yield return "tested";
                yield return configuration.TestedHash;
            }
        }
    }
}
