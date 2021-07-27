using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewSurvivalProject.source.model
{
    public class Thirst
    {
        public static double DEFAULT_VALUE = ModConfig.GetInstance().MaxThirst;
        public double value { get; set; }

        public Thirst()
        {
            this.value = DEFAULT_VALUE;
        }
    }
}
