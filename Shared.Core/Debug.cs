using System;
using System.Collections.Generic;
using System.Text;

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
            Write($"Initialised: {DateTime.Now:dd MMM HH:mm}");
        }

        private static void Write(string message, params object[] args)
        {
            if (echo == null) return;
            var line = string.Format(message, args);
            buffer.Add(line);
            echo(string.Format(message, args));
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

        public static void Write(Level level, string message, params object[] args)
        {
            if (level > maximumLevel) return;
            Write(message, args);
        }

        public static void Assert(bool condition, string failureText)
        {
            if (condition) return;
            Write("[ASSERT] {0}", failureText);
            throw new Exception(failureText);
        }

        public static void Fail(string failureText)
        {
            Write("[FAIL] {0}", failureText);
            throw new Exception(failureText);
        }

        public static void Warn(string failureText)
        {
            Write("[WARN] {0}", failureText);
        }
    }

}
