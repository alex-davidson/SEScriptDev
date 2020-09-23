using System;
using System.Collections.Generic;

namespace IngameScript
{
    public static class Debug
    {
        public enum Level
        {
            None = 0,
            Error = 1,
            Essential = 1,
            Warning = 2,
            Info = 5,
            Debug = 7,
            All = 10,
        }

        private static Level maximumLevel;
        private static Action<string> echo;
        /// <summary>
        /// Maintains log messages across yields.
        /// </summary>
        private static readonly List<string> buffer = new List<string>(100);

        public static void Initialise(Level maximumLevel, Action<string> echo)
        {
            Debug.maximumLevel = maximumLevel;
            Debug.echo = echo;
            Write(new Message("Initialised: {0}", Datestamp.Minutes));
        }

        private static void Write(Message message)
        {
            if (echo == null) return;
            var line = message.ToString();
            buffer.Add(line);
            echo(line);
        }

        public static void RestoreBuffer()
        {
            if (echo == null) return;
            foreach (var line in buffer)
            {
                echo(line);
            }
        }

        public static void ClearBuffer()
        {
            buffer.Clear();
        }

        public static void Write(Level level, Message message)
        {
            if (level > maximumLevel) return;
            Write(message);
        }

        public static void Assert(bool condition, string failureText)
        {
            if (condition) return;
            Write(new Message("[ASSERT] {0}", failureText));
            throw new Exception(failureText);
        }

        public static void Fail(string failureText)
        {
            Write(new Message("[FAIL] {0}", failureText));
            throw new Exception(failureText);
        }

        public static void Warn(string failureText)
        {
            Write(new Message("[WARN] {0}", failureText));
        }
    }

}
