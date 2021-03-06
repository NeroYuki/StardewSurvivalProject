using System;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI;
using StardewValley;

namespace StardewSurvivalProject.source.harmony_patches
{
    class ObjectPatches
    {
        private static IMonitor Monitor = null;
        // call this method from your Entry class
        public static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }

        //Directly intercept healthRecoveredOnConsumption method to disable healing via eating, only allow items specified in the whitelist
        public static bool CalculateHPGain_Prefix(StardewValley.Object __instance, ref int __result)
        {
            try
            {
                if (__instance == null)
                    return true;

                int gain_value = data.HealingItemDictionary.getHealingValue(__instance.name);
                __result = gain_value;

                if (ModConfig.GetInstance().DisableHPHealingOnEatingFood)
                {
                    return false;
                }
                else if (gain_value == 0)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(CalculateHPGain_Prefix)}:\n{ex}", LogLevel.Error);
                return true;
            }
        }
    }
}
