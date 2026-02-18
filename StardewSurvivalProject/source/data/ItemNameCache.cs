using System;
using System.Collections.Generic;
using StardewValley;

namespace StardewSurvivalProject.source.data
{
    public class ItemNameCache
    {
        public static Dictionary<string, string> name_to_id = new Dictionary<string, string>();
        
        //return false if cant find the item
        public static bool cacheItem(string name)
        {
            foreach (KeyValuePair<string, StardewValley.GameData.Objects.ObjectData> itemInfoString in Game1.objectData)
            {
                //check string start for object name
                if (itemInfoString.Value.Name != null && itemInfoString.Value.Name.StartsWith(name))
                {
                    name_to_id.Add(name, itemInfoString.Key);
                    return true;
                }
            }
            return false;
        }

        public static void clearCache()
        {
            name_to_id.Clear();
        }

        public static string getIDFromCache(string name)
        {
            if (name_to_id.ContainsKey(name))
            {
                return name_to_id[name];
            }
            return "-1";
        }
    }
}
