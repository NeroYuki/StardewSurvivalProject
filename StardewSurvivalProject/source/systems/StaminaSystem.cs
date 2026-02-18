using System;
using StardewValley;

namespace StardewSurvivalProject.source.systems
{
    /// <summary>
    /// Manages stamina rework mechanics: regeneration and drains for running/sprinting/tools
    /// </summary>
    public class StaminaSystem
    {
        /// <summary>
        /// Update stamina regeneration based on player state (standing, sitting, sleeping)
        /// </summary>
        public void UpdateStaminaRegen(Farmer farmer)
        {
            if (!ModConfig.GetInstance().UseStaminaRework) return;

            var restoredStaminaPerSecond = 0f;

            if (!farmer.isMoving())
            {
                restoredStaminaPerSecond += ModConfig.GetInstance().StaminaRegenOnNotMovingPerSecond;
            }
            if (farmer.IsSitting())
            {
                restoredStaminaPerSecond += ModConfig.GetInstance().StaminaExtraRegenOnSittingPerSecond;
            }
            if (farmer.isInBed.Value)
            {
                restoredStaminaPerSecond += ModConfig.GetInstance().StaminaExtraRegenOnNappingPerSecond;
            }

            farmer.stamina = Math.Min(farmer.MaxStamina, farmer.stamina + restoredStaminaPerSecond);
        }

        /// <summary>
        /// Handle stamina drain when running or sprinting
        /// </summary>
        public void HandleRunningStaminaDrain(Farmer farmer, bool isSprinting)
        {
            if (!ModConfig.GetInstance().UseStaminaRework) return;

            float staminaDrain = isSprinting ? 
                ModConfig.GetInstance().StaminaDrainOnSprintingPerTick : 
                ModConfig.GetInstance().StaminaDrainOnRunningPerTick;

            if (farmer.stamina <= staminaDrain)
            {
                farmer.setRunning(false, true);
            }

            farmer.stamina -= staminaDrain;

            if (isSprinting)
            {
                Game1.playSound("daggerswipe");
                effects.EffectManager.applyEffect(effects.EffectManager.sprintingEffectIndex);
            }
        }

        /// <summary>
        /// Handle stamina drain when using tools
        /// </summary>
        public void HandleToolStaminaDrain(Farmer farmer, Tool tool)
        {
            if (!ModConfig.GetInstance().UseStaminaRework) return;

            bool isFever = Game1.player.buffs.IsApplied("neroyuki.rlvalley/fever");
            int power = (int)((farmer.toolHold.Value + 20f) / 600f) + 1;

            float staminaDrain = CalculateToolStaminaDrain(farmer, tool, power);
            
            if (staminaDrain > 0)
            {
                staminaDrain *= (float)(ModConfig.GetInstance().AdditionalDrainOnToolUse / 100);

                if (isFever)
                {
                    farmer.stamina -= staminaDrain * ((float)(ModConfig.GetInstance().AdditionalPercentageStaminaDrainOnFever / 100));
                    Game1.staminaShakeTimer += 100;
                }
                else
                {
                    farmer.stamina -= staminaDrain;
                }
            }
        }

        /// <summary>
        /// Calculate stamina drain for specific tool type
        /// </summary>
        private float CalculateToolStaminaDrain(Farmer farmer, Tool tool, int power)
        {
            if (tool is StardewValley.Tools.Axe)
                return ((2 * power) - farmer.ForagingLevel * 0.1f);
            else if (tool is StardewValley.Tools.Hoe)
                return ((2 * power) - farmer.FarmingLevel * 0.1f);
            else if (tool is StardewValley.Tools.Pickaxe)
                return ((2 * power) - farmer.MiningLevel * 0.1f);
            else if (tool is StardewValley.Tools.MeleeWeapon)
                return (1f - farmer.CombatLevel * 0.08f);
            else if (tool is StardewValley.Tools.Slingshot)
                return (1f - farmer.CombatLevel * 0.08f);
            else if (tool is StardewValley.Tools.WateringCan)
                return ((2 * (power + 1)) - farmer.FarmingLevel * 0.1f);
            else if (tool is StardewValley.Tools.FishingRod)
                return (8f - farmer.FishingLevel * 0.1f);
            else if (tool is StardewValley.Tools.MilkPail)
                return (4f - farmer.FarmingLevel * 0.1f);
            else if (tool is StardewValley.Tools.Shears)
                return (4f - farmer.FarmingLevel * 0.1f);
            
            return 0f;
        }
    }
}
