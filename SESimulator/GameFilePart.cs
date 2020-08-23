using System;
using System.IO;

namespace SESimulator
{
    public class GameFilePart
    {
        private readonly string fullPath;
        public GameFile Category { get; }

        public GameFilePart(GameFile category, string fullPath)
        {
            if (!Path.IsPathRooted(fullPath)) throw new ArgumentException(String.Format("Not an absolute path: {0}", fullPath), nameof(fullPath));
            this.Category = category;
            this.fullPath = fullPath;
        }

        public Stream OpenFile()
        {
            if (!File.Exists(fullPath)) throw new FileNotFoundException(String.Format("The game data file '{0}' was not found.", fullPath));
            return File.OpenRead(fullPath);
        }
    }
}
