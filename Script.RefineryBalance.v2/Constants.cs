using System;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public static partial class Constants
    {
        public const UpdateFrequency EXPECTED_UPDATE_MODE = UpdateFrequency.Update100;

        /// <summary>
        /// We run every 100 ticks by default, and there are 60 game ticks in a seconds.
        /// Therefore we need to ensure 2 seconds of work for a refinery to avoid dropping it.
        /// </summary>
        public static readonly long TARGET_INTERVAL_SECONDS = 2;
        /// <summary>
        /// If convenient, it'd be nice to have an extra second or two of work for the refinery.
        /// </summary>
        public static readonly long OVERLAP_INTERVAL_SECONDS = 1;
        /// <summary>
        /// Regularly scan for changes to the grid.
        /// </summary>
        public static readonly TimeSpan BLOCK_RESCAN_INTERVAL = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Regularly scan for changes to ore stacks, etc. This is a count of 100-tick update
        /// loops instead of seconds; 6 loops is about 10 seconds.
        /// </summary>
        public static readonly int RESOURCE_RESCAN_INTERVAL_UPDATES = 6;

        /// <summary>
        /// Each update loop is broken down into a series of actions to aid in spreading work
        /// across multiple game ticks. We only process a limited number of actions per tick.
        /// </summary>
        public static readonly long ACTIONS_BEFORE_YIELD = 20;

        /****************************************** MEMORY ALLOCATION TUNING ***************************************/

        public const int ALLOC_REFINERY_COUNT = 20;
        public const int ALLOC_ORE_TYPE_COUNT = 10;
        public const int ALLOC_INGOT_TYPE_COUNT = 10;
        public const int ALLOC_INVENTORY_OWNER_COUNT = 10;
        public const int ALLOC_ORE_DONOR_COUNT = 10;

        /*********************************************** GAME CONSTANTS ********************************************/

        public static IngotType[] INGOT_TYPES = {
            new IngotType("Ingot/Cobalt", 220),
            new IngotType("Ingot/Gold", 5),
            new IngotType("Ingot/Iron", 80),
            new IngotType("Ingot/Nickel", 70),
            new IngotType("Ingot/Magnesium", 0.35),
            new IngotType("Ingot/Silicon", 15),
            new IngotType("Ingot/Silver", 10),
            new IngotType("Ingot/Platinum", 0.4),
            new IngotType("Ingot/Uranium", 0.01) { Enabled = false },
            new IngotType("Ingot/Stone", 0.01) { Enabled = false }
        };
        public static Blueprint[] BLUEPRINTS = {
            // Default blueprints for refining ore to ingots:
            new Blueprint("StoneOreToIngot", 10f, new ItemAndQuantity("Ore/Stone", 1000f),
                new ItemAndQuantity("Ingot/Stone", 14f), new ItemAndQuantity("Ingot/Iron", 30f), new ItemAndQuantity("Ingot/Nickel", 2.4f), new ItemAndQuantity("Ingot/Silicon", 4f)),
            new Blueprint("IronOreToIngot", 0.05f, new ItemAndQuantity("Ore/Iron", 1f),
                new ItemAndQuantity("Ingot/Iron", 0.7f)),
            new Blueprint("NickelOreToIngot", 0.66f, new ItemAndQuantity("Ore/Nickel", 1f),
                new ItemAndQuantity("Ingot/Nickel", 0.4f)),
            new Blueprint("CobaltOreToIngot", 3f, new ItemAndQuantity("Ore/Cobalt", 1f),
                new ItemAndQuantity("Ingot/Cobalt", 0.3f)),
            new Blueprint("MagnesiumOreToIngot", 0.5f, new ItemAndQuantity("Ore/Magnesium", 1f),
                new ItemAndQuantity("Ingot/Magnesium", 0.007f)),
            new Blueprint("SiliconOreToIngot", 0.6f, new ItemAndQuantity("Ore/Silicon", 1f),
                new ItemAndQuantity("Ingot/Silicon", 0.7f)),
            new Blueprint("SilverOreToIngot", 1f, new ItemAndQuantity("Ore/Silver", 1f),
                new ItemAndQuantity("Ingot/Silver", 0.1f)),
            new Blueprint("GoldOreToIngot", 0.4f, new ItemAndQuantity("Ore/Gold", 1f),
                new ItemAndQuantity("Ingot/Gold", 0.01f)),
            new Blueprint("PlatinumOreToIngot", 3f, new ItemAndQuantity("Ore/Platinum", 1f),
                new ItemAndQuantity("Ingot/Platinum", 0.005f)),
            new Blueprint("UraniumOreToIngot", 4f, new ItemAndQuantity("Ore/Uranium", 1f),
                new ItemAndQuantity("Ingot/Uranium", 0.01f)),
            new Blueprint("ScrapIngotToIronIngot", 0.04f, new ItemAndQuantity("Ingot/Scrap", 1f),
                new ItemAndQuantity("Ingot/Iron", 0.8f)),
            new Blueprint("ScrapToIronIngot", 0.04f, new ItemAndQuantity("Ore/Scrap", 1f),
                new ItemAndQuantity("Ingot/Iron", 0.8f)),
        };

        public static RefineryType[] REFINERY_TYPES = {
            new RefineryType("LargeRefinery") {
                SupportedBlueprints = { "StoneOreToIngot", "IronOreToIngot", "ScrapToIronIngot", "ScrapIngotToIronIngot", "NickelOreToIngot", "CobaltOreToIngot",
                    "MagnesiumOreToIngot", "SiliconOreToIngot", "SilverOreToIngot", "GoldOreToIngot", "PlatinumOreToIngot", "UraniumOreToIngot" },
                Efficiency = 1.0, Speed = 1.3
            },
            new RefineryType("Blast Furnace") {
                SupportedBlueprints = { "IronOreToIngot", "NickelOreToIngot", "CobaltOreToIngot", "MagnesiumOreToIngot", "ScrapToIronIngot", "SiliconOreToIngot",
                    "ScrapIngotToIronIngot", "StoneOreToIngot" },
                Efficiency = 0.7, Speed = 0.65
            },
        };
    }
}
