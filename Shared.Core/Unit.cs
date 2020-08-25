using System;
using System.Collections.Generic;

namespace Shared.Core
{
    public struct Unit
    {
        private static readonly Dictionary<string, Unit> unitsByString = new Dictionary<string, Unit>(StringComparer.OrdinalIgnoreCase);

        public static readonly Unit Mass = new Unit(0, "Kg", "t", "Kt", "Mt", "Gt");
        public static readonly Unit Volume = new Unit(0, "L", "KL", "ML", "GL");
        public static readonly Unit Force = new Unit(0, "N", "KN", "MN", "GN");
        public static readonly Unit Power = new Unit(2, "W", "KW", "MW", "GW", "TW");        // Recorded in MW by default.
        public static readonly Unit Energy = new Unit(2, "Wh", "KWh", "MWh", "GWh", "TWh");  // Recorded in MWh by default.
        private readonly int defaultMagnitude;
        private readonly string[] unitMagnitudes;

        private Unit(int defaultMagnitude, params string[] unitMagnitudes)
        {
            this.defaultMagnitude = defaultMagnitude;
            this.unitMagnitudes = unitMagnitudes;
            foreach (var unit in unitMagnitudes) unitsByString.Add(unit, this);
        }

        public string FormatSI(float value, int dp = 1)
        {
            var magnitude = defaultMagnitude;
            while (magnitude > 0)
            {
                if (value > 1) break;
                value *= 1000;

                magnitude--;
            }
            while (magnitude <= unitMagnitudes.Length)
            {
                if (value < 990) break;
                value /= 1000;

                magnitude++;
            }
            var unit = unitMagnitudes[magnitude];
            return $"{Math.Round(value, dp)} {unit}";
        }

        public bool TryParseSI(string str, out float value)
        {
            Unit unit;
            if (!TryParseSI(str, out value, out unit)) return false;
            return Equals(unit, this);
        }

        public static bool TryParseSI(string str, out float value, out Unit unit)
        {
            value = 0;
            unit = default(Unit);
            if (string.IsNullOrWhiteSpace(str)) return false;

            var m = rxValueWithUnit.Match(str);
            if (!m.Success) return false;

            if (!unitsByString.TryGetValue(m.Groups["u"].Value, out unit)) return false;
            if (!float.TryParse(m.Groups["v"].Value, out value)) return false;

            var scale = -unit.defaultMagnitude;
            for (var i = 0; i < unit.unitMagnitudes.Length; i++)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(unit.unitMagnitudes[i], m.Groups["u"].Value)) break;
                scale++;
            }
            var scaleFactor = (float)Math.Pow(10, scale * 3);
            value = value * scaleFactor;
            return true;
        }

        private static readonly System.Text.RegularExpressions.Regex rxValueWithUnit = new System.Text.RegularExpressions.Regex(@"(?<v>[-+\.\d]+)\s*(?<u>\w+)");
    }
}
