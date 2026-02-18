using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using StardewModdingAPI.Events;

namespace StardewSurvivalProject.source.handlers
{
    /// <summary>
    /// Handles mood-affecting game events
    /// </summary>
    public class MoodEventHandler
    {
        private readonly core.GameStateManager gameState;
        private readonly IMonitor monitor;
        private int lastSleepTime = 0;
        private bool passedOutYesterday = false;
        private HashSet<string> trackedBuffs = new HashSet<string>();
        private HashSet<string> trackedDebuffs = new HashSet<string>();

        public MoodEventHandler(core.GameStateManager gameState, IMonitor monitor)
        {
            this.gameState = gameState;
            this.monitor = monitor;
        }

        /// <summary>
        /// Handle day start events (festivals, new season, sleep quality)
        /// </summary>
        public void OnDayStarted()
        {
            if (!gameState.IsInitialized || !ModConfig.GetInstance().UseSanityModule)
                return;

            var player = gameState.GetPlayerModel();
            if (player?.mood == null) return;

            try
            {
                // Reset buff/debuff tracking for new day
                trackedBuffs.Clear();
                trackedDebuffs.Clear();
                
                // Check for festival day
                if (Utility.isFestivalDay())
                {
                    player.mood.AddMoodElement("Festival Day", 30, 1440, 
                        "Excited for today's festival");
                    monitor.Log("Festival day mood boost applied", LogLevel.Debug);
                }

                // Check for first day of season
                if (Game1.dayOfMonth == 1)
                {
                    player.mood.AddMoodElement("New Season", 10, 1440, 
                        "Fresh start of a new season");
                    monitor.Log("New season mood boost applied", LogLevel.Debug);
                }

                // Check sleep quality from previous night
                if (lastSleepTime > 0 && lastSleepTime <= 1200) // Slept before midnight
                {
                    // Well-slept bonus (expires at noon)
                    player.mood.AddMoodElement("Well-Slept", 5, 720, 
                        "Got a good night's rest");
                    monitor.Log("Well-slept mood boost applied", LogLevel.Debug);
                }

                // Check for insomnia (passed out after 2am)
                if (passedOutYesterday)
                {
                    player.mood.AddMoodElement("Insomnia", -5, 360, 
                        "Exhausted from passing out late");
                    monitor.Log("Insomnia mood penalty applied", LogLevel.Debug);
                    passedOutYesterday = false;
                }

                // Check for fever debuff
                if (Game1.player.buffs.IsApplied("neroyuki.rlvalley/fever"))
                {
                    player.mood.AddMoodElement("Fever", -10, -1, 
                        "Feeling sick with a fever");
                }

                lastSleepTime = 0;
            }
            catch (Exception ex)
            {
                monitor.Log($"Error in OnDayStarted mood handler: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Handle day ending (track sleep time)
        /// </summary>
        public void OnDayEnding()
        {
            if (!gameState.IsInitialized || !ModConfig.GetInstance().UseSanityModule)
                return;

            try
            {
                lastSleepTime = Game1.timeOfDay;
                passedOutYesterday = Game1.player.passedOut;
                
                monitor.Log($"Day ending tracked - Time: {lastSleepTime}, Passed out: {passedOutYesterday}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                monitor.Log($"Error in OnDayEnding mood handler: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Check for buff/debuff changes and mental break status
        /// </summary>
        public void OnUpdateTicked()
        {
            if (!gameState.IsInitialized || !ModConfig.GetInstance().UseSanityModule)
                return;

            try
            {
                var player = gameState.GetPlayerModel();
                if (player?.mood == null) return;

                // Check if fever status changed
                bool hasFever = Game1.player.buffs.IsApplied("neroyuki.rlvalley/fever");
                bool hasFeverMoodElement = player.mood.MoodElements.Exists(e => e.Name == "Fever");

                if (hasFever && !hasFeverMoodElement)
                {
                    // Fever just applied
                    player.mood.AddMoodElement("Fever", -10, -1, 
                        "Feeling sick with a fever");
                }
                else if (!hasFever && hasFeverMoodElement)
                {
                    // Fever removed
                    player.mood.MoodElements.RemoveAll(e => e.Name == "Fever");
                }

                // Track buffs and debuffs
                TrackBuffsAndDebuffs(player);
            }
            catch
            {
                // Don't log every tick to avoid spam
            }
        }

        /// <summary>
        /// Track active buffs and debuffs for mood effects
        /// </summary>
        private void TrackBuffsAndDebuffs(model.Player player)
        {
            if (player?.mood == null || Game1.player?.buffs == null) return;

            int buffCount = 0;
            int debuffCount = 0;
            var currentBuffs = new HashSet<string>();
            var currentDebuffs = new HashSet<string>();

            // Iterate through all active buffs
            foreach (var buff in Game1.player.buffs.AppliedBuffs.Values)
            {
                // Skip sprinting buff for mood calculation
                if (buff.id == null || 
                    buff.id.Contains("sprint", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Determine if it's a buff or debuff based on effects
                bool isDebuff = IsBuffDebuff(buff);

                if (isDebuff)
                {
                    currentDebuffs.Add(buff.id);
                    if (!trackedDebuffs.Contains(buff.id))
                    {
                        debuffCount++;
                        trackedDebuffs.Add(buff.id);
                    }
                }
                else
                {
                    currentBuffs.Add(buff.id);
                    if (!trackedBuffs.Contains(buff.id))
                    {
                        buffCount++;
                        trackedBuffs.Add(buff.id);
                    }
                }
            }

            // Remove buffs/debuffs that are no longer active
            trackedBuffs.RemoveWhere(b => !currentBuffs.Contains(b));
            trackedDebuffs.RemoveWhere(d => !currentDebuffs.Contains(d));

            // Apply mood modifiers for buffs (cap at 12 = 4 buffs * 3 each)
            int totalBuffMood = Math.Min(trackedBuffs.Count * 3, 12);
            if (totalBuffMood > 0)
            {
                player.mood.AddMoodElement("Active Buffs", totalBuffMood, -1, 
                    $"Feeling empowered by {trackedBuffs.Count} active buff{(trackedBuffs.Count > 1 ? "s" : "")}");
            }
            else
            {
                player.mood.MoodElements.RemoveAll(e => e.Name == "Active Buffs");
            }

            // Apply mood modifiers for debuffs (cap at -12 = 4 debuffs * -3 each)
            int totalDebuffMood = -Math.Min(trackedDebuffs.Count * 3, 12);
            if (totalDebuffMood < 0)
            {
                player.mood.AddMoodElement("Active Debuffs", totalDebuffMood, -1, 
                    $"Suffering from {trackedDebuffs.Count} debuff{(trackedDebuffs.Count > 1 ? "s" : "")}");
            }
            else
            {
                player.mood.MoodElements.RemoveAll(e => e.Name == "Active Debuffs");
            }
        }

        /// <summary>
        /// Determine if a buff is actually a debuff by checking the buff data
        /// </summary>
        private bool IsBuffDebuff(Buff buff)
        {
            if (buff.id == null) return false;

            try
            {
                // Try to look up the buff in the data to check IsDebuff flag
                var buffData = DataLoader.Buffs(Game1.content);
                if (buffData != null && buffData.TryGetValue(buff.id, out var data))
                {
                    return data.IsDebuff;
                }
            }
            catch
            {
                // Fall back to checking effects if data lookup fails
            }

            // Fallback: Check if any stats are negative
            if (buff.effects.Attack.Value < 0 ||
                buff.effects.Defense.Value < 0 ||
                buff.effects.Speed.Value < 0 ||
                buff.effects.FarmingLevel.Value < 0 ||
                buff.effects.FishingLevel.Value < 0 ||
                buff.effects.MiningLevel.Value < 0 ||
                buff.effects.LuckLevel.Value < 0 ||
                buff.effects.ForagingLevel.Value < 0 ||
                buff.effects.MaxStamina.Value < 0 ||
                buff.effects.MagneticRadius.Value < 0)
            {
                return true;
            }

            // Check for negative description keywords
            string description = buff.description?.ToLower() ?? "";
            if (description.Contains("sick") || description.Contains("weak") || 
                description.Contains("slow") || description.Contains("tired") ||
                description.Contains("nauseous") || description.Contains("injured"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Detect eating good food and apply mood boost
        /// </summary>
        public void OnItemEaten()
        {
            if (!gameState.IsInitialized || !ModConfig.GetInstance().UseSanityModule)
                return;

            try
            {
                var player = gameState.GetPlayerModel();
                if (player?.mood == null) return;

                var itemToEat = Game1.player.itemToEat as StardewValley.Object;
                if (itemToEat == null) return;

                // Check if it's cooked food (high quality)
                if (itemToEat.Category == StardewValley.Object.CookingCategory)
                {
                    // Calculate mood boost based on edibility (hunger restored)
                    double edibility = itemToEat.Edibility;
                    if (edibility >= 25)
                    {
                        // Scale from +5 to +20 based on edibility (25-100)
                        double moodBoost = 5 + Math.Min((edibility - 25) / 75.0, 1.0) * 15;
                        player.mood.AddMoodElement("Ate Good Food", moodBoost, 240,
                            $"Enjoyed a delicious {itemToEat.DisplayName}");
                        monitor.Log($"Good food mood boost applied: {moodBoost}", LogLevel.Debug);
                    }
                }
            }
            catch (Exception ex)
            {
                monitor.Log($"Error in OnItemEaten mood handler: {ex}", LogLevel.Error);
            }
        }
    }
}
