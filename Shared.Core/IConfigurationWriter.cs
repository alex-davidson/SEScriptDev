using System.Collections.Generic;

namespace IngameScript
{
    public interface IConfigurationWriter<in T>
    {
        IEnumerable<string> GenerateParts(T configuration);
    }
}