using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewSurvivalProject.source.model
{
    public class BodyTemp
    {
        private const double DEFAULT_VALUE = 37.0;
        public double value { get; set; }

        public BodyTemp()
        {
            this.value = DEFAULT_VALUE;
        }
    }
}
