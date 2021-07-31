using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewSurvivalProject.source.model
{
    public class BodyTemp
    {
        private const double DEFAULT_VALUE = 37.5;

        public double MinComfortTemp = 16.0;
        public double MaxComfortTemp = 27.0;
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
            //currently follow a sigmoid function (adjust to look good on desmos xd)
            if (envTemp.value > MaxComfortTemp)
            {
                // if more than maximum comfort temp
                value = DEFAULT_VALUE + (5 / (1 + Math.Pow(Math.E, -0.1 * (envTempVal - MaxComfortTemp - 24)))) * 1.5;
            }
            else if (envTemp.value < MinComfortTemp)
            {
                // if more than maximum comfort temp
                value = DEFAULT_VALUE - (5 / (1 + Math.Pow(Math.E, -0.2 * (MinComfortTemp - envTempVal - 12)))) * 1.5;
            }
            else
            {
                value = DEFAULT_VALUE;
            }
            //fluctuate a bit
            value += fluctuation;
        }
    }
}
