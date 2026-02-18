using System;
using StardewValley;
using SObject = StardewValley.Object;
using StardewModdingAPI;

namespace StardewSurvivalProject.source.core
{
    /// <summary>
    /// Main coordinator for game state - manages all survival systems
    /// This is the lightweight replacement for the monolithic Manager class
    /// </summary>
    public class GameStateManager
    {
        private readonly systems.PlayerStatsSystem playerStats;
        private readonly systems.TemperatureSystem temperature;
        private readonly systems.StaminaSystem stamina;
        private readonly SaveManager saveManager;

        public GameStateManager(IModHelper helper)
        {
            playerStats = new systems.PlayerStatsSystem();
            temperature = new systems.TemperatureSystem();
            stamina = new systems.StaminaSystem();
            saveManager = new SaveManager(helper);
        }

        public void Initialize(Farmer farmer)
        {
            playerStats.Initialize(farmer);
            LogHelper.Debug("GameStateManager initialized");
        }

        public bool IsInitialized => playerStats.IsInitialized;

        // === Core Update Methods ===

        /// <summary>
        /// Called every second to update effects and systems
        /// </summary>
        public void OnSecondUpdate()
        {
            if (!IsInitialized) return;

            ApplyEffects();
            ApplyEffectConsequences();
            stamina.UpdateStaminaRegen(Game1.player);
        }

        /// <summary>
        /// Called every 10 in-game minutes to update hunger/thirst/temp
        /// </summary>
        public void OnClockUpdate()
        {
            if (!IsInitialized) return;

            playerStats.UpdateTimeDrain();
            
            if (ModConfig.GetInstance().UseTemperatureModule)
            {
                playerStats.UpdateBodyTemperature(temperature.GetEnvTempModel());
            }
            
            if (ModConfig.GetInstance().UseSanityModule)
            {
                playerStats.UpdateMoodElements();
                playerStats.CheckMentalBreak();
            }
        }

        /// <summary>
        /// Update environmental temperature
        /// </summary>
        public void OnEnvironmentUpdate(int time, string season, int weatherIconId, GameLocation location = null, int currentMineLevel = 0)
        {
            temperature.UpdateEnvironmentTemp(time, season, weatherIconId, location, currentMineLevel);
        }

        // === Effect Management ===

        /// <summary>
        /// Apply all survival effects based on current stats
        /// </summary>
        private void ApplyEffects()
        {
            double hungerPercent = playerStats.GetHungerPercentage() * 100;
            double thirstPercent = playerStats.GetThirstPercentage() * 100;
            double bodyTemp = playerStats.GetBodyTemperature();
            double envTemp = temperature.GetEnvTemp();

            // Positive effects
            if (hungerPercent >= ModConfig.GetInstance().HungerWellFedEffectPercentageThreshold &&
                thirstPercent >= ModConfig.GetInstance().ThirstWellFedEffectPercentageThreshold)
                effects.EffectManager.applyEffect(effects.EffectManager.wellFedEffectIndex);

            if (envTemp >= playerStats.GetMinComfortTemp() && envTemp <= playerStats.GetMaxComfortTemp())
                effects.EffectManager.applyEffect(effects.EffectManager.refreshingEffectIndex);

            // Negative effects
            if (hungerPercent <= ModConfig.GetInstance().HungerEffectPercentageThreshold)
                effects.EffectManager.applyEffect(effects.EffectManager.hungerEffectIndex);
            if (hungerPercent <= 0)
                effects.EffectManager.applyEffect(effects.EffectManager.starvationEffectIndex);

            if (thirstPercent <= ModConfig.GetInstance().ThirstEffectPercentageThreshold)
                effects.EffectManager.applyEffect(effects.EffectManager.thirstEffectIndex);
            if (thirstPercent <= 0)
                effects.EffectManager.applyEffect(effects.EffectManager.dehydrationEffectIndex);

            // Temperature effects
            if (bodyTemp >= model.BodyTemp.HeatstrokeThreshold)
                effects.EffectManager.applyEffect(effects.EffectManager.heatstrokeEffectIndex);
            if (bodyTemp <= model.BodyTemp.HypotherminaThreshold)
                effects.EffectManager.applyEffect(effects.EffectManager.hypothermiaEffectIndex);
            if (bodyTemp >= model.BodyTemp.BurnThreshold)
                effects.EffectManager.applyEffect(effects.EffectManager.burnEffectIndex);
            if (bodyTemp <= model.BodyTemp.FrostbiteThreshold)
                effects.EffectManager.applyEffect(effects.EffectManager.frostbiteEffectIndex);
        }

