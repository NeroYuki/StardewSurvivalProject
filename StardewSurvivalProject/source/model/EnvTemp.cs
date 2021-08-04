using System;
using StardewSurvivalProject.source.utils;
using StardewValley;
using System.Collections.Generic;
using SObject = StardewValley.Object;

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
            double dayNightCycleTempDiffScale = 3;
            double fluctuationTempScale = 1;
            bool fixedTemp = false;

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
                    value = Math.Max(value - 20, -5); break;
                default: break;
            }

            
            //check for location
            if (location != null)
            {
                LogHelper.Debug(location.Name);
                data.LocationEnvironmentData locationData = data.CustomEnvironmentDictionary.GetEnvironmentData(location.Name);
                if (locationData != null)
                {
                    value += locationData.tempModifierAdditive;
                    value *= locationData.tempModifierMultiplicative;
                    if (locationData.tempModifierFixedValue > -273)
                    {
                        value = locationData.tempModifierFixedValue;
                        fixedTemp = true;
                    }
                    dayNightCycleTempDiffScale = locationData.tempModifierTimeDependentScale;
                    fluctuationTempScale = locationData.tempModifierFluctuationScale;
                }

                if (!location.IsOutdoors)
                {
                    //cut temperature difference by half if indoor if outside is colder
                    value += Math.Min((DEFAULT_VALUE - value) / 2, 0);
                }

                //special treatment for cave
                if (location.Name.Contains("UndergroundMine"))
                {
                    if (currentMineLevel >= 0 && currentMineLevel < 40)
                    {
                        value = DEFAULT_VALUE + 0.22 * currentMineLevel;
                        fixedTemp = true;
                    }
                    else if (currentMineLevel >= 40 && currentMineLevel < 80)
                    {
                        value = -0.01 * Math.Pow(currentMineLevel - 60, 2) - 6;
                        fixedTemp = true;
                    }
                    else if (currentMineLevel >= 80)
                    {
                        value = 1.1 * (currentMineLevel - 50);
                        fixedTemp = true;
                    }
                }
                else if (location.Name.Equals("SkullCave"))
                {
                    value = DEFAULT_VALUE + 0.045 * currentMineLevel;
                    fixedTemp = true;
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

        public void updateLocalEnvTemp(int playerTileX, int playerTileY)
        {
            //FIXME: approach can be improved
            //TODO: change to instead keeping track of a list of heating / cooling source (initialize on loading save file)

            //check in player proximity for any object (AxA tile square around player position
            //should change based on the biggest effectiveRange entry)
            int proximityCheckBound = (int)Math.Ceiling(data.TempControlObjectDictionary.maxEffectiveRange); 
            Dictionary<int, SObject> nearbyObject = new Dictionary<int, SObject>();
            for (int i = playerTileX - proximityCheckBound; i <= playerTileX + proximityCheckBound; i++)
            {
                for (int j = playerTileY - proximityCheckBound; j <= proximityCheckBound + proximityCheckBound; j++)
                {
                    SObject obj = Game1.currentLocation.getObjectAtTile(i, j);
                    if (obj != null && !nearbyObject.ContainsKey(obj.GetHashCode()))
                    {
                        //LogHelper.Debug($"there is a {obj.name} nearby");
                        nearbyObject.Add(obj.GetHashCode(), obj);
                    }
                }
            }
            //filter object as heating source and cooling source

            double oldVal = value;

            //LinkedList<double> tempModifier = new LinkedList<double>();

            foreach (KeyValuePair<int, SObject> o in nearbyObject)
            {
                data.TempControlObject tempControl = data.TempControlObjectDictionary.GetTempControlData(o.Value.name);
                if (tempControl != null)
                {
                    //if this item need to be active
                    if (tempControl.needActive)
                    {
                        if (!checkIfItemIsActive(o, tempControl.activeType))
                            continue;                        
                    }

                    //prioritize ambient temp
                    if ((tempControl.deviceType.Equals("heating") && tempControl.coreTemp < value) || (tempControl.deviceType.Equals("cooling") && tempControl.coreTemp > value)) continue;

                    //dealing with target temp value here?
                    double distance_sqr = distance_square(o.Value.tileLocation.X, o.Value.tileLocation.Y, playerTileX, playerTileY);
                    //LogHelper.Debug($"Distance square from player to {o.Key} is {distance_sqr}");

                    double effRange = tempControl.effectiveRange;
                    if (distance_sqr <= effRange * effRange)
                    {
                        double tempModifierEntry = (tempControl.coreTemp - this.value) * (1 / (1 + distance_sqr));
                        value += tempModifierEntry;
                    }
                }

            }
            LogHelper.Debug($"Final temperature modifier is {value - oldVal}");

        }

        private double distance_square(double aX, double aY, double bX, double bY)
        {
            return (aX - bX) * (aX - bX) + (aY - bY) * (aY - bY);
        }

        private bool checkIfItemIsActive(KeyValuePair<int, SObject> o, int checkType = 0)
        {
            //check if the object checking is a big craftable craftable
            if (checkType == 1)
            {
                //check if said big craftable is being used
                if (o.Value.minutesUntilReady >= 0 && o.Value.heldObject.Value != null)
                {
                    //LogHelper.Debug($"there is an active {o.Value.name} nearby (machine)");
                    return true;
                }
                else
                {
                    //LogHelper.Debug($"there is an inactive {o.Value.name} nearby (machine)");
                    return false;
                }
            }
            else
            {
                //if not big craftable (assuming furniture), check if said furniture is active
                if (o.Value.isOn)
                {
                    //LogHelper.Debug($"there is an active {o.Value.name} nearby");
                    return true;
                }
                else
                {
                    //LogHelper.Debug($"there is an inactive {o.Value.name} nearby");
                    return false;
                }
            }
        }
    }
}
