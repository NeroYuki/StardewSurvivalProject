using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace StardewSurvivalProject.source.model
{
    /// <summary>
    /// Represents a "pile" of items within a stack that share the same spoilage timer.
    /// E.g., 9 melons with 7 days to spoil = one pile { Count = 9, DaysRemaining = 7 }
    /// </summary>
    public class SpoilagePile
    {
        public int Count { get; set; }
        /// <summary>Days remaining before this pile spoils. Fractional days track partial progress.</summary>
        public double DaysRemaining { get; set; }

        public SpoilagePile() { }

        public SpoilagePile(int count, double daysRemaining)
        {
            Count = count;
            DaysRemaining = daysRemaining;
        }

        public SpoilagePile Clone()
        {
            return new SpoilagePile(Count, DaysRemaining);
        }
    }

    /// <summary>
    /// Manages the spoilage pile data for a single item stack.
    /// Stored/retrieved via Item.modData as serialized JSON.
    /// </summary>
    public class SpoilageData
    {
        public const string ModDataKey = "SSP.Spoilage";

        /// <summary>
        /// List of piles sorted by DaysRemaining descending (freshest first).
        /// The sum of all pile counts should equal the item's Stack.
        /// </summary>
        public List<SpoilagePile> Piles { get; set; } = new List<SpoilagePile>();

        public SpoilageData() { }

        /// <summary>Create spoilage data for a brand new item stack.</summary>
        public SpoilageData(int count, double daysRemaining)
        {
            Piles.Add(new SpoilagePile(count, daysRemaining));
        }

        /// <summary>Total items tracked across all piles.</summary>
        public int TotalCount => Piles.Sum(p => p.Count);

        /// <summary>Sort piles by DaysRemaining descending (freshest items consumed/transferred first when removing from "bottom").</summary>
        public void SortPiles()
        {
            Piles.Sort((a, b) => b.DaysRemaining.CompareTo(a.DaysRemaining));
        }

        /// <summary>Merge piles whose DaysRemaining are within 0.01 of each other (same cohort).</summary>
        public void ConsolidatePiles()
        {
            if (Piles.Count <= 1) return;

            var result = new List<SpoilagePile>();
            foreach (var pile in Piles)
            {
                // Only merge into an existing pile if the values are nearly identical —
                // this avoids incorrectly merging piles from different containers
                // (e.g., 6.0 days from inventory vs 6.33 days from a cooler).
                var match = result.FirstOrDefault(p => Math.Abs(p.DaysRemaining - pile.DaysRemaining) < 0.01);
                if (match != null)
                {
                    match.Count += pile.Count;
                    // keep the exact value of whichever is more spoiled (lower)
                    match.DaysRemaining = Math.Min(match.DaysRemaining, pile.DaysRemaining);
                }
                else
                {
                    result.Add(pile.Clone());
                }
            }
            Piles = result;
            SortPiles();
        }

        /// <summary>
        /// Remove a number of items from piles, taking the oldest (closest to spoiling) first.
        /// Returns the removed piles for transfer to another stack.
        /// </summary>
        public List<SpoilagePile> RemoveItems(int amount)
        {
            SortPiles();
            var removed = new List<SpoilagePile>();
            int remaining = amount;

            // Remove from the END of the sorted list (oldest/most spoiled first)
            for (int i = Piles.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var pile = Piles[i];
                if (pile.Count <= remaining)
                {
                    removed.Add(pile.Clone());
                    remaining -= pile.Count;
                    Piles.RemoveAt(i);
                }
                else
                {
                    removed.Add(new SpoilagePile(remaining, pile.DaysRemaining));
                    pile.Count -= remaining;
                    remaining = 0;
                }
            }

            return removed;
        }

        /// <summary>Add piles from another source (e.g., when merging stacks).</summary>
        public void AddPiles(List<SpoilagePile> pilesToAdd)
        {
            foreach (var pile in pilesToAdd)
            {
                Piles.Add(pile.Clone());
            }
            ConsolidatePiles();
        }

        /// <summary>Add a single new pile (e.g., when a fresh item is added).</summary>
        public void AddPile(int count, double daysRemaining)
        {
            Piles.Add(new SpoilagePile(count, daysRemaining));
            ConsolidatePiles();
        }

        /// <summary>
        /// Advance spoilage by one day. Returns the number of items that have fully spoiled.
        /// </summary>
        /// <param name="spoilageRateMultiplier">
        /// Multiplier for how fast items spoil. 1.0 = normal rate.
        /// Values less than 1.0 slow spoilage (e.g., in a fridge).
        /// A value of 0.5 means items spoil at half rate (extends shelf life by 100%).
        /// </param>
        public int AdvanceDay(double spoilageRateMultiplier = 1.0)
        {
            int spoiledCount = 0;
            for (int i = Piles.Count - 1; i >= 0; i--)
            {
                Piles[i].DaysRemaining -= spoilageRateMultiplier;
                if (Piles[i].DaysRemaining <= 0)
                {
                    spoiledCount += Piles[i].Count;
                    Piles.RemoveAt(i);
                }
            }
            return spoiledCount;
        }

        /// <summary>Serialize to JSON string for storage in modData.</summary>
        public string Serialize()
        {
            return JsonConvert.SerializeObject(Piles);
        }

        /// <summary>Deserialize from JSON string stored in modData.</summary>
        public static SpoilageData Deserialize(string json)
        {
            var data = new SpoilageData();
            if (string.IsNullOrEmpty(json)) return data;

            try
            {
                data.Piles = JsonConvert.DeserializeObject<List<SpoilagePile>>(json) ?? new List<SpoilagePile>();
            }
            catch
            {
                data.Piles = new List<SpoilagePile>();
            }
            return data;
        }

        /// <summary>Read spoilage data from an item's modData.</summary>
        public static SpoilageData FromItem(StardewValley.Item item)
        {
            if (item == null) return null;
            if (item.modData.TryGetValue(ModDataKey, out string json))
            {
                return Deserialize(json);
            }
            return null;
        }

        /// <summary>Write spoilage data to an item's modData.</summary>
        public void SaveToItem(StardewValley.Item item)
        {
            if (item == null) return;
            if (Piles.Count == 0)
            {
                item.modData.Remove(ModDataKey);
            }
            else
            {
                item.modData[ModDataKey] = Serialize();
            }
        }

        /// <summary>Remove spoilage data from an item.</summary>
        public static void ClearFromItem(StardewValley.Item item)
        {
            item?.modData.Remove(ModDataKey);
        }

        /// <summary>Check if an item has spoilage data.</summary>
        public static bool HasSpoilageData(StardewValley.Item item)
        {
            return item?.modData.ContainsKey(ModDataKey) == true;
        }
    }
}
