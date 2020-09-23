using System;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class DisplayRenderer
    {
        public void Rescan(IMyGridTerminalSystem gts)
        {
            display = (IMyTextPanel)gts.GetBlockWithName("Display.Engines.Messages");
        }

        private readonly StringBuilder local_Render_builder = new StringBuilder(1000);
        private IMyTextPanel display;

        public void Render(Errors stateErrors, Errors commandErrors, Message commandState)
        {
            if (display == null) return;

            local_Render_builder.Clear();
            local_Render_builder.AppendFormat("Engine modules  {0}\n", Datestamp.Minutes);
            if (!default(Message).Equals(commandState))
            {
                commandState.WriteTo(local_Render_builder);
                local_Render_builder.AppendLine();
            }
            if (commandErrors.Any())
            {
                commandErrors.WriteTo(local_Render_builder);
                local_Render_builder.AppendLine();
            }
            stateErrors.WriteTo(local_Render_builder);
            display.WriteText(local_Render_builder);
        }
    }
}
