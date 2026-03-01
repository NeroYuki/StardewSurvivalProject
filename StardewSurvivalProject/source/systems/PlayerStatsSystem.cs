using System;
using StardewValley;
using SObject = StardewValley.Object;

namespace StardewSurvivalProject.source.systems
{
    /// <summary>
    /// Manages player survival stats: hunger, thirst, body temperature, and mood
    /// Provides clean API for querying and updating player stats
    /// </summary>
    public class PlayerStatsSystem
    {
        private model.Player player;
        private Random rand;
        public int HealthPoint { get; set; }

        public PlayerStatsSystem()
        {
            player = null;
            rand = new Random();
        }

        /// <summary>
        /// Initialize the player stats system
        /// </summary>
        public void Initialize(Farmer farmer)
        {
            player = new model.Player(farmer);
            HealthPoint = farmer.maxHealth;
            LogHelper.Debug("PlayerStatsSystem initialized");
        }

        /// <summary>
        /// Check if system is initialized
        /// </summary>
        public bool IsInitialized => player != null;

        /// <summary>
        /// Update body temperature based on environmental temperature
        /// </summary>
        public void UpdateBodyTemperature(model.EnvTemp envTemp)
        {
            if (player == null || !ModConfig.GetInstance().UseTemperatureModule) return;
            player.updateBodyTemp(envTemp);
        }

        /// <summary>
        /// Update hunger and thirst drain based on time passing
        /// </summary>
        public void UpdateTimeDrain()
        {
            if (player == null) return;
            player.updateDrain();
        }

        /// <summary>
        /// Check and handle mental breakdown if mood is too low
        /// </summary>
        public void CheckMentalBreak()
        {
            if (player == null || !ModConfig.GetInstance().UseSanityModule) return;
            player.mood.CheckForMentalBreak();
        }
        
        /// <summary>
        /// Update mood elements (decay and expiration)
        /// </summary>
        public void UpdateMoodElements()
        {
            if (player == null || !ModConfig.GetInstance().UseSanityModule) return;
            player.mood.UpdateMoodElements();
        }
        
        /// <summary>
        /// Handle mood day start (reset daily elements)
        /// </summary>
        public void OnMoodDayStart()
        {
            if (player == null || !ModConfig.GetInstance().UseSanityModule) return;
            player.mood.OnDayStart();
        }

        /// <summary>
        /// Handle food/item consumption
        /// </summary>
        public void HandleItemConsumed(SObject gameObj)
        {
            if (player == null) return;

            // Return empty canteen if drinking from refillable container
            if (gameObj.name.Equals("Full Canteen") || gameObj.name.Equals("Dirty Canteen") || 
                gameObj.name.Equals("Ice Water Canteen") || gameObj.name.Equals("Ice Ionized Water Canteen") || 
                gameObj.name.Equals("Ionized Full Canteen"))
            {
                string itemId = data.ItemNameCache.getIDFromCache("Canteen");
                if (itemId != "-1")
                {
                    if (player.bindedFarmer.isInventoryFull())
                    {
                        Game1.createItemDebris(new SObject(itemId, 1), player.bindedFarmer.getStandingPosition(), 
                            player.bindedFarmer.FacingDirection, null);
                    }
                    player.bindedFarmer.addItemToInventory(new SObject(itemId, 1));
                }
            }

            // Apply stomachache chance for uncooked food
            if (gameObj.Category != SObject.CookingCategory && gameObj.Category != SObject.artisanGoodsCategory)
            {
                if (rand.NextDouble() * 100 >= (100 - ModConfig.GetInstance().PercentageChanceGettingStomachache))
                    effects.EffectManager.applyEffect(effects.EffectManager.stomachacheEffectIndex);
            }

            // Handle thirst restoration
            var isDrinkable = Game1.objectData[gameObj.ItemId].IsDrink;
            (double addThirst, double coolingModifier) = data.CustomHydrationDictionary.getHydrationAndCoolingModifierValue(gameObj.Name, isDrinkable);

            if (addThirst != 0)
            {
                double thirst = addThirst * (ModConfig.GetInstance().DefaultHydrationGainOnDrinkableItems / 10);
                if (thirst == 0) thirst = ModConfig.GetInstance().DefaultHydrationGainOnDrinkableItems;
                player.updateDrinking(thirst, coolingModifier);
            }
            else if (isDrinkable)
            {
                player.updateDrinking(ModConfig.GetInstance().DefaultHydrationGainOnDrinkableItems, 1);
            }

            // Handle healing items
            int healingValue = data.HealingItemDictionary.getHealingValue(gameObj.name);
            if (healingValue > 0 && gameObj.Edibility == 1) return;
            if (healingValue > 0 && gameObj.Edibility < 0 && gameObj.Edibility != -300)
            {
                player.bindedFarmer.health = Math.Min(player.bindedFarmer.maxHealth, player.bindedFarmer.health + healingValue);
            }

            // Handle hunger restoration
            (double addHunger, double hungerCoolingModifier) = data.CustomHungerDictionary.getHungerModifierAndCoolingModifierValue(gameObj, isDrinkable);
            player.updateEating(addHunger, coolingModifier == 0 ? hungerCoolingModifier : 0);
        }

