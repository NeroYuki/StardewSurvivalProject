using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewSurvivalProject.source.model
{
    public class BodyTemp
    {
        public const double DEFAULT_VALUE = 37.5;

        public double MinComfortTemp = 16.0;
        public double MaxComfortTemp = 24.0;

        public static double HypotherminaThreshold = 35.0;
        public static double FrostbiteThreshold = 30.0;
        public static double HeatstrokeThreshold = 38.5;
        public static double BurnThreshold = 41.0;

        public double value { get; set; }

        public BodyTemp()
        {
            this.value = DEFAULT_VALUE;
        }

        public void applyComfortTemp(double MinComfortTemp, double MaxComfortTemp)
        {
            this.MinComfortTemp = MinComfortTemp;
            this.MaxComfortTemp = MaxComfortTemp;
        }

        public void BodyTempCalc(EnvTemp envTemp, double fluctuation = 0)
        {
            double envTempVal = envTemp.value;
            double targetBodyTemp = value;
            //currently follow a segmented linear function (adjust to look good on desmos xd)
            if (envTemp.value > MaxComfortTemp)
            {
                // if more than maximum comfort temp
                targetBodyTemp = DEFAULT_VALUE + 0.09 * (envTempVal - MaxComfortTemp);
            }
            else if (envTemp.value < MinComfortTemp)
            {
                // if more than maximum comfort temp
                targetBodyTemp = DEFAULT_VALUE - 0.17 * (MinComfortTemp - envTempVal);
            }
            else
            {
                targetBodyTemp = DEFAULT_VALUE;
            }
            //gradual temp change instead of abrupted
            value += (targetBodyTemp - value) / 2;
            //fluctuate a bit
            value += fluctuation;
        }

        internal void updateComfortTemp(string hat_name, string shirt_name, string pants_name, string boots_name)
        {
            //placeholder
            MinComfortTemp = MinComfortTemp;
            MaxComfortTemp = MaxComfortTemp;
        }
    }
}
