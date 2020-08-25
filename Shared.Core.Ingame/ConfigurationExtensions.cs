using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    public static class ConfigurationExtensions
    {
        public static string Serialise<T>(this IConfigurationWriter<T> writer, T configuration)
        {
            using (var enumerator = writer.GenerateParts(configuration).GetEnumerator())
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

        private static void WriteQuotedPart(StringBuilder builder, string part)
        {
            builder.Append("\"");
            builder.Append(part);
            builder.Append("\"");
        }


        public static T Deserialise<T>(this IConfigurationReader<T> reader, string serialised) where T : new()
        {
            return Deserialise(reader, new T(), serialised);
        }

        public static T Deserialise<T>(this IConfigurationReader<T> reader, T defaultConfiguration, string serialised) where T : new()
        {
            var commandLine = new MyCommandLine();
            if (String.IsNullOrWhiteSpace(serialised) || !commandLine.TryParse(serialised))
            {
                Debug.Write(Debug.Level.Info, "No stored configuration.");
                return defaultConfiguration;
            }
            var configuration = new T();
            if (!reader.Read(configuration, commandLine.Items))
            {
                Debug.Write(Debug.Level.Error, "Unable to read the stored configuration. Resetting to defaults.");
                return defaultConfiguration;
            }
            return configuration;
        }

        public static T UpdateFromCommandLine<T>(this IConfigurationReader<T> reader, T existingConfiguration, IEnumerable<string> parts) where T : IDeepCopyable<T>, new()
        {
            var copy = existingConfiguration.Copy();
            if (!reader.Read(copy, parts)) return existingConfiguration;
            return copy;
        }
    }
}
