using System;
using System.IO;
using StardewModdingAPI;
using StardewValley;

namespace StardewSurvivalProject.source.core
{
    /// <summary>
    /// Handles save and load operations for player survival stats
    /// </summary>
    public class SaveManager
    {
        private readonly IModHelper helper;
        private string RelativeDataPath => Path.Combine("data", $"{Constants.SaveFolderName}.json");

        public SaveManager(IModHelper helper)
        {
            this.helper = helper;
        }

        /// <summary>
        /// Save data structure for player stats
        /// Version 0.4.1+: Added ItemIdMigrated flag to track item ID migration from Under_Score to camelCase format
        /// </summary>
        public class PlayerSaveData
        {
            public model.Hunger hunger;
            public model.Thirst thirst;
            public model.BodyTemp bodyTemp;
            public int healthPoint;
            public model.Mood mood;
            public bool ItemIdMigrated = false;  // Flag to track if item ID migration has been applied

            public PlayerSaveData(model.Hunger h, model.Thirst t, model.BodyTemp bt, int hp, model.Mood m, bool migrated = true)
            {
                this.hunger = h;
                this.thirst = t;
                this.bodyTemp = bt;
                this.healthPoint = hp;
                this.mood = m;
                this.ItemIdMigrated = migrated;
            }
        }

        /// <summary>
        /// Load player stats from save file
        /// Automatically handles item ID migration if needed
        /// </summary>
        public PlayerSaveData LoadData()
        {
            PlayerSaveData saveData = helper.Data.ReadJsonFile<PlayerSaveData>(RelativeDataPath);
            
            if (saveData != null && !saveData.ItemIdMigrated)
            {
                LogHelper.Info("Detected old save file format. Starting item ID migration...");
                int migratedCount = utils.MigrationHelper.MigrateAllItems();
                LogHelper.Info($"Item ID migration completed: {migratedCount} items updated");
                
                // Mark as migrated and save immediately
                saveData.ItemIdMigrated = true;
                SaveData(saveData);
                
                Game1.addHUDMessage(new HUDMessage($"Stardew Survival Project: Migrated {migratedCount} items to new format", HUDMessage.achievement_type));
            }

            return saveData;
        }

        /// <summary>
        /// Save player stats to file
        /// </summary>
        public void SaveData(PlayerSaveData data)
        {
            helper.Data.WriteJsonFile<PlayerSaveData>(RelativeDataPath, data);
        }

        /// <summary>
        /// Create save data from current game state
        /// </summary>
        public PlayerSaveData CreateSaveData(model.Hunger hunger, model.Thirst thirst, model.BodyTemp bodyTemp, int healthPoint, model.Mood mood)
        {
            return new PlayerSaveData(hunger, thirst, bodyTemp, healthPoint, mood, migrated: true);
        }
    }
}
