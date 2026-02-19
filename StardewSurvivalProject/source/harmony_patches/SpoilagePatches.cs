using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace StardewSurvivalProject.source.harmony_patches
{
    /// <summary>
    /// Harmony patches for tracking food spoilage across stack operations.
    /// Patches Item.addToStack, Item.getOne/GetOneCopyFrom, and Item.ConsumeStack
    /// to properly transfer spoilage pile data when items are stacked/split.
    /// </summary>
    class SpoilagePatches
    {
        private static IMonitor Monitor = null;

        public static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }

        /// <summary>
        /// PREFIX for Item.addToStack: Before merging, capture the spoilage data from the other stack.
        /// We store the other stack's spoilage info so the postfix can merge it.
        /// </summary>
        public static void AddToStack_Prefix(Item __instance, Item otherStack, out model.SpoilageData __state)
        {
            __state = null;
            try
            {
                if (otherStack == null) return;

                // Ensure both items have spoilage tracking
                systems.SpoilageSystem.EnsureSpoilageTracking(__instance);
                systems.SpoilageSystem.EnsureSpoilageTracking(otherStack);

                // Capture the other stack's spoilage data before the merge
                __state = model.SpoilageData.FromItem(otherStack);
            }
            catch (Exception ex)
            {
                Monitor?.Log($"Failed in {nameof(AddToStack_Prefix)}: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// POSTFIX for Item.addToStack: After the game merges stacks, transfer spoilage piles.
        /// The return value tells us how many items couldn't be added (remained in otherStack).
        /// </summary>
        public static void AddToStack_Postfix(Item __instance, Item otherStack, int __result, model.SpoilageData __state)
        {
            try
            {
                if (__state == null || otherStack == null) return;

                var mySpoilage = model.SpoilageData.FromItem(__instance);
                if (mySpoilage == null) return;

                // __result = items that couldn't be added (remained in otherStack)
                int itemsActuallyAdded = otherStack.Stack > 0 
                    ? __state.TotalCount - __result 
                    : __state.TotalCount;

                // If some items were rejected, we need to figure out which piles got added
                if (__result > 0 && __result < __state.TotalCount)
                {
                    // The oldest items get transferred first
                    // Clone the other stack's piles and take itemsActuallyAdded from them
                    var otherPilesCopy = new model.SpoilageData();
                    otherPilesCopy.Piles = new List<model.SpoilagePile>();
                    foreach (var p in __state.Piles)
                        otherPilesCopy.Piles.Add(p.Clone());

                    var transferred = otherPilesCopy.RemoveItems(itemsActuallyAdded);
                    mySpoilage.AddPiles(transferred);

                    // The remaining piles stay with otherStack
                    otherPilesCopy.SaveToItem(otherStack);
                }
                else if (__result == 0)
                {
                    // All items were added
                    mySpoilage.AddPiles(__state.Piles);
                    // Clear spoilage from the consumed stack
                    model.SpoilageData.ClearFromItem(otherStack);
                }
                // else __result >= __state.TotalCount means nothing was added

                mySpoilage.SaveToItem(__instance);
            }
            catch (Exception ex)
            {
                Monitor?.Log($"Failed in {nameof(AddToStack_Postfix)}: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// POSTFIX for Item.GetOneCopyFrom: When an item is cloned (getOne), 
        /// copy the spoilage data and set it to track only 1 item.
        /// The game calls this for right-click splitting (picks up 1 or half).
        /// </summary>
        public static void GetOneCopyFrom_Postfix(Item __instance, Item source)
        {
            try
            {
                // modData is already copied by base GetOneCopyFrom, but the data reflects
                // the full original pile structure. Since getOne sets Stack = 1,
                // we need to fix the pile data to represent just 1 item.
                // However, we should NOT modify here because the caller will adjust
                // the stack size afterwards via ConsumeStack. The actual pile transfer
                // will be handled by our ConsumeStack patch.

                // The modData copy already happens in the base method.
                // We need to flag this item as needing pile reconciliation.
                // Actually, let's clear the spoilage data and let ConsumeStack handle the transfer.
                
                var sourceSpoilage = model.SpoilageData.FromItem(source);
                if (sourceSpoilage == null) return;

                // For a getOne, we take 1 item from the oldest pile
                var taken = sourceSpoilage.RemoveItems(1);
                var newSpoilage = new model.SpoilageData();
                newSpoilage.AddPiles(taken);
                newSpoilage.SaveToItem(__instance);

                // Update source's spoilage
                sourceSpoilage.SaveToItem(source);
            }
            catch (Exception ex)
            {
                Monitor?.Log($"Failed in {nameof(GetOneCopyFrom_Postfix)}: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// POSTFIX for Item.ConsumeStack: When items are consumed from a stack (e.g., right-click split),
        /// update the spoilage piles to reflect the removed items.
        /// </summary>
        public static void ConsumeStack_Postfix(Item __instance, int amount, Item __result)
        {
            try
            {
                if (__instance == null) return;

                var spoilage = model.SpoilageData.FromItem(__instance);
                if (spoilage == null) return;

                // The game already decremented __instance.Stack.
                // If __result is null, the entire stack was consumed — nothing to track.
                // If __result is __instance, some items remain.
                if (__result != null)
                {
                    // Sync piles with the new stack size
                    // Items were already removed by GetOneCopyFrom_Postfix or we need to remove now
                    int expectedTotal = __result.Stack;
                    int currentTotal = spoilage.TotalCount;
                    
                    if (currentTotal > expectedTotal)
                    {
                        spoilage.RemoveItems(currentTotal - expectedTotal);
                        spoilage.SaveToItem(__instance);
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor?.Log($"Failed in {nameof(ConsumeStack_Postfix)}: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// POSTFIX for InventoryMenu.rightClick: After right-click splitting, reconcile spoilage piles.
        /// The right-click handler creates a new item via getOne() and then adjusts stack sizes.
        /// We need to ensure the pile data matches the final stack sizes.
        /// </summary>
        public static void InventoryMenu_RightClick_Postfix(InventoryMenu __instance, int x, int y, Item toAddTo, Item __result)
        {
            try
            {
                if (__result == null) return;

                // Find the slot that was clicked
                foreach (var component in __instance.inventory)
                {
                    if (!component.containsPoint(x, y)) continue;
                    
                    int slotNumber = Convert.ToInt32(component.name);
                    if (slotNumber >= __instance.actualInventory.Count) break;

                    var slotItem = __instance.actualInventory[slotNumber];
                    
                    // Reconcile the slot item's spoilage with its actual stack
                    if (slotItem != null)
                    {
                        systems.SpoilageSystem.SyncSpoilageWithStack(slotItem);
                    }

                    // Reconcile the returned item's spoilage with its actual stack
                    systems.SpoilageSystem.SyncSpoilageWithStack(__result);
                    break;
                }
            }
            catch (Exception ex)
            {
                Monitor?.Log($"Failed in {nameof(InventoryMenu_RightClick_Postfix)}: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// POSTFIX for Farmer.addItemToInventory: Ensure newly added items have spoilage tracking.
        /// </summary>
        public static void Farmer_AddItem_Postfix(Farmer __instance, Item item)
        {
            try
            {
                if (item == null || !__instance.IsLocalPlayer) return;
                systems.SpoilageSystem.EnsureSpoilageTracking(item);
            }
            catch (Exception ex)
            {
                Monitor?.Log($"Failed in {nameof(Farmer_AddItem_Postfix)}: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// POSTFIX for Utility.addItemToInventory: After adding an item to a container inventory,
        /// ensure spoilage tracking is maintained.
        /// </summary>
        public static void Utility_AddItem_Postfix(Item item, int position, IList<Item> items)
        {
            try
            {
                if (item == null) return;
                if (position >= 0 && position < items.Count && items[position] != null)
                {
                    systems.SpoilageSystem.SyncSpoilageWithStack(items[position]);
                }
            }
            catch (Exception ex)
            {
                Monitor?.Log($"Failed in {nameof(Utility_AddItem_Postfix)}: {ex}", LogLevel.Error);
            }
        }
    }
}
