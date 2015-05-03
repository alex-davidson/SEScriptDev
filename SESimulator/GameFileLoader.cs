using System;
using System.Globalization;
using System.IO;

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

        public Stream OpenFile(GameFile file)
        {
            var name = Path.ChangeExtension(file.ToString(), ".sbc");
            var path = Path.Combine(dataRootPath, name);
            if (!File.Exists(path)) throw new FileNotFoundException(String.Format("The game data file '{0}' was not found in the current data directory at '{1}'", name, dataRootPath));
            return File.OpenRead(path);
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