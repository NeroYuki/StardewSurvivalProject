using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewSurvivalProject
{
    public class ModConfig
    {
        //UI config
        public int UIOffsetX { get; set; } = 10;
        public int UIOffsetY { get; set; } = 10;
        public float UIScale { get; set; } = 4.0f;
        public bool IsAnchoredDown { get; set; } = false;

        //Difficulty config
        public double PassiveHungerDrainRate { get; set; } = 0.2;
        public double PassiveThirstDrainRate { get; set; } = 0.3;
        public double RunningHungerDrainRate { get; set; } = 0.001;
        public double RunningThirstDrainRate { get; set; } = 0.002;
        public double MeleeWeaponHungerDrain { get; set; } = 0.5;
        public double MeleeWeaponThirstDrain { get; set; } = 0.2;
        public double SlingshotHungerDrain { get; set; } = 0.5;
        public double SlingshotThirstDrain { get; set; } = 0.2;
        public double PickaxeHungerDrain { get; set; } = 0.5;
        public double PickaxeThirstDrain { get; set; } = 0.2;
        public double AxeHungerDrain { get; set; } = 0.5;
        public double AxeThirstDrain { get; set; } = 0.2;
        public double FishingPoleHungerDrain { get; set; } = 2.0;
        public double FishingPoleThirstDrain { get; set; } = 0.6;
        public double HoeHungerDrain { get; set; } = 0.5;
        public double HoeThirstDrain { get; set; } = 0.2;
        public double WateringCanHungerDrain { get; set; } = 0.5;
        public double WateringCanThirstDrain { get; set; } = 0.2;
        public double DefaultHydrationGainOnDrinkableItems { get; set; } = 10.0;
        public double HydrationGainOnEnvironmentWaterDrinking { get; set; } = 5.0;
        public double HungerGainMultiplierFromItemEdibility { get; set; } = 1.0;
        public int HealthPenaltyOnDehydration { get; set; } = 10;
        public int StaminaPenaltyOnStarvation { get; set; } = 10;

        public double MaxHunger { get; set; } = 100;
        public double MaxThirst { get; set; } = 100;

        //Feature config
        public bool UsePassiveDrain { get; set; } = true;
        public bool UseOnRunningDrain { get; set; } = true;
        public bool UseOnToolUseDrain { get; set; } = true;
        public bool UseTemperatureModule { get; set; } = true;
        public bool DisableHPHealingOnEatingFood { get; set; } = true;
        public bool UseOvernightPassiveDrain { get; set; } = true;

        private static ModConfig _instance;

        // This is the static method that controls the access to the singleton
        // instance. On the first run, it creates a singleton object and places
        // it into the static field. On subsequent runs, it returns the client
        // existing object stored in the static field.
        public static ModConfig GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ModConfig();
            }
            return _instance;
        }

        public void SetConfig(ModConfig input)
        {
            if (_instance == null)
            {
                _instance = new ModConfig();
            }
            if (input != null)
            {
                _instance = input;
            }
        }

    }
}
