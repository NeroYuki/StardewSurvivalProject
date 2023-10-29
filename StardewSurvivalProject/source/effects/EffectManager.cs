using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buffs;

namespace StardewSurvivalProject.source.effects
{
    public class EffectManager
    {
        public static int burnEffectIndex = 36;
        public static int starvationEffectIndex = 37;
        public static int hypothermiaEffectIndex = 38;
        public static int frostbiteEffectIndex = 39;
        public static int heatstrokeEffectIndex = 40;
        public static int dehydrationEffectIndex = 41;
        public static int feverEffectIndex = 42;
        public static int stomachacheEffectIndex = 43;
        public static int thirstEffectIndex = 44;
        public static int hungerEffectIndex = 45;
        public static int wellFedEffectIndex = 46;
        public static int refreshingEffectIndex = 47;

        //dictionary include effect index as key, a string int pair value for description and effect duration respectively
        public static Dictionary<int, Buff> effectDictionary = new Dictionary<int, Buff>();

        public static void initialize(Dictionary<string, Texture2D> effectIcons)
        {
            const int appendRow = 3;
            effectDictionary.Clear();

            burnEffectIndex = appendRow * 12 + 0;
            starvationEffectIndex = appendRow * 12 + 1;
            hypothermiaEffectIndex = appendRow * 12 + 2;
            frostbiteEffectIndex = appendRow * 12 + 3;
            heatstrokeEffectIndex = appendRow * 12 + 4;
            dehydrationEffectIndex = appendRow * 12 + 5;
            feverEffectIndex = appendRow * 12 + 6;
            stomachacheEffectIndex = appendRow * 12 + 7;
            thirstEffectIndex = appendRow * 12 + 8;
            hungerEffectIndex = appendRow * 12 + 9;
            wellFedEffectIndex = appendRow * 12 + 10;
            refreshingEffectIndex = appendRow * 12 + 11;

            effectDictionary.Add(burnEffectIndex, new Buff(
                id: "neroyuki.rlvalley/burn",
                displayName: "burn",
                description: "Holy crap you are on fire! Get away from the heat source NOW",
                iconTexture: effectIcons.GetValueSafe("Burn"),
                duration: 1_000
            ));

            // starvation effect, desc: You're starving. Please eat something...
            effectDictionary.Add(starvationEffectIndex, new Buff(
                id: "neroyuki.rlvalley/starvation",
                displayName: "starvation",
                description: "You're starving. Please eat something...",
                iconTexture: effectIcons.GetValueSafe("Starvation"),
                duration: 1_000
            ));

            // hypothermia effect, desc: Your skin is getting colder. Please seek a shelter and a campfire.
            effectDictionary.Add(hypothermiaEffectIndex, new Buff(
                id: "neroyuki.rlvalley/hypothermia",
                displayName: "hypothermia",
                description: "Your skin is getting colder. Please seek a shelter and a campfire.",
                iconTexture: effectIcons.GetValueSafe("Hypothermia"),
                duration: 1_000
            ));

            // frostbite effect, desc: Your mind is getting numb. I hope your shelter is nearby...
            effectDictionary.Add(frostbiteEffectIndex, new Buff(
                id: "neroyuki.rlvalley/frostbite",
                displayName: "frostbite",
                description: "Your mind is getting numb. I hope your shelter is nearby...",
                iconTexture: effectIcons.GetValueSafe("Frostbite"),
                duration: 1_000
            ));
            
            // heatstroke effect, desc: The heat is so bad, you begin to sweat non-stop
            effectDictionary.Add(heatstrokeEffectIndex, new Buff(
                id: "neroyuki.rlvalley/heatstroke",
                displayName: "heatstroke",
                description: "The heat is so bad, you begin to sweat non-stop",
                iconTexture: effectIcons.GetValueSafe("Heatstroke"),
                duration: 1_000
            ));

            // dehydration effect, desc: You are as dry as a kindle. Please get yourself something to drink
            effectDictionary.Add(dehydrationEffectIndex, new Buff(
                id: "neroyuki.rlvalley/dehydration",
                displayName: "dehydration",
                description: "You are as dry as a kindle. Please get yourself something to drink",
                iconTexture: effectIcons.GetValueSafe("Dehydration"),
                duration: 1_000
            ));

            // fever effect, desc: Someday you just feeling sick. You'd better not doing something to heavy
            effectDictionary.Add(feverEffectIndex, new Buff(
                id: "neroyuki.rlvalley/fever",
                displayName: "fever",
                description: "Someday you just feeling sick. You'd better not doing something to heavy",
                iconTexture: effectIcons.GetValueSafe("Fever"),
                duration: 4_800_000
            ));

            // stomachache effect, desc: Your gut felt some pain, maybe cook your food next time
            effectDictionary.Add(stomachacheEffectIndex, new Buff(
                id: "neroyuki.rlvalley/stomachache",
                displayName: "stomachache",
                description: "Your gut felt some pain, maybe cook your food next time",
                iconTexture: effectIcons.GetValueSafe("Stomachache"),
                duration: 1_000
            ));

            // thirst effect, desc: Your throat is dried, it's begging for some liquid
            effectDictionary.Add(thirstEffectIndex, new Buff(
                id: "neroyuki.rlvalley/thirst",
                displayName: "thirst",
                description: "Your throat is dried, it's begging for some liquid",
                iconTexture: effectIcons.GetValueSafe("Thirst"),
                duration: 1_000
            ));

            // hunger effect, desc: Your stomach is growling, better get something to eat
            effectDictionary.Add(hungerEffectIndex, new Buff(
                id: "neroyuki.rlvalley/hunger",
                displayName: "hunger",
                description: "Your stomach is growling, better get something to eat",
                iconTexture: effectIcons.GetValueSafe("Hunger"),
                duration: 1_000
            ));

            // well fed effect, desc: You feel fullfilled, life's good
            effectDictionary.Add(wellFedEffectIndex, new Buff(
                id: "neroyuki.rlvalley/wellfed",
                displayName: "well fed",
                description: "You feel fullfilled, life's good",
                iconTexture: effectIcons.GetValueSafe("WellFed"),
                duration: 1_000
            ));

            // refreshing effect, desc: Lovely temperature, make you so ready for work
            effectDictionary.Add(refreshingEffectIndex, new Buff(
                id: "neroyuki.rlvalley/refreshing",
                displayName: "refreshing",
                description: "Lovely temperature, make you so ready for work",
                iconTexture: effectIcons.GetValueSafe("Refreshing"),
                duration: 1_000
            ));

        }

