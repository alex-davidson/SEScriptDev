using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    public partial class Constants
    {
        public const float ALERT_POWER_SECONDS = 300;
        public const float ENABLE_REACTOR_THRESHOLD_BATTERY_POWER_SECONDS = 60;
        public const float POWER_DRAW_THRESHOLD_PERCENT = 95;
        public const float DISABLE_REACTOR_THRESHOLD_POWER_DRAW_MW = 0.01f;

        public const float URANIUM_KG_PER_MWH = 1f;

        public const string DISPLAY_NAME = "Display.PowerManagement.Messages";
    }
}
