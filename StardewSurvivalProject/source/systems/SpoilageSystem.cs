using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace StardewSurvivalProject.source.systems
{
    /// <summary>
    /// Categorization of items for spoilage timing.
    /// </summary>
    public enum SpoilageCategory
    {
        None,       // Item doesn't spoil
        Crop,       // Raw crops: 7 days
        Produce,    // Animal products, artisan goods: 14 days
        CookedFood  // Cooked/prepared food: 3 days
    }

    /// <summary>
    /// Container type that affects spoilage rate.
    /// </summary>
    public enum ContainerType
    {
        None,           // No container (inventory) — normal spoilage
        Chest,          // Regular chest — 20% slower spoilage
        PortableCooler, // Portable Cooler — 50% slower spoilage
        MiniFridge,     // Mini Fridge — 100% slower spoilage (2x shelf life)
        ChestFreezer    // Chest Freezer — 250% slower spoilage (3.5x shelf life)
    }

    /// <summary>
    /// Core system managing food spoilage mechanics.
    /// Tracks spoilage piles per item, handles daily advancement, and converts spoiled items.
    /// </summary>
    public class SpoilageSystem
    {
        private static IMonitor Monitor;

        // Spoilage durations in days by category
        public static readonly Dictionary<SpoilageCategory, int> DefaultSpoilageDays = new Dictionary<SpoilageCategory, int>
        {
            { SpoilageCategory.Crop,       7  },
            { SpoilageCategory.Produce,    14 },
            { SpoilageCategory.CookedFood, 3  }
        };

        /// <summary>
        /// Get the spoilage rate multiplier for a container type based on config.
        /// Lower value = slower spoilage.
        /// </summary>
        public static double GetContainerRateMultiplier(ContainerType containerType)
        {
            var config = ModConfig.GetInstance();
            switch (containerType)
            {
                case ContainerType.Chest:          return 1.0 / (1.0 + config.ChestSpoilageExtension / 100.0);
                case ContainerType.PortableCooler: return 1.0 / (1.0 + config.CoolerSpoilageExtension / 100.0);
                case ContainerType.MiniFridge:     return 1.0 / (1.0 + config.FridgeSpoilageExtension / 100.0);
                case ContainerType.ChestFreezer:   return 1.0 / (1.0 + config.FreezerSpoilageExtension / 100.0);
                default:                           return 1.0;
            }
        }

        // Names of our custom container items (must match Content Patcher item names)
        public static readonly string PortableCoolerName = "Portable Cooler";
        public static readonly string MiniFridgeName = "Mini Fridge";
        public static readonly string ChestFreezerName = "Chest Freezer";

        // The item ID for "Spoiled Food" — we'll use a custom item or the Trash item
        public const string SpoiledFoodItemId = "SSP.SpoiledFood";

        public static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }

        /// <summary>
        /// Determine the spoilage category for an item.
        /// </summary>
        public static SpoilageCategory GetSpoilageCategory(Item item)
        {
            if (item == null || !(item is SObject obj)) return SpoilageCategory.None;

            // Skip non-edible items (edibility of -300 means inedible in SDV)
            if (Game1.objectData.TryGetValue(item.ItemId, out var objData))
            {
                if (objData.Edibility <= -300) return SpoilageCategory.None;
            }
            else
            {
                return SpoilageCategory.None;
            }

            // Check if it's our spoiled food item
            if (item.ItemId == SpoiledFoodItemId || item.Name == "Spoiled Food")
                return SpoilageCategory.None;

            int cat = item.Category;

            // Cooked food (Category -7 = CookingCategory)
            if (cat == SObject.CookingCategory)
                return SpoilageCategory.CookedFood;

            // Animal products / Artisan goods
            if (cat == SObject.EggCategory ||       // -5: Eggs
                cat == SObject.MilkCategory ||       // -6: Milk
                cat == SObject.artisanGoodsCategory || // -26: Artisan Goods (cheese, mayo, etc.)
                cat == SObject.syrupCategory ||      // -27: Syrups
                cat == SObject.meatCategory)         // -14: Meat
            {
                return SpoilageCategory.Produce;
            }

            // Crops / raw produce
            if (cat == SObject.VegetableCategory ||  // -75: Vegetables
                cat == SObject.FruitsCategory ||     // -79: Fruits
                cat == SObject.flowersCategory ||    // -80: Flowers
                cat == SObject.GreensCategory)       // -81: Greens/Forage
            {
                return SpoilageCategory.Crop;
            }

            // Fish can also spoil (as crop rate)
            if (cat == SObject.FishCategory)         // -4: Fish
                return SpoilageCategory.Crop;

            return SpoilageCategory.None;
        }

        /// <summary>
        /// Get the default spoilage duration for an item in days.
        /// Returns -1 if the item doesn't spoil.
        /// </summary>
        public static int GetDefaultSpoilageDays(Item item)
        {
            if (!ModConfig.GetInstance().UseFoodSpoilage) return -1;

            var category = GetSpoilageCategory(item);
            if (category == SpoilageCategory.None) return -1;

            var config = ModConfig.GetInstance();
            switch (category)
            {
                case SpoilageCategory.Crop:       return config.CropSpoilageDays;
                case SpoilageCategory.Produce:    return config.ProduceSpoilageDays;
                case SpoilageCategory.CookedFood: return config.CookedFoodSpoilageDays;
                default: return -1;
            }
        }

        /// <summary>
        /// Initialize spoilage tracking on an item if it's a spoilable type and doesn't already have tracking.
        /// </summary>
        public static void EnsureSpoilageTracking(Item item)
        {
            if (item == null) return;
            if (model.SpoilageData.HasSpoilageData(item)) return;

            int days = GetDefaultSpoilageDays(item);
            if (days < 0) return;

            var data = new model.SpoilageData(item.Stack, days);
            data.SaveToItem(item);
        }

        /// <summary>
        /// Synchronize spoilage pile counts with the actual item stack size.
        /// Call this after any stack manipulation to ensure consistency.
        /// </summary>
        public static void SyncSpoilageWithStack(Item item)
        {
            if (item == null) return;
            var spoilage = model.SpoilageData.FromItem(item);
            if (spoilage == null) return;

            int diff = item.Stack - spoilage.TotalCount;
            if (diff > 0)
            {
                // Item gained items without tracked piles — add as fresh
                int days = GetDefaultSpoilageDays(item);
                if (days > 0)
                {
                    spoilage.AddPile(diff, days);
                }
            }
            else if (diff < 0)
            {
                // Item lost items — remove oldest first
                spoilage.RemoveItems(-diff);
            }

            spoilage.SaveToItem(item);
        }

        /// <summary>
        /// Determine the container type for a chest item by its name or SSP modData tag.
        /// </summary>
        public static ContainerType GetContainerType(SObject container)
        {
            if (container == null) return ContainerType.None;

            // Check if it's a fridge (built-in kitchen fridge)
            if (container is Chest chest && chest.fridge.Value)
                return ContainerType.MiniFridge;

            // Prefer the explicit modData tag written at placement time (most reliable).
            if (container.modData.TryGetValue("SSP.CustomContainer", out string customTag))
            {
                if (customTag == PortableCoolerName) return ContainerType.PortableCooler;
                if (customTag == MiniFridgeName)     return ContainerType.MiniFridge;
                if (customTag == ChestFreezerName)   return ContainerType.ChestFreezer;
            }

            // Fall back to Name-based detection (handles existing placed objects from before the patch).
            string name = container.Name;
            if (name == PortableCoolerName) return ContainerType.PortableCooler;
            if (name == MiniFridgeName) return ContainerType.MiniFridge;
            if (name == ChestFreezerName) return ContainerType.ChestFreezer;

            // Any regular chest
            if (container is Chest playerChest && playerChest.playerChest.Value)
                return ContainerType.Chest;

            return ContainerType.None;
        }

        /// <summary>
        /// Advance spoilage by one day for all items in the player's inventory.
        /// </summary>
        public static void AdvanceDayForInventory(Farmer player)
        {
            if (player?.Items == null) return;

            int totalSpoiled = 0;
            var itemsToRemove = new List<int>();
            var spoiledToAdd = 0;

            for (int i = 0; i < player.Items.Count; i++)
            {
                var item = player.Items[i];
                if (item == null) continue;

                var spoilage = model.SpoilageData.FromItem(item);
                if (spoilage == null) continue;

                // Inventory = no container, rate multiplier = 1.0
                int spoiled = spoilage.AdvanceDay(GetContainerRateMultiplier(ContainerType.None));
                if (spoiled > 0)
                {
                    totalSpoiled += spoiled;
                    spoiledToAdd += spoiled;
                    item.Stack -= spoiled;
                    if (item.Stack <= 0)
                    {
                        itemsToRemove.Add(i);
                    }
                }
                spoilage.SaveToItem(item);
            }

            // Remove empty item slots
            for (int i = itemsToRemove.Count - 1; i >= 0; i--)
            {
                player.Items[itemsToRemove[i]] = null;
            }

            // Add spoiled food items to inventory
            if (spoiledToAdd > 0)
            {
                AddSpoiledFoodToInventory(player, spoiledToAdd);
            }

            if (totalSpoiled > 0)
            {
                Game1.addHUDMessage(new HUDMessage($"{totalSpoiled} food item{(totalSpoiled > 1 ? "s" : "")} spoiled in your backpack!", HUDMessage.error_type));
                LogHelper.Info($"{totalSpoiled} food items spoiled in player inventory");
            }
        }

        /// <summary>
        /// Advance spoilage for all items in all chests/containers in the current location and farm.
        /// </summary>
        public static void AdvanceDayForContainers()
        {
            foreach (var location in Game1.locations)
            {
                AdvanceDayForLocation(location);
            }
        }

        /// <summary>
        /// Advance spoilage for a specific location's containers.
        /// </summary>
        private static void AdvanceDayForLocation(GameLocation location)
        {
            if (location == null) return;

            foreach (var obj in location.objects.Values)
            {
                if (!(obj is Chest chest)) continue;

                var containerType = GetContainerType(chest);
                double rateMultiplier = GetContainerRateMultiplier(containerType);

                int totalSpoiled = 0;
                var spoiledToAdd = 0;
                var itemsToRemove = new List<int>();

                for (int i = 0; i < chest.Items.Count; i++)
                {
                    var item = chest.Items[i];
                    if (item == null) continue;

                    var spoilage = model.SpoilageData.FromItem(item);
                    if (spoilage == null) continue;

                    int spoiled = spoilage.AdvanceDay(rateMultiplier);
                    if (spoiled > 0)
                    {
                        totalSpoiled += spoiled;
                        spoiledToAdd += spoiled;
                        item.Stack -= spoiled;
                        if (item.Stack <= 0)
                        {
                            itemsToRemove.Add(i);
                        }
                    }
                    spoilage.SaveToItem(item);
                }

                // Remove empty slots
                for (int i = itemsToRemove.Count - 1; i >= 0; i--)
                {
                    chest.Items[itemsToRemove[i]] = null;
                }

                // Add spoiled food to chest
                if (spoiledToAdd > 0)
                {
                    AddSpoiledFoodToChest(chest, spoiledToAdd);
                }

                if (totalSpoiled > 0)
                {
                    LogHelper.Debug($"{totalSpoiled} items spoiled in {containerType} at {location.Name}");
                }
            }

            // Process fridge in farmhouse/island farmhouse
            if (location is StardewValley.Locations.FarmHouse farmHouse)
            {
                var fridge = farmHouse.fridge.Value;
                if (fridge != null)
                {
                    var containerType = ContainerType.MiniFridge; // Kitchen fridge = Mini Fridge tier
                    double rateMultiplier = GetContainerRateMultiplier(containerType);

                    int totalSpoiled = 0;
                    var spoiledToAdd = 0;
                    var itemsToRemove = new List<int>();

                    for (int i = 0; i < fridge.Items.Count; i++)
                    {
                        var item = fridge.Items[i];
                        if (item == null) continue;

                        var spoilage = model.SpoilageData.FromItem(item);
                        if (spoilage == null) continue;

                        int spoiled = spoilage.AdvanceDay(rateMultiplier);
                        if (spoiled > 0)
                        {
                            totalSpoiled += spoiled;
                            spoiledToAdd += spoiled;
                            item.Stack -= spoiled;
                            if (item.Stack <= 0)
                            {
                                itemsToRemove.Add(i);
                            }
                        }
                        spoilage.SaveToItem(item);
                    }

                    for (int i = itemsToRemove.Count - 1; i >= 0; i--)
                    {
                        fridge.Items[itemsToRemove[i]] = null;
                    }

                    if (spoiledToAdd > 0)
                    {
                        AddSpoiledFoodToChest(fridge, spoiledToAdd);
                    }

                    if (totalSpoiled > 0)
                    {
                        LogHelper.Debug($"{totalSpoiled} items spoiled in kitchen fridge at {location.Name}");
                    }
                }
            }

            // Recursively handle indoor locations
            foreach (var building in location.buildings)
            {
                if (building.indoors.Value != null)
                {
                    AdvanceDayForLocation(building.indoors.Value);
                }
            }
        }

        /// <summary>
        /// Add spoiled food items to a player's inventory.
        /// </summary>
        private static void AddSpoiledFoodToInventory(Farmer player, int count)
        {
            var spoiledFood = CreateSpoiledFood(count);
            if (spoiledFood != null)
            {
                var leftover = player.addItemToInventory(spoiledFood);
                if (leftover != null)
                {
                    Game1.createItemDebris(leftover, player.getStandingPosition(), Game1.player.FacingDirection);
                }
            }
        }

        /// <summary>
        /// Add spoiled food items to a chest.
        /// </summary>
        private static void AddSpoiledFoodToChest(Chest chest, int count)
        {
            var spoiledFood = CreateSpoiledFood(count);
            if (spoiledFood != null)
            {
                var leftover = chest.addItem(spoiledFood);
                if (leftover != null)
                {
                    // If chest is full, drop on ground
                    var location = chest.Location;
                    if (location != null)
                    {
                        Game1.createItemDebris(leftover, chest.TileLocation * 64f, 0, location);
                    }
                }
            }
        }

        /// <summary>
        /// Create a spoiled food item.
        /// </summary>
        public static SObject CreateSpoiledFood(int count)
        {
            // Try to use our custom item (registered via Content Patcher)
            // If not available, use Trash (item ID 168) as fallback
            SObject spoiled;
            try
            {
                string itemId = data.ItemNameCache.getIDFromCache("Spoiled Food");
                if (itemId != null && itemId != "-1")
                {
                    spoiled = new SObject(itemId, count);
                }
                else
                {
                    // Fallback: create a Trash item (168) and rename
                    spoiled = new SObject("168", count);
                    spoiled.Name = "Spoiled Food";
                }
            }
            catch
            {
                spoiled = new SObject("168", count);
                spoiled.Name = "Spoiled Food";
            }

            return spoiled;
        }

        /// <summary>
        /// Initialize spoilage tracking for all items currently in the player's inventory.
        /// Called on save load to ensure existing items get tracking.
        /// </summary>
        public static void InitializeExistingItems(Farmer player)
        {
            if (player?.Items == null) return;

            foreach (var item in player.Items)
            {
                if (item == null) continue;
                EnsureSpoilageTracking(item);
            }
        }

        /// <summary>
        /// Called when an item is obtained (via forage, harvest, purchase, craft, etc.).
        /// Ensures the item has spoilage tracking.
        /// </summary>
        public static void OnItemObtained(Item item)
        {
            EnsureSpoilageTracking(item);
        }

        /// <summary>
        /// Get the display text for spoilage tooltip lines.
        /// Returns null if item doesn't have spoilage data.
        /// </summary>
        public static List<string> GetSpoilageTooltipLines(Item item)
        {
            if (item == null) return null;

            var spoilage = model.SpoilageData.FromItem(item);
            if (spoilage == null || spoilage.Piles.Count == 0) return null;

            spoilage.SortPiles();
            var lines = new List<string>();
            foreach (var pile in spoilage.Piles)
            {
                double rounded = Math.Round(Math.Max(0, pile.DaysRemaining), 1);
                string dayText = rounded <= 1.0 ? "day" : "days";
                lines.Add($"({pile.Count}) {rounded:0.#} {dayText} to spoil");
            }

            return lines;
        }
    }
}
