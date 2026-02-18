using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using SObject = StardewValley.Object;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using StardewSurvivalProject.source;

namespace StardewSurvivalProject
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        // New refactored systems
        private source.core.GameStateManager gameState;
        private source.ui.AssetLoader assetLoader;
        private source.ui.HudRenderer hudRenderer;
        private source.commands.Commands commandManager;
        private source.handlers.MoodEventHandler moodEventHandler;

        // Expose for harmony patches
        public static Texture2D InfoIcon;
        public static Texture2D ModIcon;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            try
            {
                ModConfig.GetInstance().SetConfig(this.Helper.ReadConfig<ModConfig>());
            }
            catch (Exception ex)
            {
                Monitor.Log($"Encountered an error while loading the config.json file. Default settings will be used instead. Full error message:\n-----\n{ex.ToString()}", LogLevel.Error);
            }
            //Initialize Global Log
            source.LogHelper.Monitor = this.Monitor;

            //for checking water tile to drink
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            //apply custom effect
            helper.Events.GameLoop.OneSecondUpdateTicked += this.OnSecondPassed;
            //for properly load manager with player save info
            helper.Events.GameLoop.SaveLoaded += this.OnLoadedSave;
            //for disconnect current player from manager
            helper.Events.GameLoop.ReturnedToTitle += this.OnExitToTitle;
            //for passive stamina drain check
            helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
            //for render additional element on screen
            helper.Events.Display.RenderedHud += this.OnRenderedHud;
            //for checking eaten item
            source.events.CustomEvents.OnItemEaten += this.OnItemEaten;
            //for checking tool used
            source.events.CustomEvents.OnToolUsed += this.OnItemUsed;
            //for feeding spouse event
            source.events.CustomEvents.OnGiftGiven += this.OnGiftGiven;
            //for saving data to separate folder
            helper.Events.GameLoop.Saved += this.OnGameSaved;
            //for checking if player is running
            helper.Events.GameLoop.UpdateTicked += this.UpdateTicked;
            //for overnight passive drain
            helper.Events.GameLoop.DayEnding += this.OnDayEnding;
            //for fever effect applying dice roll
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            //for initializing generic config menu
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            //for loading and patching assets (buff icon in particular)
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            //for confirming buff icons has been loaded properly and initialize buff with proper index
            helper.Events.Content.AssetReady += this.OnAssetReady;
            //for sanity feature
            source.events.CustomEvents.OnItemPlaced += this.OnItemPlaced;
            //mental breakdown event
            source.events.CustomEvents.OnMentalBreak += this.OnMentalBreak;
            //for forwarding mod config to multiplayer client
            helper.Events.Multiplayer.PeerConnected += this.OnPeerConnected;
            //for receiving mod config from host
            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;


            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            source.data.HealingItemDictionary.loadList(this);
            source.data.CustomHydrationDictionary.loadList(this);
            source.data.CustomEnvironmentDictionary.loadList(this);
            source.data.TempControlObjectDictionary.loadList(this);
            source.data.ClothingTempResistantDictionary.loadList(this);
            source.data.CustomHungerDictionary.loadList(this);

            // Initialize new refactored systems
            gameState = new source.core.GameStateManager(helper);
            assetLoader = new source.ui.AssetLoader(helper, this.Monitor);
            moodEventHandler = new source.handlers.MoodEventHandler(gameState, this.Monitor);
            
            this.Monitor.Log("Initiating Harmony patches", LogLevel.Debug);
            source.harmony_patches.HarmonyPatches.InitPatches(this.ModManifest.UniqueID, this.Monitor, gameState);

            // Load all assets using new AssetLoader
            assetLoader.LoadAllAssets();
            InfoIcon = assetLoader.InfoIcon;
            ModIcon = assetLoader.ModIcon;

            // Initialize HUD renderer (will be created after gameState is initialized)
            // hudRenderer is created in OnLoadedSave when we have a valid player

            // Load console commands
            commandManager = new source.commands.Commands(gameState);
            helper.ConsoleCommands.Add("player_sethunger", "Set your hunger to a specified amount", commandManager.SetHungerCmd);
            helper.ConsoleCommands.Add("player_setthirst", "Set your hydration level to a specified amount", commandManager.SetThirstCmd);
            helper.ConsoleCommands.Add("player_testeffect", "Test applying effect to player", commandManager.SetEffect);
            helper.ConsoleCommands.Add("player_settemp", "Set your body temperature to a specified value", commandManager.SetBodyTemp);
            helper.ConsoleCommands.Add("player_setmood", "Set your mood to a specified value", commandManager.SetMood);
        }

        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            // if the message doesn't have the right format, ignore it
            if (e.FromModID != this.ModManifest.UniqueID || e.Type != "SyncModConfig")
                return;

            // deserialize the message data
            ModConfig config = e.ReadAs<ModConfig>();
            // set the config
            ModConfig.GetInstance().SetConfig(config);
        }

        private void OnPeerConnected(object sender, PeerConnectedEventArgs e)
        {
            // send the mod config to the client
            this.Helper.Multiplayer.SendMessage(ModConfig.GetInstance(), "SyncModConfig", modIDs: new[] { this.ModManifest.UniqueID }, playerIDs: new[] { e.Peer.PlayerID });
        }

        private void OnMentalBreak(object sender, EventArgs e)
        {
            if (sender != Game1.player) return;

            Game1.addHUDMessage(new HUDMessage("Player is had mental breakdown, they spent 1 hour contemplating their life", HUDMessage.error_type));

            // Apply catharsis buff immediately (6 hours duration)
            var player = gameState?.GetPlayerModel();
            if (player?.mood != null)
            {
                player.mood.AddMoodElement("Catharsis", 25, 360, 
                    "Feeling relief after emotional release");
                Monitor.Log("Catharsis mood boost applied after mental break", LogLevel.Debug);
            }

            // If in single player, advance the time by 1 hour
            if (!Context.IsMultiplayer)
            {
                Game1.timeOfDay += 100;
            }
            else
            {
                // If in multiplayer, lock the player from moving for 1 hour in-game
                Game1.player.freezePause = 3600;
            }
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("TileSheets/BuffsIcons"))
            {
                e.Edit(assets =>
                {
                    var editor = assets.AsImage();
                    // Use AssetLoader to load effect icons
                    Dictionary<string, Texture2D> effectIcons = assetLoader.LoadEffectIcons();
                    source.effects.EffectManager.initialize(effectIcons);
                });
            }

        }

        private void OnAssetReady(object sender, AssetReadyEventArgs e)
        {
            if (e.Name.IsEquivalentTo("TileSheets/BuffsIcons"))
            {
                this.Monitor.Log($"Buff Icon has been loaded with preset: {assetLoader.GetCurrentPreset()}", LogLevel.Debug);

            }
        } 

        //handle events
        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            
            moodEventHandler?.OnDayEnding();
            gameState.OnDayEnding();
        }

        private void UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsPlayerFree || !Context.IsWorldReady || Game1.paused)
                return;

            if (Game1.player.running && Game1.player.isMoving())
            {
                bool isSprinting = ModConfig.GetInstance().UseStaminaRework && 
                    Game1.input.GetKeyboardState().IsKeyDown((Microsoft.Xna.Framework.Input.Keys)ModConfig.GetInstance().SprintButton);
                
                gameState.OnRunning(isSprinting);
                gameState.OnRunningWithStamina(isSprinting);
            }
            
            if (Game1.player.health <= 0 || Game1.player.stamina <= -15)
            {
                gameState.ResetPlayerStats();
            }
            
            // Update mood handler periodically (every 60 ticks = ~1 second)
            if (e.IsMultipleOf(60))
            {
                moodEventHandler?.OnUpdateTicked();
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (!e.Button.IsActionButton()) return;

            //should only check if player is not holding anything
            //this.Monitor.Log((Game1.player.CurrentTool is StardewValley.Tools.WateringCan).ToString(), LogLevel.Debug);

            if (Game1.player.CurrentItem != null)
            {
                if (!(Game1.player.CurrentTool is StardewValley.Tools.WateringCan)
                    && !Game1.player.CurrentItem.Name.Equals("Canteen")) return;
            }

            //if player is eating or drinking, don't do anything
            if (Game1.player.isEating) return;

            //if the grab-able tile is "open-water" (3x3 tile radius around it is not passable (exclude bridges))
            bool isOpenWater = Game1.currentLocation.isOpenWater((int)e.Cursor.GrabTile.X, (int)e.Cursor.GrabTile.Y);

            //if the grab-able tile is water (3x3 tile radius around player)
            bool isWater = Game1.currentLocation.isWaterTile((int)e.Cursor.GrabTile.X, (int)e.Cursor.GrabTile.Y);

            //check if the player will drink sea water by checking their location or are they able to catch ocean crab pot
            bool isOcean = (Game1.currentLocation is StardewValley.Locations.Beach) || Game1.currentLocation.catchOceanCrabPotFishFromThisSpot((int) (e.Cursor.GrabTile.X), (int) (e.Cursor.GrabTile.Y));
            //this.Monitor.Log($"ocean = {isOcean}; water = {isWater}", LogLevel.Debug);

            //drink on watering can, make sense right?
            if (Game1.player.CurrentTool is StardewValley.Tools.WateringCan)
            {
                //this.Monitor.Log("will drink from watering can");

                if (ModConfig.GetInstance().EnvironmentHydrationMode == "disable") return;

                if (ModConfig.GetInstance().EnvironmentHydrationMode == "strict" && !Game1.input.GetKeyboardState().IsKeyDown((Microsoft.Xna.Framework.Input.Keys)ModConfig.GetInstance().SecondaryLayerButton)) return;

                //TODO: subtract amount move to config
                if (Game1.player.CurrentTool is StardewValley.Tools.WateringCan && ((StardewValley.Tools.WateringCan)Game1.player.CurrentTool).WaterLeft >= ModConfig.GetInstance().HydrationGainOnEnvironmentWaterDrinking)
                {
                    ((StardewValley.Tools.WateringCan)Game1.player.CurrentTool).WaterLeft -= (int)ModConfig.GetInstance().HydrationGainOnEnvironmentWaterDrinking;
                    gameState.OnEnvironmentalDrink(false, true);
                    this.Monitor.Log("drank from watering can");
                }
                else
                {
                    this.Monitor.Log("cant drink from watering can");
                    Game1.addHUDMessage(new HUDMessage("There is not enough water in your Watering Can", HUDMessage.error_type));
                }   
            }
            else if (Game1.player.CurrentItem != null && Game1.player.CurrentItem.Name.Equals("Canteen") && isWater && !isOcean)
            {
                //remove empty canteen
                Game1.player.reduceActiveItemByOne();
                //give dirty canteen
                string itemId = source.data.ItemNameCache.getIDFromCache("Dirty Canteen");
                if (itemId != "-1")
                {
                    Game1.player.addItemToInventory(new SObject(itemId, 1));
                }
            }
            else if (isWater)
            {
                if (ModConfig.GetInstance().EnvironmentHydrationMode == "disable") return;

                if (ModConfig.GetInstance().EnvironmentHydrationMode == "strict" && !Game1.input.GetKeyboardState().IsKeyDown((Microsoft.Xna.Framework.Input.Keys)ModConfig.GetInstance().SecondaryLayerButton)) return;

                gameState.OnEnvironmentalDrink(isOcean, isWater);
            }
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.eventUp || hudRenderer == null)
                return;

            // Use the new HudRenderer
            hudRenderer.RenderHud(e.SpriteBatch);
        }

        private void OnSecondPassed(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            
            gameState.OnSecondUpdate();
        }

        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            
            gameState.OnEnvironmentUpdate(e.NewTime, Game1.currentSeason, Game1.weatherIcon, 
                Game1.currentLocation, Game1.CurrentMineLevel);
            gameState.OnClockUpdate();
        }

        private void OnItemEaten(object sender, EventArgs e)
        {
            if (sender != Game1.player)
                return;

            SObject ateItem = Game1.player.itemToEat as SObject;
            gameState.OnItemEaten(ateItem);            moodEventHandler?.OnItemEaten();        }

        private void OnItemUsed(object sender, EventArgs e)
        {
            if (sender != Game1.player)
                return;

            gameState.OnToolUsed(Game1.player.CurrentTool);
        }

        private void OnGiftGiven(object sender, source.events.GiftEventArgs e)
        {
            if (sender != Game1.player)
                return;

            gameState.OnGiftGiven(e.Npc, e.Gift);
        }

        private void OnItemPlaced(object sender, EventArgs e)
        {
            SObject obj = sender as SObject;
            LogHelper.Info($"Item placed: {obj.Name}");
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            gameState.OnDayStarted();
            moodEventHandler?.OnDayStarted();
        }

        private void OnLoadedSave(object sender, SaveLoadedEventArgs e)
        {
            gameState.Initialize(Game1.player);
            gameState.LoadData(this);
            
            // Initialize HUD renderer now that we have a valid game state
            hudRenderer = new source.ui.HudRenderer(assetLoader, gameState);
            
            //cache these item's id
            source.data.ItemNameCache.cacheItem("Canteen");
            source.data.ItemNameCache.cacheItem("Full Canteen");
            source.data.ItemNameCache.cacheItem("Dirty Canteen");
            source.data.ItemNameCache.cacheItem("Ice Water Canteen");
            source.data.ItemNameCache.cacheItem("Ice Ionized Water Canteen");
            source.data.ItemNameCache.cacheItem("Ionized Full Canteen");
        }

        private void OnGameSaved(object sender, SavedEventArgs e)
        {
            gameState.SaveData(this);
        }

        private void OnExitToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            gameState.OnExit();
            hudRenderer = null;  // Clean up HUD renderer
            source.data.ItemNameCache.clearCache();
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            source.api.ConfigMenu.Init(this);
        }
    }
}