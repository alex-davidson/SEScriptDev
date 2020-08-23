using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    public class ConfigurationSerialiser
    {
        public string Serialise(RequestedConfiguration configuration)
        {
            using (var enumerator = new ConfigurationWriter().GenerateParts(configuration).GetEnumerator())
            {
                if (!enumerator.MoveNext()) return "";
                var builder = new StringBuilder();
                WriteQuotedPart(builder, enumerator.Current);
                while (enumerator.MoveNext())
                {
                    builder.Append(" ");
                    WriteQuotedPart(builder, enumerator.Current);
                }
                return builder.ToString();
            }
        }

        private void WriteQuotedPart(StringBuilder builder, string part)
        {
            builder.Append("\"");
            builder.Append(part);
            builder.Append("\"");
        }

        public RequestedConfiguration Deserialise(string serialised)
        {
            var commandLine = new MyCommandLine();
            if (String.IsNullOrWhiteSpace(serialised) || !commandLine.TryParse(serialised))
            {
                Debug.Write(Debug.Level.Info, "No stored configuration.");
                return new RequestedConfiguration();
            }
            var configuration = new RequestedConfiguration();
            if (!new ConfigurationReader().Read(configuration, commandLine.Items))
            {
                Debug.Write(Debug.Level.Error, "Unable to read the stored configuration. Resetting to defaults.");
                return new RequestedConfiguration();
            }
            return configuration;
        }

        public RequestedConfiguration UpdateFromCommandLine(RequestedConfiguration existingConfiguration, IEnumerable<string> parts)
        {
            var copy = existingConfiguration.Copy();
            if (!new ConfigurationReader().Read(copy, parts)) return existingConfiguration;
            return copy;
        }
    }
}
