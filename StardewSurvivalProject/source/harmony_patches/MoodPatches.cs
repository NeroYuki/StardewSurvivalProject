using System;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace StardewSurvivalProject.source.harmony_patches
{
    /// <summary>
    /// Harmony patches for detecting mood-affecting events
    /// </summary>
    public class MoodPatches
    {
        private static IMonitor Monitor = null;
        private static core.GameStateManager GameState = null;

        public static void Initialize(IMonitor monitor, core.GameStateManager gameState)
        {
            Monitor = monitor;
            GameState = gameState;
        }

        /// <summary>
        /// Postfix for Farmer.gainExperience to detect skill level ups
        /// </summary>
        public static void GainExperience_PostFix(Farmer __instance, int which, int howMuch)
        {
            try
            {
                if (!__instance.IsLocalPlayer || GameState == null || !GameState.IsInitialized)
                    return;

                if (!ModConfig.GetInstance().UseSanityModule)
                    return;

                // Check if this experience gain caused a level up
                // The game already applied the experience, so we check newSkillPointsTotal
                int oldLevel = __instance.experiencePoints[which] - howMuch;
                int newLevel = __instance.experiencePoints[which];
                
                // Each level requires cumulative points, check if we crossed a level boundary
                // Level boundaries: 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000
                if (HasLeveledUp(oldLevel, newLevel))
                {
                    // Apply mood boost for skill level up
                    var player = GameState.GetPlayerModel();
                    if (player?.mood != null)
                    {
                        player.mood.AddMoodElement("Skill Level Up", 5, 1440, 
                            "Feeling accomplished after leveling up a skill");
                        Game1.addHUDMessage(new HUDMessage("You feel accomplished!", HUDMessage.achievement_type));
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(GainExperience_PostFix)}:\n{ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Check if experience points crossed a level boundary
        /// </summary>
        private static bool HasLeveledUp(int oldExp, int newExp)
        {
            int[] levelThresholds = { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };
            
            foreach (int threshold in levelThresholds)
            {
                if (oldExp < threshold && newExp >= threshold)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Postfix for FishingRod.pullFishFromWater to detect trash fishing
        /// </summary>
        public static void PullFishFromWater_PostFix(FishingRod __instance, int whichFish, int fishSize, int fishQuality, int fishDifficulty, bool treasure, bool wasPerfect, bool fromFishPond, bool setFlagOnCatch, bool isBossFish, string itemCategory)
        {
            try
            {
                if (GameState == null || !GameState.IsInitialized || !ModConfig.GetInstance().UseSanityModule)
                    return;

                // Check if caught trash (category -20 is trash)
                if (itemCategory == "-20" || whichFish < 0)
                {
                    var player = GameState.GetPlayerModel();
                    if (player?.mood != null)
                    {
                        player.mood.AddMoodElement("Caught Trash", -10, 60, 
                            "Frustrated from catching trash while fishing");
                        Game1.addHUDMessage(new HUDMessage("Ugh, more trash...", HUDMessage.error_type));
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(PullFishFromWater_PostFix)}:\n{ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Postfix for detecting raw food consumption
        /// </summary>
        public static void EatRawFood_Check(Farmer __instance)
        {
            try
            {
                if (!__instance.IsLocalPlayer || GameState == null || !GameState.IsInitialized)
                    return;

                if (!ModConfig.GetInstance().UseSanityModule)
                    return;

                var itemToEat = __instance.itemToEat as SObject;
                if (itemToEat == null) return;

                // Check if food is raw (not cooked and not artisan goods)
                bool isRaw = itemToEat.Category != SObject.CookingCategory && 
                            itemToEat.Category != SObject.artisanGoodsCategory &&
                            itemToEat.Edibility > 0;

                if (isRaw)
                {
                    var player = GameState.GetPlayerModel();
                    if (player?.mood != null)
                    {
                        player.mood.AddMoodElement("Ate Raw Food", -10, 240, 
                            "Unsatisfied from eating uncooked food");
                    }
                }
                
                // Track food for variety
                var playerModel = GameState.GetPlayerModel();
                if (playerModel?.mood != null)
                {
                    playerModel.mood.TrackFoodEaten(itemToEat.Name);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(EatRawFood_Check)}:\n{ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Detect NPC interactions and conversations
        /// </summary>
        public static void NPCDialogue_PostFix(NPC __instance, Farmer who)
        {
            try
            {
                if (!who.IsLocalPlayer || GameState == null || !GameState.IsInitialized)
                    return;

                if (!ModConfig.GetInstance().UseSanityModule)
                    return;

                // Only count actual NPCs, not temporary or special characters
                if (__instance.IsVillager)
                {
                    var player = GameState.GetPlayerModel();
                    if (player?.mood != null)
                    {
                        // Add small mood boost for talking to NPCs (max +5 per day)
                        // Check if we already talked to this NPC today
                        string elementName = $"Talked to {__instance.Name}";
                        var existing = player.mood.MoodElements.Find(e => e.Name == elementName);
                        
                        if (existing == null)
                        {
                            // Count current "Talked to" elements
                            int talkCount = player.mood.MoodElements.FindAll(e => e.Name.StartsWith("Talked to")).Count;
                            if (talkCount < 5)
                            {
                                player.mood.AddMoodElement(elementName, 1, 1440, 
                                    $"Had a nice chat with {__instance.Name}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(NPCDialogue_PostFix)}:\n{ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Detect gift reactions (like/love/dislike/hate)
        /// </summary>
        public static void GiftReaction_PostFix(NPC __instance, StardewValley.Object o, Farmer giver)
        {
            try
            {
                if (!giver.IsLocalPlayer || GameState == null || !GameState.IsInitialized)
                    return;

                if (!ModConfig.GetInstance().UseSanityModule)
                    return;

                // Get gift taste
                int giftTaste = __instance.getGiftTasteForThisItem(o);
                
                var player = GameState.GetPlayerModel();
                if (player?.mood != null)
                {
                    switch (giftTaste)
                    {
                        case NPC.gift_taste_love:
                            player.mood.AddMoodElement("NPC Loved Gift", 10, 120, 
                                $"{__instance.Name} absolutely loved your gift!");
                            break;
                        case NPC.gift_taste_like:
                            player.mood.AddMoodElement("NPC Liked Gift", 5, 120, 
                                $"{__instance.Name} liked your gift");
                            break;
                        case NPC.gift_taste_dislike:
                            player.mood.AddMoodElement("NPC Disliked Gift", -5, 120, 
                                $"{__instance.Name} didn't like your gift");
                            break;
                        case NPC.gift_taste_hate:
                            player.mood.AddMoodElement("NPC Hated Gift", -10, 120, 
                                $"{__instance.Name} hated your gift");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(GiftReaction_PostFix)}:\n{ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Track tool usage for monotony detection
        /// </summary>
        public static void ToolUsed_Track(Farmer __instance)
        {
            try
            {
                if (!__instance.IsLocalPlayer || GameState == null || !GameState.IsInitialized)
                    return;

                if (!ModConfig.GetInstance().UseSanityModule)
                    return;

                var tool = __instance.CurrentTool;
                if (tool == null) return;

                var player = GameState.GetPlayerModel();
                if (player?.mood != null)
                {
                    string actionType = tool switch
                    {
                        Hoe => "Hoeing",
                        WateringCan => "Watering",
                        Axe => "Lumbering",
                        Pickaxe => "Mining",
                        FishingRod => "Fishing",
                        MeleeWeapon => "Combat",
                        _ => "Tool Use"
                    };

                    player.mood.TrackAction(actionType);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(ToolUsed_Track)}:\n{ex}", LogLevel.Error);
            }
        }
    }
}
