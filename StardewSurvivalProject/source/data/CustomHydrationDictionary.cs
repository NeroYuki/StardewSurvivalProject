using System;
using System.Collections.Generic;
using StardewModdingAPI;
using System.IO;
using Newtonsoft.Json;

namespace StardewSurvivalProject.source.data
{
    public class HydrationItemData
    {
        public String name { get; set; } = "";
        public double value { get; set; } = 0;
        public double coolingModifier { get; set; } = 1;
    }

    public class CustomHydrationDictionary
    {
        //load a whitelist of item that can be used to heal player (healing value is separated from edibility)
        public static Dictionary<String, HydrationItemData> value_list = new Dictionary<string, HydrationItemData>();
        public static Dictionary<String, HydrationItemData> wildcard_value_list = new Dictionary<string, HydrationItemData>();

        public static void loadList(Mod context)
        {

            String RelativePath = Path.Combine("customHydrationData.json");
            HydrationItemData[] tempArray = context.Helper.Data.ReadJsonFile<HydrationItemData[]>(RelativePath);
            if (tempArray == null)
            {
                LogHelper.Warn("No hydration item entry is found");
                return;
            }
            for (int i = 0; i < tempArray.Length; i++)
                if (tempArray[i].name.Contains("*"))
                    wildcard_value_list.Add(tempArray[i].name, tempArray[i]);
                else
                    value_list.Add(tempArray[i].name, tempArray[i]);
            LogHelper.Debug("Hydration Item Data loaded");
        }

        public static double getHydrationValue(string itemName)
        {
            if (value_list.ContainsKey(itemName))
            {
                return value_list[itemName].value;
            }
            else
            {
                // iterate through wildcard list
                foreach (KeyValuePair<string, HydrationItemData> entry in wildcard_value_list)
                {
                    // contain match
                    if (entry.Key.StartsWith("*") && entry.Key.EndsWith("*") && itemName.Contains(entry.Key.Substring(1, entry.Key.Length - 2)))
                    {
                        return entry.Value.value;
                    }
                    // prefix match
                    else if (entry.Key.StartsWith("*") && itemName.EndsWith(entry.Key.Substring(1)))
                    {
                        return entry.Value.value;
                    }
                    // postfix match
                    else if (entry.Key.EndsWith("*") && itemName.StartsWith(entry.Key.Substring(0, entry.Key.Length - 1)))
                    {
                        return entry.Value.value;
                    }
                }
                return 0;
            }
        }

        public static double getCoolingModifierValue(string itemName)
        {
            if (value_list.ContainsKey(itemName))
            {
                double res = value_list[itemName].coolingModifier;
                if (res < 0) return 1;
                else return res;
            }
            else
            {
                return 1;
            }
        }

        public static HydrationItemData getItemData(string itemName)
        {
            if (value_list.ContainsKey(itemName))
            {
                return value_list[itemName];
            }
            else
            {
                return null;
            }
        }
    }
}
