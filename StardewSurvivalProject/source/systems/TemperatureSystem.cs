using System;
using StardewValley;
using StardewModdingAPI;

namespace StardewSurvivalProject.source.systems
{
    /// <summary>
    /// Manages environmental temperature calculations
    /// Handles base temperature, weather modifiers, location modifiers, and local temperature from objects
    /// </summary>
    public class TemperatureSystem
    {
        private model.EnvTemp envTemp;

        public TemperatureSystem()
        {
            envTemp = new model.EnvTemp();
        }

        /// <summary>
        /// Update environmental temperature based on time, season, weather, and location
        /// </summary>
        public void UpdateEnvironmentTemp(int time, string season, int weatherIconId, GameLocation location = null, int currentMineLevel = 0)
        {
            if (!ModConfig.GetInstance().UseTemperatureModule) return;
            
            envTemp.updateEnvTemp(time, season, weatherIconId, location, currentMineLevel);
            if (Game1.player != null)
            {
                envTemp.updateLocalEnvTemp((int)Game1.player.Tile.X, (int)Game1.player.Tile.Y);
            }
        }

        /// <summary>
        /// Get current environmental temperature
        /// </summary>
        public double GetEnvTemp()
        {
            return envTemp.value;
        }

        /// <summary>
        /// Get the env temp model (for player body temp updates)
        /// </summary>
        public model.EnvTemp GetEnvTempModel()
        {
            return envTemp;
        }

        /// <summary>
        /// Get environmental temperature as formatted string with unit
        /// </summary>
        public string GetEnvTempString()
        {
            if (ModConfig.GetInstance().TemperatureUnit.Equals("Fahrenheit"))
                return ((envTemp.value * 9 / 5) + 32).ToString("#.##") + "F";
            else if (ModConfig.GetInstance().TemperatureUnit.Equals("Kelvin"))
                return (envTemp.value + 273).ToString("#.##") + "K";
            return envTemp.value.ToString("#.##") + "C";
        }
    }
}
