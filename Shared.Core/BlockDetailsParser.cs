using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public class BlockDetailsParser
    {
        public bool Parse(string blockDetails)
        {
            if (string.IsNullOrWhiteSpace(blockDetails)) return false;  // Nothing to parse.
            if (blockDetails == currentBlockDetails) return true;       // Already parsed.

            currentBlockDetails = "";
            lines.Clear();

            for (var i = 0; i < blockDetails.Length; i++)
            {
                if (!SkipWhitespace(blockDetails, ref i, true)) break;
                // Non-whitespace character.
                var endOfHeader = blockDetails.IndexOf(':', i);
                if (endOfHeader < 0)
                {
                    SkipToEndOfLine(blockDetails, ref i);
                    continue;
                }
                var line = new Line();
                line.HeaderStart = i;
                line.HeaderLength = endOfHeader - i;
                i = endOfHeader + 1;

                if (!SkipWhitespace(blockDetails, ref i, false)) break;

                line.ValueStart = i;
                SkipToEndOfLine(blockDetails, ref i);
                line.ValueLength = i - line.ValueStart;

                lines.Add(line);
            }
            currentBlockDetails = blockDetails;
            return lines.Any();
        }

        /// <summary>
        /// Skip whitespace, leaving i pointing to the next non-whitespace character.
        /// Optionally skips line-breaks too.
        /// </summary>
        private static bool SkipWhitespace(string str, ref int i, bool skipLineBreaks)
        {
            while (i < str.Length)
            {
                if (!char.IsWhiteSpace(str[i])) return true;
                if (!skipLineBreaks && IsLineBreak(str[i])) return true;
                i++;
            }
            return false;
        }

        /// <summary>
        /// Skip to the end of the line, leaving i pointing at the start of the line-break.
        /// </summary>
        private static bool SkipToEndOfLine(string str, ref int i)
        {
            while (i < str.Length)
            {
                if (IsLineBreak(str[i])) return true;
                i++;
            }
            return false;
        }

        private static bool IsLineBreak(char c)
        {
            return c == '\n' || c == '\r';
        }

        private string currentBlockDetails = "";
        private readonly List<Line> lines = new List<Line>(10);

        public string Type => Get("Type");

        /// <summary>
        /// Find the value associated with the first header which starts with the specified text.
        /// </summary>
        public string Get(string headerPrefix)
        {
            foreach (var line in lines)
            {
                if (line.HeaderLength < headerPrefix.Length) continue;
                if (currentBlockDetails.IndexOf(headerPrefix, line.HeaderStart, StringComparison.OrdinalIgnoreCase) != line.HeaderStart) continue;
                return currentBlockDetails.Substring(line.ValueStart, line.ValueLength);
            }
            return null;
        }

        public IEnumerable<string> GetValues()
        {
            foreach (var line in lines)
            {
                yield return currentBlockDetails.Substring(line.ValueStart, line.ValueLength);
            }
        }

        struct Line
        {
            public int HeaderStart { get; set; }
            public int HeaderLength { get; set; }
            public int ValueStart { get; set; }
            public int ValueLength { get; set; }
        }
    }
}
