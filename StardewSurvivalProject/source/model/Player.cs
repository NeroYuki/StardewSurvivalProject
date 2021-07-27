using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewModdingAPI;

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
            temp.value = rand.NextDouble() * 0.5 + 36.5;
            checkIsDangerValue();
        }

        public void checkIsDangerValue()
        {
            if (hunger.value <= 0)
            {
                hunger.value = 0;
                bindedFarmer.stamina -= ModConfig.GetInstance().StaminaPenaltyOnStarvation;
            }
            if (thirst.value <= 0)
            {
                thirst.value = 0;
                bindedFarmer.health -= ModConfig.GetInstance().HealthPenaltyOnDehydration;
            }
        }

        //TODO: update drain if player running, using tools
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

        public void updateDrinking(double addValue)
        {
            thirst.value = Math.Min(thirst.value + addValue, Thirst.DEFAULT_VALUE);
            if (addValue == 0) return;
            Game1.addHUDMessage(new HUDMessage($"{(addValue >= 0 ? "+" : "") + addValue} Hydration", (addValue >= 0 ? HUDMessage.stamina_type : HUDMessage.error_type)));
            checkIsDangerValue();
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
            hunger.value = Hunger.DEFAULT_VALUE / 4;
            thirst.value = Thirst.DEFAULT_VALUE / 4;
        }
    }
}
