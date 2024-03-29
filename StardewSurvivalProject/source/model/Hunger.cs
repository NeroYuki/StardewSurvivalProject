﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewSurvivalProject.source.model
{
    public class Hunger
    {
        public static double DEFAULT_VALUE = ModConfig.GetInstance().MaxHunger;   
        public double value { get; set; }
        public double saturation { get; set; } = 0;

        public Hunger()
        {
            this.value = DEFAULT_VALUE;
            this.saturation = 0;
        }
    }
}
