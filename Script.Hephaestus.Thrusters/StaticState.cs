namespace IngameScript
{
    public class StaticState
    {
        public static readonly StaticState Instance = new StaticState();

        public EngineTierDef Bow { get; }
        public EngineTierDef Midships { get; }

        private StaticState()
        {
            Bow = new EngineTierDef(
                GenerateModuleTier("Bow"),
                new [] {
                    new EnginePresetDef("Forward", 180),
                    new EnginePresetDef("Outward", 90),
                    new EnginePresetDef("Backward", 15),
                });
            Midships = new EngineTierDef(
                GenerateModuleTier("Midships"),
                new [] {
                    new EnginePresetDef("Outward", 90),
                    new EnginePresetDef("Backward", 5),
                });
        }

        private static EngineModuleDef[] GenerateModuleTier(string tierName)
        {
            return new []
            {
                CreateModule(tierName, "Inner", "Outer", "AB"),
                CreateModule(tierName, "Inner", "Outer", "BC"),
                CreateModule(tierName, "Inner", "Outer", "CD"),
                CreateModule(tierName, "Inner", "Outer", "DA"),
                CreateModule(tierName, "Outer", "Inner", "BA"),
                CreateModule(tierName, "Outer", "Inner", "CB"),
                CreateModule(tierName, "Outer", "Inner", "DC"),
                CreateModule(tierName, "Outer", "Inner", "AD"),
            };
        }

        private static EngineModuleDef CreateModule(string tierName, string governing, string opposing, string set) =>
            new EngineModuleDef(
                $"{tierName} Engine Module {set}",
                $"{tierName} Rotor ({governing}, {set})",
                $"{tierName} Rotor ({opposing}, {set})");

        public struct EngineModuleDef
        {
            public string Name { get; }
            public string GoverningRotorName { get; }
            public string OpposingRotorName { get; }

            public EngineModuleDef(string name, string governingRotorName, string opposingRotorName) : this()
            {
                Name = name;
                GoverningRotorName = governingRotorName;
                OpposingRotorName = opposingRotorName;
            }
        }

        public struct EnginePresetDef
        {
            public string Name { get; }
            public float Angle { get; }

            public EnginePresetDef(string name, float angle)
            {
                Name = name;
                Angle = angle;
            }
        }

        public struct EngineTierDef
        {
            public EngineModuleDef[] Modules { get; }
            public EnginePresetDef[] Presets { get; }

            public EngineTierDef(EngineModuleDef[] modules, EnginePresetDef[] presets)
            {
                Modules = modules;
                Presets = presets;
            }
        }
    }
}
