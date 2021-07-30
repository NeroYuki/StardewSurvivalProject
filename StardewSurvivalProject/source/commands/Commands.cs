using StardewSurvivalProject.source.effects;

namespace StardewSurvivalProject.source.commands
{
    public class Commands
    {
        private Manager instance = null;
        public Commands(Manager instance)
        {
            this.instance = instance;
        }
        public void SetHungerCmd(string cmd, string[] args)
        {
            if (cmd == "player_sethunger")
            {
                if (args.Length != 1)
                    LogHelper.Info("Usage: player_sethunger <amt>");
                else
                    instance.setPlayerHunger(double.Parse(args[0]));
            }

        }
        public void SetThirstCmd(string cmd, string[] args)
        {
            if (cmd == "player_setthirst")
            {
                if (args.Length != 1)
                    LogHelper.Info("Usage: player_setthirst <amt>");
                else
                    instance.setPlayerThirst(double.Parse(args[0]));
            }
        }
        
        public void SetEffect(string cmd, string[] args)
        {
            if (cmd == "player_testeffect")
            {
                EffectManager.addEffect(EffectManager.burnEffectIndex);
                EffectManager.addEffect(EffectManager.dehydrationEffectIndex);
                EffectManager.addEffect(EffectManager.feverEffectIndex);
                EffectManager.addEffect(EffectManager.frostbiteEffectIndex);
                EffectManager.addEffect(EffectManager.heatstrokeEffectIndex);
                EffectManager.addEffect(EffectManager.hypothermiaEffectIndex);
                EffectManager.addEffect(EffectManager.starvationEffectIndex);
                EffectManager.addEffect(EffectManager.stomachacheEffectIndex);
            }
        }
    }
}
