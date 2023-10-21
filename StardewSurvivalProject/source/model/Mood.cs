using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewSurvivalProject.source.model
{
    public enum MoodLevel
    {
        MentalBreak,
        Distress,
        Sad,
        Discontent,
        Neutral,
        Content,
        Happy,
        Overjoy,
    }

    public class Mood
    {
        private double _value = 50;

        public double Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value > 120 ? 100 : value < -40 ? -40 : value;
                Level = GetMoodLevel(value);
            }
        }

        public MoodLevel Level = MoodLevel.Neutral;

        // Maybe the mood mechanic from Rimworld would be good here

        /** Potential penalty
         * Monotonous task (-5 to -50)
         * Eating the same food (-5 to -50)
         * Eating raw food (-10)
         * Get trash from fishing (-10)
         * NPC dislike your gift (-10)
         * Get fever (-10)
         * (From butcher mod) kill animal (-10 to 0)
        **/

        public MoodLevel GetMoodLevel(double value)
        {
            if (Value < 0)
                return MoodLevel.MentalBreak;
            if (Value < 10)
                return MoodLevel.Distress;
            if (Value < 25)
                return MoodLevel.Sad;
            if (Value < 40)
                return MoodLevel.Discontent;
            if (Value < 50)
                return MoodLevel.Neutral;
            if (Value < 65)
                return MoodLevel.Content;
            if (Value < 75)
                return MoodLevel.Happy;
            else
                return MoodLevel.Overjoy;
        }
    }
}
