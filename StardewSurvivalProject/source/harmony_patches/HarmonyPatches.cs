using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HarmonyLib;
using StardewValley;
using SObject = StardewValley.Object;
using StardewModdingAPI;

namespace StardewSurvivalProject.source.harmony_patches
{
    public class HarmonyPatches
    {
        public static void InitPatches(string uniqueModId, IMonitor monitor, core.GameStateManager gameState)
        {
            FarmerPatches.Initialize(monitor);
            ObjectPatches.Initialize(monitor);
            UIDrawPatches.Initialize(monitor);
            NPCPatches.Initialize(monitor);
            MoodPatches.Initialize(monitor, gameState);

            var harmony = new Harmony(uniqueModId);

            harmony.Patch(
               original: AccessTools.Method(typeof(Farmer), nameof(Farmer.doneEating)),
               postfix: new HarmonyMethod(typeof(FarmerPatches), nameof(FarmerPatches.DoneEating_PostFix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), nameof(Farmer.EndUsingTool)),
                postfix: new HarmonyMethod(typeof(FarmerPatches), nameof(FarmerPatches.EndUsingTool_PostFix))
             );

            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.receiveGift), new Type[] { typeof(SObject), typeof(Farmer), typeof(bool), typeof(float), typeof(bool) }),
                postfix: new HarmonyMethod(typeof(NPCPatches), nameof(NPCPatches.GiftGiving_PostFix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.healthRecoveredOnConsumption)),
                prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.CalculateHPGain_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.placementAction), new Type[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) }),
                postfix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.ItemPlace_PostFix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Menus.IClickableMenu), nameof(StardewValley.Menus.IClickableMenu.drawHoverText),
                new[] { typeof(SpriteBatch), typeof(StringBuilder), typeof(SpriteFont), typeof(int), typeof(int), typeof(int), typeof(string), typeof(int), typeof(string[]), typeof(Item), typeof(int), typeof(string), typeof(int), typeof(int), typeof(int), typeof(float), typeof(CraftingRecipe), typeof(IList<Item>), typeof(Texture2D), typeof(Rectangle?), typeof(Color?), typeof(Color?), typeof(float), typeof(int), typeof(int) }),
                postfix: new HarmonyMethod(typeof(UIDrawPatches), nameof(UIDrawPatches.DrawHoverText_Postfix))
            );

            // Mood-related patches
            if (ModConfig.GetInstance().UseSanityModule)
            {
                // Skill level up detection
                harmony.Patch(
                    original: AccessTools.Method(typeof(Farmer), nameof(Farmer.gainExperience)),
                    postfix: new HarmonyMethod(typeof(MoodPatches), nameof(MoodPatches.GainExperience_PostFix))
                );

                // Raw food and food tracking (reuse existing patch, add new postfix)
                harmony.Patch(
                    original: AccessTools.Method(typeof(Farmer), nameof(Farmer.doneEating)),
                    postfix: new HarmonyMethod(typeof(MoodPatches), nameof(MoodPatches.EatRawFood_Check))
                );

                // Tool usage for monotony tracking (reuse existing patch)
                harmony.Patch(
                    original: AccessTools.Method(typeof(Farmer), nameof(Farmer.EndUsingTool)),
                    postfix: new HarmonyMethod(typeof(MoodPatches), nameof(MoodPatches.ToolUsed_Track))
                );

                // Gift reactions
                harmony.Patch(
                    original: AccessTools.Method(typeof(NPC), nameof(NPC.receiveGift), new Type[] { typeof(SObject), typeof(Farmer), typeof(bool), typeof(float), typeof(bool) }),
                    postfix: new HarmonyMethod(typeof(MoodPatches), nameof(MoodPatches.GiftReaction_PostFix))
                );

                // NPC dialogue for social interaction tracking
                harmony.Patch(
                    original: AccessTools.Method(typeof(NPC), nameof(NPC.checkAction)),
                    postfix: new HarmonyMethod(typeof(MoodPatches), nameof(MoodPatches.NPCDialogue_PostFix))
                );
            }
        }
    }
}