        /// <summary>
        /// Apply consequences of active effects (damage, drain, etc.)
        /// </summary>
        private void ApplyEffectConsequences()
        {
            // Only apply when game is not paused
            if (Game1.eventUp || (Game1.activeClickableMenu != null && !Game1.IsMultiplayer) || Game1.paused)
                return;

            if (Game1.player.buffs.IsApplied("neroyuki.rlvalley/stomachache"))
            {
                double hungerDrain = -model.Hunger.DEFAULT_VALUE * (ModConfig.GetInstance().StomachacheHungerPercentageDrainPerSecond / 100);
                playerStats.ApplyHungerDrain(hungerDrain);
            }

            if (Game1.player.buffs.IsApplied("neroyuki.rlvalley/burn"))
            {
                Game1.player.health -= ModConfig.GetInstance().HealthDrainOnBurnPerSecond;
                Game1.currentLocation.playSound("ow");
                Game1.hitShakeTimer = 100 * ModConfig.GetInstance().HealthDrainOnBurnPerSecond;
            }
            else if (Game1.player.buffs.IsApplied("neroyuki.rlvalley/frostbite"))
            {
                Game1.player.health -= ModConfig.GetInstance().HealthDrainOnFrostbitePerSecond;
                Game1.currentLocation.playSound("ow");
                Game1.hitShakeTimer = 100 * ModConfig.GetInstance().HealthDrainOnFrostbitePerSecond;
            }

            if (Game1.player.buffs.IsApplied("neroyuki.rlvalley/heatstroke"))
            {
                double thirstDrain = -ModConfig.GetInstance().HeatstrokeThirstDrainPerSecond;
                playerStats.ApplyThirstDrain(thirstDrain);
            }
        }

        // === Event Handlers ===

        public void OnItemEaten(SObject gameObj) => playerStats.HandleItemConsumed(gameObj);
        public void OnEnvironmentalDrink(bool isOcean, bool isWater) => playerStats.HandleEnvironmentalDrinking(isOcean, isWater);
        public void OnRunning(bool isSprinting) => playerStats.UpdateRunningDrain(isSprinting);
        public void OnRunningWithStamina(bool isSprinting) => stamina.HandleRunningStaminaDrain(Game1.player, isSprinting);
        public void OnToolUsed(Tool tool) => stamina.HandleToolStaminaDrain(Game1.player, tool);
        public void OnGiftGiven(NPC npc, SObject gift) => playerStats.HandleGiftToSpouse(npc, gift);
        public void OnDayEnding() => playerStats.HandleOvernightDrain();
        public void OnDayStarted() 
        { 
            playerStats.HandleDayStart();
            if (ModConfig.GetInstance().UseSanityModule)
            {
                playerStats.OnMoodDayStart();
            }
        }
        public void ResetPlayerStats() => playerStats.ResetStats();
        public void OnExit() => playerStats.Cleanup();

        // === Save/Load ===

        public void LoadData(Mod context)
        {
            var saveData = saveManager.LoadData();
            if (saveData != null)
            {
                playerStats.LoadFromModels(saveData.hunger, saveData.thirst, saveData.bodyTemp, 
                    saveData.mood, saveData.healthPoint);
            }
        }

        public void SaveData(Mod context)
        {
            var saveData = saveManager.CreateSaveData(
                playerStats.GetHungerModel(),
                playerStats.GetThirstModel(),
                playerStats.GetBodyTempModel(),
                playerStats.HealthPoint,
                playerStats.GetMoodModel());
            saveManager.SaveData(saveData);
        }

        // === Getters for UI ===

        public string getPlayerHungerStat() => playerStats.GetHungerStat();
        public double getPlayerHungerPercentage() => playerStats.GetHungerPercentage();
        public double getPlayerHungerSaturationStat() => playerStats.GetHungerSaturation();
        public string getPlayerThirstStat() => playerStats.GetThirstStat();
        public double getPlayerThirstPercentage() => playerStats.GetThirstPercentage();
        public double getPlayerBodyTemp() => playerStats.GetBodyTemperature();
        public string getPlayerBodyTempString() => playerStats.GetBodyTempString();
        public double getEnvTemp() => temperature.GetEnvTemp();
        public string getEnvTempString() => temperature.GetEnvTempString();
        public double getMinComfyEnvTemp() => playerStats.GetMinComfortTemp();
        public double getMaxComfyEnvTemp() => playerStats.GetMaxComfortTemp();
        public int getPlayerMoodIndex() => playerStats.GetMoodIndex();
        public string getPlayerMoodStat() => playerStats.GetMoodStat();
        public model.Player GetPlayerModel() => playerStats.GetPlayerModel();

        // === Debug Commands ===

        public void setPlayerHunger(double amt) => playerStats.SetHunger(amt);
        public void setPlayerThirst(double amt) => playerStats.SetThirst(amt);
        public void setPlayerBodyTemp(double v) => playerStats.SetBodyTemp(v);
        public void setPlayerMood(double v) => playerStats.SetMood(v);
    }
}
