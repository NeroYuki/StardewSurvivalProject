using System;
using StardewValley;
using StardewModdingAPI;
using SObject = StardewValley.Object;
using StardewSurvivalProject.source.utils;
using System.IO;
using System.Collections.Generic;

namespace StardewSurvivalProject.source
{
    public class Manager
    {
        private model.Player player;
        private model.EnvTemp envTemp;
        private String displayString = "";
        private Random rand = null;

        private string RelativeDataPath => Path.Combine("data", $"{Constants.SaveFolderName}.json");

        public Manager()
        {
            player = null;
            envTemp = null;
        }

        public void init(Farmer farmer)
        {
            player = new model.Player(farmer);
            envTemp = new model.EnvTemp();
            displayString = player.getStatStringUI();
            LogHelper.Debug("Manager initialized");
            rand = new Random();
        }

        public void onSecondUpdate()
        {
            //poition effect apply here
            if (player.hunger.value <= 0) effects.EffectManager.applyEffect(effects.EffectManager.starvationEffectIndex);
            if (player.thirst.value <= 0) effects.EffectManager.applyEffect(effects.EffectManager.dehydrationEffectIndex);
            if (player.temp.value >= model.BodyTemp.HeatstrokeThreshold) effects.EffectManager.applyEffect(effects.EffectManager.heatstrokeEffectIndex);
            if (player.temp.value <= model.BodyTemp.HypotherminaThreshold) effects.EffectManager.applyEffect(effects.EffectManager.hypothermiaEffectIndex);
            if (player.temp.value >= model.BodyTemp.BurnThreshold) effects.EffectManager.applyEffect(effects.EffectManager.burnEffectIndex);
            if (player.temp.value <= model.BodyTemp.FrostbiteThreshold) effects.EffectManager.applyEffect(effects.EffectManager.frostbiteEffectIndex);

            //the real isPause code xd
            if (!Game1.eventUp && (Game1.activeClickableMenu == null || Game1.IsMultiplayer) && !Game1.paused)
            {
                //apply some effects' result every second
                if (Game1.buffsDisplay.otherBuffs.Exists(e => e.which == effects.EffectManager.stomachacheEffectIndex))
                {
                    player.updateActiveDrain(-model.Hunger.DEFAULT_VALUE * 0.01, 0);
                }
                if (Game1.buffsDisplay.otherBuffs.Exists(e => e.which == effects.EffectManager.burnEffectIndex || e.which == effects.EffectManager.frostbiteEffectIndex))
                {
                    player.bindedFarmer.health -= 3;
                    Game1.currentLocation.playSound("ow");
                    Game1.hitShakeTimer = 100 * 3;
                }
                if (Game1.buffsDisplay.otherBuffs.Exists(e => e.which == effects.EffectManager.heatstrokeEffectIndex))
                {
                    player.updateActiveDrain(0, -0.8);
                }
            }
        }

        public void onEnvUpdate(int time, string season, int weatherIconId, GameLocation location = null, int currentMineLevel = 0)
        {
            envTemp.updateEnvTemp(time, season, weatherIconId, location, currentMineLevel);
            envTemp.updateLocalEnvTemp(player.bindedFarmer.getTileX(), player.bindedFarmer.getTileY());
        }

        public void onClockUpdate()
        {
            if (player == null) return;
            player.updateDrain();
            //TODO: update player body temp based on env temp, whether user is indoor and nearby heating / cooling source (light source / nearby big craftable)
            player.updateBodyTemp(envTemp);
            displayString = player.getStatStringUI();
        }

        public void onEatingFood(SObject gameObj)
        {
            if (player == null) return;

            //addition: if player is drinking a refillable container, give back the empty container item
            if (gameObj.name.Equals("Full Canteen"))
            {
                LogHelper.Debug("finding item");
                //why i have to do this?
                //TODO: cache container id on json shuffle process
                foreach (KeyValuePair<int, string> itemInfoString in Game1.objectInformation)
                {
                    //check string start for object name
                    if (itemInfoString.Value.StartsWith("Canteen/")) {
                        player.bindedFarmer.addItemToInventory(new SObject(itemInfoString.Key, 1));
                    }
                }
            }

            //check if eaten food is cooked or artisan product, if no apply chance for stomachache effect
            if (gameObj.Category != SObject.CookingCategory && gameObj.Category != SObject.artisanGoodsCategory)
            {
                if (rand.NextDouble() * 100 >= (100 - 5))
                    effects.EffectManager.applyEffect(effects.EffectManager.stomachacheEffectIndex);
            }

            //band-aid fix coming, if edibility is 1 and healing value is not 0, dont add hunger
            //TODO: document this weird anomaly
            if (data.HealingItemDictionary.getHealingValue(gameObj.name) > 0 && gameObj.Edibility == 1) return;

            double addHunger = gameObj.Edibility * ModConfig.GetInstance().HungerGainMultiplierFromItemEdibility;
            player.updateEating(addHunger);
                
            displayString = player.getStatStringUI();
        }

