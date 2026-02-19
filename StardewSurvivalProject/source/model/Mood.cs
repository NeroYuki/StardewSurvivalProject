using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using static StardewValley.LocationRequest;

public delegate void Callback();

namespace StardewSurvivalProject.source.model
{
    public enum MoodLevel
    {
        MentalBreak,
        Distress,
        Sad,
        Discontent,
        Neutral,
        Content,
        Happy,
        Overjoy,
    }

    public class MoodElement
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double Value { get; set; }
        public double MinutesToExpire { get; set; } // -1 = never expire (needs manual decay)
        public int DayAdded { get; set; }
        public int TimeAdded { get; set; }
        
        public MoodElement(string name, double value, double minutesToExpire, int dayAdded, int timeAdded, string description = null)
        {
            Name = name;
            Value = value;
            MinutesToExpire = minutesToExpire;
            DayAdded = dayAdded;
            TimeAdded = timeAdded;
            Description = description ?? name;
        }
    }

    public class Mood
    {
        private double _baseValue = 50; // Base mood value — overridden from config on construction

        // Track mood modifiers with their expiration
        public List<MoodElement> MoodElements = new List<MoodElement>();
        
        // Track actions done today for monotony detection
        private Dictionary<string, int> ActionCountToday = new Dictionary<string, int>();
        
        // Track recent food for variety detection (queue of last 20 items)
        private Queue<string> RecentFoodQueue = new Queue<string>(20);

        private readonly Callback _onMentalBreakCallback;

        [JsonConstructor]
        public Mood(double value, MoodLevel level)
        {
            this._baseValue = value;
            this.Level = level;
        }

        // for new player
        public Mood(Callback OnMentalBreakCallback)
        {
            _baseValue = ModConfig.GetInstance().MoodBaseValue;
            _onMentalBreakCallback = OnMentalBreakCallback;
        }

        // for save data
        public Mood(Mood mood, Callback OnMentalBreakCallback)
        {
            this._baseValue = mood._baseValue;
            this.MoodElements = new List<MoodElement>(mood.MoodElements);
            this.ActionCountToday = new Dictionary<string, int>(mood.ActionCountToday);
            this.RecentFoodQueue = new Queue<string>(mood.RecentFoodQueue);
            _onMentalBreakCallback = OnMentalBreakCallback;
            RecalculateValue();
        }

        public double Value
        {
            get
            {
                return _baseValue;
            }
            set
            {
                var cfg = ModConfig.GetInstance();
                _baseValue = value > cfg.MoodCap ? cfg.MoodCap : value < cfg.MoodMinValue ? cfg.MoodMinValue : value;
                Level = GetMoodLevel(_baseValue);
            }
        }
        
        // Get total mood (base + all active elements)
        public double TotalMood
        {
            get
            {
                var cfg = ModConfig.GetInstance();
                double total = _baseValue + MoodElements.Sum(e => e.Value);
                return Math.Max(cfg.MoodMinValue, Math.Min(cfg.MoodCap, total));
            }
        }

        public MoodLevel Level = MoodLevel.Neutral;

        // Maybe the mood mechanic from Rimworld would be good here

        /** Potential penalty
         * Monotonous task (-5 to -50, minute to expire is -1, since it will instead decay over time, 
		 simply have a counter for each action type the player did - planting, watering, mining, lumbering, etc., have a cap of x times of the same action thorughout the day, 
		 the penalty will scale linearly with minimum threshold of 10% up to 100% of the cap for the action that has been done the most in the day)
         * Eating the same food (-5 to -20, have a queue of the last 20 food item the player ate, if 5 of them are the same, start applying a -5, scale up to -20 when the queue
		  is full of the same item)
         * Eating raw food (-10, non-cooked food, not stacked, last 4 hours)
         * Get trash from fishing (-10, make sense, last 1 hour)
         * NPC dislike (-5) or hate (-10) your gift (last 2 hours)
         * Get fever (-10) (last until fever is gone, can be combined with the additional stamina drain on fever effect to make it more punishing)
         * (From butcher mod) kill animal (-10 to 0) (last 4 hours)
		 * Insomnia (-5, if player passed out after 2am) (last 6 hours)
        **/

		/** Potential bonus
		 * Talk to NPC (+1 to +5, 1 for each unique NPC, cap at 5 per day, last until end of day)
		 * Eat good food (+5 to +20, scale by hunger restored, minimum of 25, maximum of 100, last 4 hours)
		 * NPC like (+5) or love (+10) your gift (last 2 hours)
		 * Skill level up (+5, if multiple skill level up at the same time, stack to +15) (last until end of day)
		 * Playing the mini game at the arcade (+5 to +15 depends on the score) (last until end of day)
		 * Festival (+30, during the festival day) 
		 * First day of the the season (+10, last until end of the day)
		 * Well-slept (+5, sleep before 12am, last until 12pm)
		*/

        /// <summary>
        /// Add or update a mood element
        /// </summary>
        public void AddMoodElement(string name, double value, double minutesToExpire, string description = null)
        {
            var game = StardewValley.Game1.game1;
            int currentDay = StardewValley.Game1.Date.TotalDays;
            int currentTime = StardewValley.Game1.timeOfDay;
            
            // Remove existing element with same name
            MoodElements.RemoveAll(e => e.Name == name);
            
            // Add new element
            MoodElements.Add(new MoodElement(name, value, minutesToExpire, currentDay, currentTime, description));
            RecalculateValue();
        }
        
        /// <summary>
        /// Update mood elements - remove expired ones and apply decay
        /// Call this every 10 in-game minutes
        /// </summary>
        public void UpdateMoodElements()
        {
            int currentDay = StardewValley.Game1.Date.TotalDays;
            int currentTime = StardewValley.Game1.timeOfDay;
            
            // Calculate elapsed minutes
            var toRemove = new List<MoodElement>();
            foreach (var element in MoodElements)
            {
                if (element.MinutesToExpire == -1) continue; // Never expires (manual decay)
                
                // Calculate total minutes elapsed
                int daysPassed = currentDay - element.DayAdded;
                int minutesFromDays = daysPassed * 24 * 60; // Convert days to minutes
                
                // Calculate minutes within same day
                int startMinutes = (element.TimeAdded / 100) * 60 + (element.TimeAdded % 100);
                int currentMinutes = (currentTime / 100) * 60 + (currentTime % 100);
                int minutesInDay = currentMinutes - startMinutes;
                
                int totalMinutes = minutesFromDays + minutesInDay;
                
                if (totalMinutes >= element.MinutesToExpire)
                {
                    toRemove.Add(element);
                }
            }
            
            // Remove expired elements
            foreach (var element in toRemove)
            {
                MoodElements.Remove(element);
            }
            
            RecalculateValue();
        }
        
        /// <summary>
        /// Reset daily elements (call at day start)
        /// </summary>
        public void OnDayStart()
        {
            // Reset action counts for monotony tracking
            ActionCountToday.Clear();
            
            // Remove daily-expiring mood elements
            MoodElements.RemoveAll(e => e.MinutesToExpire > 0 && e.MinutesToExpire <= 1440);
            
            RecalculateValue();
        }
        
        /// <summary>
        /// Track an action for monotony detection
        /// </summary>
        public void TrackAction(string actionType)
        {
            if (!ActionCountToday.ContainsKey(actionType))
                ActionCountToday[actionType] = 0;
                
            ActionCountToday[actionType]++;
            
            // Update monotony penalty
            UpdateMonotonyPenalty();
        }
        
        /// <summary>
        /// Track food eaten for variety detection
        /// </summary>
        public void TrackFoodEaten(string foodName)
        {
            if (RecentFoodQueue.Count >= 20)
                RecentFoodQueue.Dequeue();
                
            RecentFoodQueue.Enqueue(foodName);
            
            // Update food variety penalty
            UpdateFoodVarietyPenalty();
        }
        
        /// <summary>
        /// Update monotony penalty based on action counts
        /// </summary>
        private void UpdateMonotonyPenalty()
        {
            if (ActionCountToday.Count == 0) return;
            
            // Find the most repeated action
            int maxCount = ActionCountToday.Values.Max();
            int actionCap = 100; // Cap for maximum repetition
            
            // Apply penalty if threshold is exceeded (10% of cap)
            if (maxCount > actionCap * 0.1)
            {
                double percentage = Math.Min((maxCount - actionCap * 0.1) / (actionCap * 0.9), 1.0);
                double penalty = -5 + (-45 * percentage); // Scale from -5 to -50
                
                AddMoodElement("Monotonous Task", penalty, -1);
            }
            else
            {
                MoodElements.RemoveAll(e => e.Name == "Monotonous Task");
            }
        }
        
        /// <summary>
        /// Update food variety penalty based on recent foods
        /// </summary>
        private void UpdateFoodVarietyPenalty()
        {
            if (RecentFoodQueue.Count < 5) return;
            
            // Group by food name and count occurrences
            var foodCounts = RecentFoodQueue.GroupBy(f => f).ToDictionary(g => g.Key, g => g.Count());
            var mostCommonCount = foodCounts.Values.Max();
            
            // Apply penalty if same food eaten 5+ times
            if (mostCommonCount >= 5)
            {
                double percentage = Math.Min((mostCommonCount - 5) / 15.0, 1.0); // Scale to max at 20
                double penalty = -5 + (-15 * percentage); // Scale from -5 to -20
                
                AddMoodElement("Eating Same Food", penalty, 240); // 4 hours
            }
            else
            {
                MoodElements.RemoveAll(e => e.Name == "Eating Same Food");
            }
        }
        
        /// <summary>
        /// Get a formatted breakdown of current mood modifiers for tooltip
        /// </summary>
        public string GetMoodBreakdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Mood: {TotalMood:F1} ({Level})");
            sb.AppendLine($"Base: {_baseValue:F1}");
            
            if (MoodElements.Count > 0)
            {
                sb.AppendLine("Modifiers:");
                foreach (var element in MoodElements.OrderByDescending(e => Math.Abs(e.Value)))
                {
                    string sign = element.Value >= 0 ? "+" : "";
                    sb.AppendLine($"  {element.Name}: {sign}{element.Value:F1}");
                }
            }
            
            return sb.ToString().TrimEnd();
        }
        
        /// <summary>
        /// Recalculate level based on total mood
        /// </summary>
        private void RecalculateValue()
        {
            Level = GetMoodLevel(TotalMood);
        }

        public MoodLevel GetMoodLevel(double value)
        {
            var cfg = ModConfig.GetInstance();
            if (value < cfg.MoodDistressThreshold)
                return MoodLevel.Distress;
            if (value < cfg.MoodSadThreshold)
                return MoodLevel.Sad;
            if (value < cfg.MoodDiscontentThreshold)
                return MoodLevel.Discontent;
            if (value < cfg.MoodNeutralThreshold)
                return MoodLevel.Neutral;
            if (value < cfg.MoodContentThreshold)
                return MoodLevel.Content;
            if (value < cfg.MoodHappyThreshold)
                return MoodLevel.Happy;
            else
                return MoodLevel.Overjoy;
        }

        public void CheckForMentalBreak()
        {
            var rand = new Random();
            var out_val = rand.NextDouble();
            var currentLevel = GetMoodLevel(TotalMood);
            var cfg = ModConfig.GetInstance();
            
            // Roll every 10 minutes; chances are read from config (stored as percentages)
            if (currentLevel == MoodLevel.Discontent && out_val < cfg.MentalBreakChanceOnDiscontent / 100.0
                || currentLevel == MoodLevel.Sad && out_val < cfg.MentalBreakChanceOnSad / 100.0
                || currentLevel == MoodLevel.Distress && out_val < cfg.MentalBreakChanceOnDistress / 100.0)
            {
                Level = MoodLevel.MentalBreak;
                _onMentalBreakCallback?.Invoke();
            }
        }
    }
}
