using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;
using Shared.Core;

namespace IngameScript
{
    public struct DisplayRenderer
    {
        private static readonly StringBuilder local_DrawDisplay_builder = new StringBuilder(1000);

        public void Draw(IMyTextPanel display, BatteriesState batteriesState, ReactorsState reactorsState)
        {
            local_DrawDisplay_builder.Clear();
            local_DrawDisplay_builder.AppendFormat("Power Management  {0}\n", Datestamp.Minutes);

            var totalEnergyRemainingMWh = batteriesState.EnergyRemainingMWh + reactorsState.EnergyRemainingMWh;
            var totalPowerConsumptionMW = batteriesState.PowerConsumedMW + reactorsState.PowerConsumedMW;
            var totalSecondsRemaining = totalPowerConsumptionMW > 0 ? (3600 * totalEnergyRemainingMWh / totalPowerConsumptionMW) : float.PositiveInfinity;

            local_DrawDisplay_builder.Append("Total Time: ");
            AppendTimeRemaining(local_DrawDisplay_builder, totalSecondsRemaining);
            local_DrawDisplay_builder.AppendLine();

            if (totalSecondsRemaining < Constants.ALERT_POWER_SECONDS)
            {
                local_DrawDisplay_builder.AppendLine("*** L O W    P O W E R ***");
            }

            local_DrawDisplay_builder.AppendLine();

            local_DrawDisplay_builder.Append("Batteries: ");
            local_DrawDisplay_builder.AppendLine(batteriesState.Charging > 0 ? "Auto" : "Discharging");

            local_DrawDisplay_builder.Append("* Stored: ");
            local_DrawDisplay_builder.Append(Unit.Energy.FormatSI(batteriesState.EnergyRemainingMWh));
            local_DrawDisplay_builder.Append(" / ");
            local_DrawDisplay_builder.Append(Unit.Energy.FormatSI(batteriesState.MaxEnergyMWh));
            local_DrawDisplay_builder.AppendLine();

            local_DrawDisplay_builder.Append("* Power: ");
            local_DrawDisplay_builder.Append(Unit.Power.FormatSI(Math.Max(batteriesState.PowerConsumedMW, 0)));
            local_DrawDisplay_builder.AppendFormat("  ({0}%)", batteriesState.PowerDrawPercent);
            local_DrawDisplay_builder.AppendLine();

            local_DrawDisplay_builder.Append("* Time: ");
            AppendTimeRemaining(local_DrawDisplay_builder, batteriesState.SecondsRemaining);
            local_DrawDisplay_builder.AppendLine();

            local_DrawDisplay_builder.AppendLine();

            local_DrawDisplay_builder.Append("Reactors: ");
            local_DrawDisplay_builder.AppendLine(reactorsState.Enabled > 0 ? "Enabled" : "Disabled");

            local_DrawDisplay_builder.Append("* Fuel: ");
            local_DrawDisplay_builder.Append(Unit.Mass.FormatSI(reactorsState.FuelAvailableKg));
            local_DrawDisplay_builder.Append(" (");
            local_DrawDisplay_builder.Append(Unit.Energy.FormatSI(reactorsState.EnergyRemainingMWh));
            local_DrawDisplay_builder.AppendLine(")");

            local_DrawDisplay_builder.Append("* Power: ");
            local_DrawDisplay_builder.Append(Unit.Power.FormatSI(Math.Max(reactorsState.PowerConsumedMW, 0)));
            local_DrawDisplay_builder.AppendFormat("  ({0:P})", reactorsState.PowerDrawPercent);
            local_DrawDisplay_builder.AppendLine();

            local_DrawDisplay_builder.Append("* Time: ");
            AppendTimeRemaining(local_DrawDisplay_builder, reactorsState.SecondsRemaining);
            local_DrawDisplay_builder.AppendLine();

            if (totalSecondsRemaining < Constants.ALERT_POWER_SECONDS)
            {
                local_DrawDisplay_builder.AppendLine();
                local_DrawDisplay_builder.AppendLine("*** L O W    P O W E R ***");
            }

            display.WriteText(local_DrawDisplay_builder);
        }

        private static void AppendTimeRemaining(StringBuilder builder, float seconds)
        {
            if (float.IsInfinity(seconds))
            {
                builder.Append("-");
                return;
            }
            builder.Append(TimeSpan.FromSeconds((int)seconds));
        }
    }
}
