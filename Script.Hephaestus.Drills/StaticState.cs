namespace IngameScript
{
    public class StaticState
    {
        public static readonly StaticState Instance = new StaticState();

        public DrillTowerDef[] DrillTowers =
        {
            new DrillTowerDef('A'),
            new DrillTowerDef('B'),
            new DrillTowerDef('C'),
            new DrillTowerDef('D'),
        };

        public struct DrillTowerDef
        {
            public char Letter { get; }
            public string PistonGroupName { get; }
            public string RotorName { get; }
            public string DrillGroupName { get; }
            public string DrillClampsGroupName { get; }
            public string FloodlightsName { get; }
            public string DisplayName { get; }

            public DrillTowerDef(char letter)
            {
                Letter = letter;
                PistonGroupName = $"Pistons (Drill Tower {letter})";
                RotorName = $"Rotor (Drill Tower {letter})";
                DrillGroupName = $"Drills (Drill Tower {letter})";
                DrillClampsGroupName = $"Clamps (Drill Tower {letter})";
                FloodlightsName = $"Floodlight (Drill Tower {letter})";
                DisplayName = $"Display.Drill.{letter}";
            }
        }
    }
}
