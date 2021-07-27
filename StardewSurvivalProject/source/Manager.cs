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
        private ModConfig Config = null;

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
        }

        public void onEnvUpdate(int time, string season, int weatherId)
        {
            //TODO: temp follow a curve increase from morning to noon and decrease from noon to midnight, also depend on current season and current weather
            const double BASE_VALUE = 27.0;
            double value = BASE_VALUE;
            //start with applying adjustment based on season
            if (season.Equals("spring") || season.Equals("fall")) value *= 0.9;
            else if (season.Equals("summer")) value *= 1.1;
            else if (season.Equals("winter")) value *= 0.2;

            //next, check for weather
            switch (weatherId)
            {
                case (int)weatherType.SUNNY: case (int)weatherType.FESTIVAL: case (int)weatherType.WEDDING:
                    value *= 1.2; break;
                case (int)weatherType.STORM:
                    value *= 0.8; break;
                case (int)weatherType.RAIN:
                    value *= 0.8; break;
                case (int)weatherType.WINDY:
                    value *= 0.9; break;
                case (int)weatherType.SNOW:
                    value *= -1; break;
                default: break;
            }

            //next, check for time
            //convert time to actual decimal format to run on a time-dependent function
            double decTime = ((double)(time / 100) + ((double)(time % 100) / 60.0));
            LogHelper.Debug(decTime.ToString());
            //curve look good enough on desmos so YOLO
            double timeTempModifier = Math.Sin((decTime - 8.5) / (Math.PI * 1.2)) * 3; //TODO change number 3 to a season-dependent multiplier
            value += timeTempModifier;

            //finally, add some randomness XD

            value += rand.NextDouble() - 0.5;
            envTemp.value = value;
        }

        public void onClockUpdate()
        {
            if (player == null) return;
            player.updateDrain();
            //TODO: update player body temp based on env temp, whether user is indoor and nearby heating / cooling source (light source / nearby big craftable)

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
            if (!ModConfig.GetInstance().UseOnToolUseDrain) return;

            if (toolHold is StardewValley.Tools.Axe)
                player.updateActiveDrain(-ModConfig.GetInstance().AxeHungerDrain, -ModConfig.GetInstance().AxeThirstDrain);

            else if (toolHold is StardewValley.Tools.Hoe)
                player.updateActiveDrain(-ModConfig.GetInstance().HoeHungerDrain, -ModConfig.GetInstance().HoeThirstDrain);

            else if (toolHold is StardewValley.Tools.Pickaxe)
                player.updateActiveDrain(-ModConfig.GetInstance().PickaxeHungerDrain, -ModConfig.GetInstance().PickaxeThirstDrain);

            else if (toolHold is StardewValley.Tools.MeleeWeapon)
                player.updateActiveDrain(-ModConfig.GetInstance().MeleeWeaponHungerDrain, -ModConfig.GetInstance().MeleeWeaponThirstDrain);

            else if (toolHold is StardewValley.Tools.Slingshot)
                player.updateActiveDrain(-ModConfig.GetInstance().SlingshotHungerDrain, -ModConfig.GetInstance().SlingshotThirstDrain);

            else if (toolHold is StardewValley.Tools.WateringCan)
                player.updateActiveDrain(-ModConfig.GetInstance().WateringCanHungerDrain, -ModConfig.GetInstance().WateringCanThirstDrain);

            else if (toolHold is StardewValley.Tools.FishingRod)
                player.updateActiveDrain(-ModConfig.GetInstance().FishingPoleHungerDrain, -ModConfig.GetInstance().FishingPoleThirstDrain);

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
    }
}
