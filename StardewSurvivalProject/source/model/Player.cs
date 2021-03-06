using System;
using System.Collections.Generic;
using StardewValley;
using StardewModdingAPI;
using Newtonsoft.Json;

namespace StardewSurvivalProject.source.model
{
    public class Player
    {
        public Farmer bindedFarmer { get; }
        public Hunger hunger;
        public BodyTemp temp;
        public Thirst thirst;
        private Random rand = new Random();

        public Player(Farmer farmer)
        {
            hunger = new Hunger();
            temp = new BodyTemp();
            thirst = new Thirst();
            bindedFarmer = farmer;
        }

        //update drain passively, should happen every 10 in-game minutes
        public void updateDrain()
        {
            if (ModConfig.GetInstance().UsePassiveDrain)
            {
                hunger.value -= ModConfig.GetInstance().PassiveHungerDrainRate;
                thirst.value -= ModConfig.GetInstance().PassiveThirstDrainRate;
            }
            checkIsDangerValue();
        }

        public void checkIsDangerValue()
        {
            if (hunger.value <= 0)
            {
                hunger.value = 0;
                int staminaPenalty = ModConfig.GetInstance().StaminaPenaltyOnStarvation;
                bindedFarmer.stamina -= staminaPenalty;
                //Game1.currentLocation.playSound("ow");
                Game1.staminaShakeTimer = 100 * staminaPenalty;
            }
            if (thirst.value <= 0)
            {
                thirst.value = 0;
                int healthPenalty = ModConfig.GetInstance().HealthPenaltyOnDehydration;
                bindedFarmer.health -= healthPenalty; 
                Game1.currentLocation.playSound("ow");
                Game1.hitShakeTimer = 100 * healthPenalty;
            }
        }

        public void updateActiveDrain(double deltaHunger, double deltaThirst)
        {
            hunger.value += deltaHunger;
            thirst.value += deltaThirst;
            checkIsDangerValue();
        }

        //update hunger after eating food
        public void updateEating(double addValue)
        {
            hunger.value = Math.Min(hunger.value + addValue, Hunger.DEFAULT_VALUE);
            if (addValue == 0) return;
            Game1.addHUDMessage(new HUDMessage($"{(addValue >= 0 ? "+" : "") + addValue} Hunger", (addValue >= 0 ? HUDMessage.stamina_type : HUDMessage.error_type)));
            checkIsDangerValue();
        }

        public void updateDrinking(double addValue, double cooling_modifier = 1)
        {
            thirst.value = Math.Min(thirst.value + addValue, Thirst.DEFAULT_VALUE);
            if (addValue == 0) return;
            Game1.addHUDMessage(new HUDMessage($"{(addValue >= 0 ? "+" : "") + addValue} Hydration", (addValue >= 0 ? HUDMessage.stamina_type : HUDMessage.error_type)));

            //cooling down player if water was drank
            if (addValue > 0 || this.temp.value > BodyTemp.DEFAULT_VALUE)
            {
                this.temp.value -= (this.temp.value - (BodyTemp.DEFAULT_VALUE)) * (1 - 1 / (0.01 * cooling_modifier * addValue + 1));
            }
            checkIsDangerValue();
        }

        public void updateBodyTemp(EnvTemp envTemp)
        {
            LogHelper.Debug($"shirtIndex={bindedFarmer.GetShirtIndex()} pantIndex={bindedFarmer.GetPantsIndex()}");
            String hat_name = "", shirt_name = "", pants_name = "", boots_name = "";
            if (bindedFarmer.hat.Value != null) hat_name = bindedFarmer.hat.Value.Name;
            if (bindedFarmer.shirtItem.Value != null) shirt_name = bindedFarmer.shirtItem.Value.Name;
            if (bindedFarmer.pantsItem.Value != null) pants_name = bindedFarmer.pantsItem.Value.Name;
            if (bindedFarmer.boots.Value != null) boots_name = bindedFarmer.boots.Value.Name;
            temp.updateComfortTemp(hat_name, shirt_name, pants_name, boots_name);
            temp.BodyTempCalc(envTemp, (rand.NextDouble() * 0.2) - 0.1);
        }

        public String getStatString()
        {
            return $"Hunger = {hunger.value.ToString("#.##")}; Thirst = {thirst.value.ToString("#.##")}; Body Temp. = {temp.value.ToString("#.##")}";
        }

        public String getStatStringUI()
        {
            return $"Hunger: {hunger.value.ToString("#.##")}\nThirst: {thirst.value.ToString("#.##")}\nBody Temp.: {temp.value.ToString("#.##")}";
        }

        internal void updateRunningDrain()
        {
            double THIRST_DRAIN_ON_RUNNING = ModConfig.GetInstance().RunningThirstDrainRate, HUNGER_DRAIN_ON_RUNNING = ModConfig.GetInstance().RunningHungerDrainRate;
            hunger.value -= HUNGER_DRAIN_ON_RUNNING;
            thirst.value -= THIRST_DRAIN_ON_RUNNING;
            checkIsDangerValue();
        }

        internal void resetPlayerHungerAndThirst()
        {
            if (ModConfig.GetInstance().HungerEffectPercentageThreshold > 0)
                hunger.value = Hunger.DEFAULT_VALUE * ModConfig.GetInstance().HungerEffectPercentageThreshold / 100;
            else
                hunger.value = Hunger.DEFAULT_VALUE / 4;
            if (ModConfig.GetInstance().ThirstEffectPercentageThreshold > 0)
                thirst.value = Thirst.DEFAULT_VALUE * ModConfig.GetInstance().ThirstEffectPercentageThreshold / 100;
            else 
                thirst.value = Thirst.DEFAULT_VALUE / 4;
        }
    }
}