        public void setPlayerHunger(double amt)
        {
            if (player == null || amt < 0 || amt > 1000000) return;
            player.hunger.value = amt;
        }

        public void setPlayerThirst(double amt)
        {
            if (player == null || amt < 0 || amt > 1000000) return;
            player.thirst.value = amt;
        }

        public  void setPlayerBodyTemp(double v)
        {
            if (player == null || v < -274 || v > 10000) return;
            player.temp.value = v;
        }

        public string getDisplayString()
        {
            return displayString;
        }

        public void onExit()
        {
            if (player == null) return;
            else player = null;
        }

        public void onEnvDrinkingUpdate(bool isOcean, bool isWater)
        {
            if (player == null) return;
            double addThirst = isWater ? ModConfig.GetInstance().HydrationGainOnEnvironmentWaterDrinking : 0;
            addThirst = isOcean ? -ModConfig.GetInstance().HydrationGainOnEnvironmentWaterDrinking : addThirst;

            //294 is drinking animation id
            player.bindedFarmer.animateOnce(294);

            //set isEating to true to prevent constant drinking by spamming action button 
            //FIXME: conflicted with spacecore's DoneEating event
            player.bindedFarmer.isEating = true;
            //Fixing by setting itemToEat to something that doesnt do anything to player HP and stamina (in this case, daffodil)
            player.bindedFarmer.itemToEat = (Item)new SObject(18, 1); 

            player.updateDrinking(addThirst);
            displayString = player.getStatStringUI();
        }

        public void onDayEnding()
        {
            //clear all buff on day ending (bug-free?)
            Game1.buffsDisplay.clearAllBuffs();

            if (player == null || !ModConfig.GetInstance().UseOvernightPassiveDrain || player.bindedFarmer.passedOut) return;
            //24 mean 240 minutes of sleep (from 2am to 6am)
            player.updateActiveDrain(-ModConfig.GetInstance().PassiveHungerDrainRate * 24, -ModConfig.GetInstance().PassiveThirstDrainRate * 24);
        }

        public void updateOnRunning()
        {
            if (player == null || !ModConfig.GetInstance().UseOnRunningDrain) return;
            double THIRST_DRAIN_ON_RUNNING = ModConfig.GetInstance().RunningThirstDrainRate, HUNGER_DRAIN_ON_RUNNING = ModConfig.GetInstance().RunningHungerDrainRate;
            if (player.thirst.value <= THIRST_DRAIN_ON_RUNNING || player.hunger.value <= HUNGER_DRAIN_ON_RUNNING)
            {
                player.bindedFarmer.setRunning(false, true);
                return;
            }
            player.updateRunningDrain();
        }

        internal void ResetPlayerHungerAndThirst()
        {
            if (player == null) return;

            player.resetPlayerHungerAndThirst();
            LogHelper.Debug("Reset player stats");
        }

        public void onItemDrinkingUpdate(SObject gameObj)
        {
            if (player == null) return;
            double addThirst = data.CustomHydrationDictionary.getHydrationValue(gameObj.name);
            if (addThirst == 0) addThirst = ModConfig.GetInstance().DefaultHydrationGainOnDrinkableItems;

            player.updateDrinking(addThirst);
            displayString = player.getStatStringUI();
        }

        public String getPlayerHungerStat()
        {
            return $"{player.hunger.value.ToString("#.##")} / {model.Hunger.DEFAULT_VALUE}";
        }

        public double getPlayerHungerPercentage()
        {
            return player.hunger.value / model.Hunger.DEFAULT_VALUE;
        }

        public String getPlayerThirstStat()
        {
            return $"{player.thirst.value.ToString("#.##")} / {model.Thirst.DEFAULT_VALUE}";
        }

        public double getPlayerThirstPercentage()
        {
            return player.thirst.value / model.Thirst.DEFAULT_VALUE;
        }

        public double getPlayerBodyTemp()
        {
            return player.temp.value;
        }

        public double getEnvTemp()
        {
            return this.envTemp.value;
        }