        /// <summary>
        /// Handle drinking from environmental water sources
        /// </summary>
        public void HandleEnvironmentalDrinking(bool isOcean, bool isWater)
        {
            if (player == null) return;
            
            double addThirst = isWater ? ModConfig.GetInstance().HydrationGainOnEnvironmentWaterDrinking : 0;
            addThirst = isOcean ? -ModConfig.GetInstance().HydrationGainOnEnvironmentWaterDrinking : addThirst;

            player.bindedFarmer.animateOnce(294);
            player.bindedFarmer.isEating = true;
            player.bindedFarmer.itemToEat = new SObject("(O)18", 1);
            
            player.updateDrinking(addThirst);
        }

        /// <summary>
        /// Apply direct hunger drain (for effect consequences)
        /// </summary>
        public void ApplyHungerDrain(double amount)
        {
            if (player == null) return;
            player.updateHungerThirstDrain(amount, 0, true);
        }

        /// <summary>
        /// Apply direct thirst drain (for effect consequences)
        /// </summary>
        public void ApplyThirstDrain(double amount)
        {
            if (player == null) return;
            player.updateHungerThirstDrain(0, amount, false);
        }

        /// <summary>
        /// Update stats when running/sprinting
        /// </summary>
        public void UpdateRunningDrain(bool isSprinting = false)
        {
            if (player == null || !ModConfig.GetInstance().UseOnRunningDrain) return;

            double thirstDrain = ModConfig.GetInstance().RunningThirstDrainRate * (270f / player.bindedFarmer.MaxStamina);
            double hungerDrain = ModConfig.GetInstance().RunningHungerDrainRate * (270f / player.bindedFarmer.MaxStamina);

            if (player.thirst.value <= thirstDrain || player.hunger.value <= hungerDrain)
            {
                player.bindedFarmer.setRunning(false, true);
            }
            else
            {
                player.updateRunningDrain();
            }
        }

        /// <summary>
        /// Handle overnight passive drain
        /// </summary>
        public void HandleOvernightDrain()
        {
            if (player == null) return;

            if (!player.spouseFeed && player.bindedFarmer.getSpouse() != null)
            {
                player.bindedFarmer.changeFriendship(-ModConfig.GetInstance().FriendshipPenaltyOnNotFeedingSpouse, 
                    player.bindedFarmer.getSpouse());
                player.spouseFeed = false;
            }

            if (!ModConfig.GetInstance().UseOvernightPassiveDrain || player.bindedFarmer.passedOut) return;
            
            player.updateHungerThirstDrain(
                -ModConfig.GetInstance().PassiveHungerDrainRate * 24, 
                -ModConfig.GetInstance().PassiveThirstDrainRate * 24);

            HealthPoint = Math.Min(player.bindedFarmer.health + ModConfig.GetInstance().HealthRestoreOnSleep, 
                player.bindedFarmer.maxHealth);
        }

