using System;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public static partial class Constants
    {
        public const UpdateFrequency EXPECTED_UPDATE_MODE = UpdateFrequency.Update100;

        /// <summary>
        /// Regularly scan for changes to the grid.
        /// </summary>
        public static readonly TimeSpan BLOCK_RESCAN_INTERVAL = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Each update loop is broken down into a series of actions to aid in spreading work
        /// across multiple game ticks. We only process a limited number of actions per tick.
        /// </summary>
        public static readonly long ACTIONS_BEFORE_YIELD = 20;

        /****************************************** MEMORY ALLOCATION TUNING ***************************************/

        public const int ALLOC_DISPLAY_COUNT = 5;
        public const int ALLOC_PARTS_PER_DISPLAY_COUNT = 10;
        public const int ALLOC_DISPLAY_BUFFER_SIZE = 2000;
        public const int ALLOC_SCAN_BLOCK_COUNT = 500;
        public const int ALLOC_SCAN_RULE_COUNT = 10;
    }
}
