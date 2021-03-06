using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using HarmonyLib;
using SObject = StardewValley.Object;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace StardewSurvivalProject
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod, IAssetEditor
    {
        private source.Manager instance;
        private source.commands.Commands commandManager;
        private Texture2D HungerBar;
        private Texture2D ThirstBar;
        private Texture2D EnvTempBar;
        private Texture2D BodyTempBar;
        private Texture2D TempIndicator;
        private Texture2D fillRect;
        //expose for harmony patches
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
            

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            source.harmony_patches.FarmerPatches.Initialize(this.Monitor);
            source.harmony_patches.ObjectPatches.Initialize(this.Monitor);
            source.harmony_patches.UIDrawPatches.Initialize(this.Monitor);

            source.data.HealingItemDictionary.loadList(this);
            source.data.CustomHydrationDictionary.loadList(this);
            source.data.CustomEnvironmentDictionary.loadList(this);
            source.data.TempControlObjectDictionary.loadList(this);
            source.data.ClothingTempResistantDictionary.loadList(this);

            instance = new source.Manager();

            source.effects.EffectManager.initialize();

            var harmony = new Harmony(this.ModManifest.UniqueID);

            // TODO: move this somewhere else lol
            harmony.Patch(
               original: AccessTools.Method(typeof(Farmer), nameof(Farmer.doneEating)),
               postfix: new HarmonyMethod(typeof(source.harmony_patches.FarmerPatches), nameof(source.harmony_patches.FarmerPatches.DoneEating_PostFix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), nameof(Farmer.EndUsingTool)),
                postfix: new HarmonyMethod(typeof(source.harmony_patches.FarmerPatches), nameof(source.harmony_patches.FarmerPatches.EndUsingTool_PostFix))
             );

            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.healthRecoveredOnConsumption)),
                prefix: new HarmonyMethod(typeof(source.harmony_patches.ObjectPatches), nameof(source.harmony_patches.ObjectPatches.CalculateHPGain_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Menus.IClickableMenu), nameof(StardewValley.Menus.IClickableMenu.drawHoverText),
                new Type[] { typeof(SpriteBatch), typeof(StringBuilder), typeof(SpriteFont), typeof(int), typeof(int), typeof(int), typeof(string), typeof(int), typeof(string[]), typeof(Item), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(CraftingRecipe), typeof(IList<Item>) }),
                postfix: new HarmonyMethod(typeof(source.harmony_patches.UIDrawPatches), nameof(source.harmony_patches.UIDrawPatches.DrawHoverText_Postfix))
            );

            //Load assets
            this.HungerBar = helper.Content.Load<Texture2D>("assets/HungerBar.png");
            this.ThirstBar = helper.Content.Load<Texture2D>("assets/ThirstBar.png");
            this.EnvTempBar = helper.Content.Load<Texture2D>("assets/EnvTempBar.png");
            this.BodyTempBar = helper.Content.Load<Texture2D>("assets/BodyTempBar.png");
            this.TempIndicator = helper.Content.Load<Texture2D>("assets/TempIndicator.png");
            this.fillRect = helper.Content.Load<Texture2D>("assets/fillRect.png");
            
            InfoIcon = helper.Content.Load<Texture2D>("assets/InfoIcon.png");
            ModIcon = helper.Content.Load<Texture2D>("assets/ModIcon.png");

            //load command
            commandManager = new source.commands.Commands(instance);
            helper.ConsoleCommands.Add("player_sethunger", "Set your hunger to a specified amount", commandManager.SetHungerCmd);
            helper.ConsoleCommands.Add("player_setthirst", "Set your hydration level to a specified amount", commandManager.SetThirstCmd);
            helper.ConsoleCommands.Add("player_testeffect", "Test applying effect to player", commandManager.SetEffect);
            helper.ConsoleCommands.Add("player_settemp", "Set your body temperature to a specified value", commandManager.SetBodyTemp);
        }

        //patch game assets
        public bool CanEdit<T>(IAssetInfo assets)
        {
            return assets.AssetNameEquals("TileSheets/BuffsIcons");
        }

        public void Edit<T>(IAssetData assets)
        {
            if (assets.AssetNameEquals("TileSheets/BuffsIcons"))
            {
                var editor = assets.AsImage();

                Texture2D burnEffectIcon = this.Helper.Content.Load<Texture2D>("assets/BurnEffect.png");
                Texture2D starvationEffectIcon = this.Helper.Content.Load<Texture2D>("assets/StarvationEffect.png");
                Texture2D hypothermiaEffectIcon = this.Helper.Content.Load<Texture2D>("assets/HypothermiaEffect.png");
                Texture2D frostbiteEffectIcon = this.Helper.Content.Load<Texture2D>("assets/FrostbiteEffect.png");
                Texture2D heatstrokeEffectIcon = this.Helper.Content.Load<Texture2D>("assets/HeatstrokeEffect.png");
                Texture2D dehydrationEffectIcon = this.Helper.Content.Load<Texture2D>("assets/DehydratedEffect.png");
                Texture2D feverEffectIcon = this.Helper.Content.Load<Texture2D>("assets/FeverEffect.png");
                Texture2D stomachacheEffectIcon = this.Helper.Content.Load<Texture2D>("assets/StomachacheEffect.png");
                Texture2D thirstEffectIcon = this.Helper.Content.Load<Texture2D>("assets/ThirstEffect.png");
                Texture2D hungerEffectIcon = this.Helper.Content.Load<Texture2D>("assets/HungerEffect.png");
                Texture2D wellFedEffectIcon = this.Helper.Content.Load<Texture2D>("assets/WellFedEffect.png");

                //extend the image to occupy a different row from other effects
                int extraEffectYCoord = editor.Data.Height;
                editor.ExtendImage(minWidth: editor.Data.Width, minHeight: extraEffectYCoord + 16);

                editor.PatchImage(burnEffectIcon, targetArea: new Rectangle(0 * 16, extraEffectYCoord, 16, 16));
                editor.PatchImage(starvationEffectIcon, targetArea: new Rectangle(1 * 16, extraEffectYCoord, 16, 16));
                editor.PatchImage(hypothermiaEffectIcon, targetArea: new Rectangle(2 * 16, extraEffectYCoord, 16, 16));
                editor.PatchImage(frostbiteEffectIcon, targetArea: new Rectangle(3 * 16, extraEffectYCoord, 16, 16));
                editor.PatchImage(heatstrokeEffectIcon, targetArea: new Rectangle(4 * 16, extraEffectYCoord, 16, 16));
                editor.PatchImage(dehydrationEffectIcon, targetArea: new Rectangle(5 * 16, extraEffectYCoord, 16, 16));
                editor.PatchImage(feverEffectIcon, targetArea: new Rectangle(6 * 16, extraEffectYCoord, 16, 16));
                editor.PatchImage(stomachacheEffectIcon, targetArea: new Rectangle(7 * 16, extraEffectYCoord, 16, 16));
                editor.PatchImage(thirstEffectIcon, targetArea: new Rectangle(8 * 16, extraEffectYCoord, 16, 16));
                editor.PatchImage(hungerEffectIcon, targetArea: new Rectangle(9 * 16, extraEffectYCoord, 16, 16));
                editor.PatchImage(wellFedEffectIcon, targetArea: new Rectangle(10 * 16, extraEffectYCoord, 16, 16));

                this.Monitor.Log("Patched effect icon to game assets", LogLevel.Debug);
            }
        }

        //handle events
        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            
            instance.onDayEnding();
        }

        private void UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsPlayerFree || !Context.IsWorldReady || Game1.paused)
                return;

            if (Game1.player.running && Game1.player.isMoving())
            {
                instance.updateOnRunning();
            }
            if (Game1.player.health <= 0 || Game1.player.stamina <= -15)
            {
                instance.ResetPlayerHungerAndThirst();
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
                    && !(Game1.player.CurrentItem.Name.Equals("Canteen"))) return;
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
                this.Monitor.Log("will drink from watering can");
                //TODO: subtract amount move to config
                if (Game1.player.CurrentTool is StardewValley.Tools.WateringCan && ((StardewValley.Tools.WateringCan)Game1.player.CurrentTool).WaterLeft >= ModConfig.GetInstance().HydrationGainOnEnvironmentWaterDrinking)
                {
                    ((StardewValley.Tools.WateringCan)Game1.player.CurrentTool).WaterLeft -= (int)ModConfig.GetInstance().HydrationGainOnEnvironmentWaterDrinking;
                    instance.onEnvDrinkingUpdate(false, true);
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
                Game1.player.removeItemFromInventory(Game1.player.CurrentItem);
                //give dirty canteen
                int itemId = source.data.ItemNameCache.getIDFromCache("Dirty Canteen");
                if (itemId != -1)
                {
                    Game1.player.addItemToInventory(new SObject(itemId, 1));
                }
            }
            else if (isWater)
            {
                instance.onEnvDrinkingUpdate(isOcean, isWater);
            }
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.eventUp)
                return;

            if (Game1.activeClickableMenu != null)
            {
                //Game1.tooltip
            }

            SpriteBatch b = e.SpriteBatch;

            //currently hardcoded
            //TODO: move this to manager

            int OffsetX = ModConfig.GetInstance().UIOffsetX;
            int OffsetY = ModConfig.GetInstance().UIOffsetY;
            float Scale = ModConfig.GetInstance().UIScale;

            Vector2 hunger_pos = new Vector2(OffsetX, OffsetY);
            b.Draw(this.HungerBar, hunger_pos, new Rectangle(0, 0, this.HungerBar.Width, this.HungerBar.Height), Color.White, 0, new Vector2(), Scale, SpriteEffects.None, 1);

            Vector2 thirst_pos = new Vector2(OffsetX, OffsetY + this.HungerBar.Height * Scale * 1);
            b.Draw(this.ThirstBar, thirst_pos, new Rectangle(0, 0, this.ThirstBar.Width, this.ThirstBar.Height), Color.White, 0, new Vector2(), Scale, SpriteEffects.None, 1);

            Vector2 env_temp_pos = new Vector2(OffsetX, OffsetY + this.HungerBar.Height * Scale * 2);
            b.Draw(this.EnvTempBar, env_temp_pos, new Rectangle(0, 0, this.EnvTempBar.Width, this.EnvTempBar.Height), Color.White, 0, new Vector2(), Scale, SpriteEffects.None, 1);

            Vector2 body_temp_pos = new Vector2(OffsetX, OffsetY + this.HungerBar.Height * Scale * 3);
            b.Draw(this.BodyTempBar, body_temp_pos, new Rectangle(0, 0, this.BodyTempBar.Width, this.BodyTempBar.Height), Color.White, 0, new Vector2(), Scale, SpriteEffects.None, 1);

            //render indicators
            double ENV_TEMP_BOUND_LOW = ModConfig.GetInstance().EnvironmentTemperatureDisplayLowerBound;
            double ENV_TEMP_BOUND_HIGH = ModConfig.GetInstance().EnvironmentTemperatureDisplayHigherBound;

            double x_coord_env_temp = ((instance.getEnvTemp() - ENV_TEMP_BOUND_LOW) / (ENV_TEMP_BOUND_HIGH - ENV_TEMP_BOUND_LOW)) * (50 * Scale);
            Vector2 env_ind_pos = new Vector2(OffsetX + (float) x_coord_env_temp, OffsetY + this.HungerBar.Height * Scale * 2);
            b.Draw(this.TempIndicator, env_ind_pos, new Rectangle(0, 0, this.TempIndicator.Width, this.TempIndicator.Height), Color.White, 0, new Vector2(), Scale, SpriteEffects.None, 1);

            double BODY_TEMP_BOUND_LOW = ModConfig.GetInstance().BodyTemperatureDisplayLowerBound;
            double BODY_TEMP_BOUND_HIGH = ModConfig.GetInstance().BodyTemperatureDisplayHigherBound;

            double x_coord_body_temp = ((instance.getPlayerBodyTemp() - BODY_TEMP_BOUND_LOW) / (BODY_TEMP_BOUND_HIGH - BODY_TEMP_BOUND_LOW)) * (50 * Scale);
            Vector2 body_ind_pos = new Vector2(OffsetX + (float) x_coord_body_temp, OffsetY + this.HungerBar.Height * Scale * 3);
            b.Draw(this.TempIndicator, body_ind_pos, new Rectangle(0, 0, this.TempIndicator.Width, this.TempIndicator.Height), Color.White, 0, new Vector2(), Scale, SpriteEffects.None, 1);

            //draw hunger and thirst indicator
            if (instance.getPlayerHungerPercentage() > 0)
            {
                float perc = (float)instance.getPlayerHungerPercentage();
                b.Draw(this.fillRect, hunger_pos + new Vector2(4 * Scale, 5 * Scale), new Rectangle(0, 0, (int)(perc * 50 * Scale), (int)(6 * Scale)), source.utils.ColorHelper.ColorFromHSV(perc * 100f, 1, 1));
            }
            if (instance.getPlayerThirstPercentage() > 0)
            {
                float perc = (float)instance.getPlayerThirstPercentage();
                b.Draw(this.fillRect, thirst_pos + new Vector2(4 * Scale, 5 * Scale), new Rectangle(0, 0, (int)(perc * 50 * Scale), (int)(6 * Scale)), source.utils.ColorHelper.ColorFromHSV(perc * 100f, 1, 1));
            }
            
            Rectangle hunger_hover_area = new Rectangle((int) hunger_pos.X, (int) hunger_pos.Y, (int) (this.HungerBar.Width * Scale), (int) (this.HungerBar.Height * Scale));
            Rectangle thirst_hover_area = new Rectangle((int) thirst_pos.X, (int) thirst_pos.Y, (int) (this.ThirstBar.Width * Scale), (int) (this.ThirstBar.Height * Scale));
            Rectangle env_temp_hover_area = new Rectangle((int) env_temp_pos.X, (int) env_temp_pos.Y, (int) (this.EnvTempBar.Width * Scale), (int) (this.EnvTempBar.Height * Scale));
            Rectangle body_temp_hover_area = new Rectangle((int) body_temp_pos.X, (int) body_temp_pos.Y, (int) (this.BodyTempBar.Width * Scale), (int) (this.BodyTempBar.Height * Scale));

            //show number stat on bar hover

            if (Game1.getOldMouseX() >= (double)hunger_hover_area.X && Game1.getOldMouseY() >= (double)hunger_hover_area.Y && Game1.getOldMouseX() < (double)hunger_hover_area.X + hunger_hover_area.Width && Game1.getOldMouseY() < hunger_hover_area.Y + hunger_hover_area.Height)
                Game1.drawWithBorder(instance.getPlayerHungerStat(), Color.Black * 0.0f, Color.White, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY() - 32));

            if (Game1.getOldMouseX() >= (double)thirst_hover_area.X && Game1.getOldMouseY() >= (double)thirst_hover_area.Y && Game1.getOldMouseX() < (double)thirst_hover_area.X + thirst_hover_area.Width && Game1.getOldMouseY() < thirst_hover_area.Y + thirst_hover_area.Height)
                Game1.drawWithBorder(instance.getPlayerThirstStat(), Color.Black * 0.0f, Color.White, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY() - 32));

            if (Game1.getOldMouseX() >= (double)body_temp_hover_area.X && Game1.getOldMouseY() >= (double)body_temp_hover_area.Y && Game1.getOldMouseX() < (double)body_temp_hover_area.X + body_temp_hover_area.Width && Game1.getOldMouseY() < body_temp_hover_area.Y + body_temp_hover_area.Height)
                Game1.drawWithBorder(instance.getPlayerBodyTempString(), Color.Black * 0.0f, Color.White, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY() - 32));

            if (Game1.getOldMouseX() >= (double)env_temp_hover_area.X && Game1.getOldMouseY() >= (double)env_temp_hover_area.Y && Game1.getOldMouseX() < (double)env_temp_hover_area.X + env_temp_hover_area.Width && Game1.getOldMouseY() < env_temp_hover_area.Y + env_temp_hover_area.Height)
                Game1.drawWithBorder(instance.getEnvTempString(), Color.Black * 0.0f, Color.White, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY() - 32));
        }

        private void OnSecondPassed(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            else
            {
                instance.onSecondUpdate();
            }
        }

        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            else
            {
                //foreach (GameLocation l in Game1.locations)
                //{
                //    this.Monitor.Log($"name={l.name}, isOutdoor={l.isOutdoors}");
                //}
                //int mine_level = Game1.CurrentMineLevel; 
                
                instance.onEnvUpdate(e.NewTime, Game1.currentSeason, Game1.weatherIcon, Game1.currentLocation, Game1.CurrentMineLevel);
                instance.onClockUpdate();
            }
        }

        private void OnItemEaten(object sender, EventArgs e)
        {
            if (sender != Game1.player)
                return;

            SObject ateItem = Game1.player.itemToEat as SObject;
            this.Monitor.Log($"{Game1.player.name} ate {ateItem.name}");
            instance.onEatingFood(ateItem);

            //for whatever reason the field determine whether a player can drink the "edible" is never exposed in the SObject field
            //the result is this abhorent
            double addThirst = source.data.CustomHydrationDictionary.getHydrationValue(ateItem.name);
            double coolingModifier = source.data.CustomHydrationDictionary.getCoolingModifierValue(ateItem.name);

            var arrInfo = Game1.objectInformation[ateItem.parentSheetIndex].Split('/');
            if (addThirst != 0)
            {
                instance.onItemDrinkingUpdate(ateItem, addThirst, coolingModifier);
            }
            else if (arrInfo.Length > 6)
            {
                if (arrInfo[6].Equals("drink"))
                {
                    instance.onItemDrinkingUpdate(ateItem, ModConfig.GetInstance().DefaultHydrationGainOnDrinkableItems, coolingModifier);
                }
            }
        }

        private void OnItemUsed(object sender, EventArgs e)
        {
            if (sender != Game1.player)
                return;

            //this.Monitor.Log(Game1.player.CurrentTool.GetType().Name);
            instance.updateOnToolUsed(Game1.player.CurrentTool);
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            instance.dayStartProcedure();
        }

        private void OnLoadedSave(object sender, SaveLoadedEventArgs e)
        {
            instance.init(Game1.player);
            instance.loadData(this);
            //cache these item's id
            source.data.ItemNameCache.cacheItem("Canteen");
            source.data.ItemNameCache.cacheItem("Full Canteen");
            source.data.ItemNameCache.cacheItem("Dirty Canteen");
        }

        private void OnGameSaved(object sender, SavedEventArgs e)
        {
            instance.saveData(this);
        }

        private void OnExitToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            instance.onExit();
            source.data.ItemNameCache.clearCache();
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            source.api.ConfigMenu.Init(this);
        }
    }
}