using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SESimulator
{
    /// <summary>
    /// Locates and opens game files.
    /// </summary>
    public class GameFileLoader
    {
        private readonly string dataRootPath;

        public GameFileLoader(string dataRootPath)
        {
            this.dataRootPath = dataRootPath;
        }

        public IEnumerable<GameFilePart> GetGameFileParts(GameFile file)
        {
            var hasContent = false;
            var path = Path.Combine(dataRootPath, file.ToString());
            var filePath = Path.ChangeExtension(path, ".sbc");
            if (File.Exists(filePath))
            {
                hasContent = true;
                yield return new GameFilePart(file, filePath);
            }
            foreach (var part in ResolveGameFilePart(file, path))
            {
                hasContent = true;
                yield return part;
            }
            if (!hasContent) throw new FileNotFoundException(String.Format("The game data file '{0}' was not found in the current data directory at '{1}'", filePath, dataRootPath));
        }

        private IEnumerable<GameFilePart> ResolveGameFilePart(GameFile file, string path)
        {
            if (File.Exists(path))
            {
                yield return new GameFilePart(file, path);
            }
            if (Directory.Exists(path))
            {
                foreach (var child in Directory.EnumerateFileSystemEntries(path).SelectMany(p => ResolveGameFilePart(file, p)))
                {
                    yield return child;
                }
            }
        }

        public Stream OpenLocalisationFile(CultureInfo culture)
        {
            Stream stream;
            if (TryOpenLocalisationFile(String.Format("MyTexts.{0}.resx", culture.IetfLanguageTag), out stream)) return stream;
            if (TryOpenLocalisationFile(String.Format("MyTexts.{0}.resx", culture.TwoLetterISOLanguageName), out stream)) return stream;
            TryOpenLocalisationFile("MyTexts.resx", out stream);
            return stream;
        }

        private bool TryOpenLocalisationFile(string name, out Stream stream)
        {
            stream = null;
            var path = Path.Combine(dataRootPath, "Localization", name);
            if (!File.Exists(path)) return false;

            stream = File.OpenRead(path);
            return true;
        }
    }
}
