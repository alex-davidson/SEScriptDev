using System.Collections.Generic;

namespace IngameScript
{
    public interface IConfigurationReader<in T>
    {
        bool Read(T configuration, IEnumerator<string> parts);
    }
}
