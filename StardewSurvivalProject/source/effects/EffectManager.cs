using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace StardewSurvivalProject.source.effects
{
    public class EffectManager
    {
        public const int burnEffectIndex = 36;
        public const int starvationEffectIndex = 37;
        public const int hypothermiaEffectIndex = 38;
        public const int frostbiteEffectIndex = 39;
        public const int heatstrokeEffectIndex = 40;
        public const int dehydrationEffectIndex = 41;
        public const int feverEffectIndex = 42;
        public const int stomachacheEffectIndex = 43;

        public static Dictionary<int, string> effectDescDictionary = new Dictionary<int, string>();

        public static void initialize()
        {
            effectDescDictionary.Add(burnEffectIndex, "Holy crap you are on fire! Get away from the heat source NOW");
            effectDescDictionary.Add(starvationEffectIndex, "You're starving. Please eat something...");
            effectDescDictionary.Add(hypothermiaEffectIndex, "Your skin is getting colder. Please seek a shelter and a campfire.");
            effectDescDictionary.Add(frostbiteEffectIndex, "Your mind is getting numb. I hope your shelter is nearby...");
            effectDescDictionary.Add(heatstrokeEffectIndex, "The heat is so bad, you begin to sweat non-stop");
            effectDescDictionary.Add(dehydrationEffectIndex, "You are as dry as a kindle. Please get yourself something to drink");
            effectDescDictionary.Add(feverEffectIndex, "Someday you just feeling sick. You'd better not doing something to heavy");
            effectDescDictionary.Add(stomachacheEffectIndex, "Your gut felt some pain, maybe cook your food next time");
        }

        public static void addEffect(int effectIndex)
        {
            Buff effect = null;

            Game1.buffsDisplay.addOtherBuff(effect = new Buff(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "", ""));

            effect.which = effectIndex;
            effect.sheetIndex = effectIndex;
            effect.millisecondsDuration = (effectIndex == feverEffectIndex) ? 480000 : 10000;
            bool res = effectDescDictionary.TryGetValue(effectIndex, out effect.description);
        }

        public static void applyEffect(int effectIndex)
        {
            Buff effect = Game1.buffsDisplay.otherBuffs.FirstOrDefault(e => e.which == effectIndex);
            if (effect != null)
            {
                renewEffect(effect);
            }
            else
            {
                addEffect(effectIndex);
            }
        }

        public static void renewEffect(Buff effect)
        {

            effect.millisecondsDuration = (effect.which == feverEffectIndex) ? 480000 : 10000;
        }
    }
}
