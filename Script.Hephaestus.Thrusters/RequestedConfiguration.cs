using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public class RequestedConfiguration : IDeepCopyable<RequestedConfiguration>
    {
        public string TestedHash { get; set; }

        public RequestedConfiguration Copy()
        {
            return new RequestedConfiguration
            {
                TestedHash = TestedHash,
            };
        }

        public static RequestedConfiguration GetDefault()
        {
            return new RequestedConfiguration();
        }
    }
}
