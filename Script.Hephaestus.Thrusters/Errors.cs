using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IngameScript
{
    public class Errors
    {
        public List<Message> Warnings { get; } = new List<Message>(10);
        public List<Message> SafetyConcerns { get; } = new List<Message>(10);
        public List<Message> SanityChecks { get; } = new List<Message>(10);

        public bool Any() => Warnings.Any() || SafetyConcerns.Any() || SanityChecks.Any();

        public void Clear()
        {
            Warnings.Clear();
            SafetyConcerns.Clear();
            SanityChecks.Clear();
        }

        public void WriteTo(StringBuilder builder)
        {
            foreach (var message in Warnings)
            {
                builder.Append("WARNING: ");
                message.WriteTo(builder);
                builder.AppendLine();
            }
            foreach (var message in SafetyConcerns)
            {
                builder.Append("SAFETY: ");
                message.WriteTo(builder);
                builder.AppendLine();
            }
            foreach (var message in SanityChecks)
            {
                builder.Append("SANITY: ");
                message.WriteTo(builder);
                builder.AppendLine();
            }
        }
    }
}
