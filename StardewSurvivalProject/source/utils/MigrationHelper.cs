using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace StardewSurvivalProject.source.utils
{
    /// <summary>
    /// Handles migration of item IDs from old format to new format
    /// For the v0.4.1 breaking change where item IDs changed from Under_Score to camelCase
    /// </summary>
    public static class MigrationHelper
    {
        private static readonly Dictionary<string, string> ItemIdMigrationMap = new Dictionary<string, string>
        {
            { "neroyuki.rlvalleycpitems_Advanced_Medkit", "neroyuki.rlvalleycpitems_AdvancedMedkit" },
            { "neroyuki.rlvalleycpitems_Air_Conditioner", "neroyuki.rlvalleycpitems_AirConditioner" },
            { "neroyuki.rlvalleycpitems_Air_Cooler", "neroyuki.rlvalleycpitems_AirCooler" },
            { "neroyuki.rlvalleycpitems_Dirty_Canteen", "neroyuki.rlvalleycpitems_DirtyCanteen" },
            { "neroyuki.rlvalleycpitems_Full_Canteen", "neroyuki.rlvalleycpitems_FullCanteen" },
            { "neroyuki.rlvalleycpitems_Groundwater_Collector", "neroyuki.rlvalleycpitems_GroundwaterCollector" },
            { "neroyuki.rlvalleycpitems_Herbal_Medicine", "neroyuki.rlvalleycpitems_HerbalMedicine" },
            { "neroyuki.rlvalleycpitems_Ice_Cube", "neroyuki.rlvalleycpitems_IceCube" },
            { "neroyuki.rlvalleycpitems_Ice_Ionized_Water_Canteen", "neroyuki.rlvalleycpitems_IceIonizedWaterCanteen" },
            { "neroyuki.rlvalleycpitems_Ice_Machine", "neroyuki.rlvalleycpitems_IceMachine" },
            { "neroyuki.rlvalleycpitems_Ice_Water_Canteen", "neroyuki.rlvalleycpitems_IceWaterCanteen" },
            { "neroyuki.rlvalleycpitems_Ionized_Full_Canteen", "neroyuki.rlvalleycpitems_IonizedFullCanteen" },
            { "neroyuki.rlvalleycpitems_Ionizing_Machine", "neroyuki.rlvalleycpitems_IonizingMachine" },
            { "neroyuki.rlvalleycpitems_Joja_Fresh_Water", "neroyuki.rlvalleycpitems_JojaFreshWater" },
            { "neroyuki.rlvalleycpitems_Joja_Sport_Drink", "neroyuki.rlvalleycpitems_JojaSportDrink" },
            { "neroyuki.rlvalleycpitems_Magic_Herbal_Medicine", "neroyuki.rlvalleycpitems_MagicHerbalMedicine" },
            { "neroyuki.rlvalleycpitems_Magical_Medicine_Stove", "neroyuki.rlvalleycpitems_MagicalMedicineStove" },
            { "neroyuki.rlvalleycpitems_Medicine_Worktable", "neroyuki.rlvalleycpitems_MedicineWorktable" },
            { "neroyuki.rlvalleycpitems_Passive_Cooler", "neroyuki.rlvalleycpitems_PassiveCooler" },
            { "neroyuki.rlvalleycpitems_Portable_Heater", "neroyuki.rlvalleycpitems_PortableHeater" },
            { "neroyuki.rlvalleycpitems_Primitive_Water_Filter", "neroyuki.rlvalleycpitems_PrimitiveWaterFilter" },
            { "neroyuki.rlvalleycpitems_Rain_Collector", "neroyuki.rlvalleycpitems_RainCollector" },
            { "neroyuki.rlvalleycpitems_Regular_Medkit", "neroyuki.rlvalleycpitems_RegularMedkit" },
            { "neroyuki.rlvalleycpitems_Tall_Air_Conditioner", "neroyuki.rlvalleycpitems_TallAirConditioner" },
            { "neroyuki.rlvalleycpitems_Tubular_Bandage", "neroyuki.rlvalleycpitems_TubularBandage" },
        };

        /// <summary>
        /// Migrate all items in the game world from old IDs to new IDs
        /// </summary>
        public static int MigrateAllItems()
        {
            int totalMigrated = 0;

            try
            {
                // Migrate player inventory
                totalMigrated += MigrateInventory(Game1.player.Items);

                // Migrate all game locations
                foreach (GameLocation location in Game1.locations)
                {
                    totalMigrated += MigrateLocation(location);
                }

                LogHelper.Info($"Item migration complete: {totalMigrated} items migrated");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error during item migration: {ex}");
            }

            return totalMigrated;
        }

        /// <summary>
        /// Migrate items in a specific location
        /// </summary>
        private static int MigrateLocation(GameLocation location)
        {
            int migrated = 0;

            try
            {
                // Migrate chests and containers
                foreach (var obj in location.Objects.Values)
                {
                    // Check if the object itself needs migration
                    if (obj is SObject sObj && ItemIdMigrationMap.ContainsKey(sObj.QualifiedItemId))
                    {
                        string newId = ItemIdMigrationMap[sObj.QualifiedItemId];
                        sObj.ItemId = newId.Replace("(O)", "").Replace("(BC)", "");
                        migrated++;
                        LogHelper.Debug($"Migrated placed object: {sObj.QualifiedItemId} -> {newId}");
                    }

                    // Migrate held objects in machines/objects
                    if (obj.heldObject.Value != null && ItemIdMigrationMap.ContainsKey(obj.heldObject.Value.QualifiedItemId))
                    {
                        string oldId = obj.heldObject.Value.QualifiedItemId;
                        string newId = ItemIdMigrationMap[oldId];
                        int stack = obj.heldObject.Value.Stack;
                        int quality = obj.heldObject.Value is SObject heldObj ? heldObj.Quality : 0;

                        obj.heldObject.Value = ItemRegistry.Create(newId, stack, quality) as SObject;
                        migrated++;
                        LogHelper.Debug($"Migrated held object: {oldId} -> {newId}");
                    }

                    // Migrate items inside chests
                    if (obj is Chest chest)
                    {
                        migrated += MigrateInventory(chest.Items);
                    }

                    // Migrate items inside storage furniture (dressers, etc.)
                    if (obj is StorageFurniture storageFurn)
                    {
                        migrated += MigrateInventory(storageFurn.heldItems);
                    }
                }

                // Migrate building interiors (like farmhouse, barns, coops, etc.)
                if (location is Farm farm)
                {
                    foreach (var building in farm.buildings)
                    {
                        if (building.indoors.Value != null)
                        {
                            migrated += MigrateLocation(building.indoors.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error migrating location {location.Name}: {ex}");
            }

            return migrated;
        }

        /// <summary>
        /// Migrate items in an inventory/item list
        /// </summary>
        private static int MigrateInventory(IList<Item> items)
        {
            int migrated = 0;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null) continue;

                try
                {
                    var item = items[i];

                    // Check if item needs migration
                    if (ItemIdMigrationMap.ContainsKey(item.QualifiedItemId))
                    {
                        string oldId = item.QualifiedItemId;
                        string newId = ItemIdMigrationMap[oldId];
                        int stack = item.Stack;
                        int quality = 0;

                        // Preserve quality for objects
                        if (item is SObject obj)
                        {
                            quality = obj.Quality;
                        }

                        // Create new item with new ID
                        Item newItem;
                        if (newId.StartsWith("(BC)"))
                        {
                            newItem = new SObject(newId, stack);
                        }
                        else
                        {
                            newItem = ItemRegistry.Create(newId, stack, quality);
                        }

                        // Replace in inventory
                        items[i] = newItem;
                        migrated++;

                        LogHelper.Debug($"Migrated item in inventory: {oldId} -> {newId} (x{stack})");
                    }

                    // Recursively handle items that contain other items (like chests in inventory)
                    if (item is Chest chestItem)
                    {
                        migrated += MigrateInventory(chestItem.Items);
                    }

                    // Recursively handle storage furniture that might be in inventory
                    if (item is StorageFurniture storageFurnItem)
                    {
                        migrated += MigrateInventory(storageFurnItem.heldItems);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"Error migrating item at index {i}: {ex}");
                }
            }

            return migrated;
        }

        /// <summary>
        /// Check if an item ID needs migration
        /// </summary>
        public static bool NeedsMigration(string qualifiedItemId)
        {
            return ItemIdMigrationMap.ContainsKey(qualifiedItemId);
        }

        /// <summary>
        /// Get the new ID for an old ID, or return the original if no migration needed
        /// </summary>
        public static string GetMigratedId(string qualifiedItemId)
        {
            if (ItemIdMigrationMap.ContainsKey(qualifiedItemId))
            {
                return ItemIdMigrationMap[qualifiedItemId];
            }
            return qualifiedItemId;
        }
    }
}
