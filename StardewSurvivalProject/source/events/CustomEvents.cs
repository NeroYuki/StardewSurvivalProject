using System;
using StardewValley;
using StardewValley.Events;

namespace StardewSurvivalProject.source.events
{
    class CustomEvents
    {
        public static event EventHandler OnItemEaten;

        public static event EventHandler OnToolUsed;

        internal static void InvokeOnItemEaten(Farmer farmer)
        {
            if (CustomEvents.OnItemEaten == null || !farmer.IsLocalPlayer)
                return;

            var args = new EventArgs();
            var name = "CustomEvents.onItemEaten";

            foreach (EventHandler handler in CustomEvents.OnItemEaten.GetInvocationList())
            {
                try
                {
                    handler.Invoke(farmer, args);
                }
                catch (Exception e)
                {
                    LogHelper.Error($"Exception while handling event {name}:\n{e}");
                }
            }
        }

        internal static void InvokeOnToolUsed(Farmer farmer)
        {
            if (CustomEvents.OnToolUsed == null || !farmer.IsLocalPlayer)
                return;

            var args = new EventArgs();
            var name = "CustomEvents.onToolUsed";

            foreach (EventHandler handler in CustomEvents.OnToolUsed.GetInvocationList())
            {
                try
                {
                    handler.Invoke(farmer, args);
                }
                catch (Exception e)
                {
                    LogHelper.Error($"Exception while handling event {name}:\n{e}");
                }
            }
        }
    }


}