        public static void addEffect(int effectIndex)
        {
            if (effectIndex == hypothermiaEffectIndex)
                Game1.player.applyBuff(effectDictionary.GetValueSafe(hypothermiaEffectIndex));
            else if (effectIndex == hungerEffectIndex)
                Game1.player.applyBuff(effectDictionary.GetValueSafe(hungerEffectIndex));
            else if (effectIndex == thirstEffectIndex)
                Game1.player.applyBuff(effectDictionary.GetValueSafe(thirstEffectIndex));
            else if (effectIndex == wellFedEffectIndex)
                Game1.player.applyBuff(effectDictionary.GetValueSafe(wellFedEffectIndex));
            else if (effectIndex == refreshingEffectIndex)
                Game1.player.applyBuff(effectDictionary.GetValueSafe(refreshingEffectIndex));
            else
                Game1.player.applyBuff(effectDictionary.GetValueSafe(effectIndex));
        }

        public static void applyEffect(int effectIndex)
        {
            addEffect(effectIndex);
        }

        //public static void renewEffect(Buff effect)
        //{
        //    KeyValuePair<string, int> effectExtraInfo = new KeyValuePair<string, int>("Unknown Effect", 0);
        //    bool res = effectDescDictionary.TryGetValue(effect.which, out effectExtraInfo);
        //    effect.millisecondsDuration = effectExtraInfo.Value;
        //}
    }
}
