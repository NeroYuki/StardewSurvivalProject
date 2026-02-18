using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace StardewSurvivalProject.source.ui
{
    /// <summary>
    /// Handles loading of mod assets with preset support
    /// Supports auto-detection of UI retexture mods and fallback to default
    /// </summary>
    public class AssetLoader
    {
        private readonly IModHelper helper;
        private readonly IMonitor monitor;
        private readonly string preset;

        // Loaded textures
        public Texture2D HungerBar { get; private set; }
        public Texture2D ThirstBar { get; private set; }
        public Texture2D EnvTempBar { get; private set; }
        public Texture2D BodyTempBar { get; private set; }
        public Texture2D TempIndicator { get; private set; }
        public Texture2D FillRect { get; private set; }
        public Texture2D TempRangeIndicator { get; private set; }
        public List<Texture2D> MoodIcons { get; private set; }
        public Texture2D InfoIcon { get; private set; }
        public Texture2D ModIcon { get; private set; }

        public AssetLoader(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;
            this.preset = DeterminePreset();
        }

        /// <summary>
        /// Determine which asset preset to use based on config and installed retexture mods
        /// </summary>
        private string DeterminePreset()
        {
            string configPreset = ModConfig.GetInstance().RetexturePreset;
            
            if (!configPreset.Equals("auto"))
                return configPreset;

            // Auto-detect retexture mods
            if (helper.ModRegistry.Get("ManaKirel.VintageInterface2") != null)
                return "vintage2";
            else if (helper.ModRegistry.Get("Maraluna.OvergrownFloweryInterface") != null)
                return "overgrown";
            else if (helper.ModRegistry.Get("DaisyNiko.EarthyRecolour") != null)
                return "earthy";
            else
                return "default";
        }

        /// <summary>
        /// Load a texture asset with preset fallback
        /// </summary>
        private Texture2D LoadAssetWithPreset(string assetFileName)
        {
            Texture2D tex = helper.ModContent.Load<Texture2D>(String.Format("assets/{0}", assetFileName));
            try
            {
                if (!preset.Equals("default"))
                {
                    tex = helper.ModContent.Load<Texture2D>(String.Format("assets/{0}/{1}", preset, assetFileName));
                }
            }
            catch (Exception)
            {
                monitor.Log(String.Format("Failed to load texture {0} from preset {1}, fallback to default", assetFileName, preset), LogLevel.Warn);
            }
            return tex;
        }

        /// <summary>
        /// Load all mod assets
        /// Should be called during mod initialization
        /// </summary>
        public void LoadAllAssets()
        {
            monitor.Log($"Loading assets with preset: {preset}", LogLevel.Debug);

            // Load UI bars
            HungerBar = LoadAssetWithPreset("HungerBar.png");
            ThirstBar = LoadAssetWithPreset("ThirstBar.png");
            EnvTempBar = LoadAssetWithPreset("EnvTempBar.png");
            BodyTempBar = LoadAssetWithPreset("BodyTempBar.png");
            TempIndicator = LoadAssetWithPreset("TempIndicator.png");
            FillRect = LoadAssetWithPreset("fillRect.png");
            TempRangeIndicator = LoadAssetWithPreset("TempRangeIndicator.png");

            // Load mood icons
            MoodIcons = new List<Texture2D>
            {
                LoadAssetWithPreset("MoodMentalBreak.png"),
                LoadAssetWithPreset("MoodDistress.png"),
                LoadAssetWithPreset("MoodSad.png"),
                LoadAssetWithPreset("MoodDiscontent.png"),
                LoadAssetWithPreset("MoodNeutral.png"),
                LoadAssetWithPreset("MoodContent.png"),
                LoadAssetWithPreset("MoodHappy.png"),
                LoadAssetWithPreset("MoodOverjoy.png"),
            };

            // Load icons
            InfoIcon = LoadAssetWithPreset("InfoIcon.png");
            ModIcon = LoadAssetWithPreset("ModIcon.png");

            monitor.Log("All assets loaded successfully", LogLevel.Debug);
        }

        /// <summary>
        /// Load buff effect icons for the buff icon sheet
        /// Called during asset patching
        /// </summary>
        public Dictionary<string, Texture2D> LoadEffectIcons()
        {
            return new Dictionary<string, Texture2D>
            {
                { "Burn", LoadAssetWithPreset("BurnEffect.png") },
                { "Starvation", LoadAssetWithPreset("StarvationEffect.png") },
                { "Hypothermia", LoadAssetWithPreset("HypothermiaEffect.png") },
                { "Frostbite", LoadAssetWithPreset("FrostbiteEffect.png") },
                { "Heatstroke", LoadAssetWithPreset("HeatstrokeEffect.png") },
                { "Dehydration", LoadAssetWithPreset("DehydratedEffect.png") },
                { "Fever", LoadAssetWithPreset("FeverEffect.png") },
                { "Stomachache", LoadAssetWithPreset("StomachaceEffect.png") },
                { "Thirst", LoadAssetWithPreset("ThirstEffect.png") },
                { "Hunger", LoadAssetWithPreset("HungerEffect.png") },
                { "WellFed", LoadAssetWithPreset("WellFedEffect.png") },
                { "Refreshing", LoadAssetWithPreset("RefreshingEffect.png") },
                { "Sprinting", LoadAssetWithPreset("SprintingEffect.png") }
            };
        }

        /// <summary>
        /// Get the current preset being used
        /// </summary>
        public string GetCurrentPreset()
        {
            return preset;
        }
    }
}
