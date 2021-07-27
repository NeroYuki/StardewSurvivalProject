using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewSurvivalProject.source.model
{
    public class EnvTemp
    {
        private const double DEFAULT_VALUE = 27.0;
        public double value { get; set; }

        public EnvTemp()
        {
            this.value = DEFAULT_VALUE;
        }
    }
}