        /// <summary>
        /// Handle day start procedures (fever chance, reset saturation)
        /// </summary>
        public void HandleDayStart()
        {
            if (player == null) return;

            double diceRoll = rand.NextDouble() * 100;
            double feverChance = ModConfig.GetInstance().PercentageChanceGettingFever + 
                ModConfig.GetInstance().PercentageChanceGettingFever * (1 - player.bindedFarmer.stamina / player.bindedFarmer.MaxStamina);
            
            if (diceRoll >= 100 - feverChance)
            {
                effects.EffectManager.applyEffect(effects.EffectManager.feverEffectIndex);
            }

            player.bindedFarmer.health = HealthPoint;
            player.hunger.saturation = 0;
        }

        /// <summary>
        /// Handle gift given to spouse
        /// </summary>
        public void HandleGiftToSpouse(NPC npc, SObject gift)
        {
            if (player == null) return;
            
            if (npc == player.bindedFarmer.getSpouse() && gift.Category == SObject.CookingCategory)
            {
                player.spouseFeed = true;
            }
        }

        /// <summary>
        /// Reset player stats (on death/collapse)
        /// </summary>
        public void ResetStats()
        {
            if (player == null) return;
            player.resetPlayerHungerAndThirst();
            LogHelper.Debug("Reset player stats");
        }

        /// <summary>
        /// Cleanup on exit
        /// </summary>
        public void Cleanup()
        {
            player = null;
        }

        // Getters for stats
        public double GetHungerPercentage() => player?.hunger.value / model.Hunger.DEFAULT_VALUE ?? 0;
        public double GetThirstPercentage() => player?.thirst.value / model.Thirst.DEFAULT_VALUE ?? 0;
        public double GetHungerSaturation() => player?.hunger.saturation / 100 ?? 0;
        public double GetBodyTemperature() => player?.temp.value ?? 0;
        public double GetMinComfortTemp() => player?.temp.MinComfortTemp ?? 0;
        public double GetMaxComfortTemp() => player?.temp.MaxComfortTemp ?? 0;
        public int GetMoodIndex() => player != null ? Math.Max(0, Math.Min(7, (int)player.mood.Level)) : 4;
        public string GetMoodStat() => player?.mood.GetMoodBreakdown() ?? "N/A";
        public model.Player GetPlayerModel() => player;
        
        public string GetHungerStat() => player != null ? $"{player.hunger.value.ToString("#.##")} / {model.Hunger.DEFAULT_VALUE}" : "N/A";
        public string GetThirstStat() => player != null ? $"{player.thirst.value.ToString("#.##")} / {model.Thirst.DEFAULT_VALUE}" : "N/A";
        
        public string GetBodyTempString()
        {
            if (player == null) return "N/A";
            if (ModConfig.GetInstance().TemperatureUnit.Equals("Fahrenheit"))
                return ((player.temp.value * 9 / 5) + 32).ToString("#.##") + "F";
            else if (ModConfig.GetInstance().TemperatureUnit.Equals("Kelvin"))
                return (player.temp.value + 273).ToString("#.##") + "K";
            return player.temp.value.ToString("#.##") + "C";
        }

        // Setters for debug commands
        public void SetHunger(double amt) { if (player != null && amt >= 0 && amt <= 1000000) player.hunger.value = amt; }
        public void SetThirst(double amt) { if (player != null && amt >= 0 && amt <= 1000000) player.thirst.value = amt; }
        public void SetBodyTemp(double v) { if (player != null && v >= -274 && v <= 10000) player.temp.value = v; }
        public void SetMood(double v) { if (player != null && v >= -40 && v <= 120) player.mood.Value = v; }

        // Get internal models for save/load
        public model.Hunger GetHungerModel() => player.hunger;
        public model.Thirst GetThirstModel() => player.thirst;
        public model.BodyTemp GetBodyTempModel() => player.temp;
        public model.Mood GetMoodModel() => player.mood;

        // Set internal models for load
        public void LoadFromModels(model.Hunger hunger, model.Thirst thirst, model.BodyTemp bodyTemp, model.Mood mood, int healthPoint)
        {
            if (player == null) return;
            player.hunger = hunger;
            player.thirst = thirst;
            player.temp = bodyTemp;
            if (healthPoint > 0) HealthPoint = healthPoint;
            if (mood != null) player.mood = new model.Mood(mood, player.OnFarmerMentalBreak);
        }
    }
}