        public void updateOnToolUsed(StardewValley.Tool toolHold)
        {
            bool isFever = Game1.buffsDisplay.otherBuffs.Exists(e => e.which == effects.EffectManager.feverEffectIndex);
            int power = (int)((player.bindedFarmer.toolHold + 20f) / 600f) + 1;
            LogHelper.Debug($"Tool Power = {power}");

            if (!ModConfig.GetInstance().UseOnToolUseDrain) return;

            //yea this is terrible
            //TODO: more generic code
            if (toolHold is StardewValley.Tools.Axe)
            {
                player.updateActiveDrain(-ModConfig.GetInstance().AxeHungerDrain, -ModConfig.GetInstance().AxeThirstDrain);
                if (isFever)
                {
                    player.bindedFarmer.stamina -= ((float)(2 * power) - (float)player.bindedFarmer.ForagingLevel * 0.1f) * 2.0f;
                    Game1.staminaShakeTimer += 100;
                }
            }
            else if (toolHold is StardewValley.Tools.Hoe)
            {
                player.updateActiveDrain(-ModConfig.GetInstance().HoeHungerDrain, -ModConfig.GetInstance().HoeThirstDrain);
                if (isFever)
                {
                    player.bindedFarmer.stamina -= ((float)(2 * power) - (float)player.bindedFarmer.FarmingLevel * 0.1f) * 2.0f;
                    Game1.staminaShakeTimer += 100;
                }
            }
            else if (toolHold is StardewValley.Tools.Pickaxe)
            {
                player.updateActiveDrain(-ModConfig.GetInstance().PickaxeHungerDrain, -ModConfig.GetInstance().PickaxeThirstDrain);
                if (isFever)
                {
                    player.bindedFarmer.stamina -= ((float)(2 * power) - (float)player.bindedFarmer.MiningLevel * 0.1f) * 2.0f;
                    Game1.staminaShakeTimer += 100;
                }
            }
            else if (toolHold is StardewValley.Tools.MeleeWeapon)
            {
                player.updateActiveDrain(-ModConfig.GetInstance().MeleeWeaponHungerDrain, -ModConfig.GetInstance().MeleeWeaponThirstDrain);
                if (isFever)
                {
                    player.bindedFarmer.stamina -= (1f - (float)player.bindedFarmer.CombatLevel * 0.08f) * 2.0f;
                }
            }
            else if (toolHold is StardewValley.Tools.Slingshot)
            {
                player.updateActiveDrain(-ModConfig.GetInstance().SlingshotHungerDrain, -ModConfig.GetInstance().SlingshotThirstDrain);
                if (isFever)
                {
                    player.bindedFarmer.stamina -= (1f - (float)player.bindedFarmer.CombatLevel * 0.08f) * 2.0f;
                }
            }
            else if (toolHold is StardewValley.Tools.WateringCan)
            {
                player.updateActiveDrain(-ModConfig.GetInstance().WateringCanHungerDrain, -ModConfig.GetInstance().WateringCanThirstDrain);
                if (isFever)
                {
                    player.bindedFarmer.stamina -= ((float)(2 * (power + 1)) - (float)player.bindedFarmer.FarmingLevel * 0.1f) * 2.0f;
                    Game1.staminaShakeTimer += 100;
                }
            }
            else if (toolHold is StardewValley.Tools.FishingRod)
            {
                player.updateActiveDrain(-ModConfig.GetInstance().FishingPoleHungerDrain, -ModConfig.GetInstance().FishingPoleThirstDrain);
                if (isFever)
                {
                    player.bindedFarmer.stamina -= (8f - (float)player.bindedFarmer.FishingLevel * 0.1f) * 2.0f;
                    Game1.staminaShakeTimer += 100;
                }
            }
            else if (toolHold is StardewValley.Tools.MilkPail)
            {
                player.updateActiveDrain(-1.0, -1.0);
                if (isFever)
                {
                    player.bindedFarmer.stamina -= (4f - (float)player.bindedFarmer.FarmingLevel * 0.1f) * 2.0f;
                    Game1.staminaShakeTimer += 100;
                }
            }
            else if (toolHold is StardewValley.Tools.Shears)
            {
                player.updateActiveDrain(-1.0, -1.0);
                if (isFever)
                {
                    player.bindedFarmer.stamina -= (4f - (float)player.bindedFarmer.FarmingLevel * 0.1f) * 2.0f;
                    Game1.staminaShakeTimer += 100;
                }
            }
            else
                LogHelper.Debug("Unknown tool type");

            displayString = player.getStatStringUI();
        }

        public class SaveData
        {
            public model.Hunger hunger;
            public model.Thirst thirst;
            public model.BodyTemp bodyTemp;

            public SaveData(model.Hunger h, model.Thirst t, model.BodyTemp bt)
            {
                this.hunger = h;
                this.thirst = t;
                this.bodyTemp = bt;
            }
        }

        public void loadData(Mod context)
        {
            SaveData saveData = context.Helper.Data.ReadJsonFile<SaveData>(this.RelativeDataPath);
            if (saveData != null)
            {
                this.player.hunger = saveData.hunger;
                this.player.thirst = saveData.thirst;
                this.player.temp = saveData.bodyTemp;
            }
        }

        public void saveData(Mod context)
        {

            SaveData savingData = new SaveData(this.player.hunger, this.player.thirst, this.player.temp);
            context.Helper.Data.WriteJsonFile<SaveData>(this.RelativeDataPath, savingData);
        }

        internal void dayStartProcedure()
        {
            double dice_roll = rand.NextDouble() * 100;
            if (dice_roll >= 100 - 2)
            {
                effects.EffectManager.applyEffect(effects.EffectManager.feverEffectIndex);
            }
        }

    }
}
