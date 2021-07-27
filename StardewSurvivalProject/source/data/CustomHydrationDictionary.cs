using System;
using System.Collections.Generic;
using StardewModdingAPI;
using System.IO;
using Newtonsoft.Json;

namespace StardewSurvivalProject.source.data
{
    class CustomHydrationDictionary
    {
        public class HydrationItemData
        {
            public String name { get; set; } = "";
            public int value { get; set; } = 0;
        }
        //load a whitelist of item that can be used to heal player (healing value is separated from edibility)
        public static Dictionary<String, HydrationItemData> value_list = new Dictionary<string, HydrationItemData>();

        public static void loadList(Mod context)
        {

            String RelativePath = Path.Combine(context.Helper.DirectoryPath, "customHydrationData.json");
            String jsonData = File.ReadAllText(RelativePath);
            HydrationItemData[] tempArray = JsonConvert.DeserializeObject<HydrationItemData[]>(jsonData);
            //TODO: load this list from a file
            for (int i = 0; i < tempArray.Length; i++)
                value_list.Add(tempArray[i].name, tempArray[i]);
            LogHelper.Debug("Hydration Item Data loaded");
        }

        public static int getHydrationValue(string itemName)
        {
            if (value_list.ContainsKey(itemName))
            {
                return value_list[itemName].value;
            }
            else
            {
                return 0;
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
