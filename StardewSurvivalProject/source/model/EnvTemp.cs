using System;
using StardewSurvivalProject.source.utils;
using StardewValley;

namespace StardewSurvivalProject.source.model
{
    public class EnvTemp
    {
        private const double DEFAULT_VALUE = 25.0;
        public double value { get; set; }
        private Random rand = null;

        public EnvTemp()
        {
            this.value = DEFAULT_VALUE;
            this.rand = new Random();
        }

        public void updateEnvTemp(int time, string season, int weatherIconId, GameLocation location = null, int currentMineLevel = 0)
        {
            //TODO: temp follow a curve increase from morning to noon and decrease from noon to midnight, also depend on current season and current weather
            const double BASE_VALUE = DEFAULT_VALUE;
            double value = BASE_VALUE;

            //LogHelper.Debug($"season={season} time={time} weatherId={weatherIconId}");

            //start with applying adjustment based on season
            if (season.Equals("spring") || season.Equals("fall")) value *= 0.9;
            else if (season.Equals("summer")) value *= 1.1;
            else if (season.Equals("winter")) value *= 0.2;

            //next, check for weather
            switch (weatherIconId)
            {
                case (int)weatherIconType.SUNNY:
                case (int)weatherIconType.FESTIVAL:
                case (int)weatherIconType.WEDDING:
                    value *= 1.2; break;
                case (int)weatherIconType.STORM:
                    value *= 0.8; break;
                case (int)weatherIconType.RAIN:
                    value *= 0.8; break;
                case (int)weatherIconType.WINDY_SPRING:
                case (int)weatherIconType.WINDY_FALL:
                    value *= 0.9; break;
                case (int)weatherIconType.SNOW:
                    value *= -1; break;
                default: break;
            }

            double dayNightCycleTempDiffScale = 3;
            double fluctuationTempScale = 1;
            bool fixedTemp = false;

            //check for location
            if (location != null)
            {
                data.LocationEnvironmentData locationData = data.CustomEnvironmentDictionary.GetEnvironmentData(location.Name);
                if (locationData != null)
                {
                    value += locationData.tempModifierAdditive;
                    value *= locationData.tempModifierMultiplicative;
                    if (locationData.tempModifierFixedValue <= -274)
                    {
                        value = locationData.tempModifierFixedValue;
                        fixedTemp = true;
                    }
                    dayNightCycleTempDiffScale = locationData.tempModifierTimeDependentScale;
                    fluctuationTempScale = locationData.tempModifierFluctuationScale;
                }
            }

            //next, check for time
            //convert time to actual decimal format to run on a time-dependent function
            double decTime = ((double)(time / 100) + ((double)(time % 100) / 60.0));
            LogHelper.Debug(decTime.ToString());
            //curve look good enough on desmos so YOLO
            double timeTempModifier = Math.Sin((decTime - 8.5) / (Math.PI * 1.2)) * dayNightCycleTempDiffScale; //TODO change number 3 to a season and location-dependent multiplier
            value += (fixedTemp) ? 0 : timeTempModifier;

            //finally, add some randomness XD

            value += rand.NextDouble() * fluctuationTempScale - 0.5 * fluctuationTempScale;
            this.value = value;
        }
    }
}
