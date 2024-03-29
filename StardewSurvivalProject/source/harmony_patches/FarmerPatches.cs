﻿using System;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI;
using StardewValley;

namespace StardewSurvivalProject.source.harmony_patches
{
    class FarmerPatches
    {
        private static IMonitor Monitor = null;
        // call this method from your Entry class
        public static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }

        public static void DoneEating_PostFix(Farmer __instance)
        {
            try
            {
                if (__instance.itemToEat == null)
                    return;

                events.CustomEvents.InvokeOnItemEaten(__instance);
                return;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(DoneEating_PostFix)}:\n{ex}", LogLevel.Error);
                return;
            }
        }

        public static void EndUsingTool_PostFix(Farmer __instance)
        {
            try
            {
                if (__instance.CurrentTool == null)
                    return;

                events.CustomEvents.InvokeOnToolUsed(__instance);
                return;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(EndUsingTool_PostFix)}:\n{ex}", LogLevel.Error);
                return;
            }
        }
    }
}
