using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace StardewSurvivalProject.source.ui
{
    /// <summary>
    /// Handles rendering of custom HUD elements (hunger, thirst, temperature, mood bars)
    /// </summary>
    public class HudRenderer
    {
        private readonly AssetLoader assetLoader;
        private readonly core.GameStateManager gameState;

        public HudRenderer(AssetLoader assetLoader, core.GameStateManager gameState)
        {
            this.assetLoader = assetLoader;
            this.gameState = gameState;
        }

        /// <summary>
        /// Render all HUD elements
        /// </summary>
        public void RenderHud(SpriteBatch spriteBatch)
        {
            if (!Context.IsWorldReady || Game1.eventUp)
                return;

            // Get config values
            int offsetX = ModConfig.GetInstance().UIOffsetX;
            int offsetY = ModConfig.GetInstance().UIOffsetY;
            float scale = ModConfig.GetInstance().UIScale;
            bool overlayComfyTemp = ModConfig.GetInstance().IndicateComfortableTemperatureRange;

            // Render bars
            RenderBars(spriteBatch, offsetX, offsetY, scale);

            // Render indicators
            RenderIndicators(spriteBatch, offsetX, offsetY, scale, overlayComfyTemp);

            // Render fill bars
            RenderFillBars(spriteBatch, offsetX, offsetY, scale);

            // Render tooltips
            RenderTooltips(spriteBatch, offsetX, offsetY, scale);
        }

        /// <summary>
        /// Render the UI bars (backgrounds)
        /// </summary>
        private void RenderBars(SpriteBatch b, int offsetX, int offsetY, float scale)
        {
            Vector2 hungerPos = new Vector2(offsetX, offsetY);
            b.Draw(assetLoader.HungerBar, hungerPos, new Rectangle(0, 0, assetLoader.HungerBar.Width, assetLoader.HungerBar.Height), 
                Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 1);

            Vector2 thirstPos = new Vector2(offsetX, offsetY + assetLoader.HungerBar.Height * scale * 1);
            b.Draw(assetLoader.ThirstBar, thirstPos, new Rectangle(0, 0, assetLoader.ThirstBar.Width, assetLoader.ThirstBar.Height), 
                Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 1);

            Vector2 envTempPos = new Vector2(offsetX, offsetY + assetLoader.HungerBar.Height * scale * 2);
            b.Draw(assetLoader.EnvTempBar, envTempPos, new Rectangle(0, 0, assetLoader.EnvTempBar.Width, assetLoader.EnvTempBar.Height), 
                Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 1);

            Vector2 bodyTempPos = new Vector2(offsetX, offsetY + assetLoader.HungerBar.Height * scale * 3);
            b.Draw(assetLoader.BodyTempBar, bodyTempPos, new Rectangle(0, 0, assetLoader.BodyTempBar.Width, assetLoader.BodyTempBar.Height), 
                Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 1);

            // Render mood icon if sanity module is enabled
            if (ModConfig.GetInstance().UseSanityModule)
            {
                Vector2 moodPos = new Vector2(offsetX, offsetY + assetLoader.HungerBar.Height * scale * 4);
                int moodIndex = gameState.getPlayerMoodIndex();
                b.Draw(assetLoader.MoodIcons[moodIndex], moodPos, 
                    new Rectangle(0, 0, assetLoader.MoodIcons[moodIndex].Width, assetLoader.MoodIcons[moodIndex].Height), 
                    Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 1);
            }
        }

        /// <summary>
        /// Render temperature indicators
        /// </summary>
        private void RenderIndicators(SpriteBatch b, int offsetX, int offsetY, float scale, bool overlayComfyTemp)
        {
            double envTempBoundLow = ModConfig.GetInstance().EnvironmentTemperatureDisplayLowerBound;
            double envTempBoundHigh = ModConfig.GetInstance().EnvironmentTemperatureDisplayHigherBound;

            // Environment temperature indicator
            double envTemp = gameState.getEnvTemp();
            double xCoordEnvTemp = ((envTemp - envTempBoundLow) / (envTempBoundHigh - envTempBoundLow)) * (50 * scale);
            Vector2 envIndPos = new Vector2(offsetX + (float)xCoordEnvTemp, offsetY + assetLoader.HungerBar.Height * scale * 2);

            // Comfort zone overlay
            if (overlayComfyTemp)
            {
                double minComfyTemp = gameState.getMinComfyEnvTemp();
                double maxComfyTemp = gameState.getMaxComfyEnvTemp();

                double xCoordMinComfTemp = ((minComfyTemp - envTempBoundLow) / (envTempBoundHigh - envTempBoundLow)) * (50 * scale);
                double xCoordMaxComfTemp = ((maxComfyTemp - envTempBoundLow) / (envTempBoundHigh - envTempBoundLow)) * (50 * scale);

                Vector2 envTempPos = new Vector2(offsetX, offsetY + assetLoader.HungerBar.Height * scale * 2);
                Vector2 comfortOverlayPos = envTempPos + new Vector2((float)xCoordMinComfTemp, 5 * scale);
                int comfortWidth = (int)Math.Max(xCoordMaxComfTemp - xCoordMinComfTemp, 0);

                b.Draw(assetLoader.FillRect, comfortOverlayPos, 
                    new Rectangle(0, 0, comfortWidth, (int)(6 * scale)), 
                    new Color(Color.Green, 0.3f));
            }

            b.Draw(assetLoader.TempIndicator, envIndPos, 
                new Rectangle(0, 0, assetLoader.TempIndicator.Width, assetLoader.TempIndicator.Height), 
                Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 1);

            // Body temperature indicator
            double bodyTempBoundLow = ModConfig.GetInstance().BodyTemperatureDisplayLowerBound;
            double bodyTempBoundHigh = ModConfig.GetInstance().BodyTemperatureDisplayHigherBound;
            double bodyTemp = gameState.getPlayerBodyTemp();

            double xCoordBodyTemp = ((bodyTemp - bodyTempBoundLow) / (bodyTempBoundHigh - bodyTempBoundLow)) * (50 * scale);
            Vector2 bodyIndPos = new Vector2(offsetX + (float)xCoordBodyTemp, offsetY + assetLoader.HungerBar.Height * scale * 3);

            b.Draw(assetLoader.TempIndicator, bodyIndPos, 
                new Rectangle(0, 0, assetLoader.TempIndicator.Width, assetLoader.TempIndicator.Height), 
                Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 1);
        }

        /// <summary>
        /// Render filled portions of hunger and thirst bars
        /// </summary>
        private void RenderFillBars(SpriteBatch b, int offsetX, int offsetY, float scale)
        {
            Vector2 hungerPos = new Vector2(offsetX, offsetY);
            Vector2 thirstPos = new Vector2(offsetX, offsetY + assetLoader.HungerBar.Height * scale * 1);

            // Hunger bar fill
            if (gameState.getPlayerHungerPercentage() > 0)
            {
                float perc = (float)gameState.getPlayerHungerPercentage();
                b.Draw(assetLoader.FillRect, hungerPos + new Vector2(4 * scale, 5 * scale), 
                    new Rectangle(0, 0, (int)(perc * 50 * scale), (int)(6 * scale)), 
                    utils.ColorHelper.ColorFromHSV(perc * 100f, 1, 1));
            }

            // Thirst bar fill
            if (gameState.getPlayerThirstPercentage() > 0)
            {
                float perc = (float)gameState.getPlayerThirstPercentage();
                b.Draw(assetLoader.FillRect, thirstPos + new Vector2(4 * scale, 5 * scale), 
                    new Rectangle(0, 0, (int)(perc * 50 * scale), (int)(6 * scale)), 
                    utils.ColorHelper.ColorFromHSV(perc * 100f, 1, 1));
            }

            // Saturation overlay on hunger bar
            if (gameState.getPlayerHungerPercentage() > 0)
            {
                float perc = (float)gameState.getPlayerHungerSaturationStat();
                b.Draw(assetLoader.FillRect, hungerPos + new Vector2(4 * scale, 9 * scale), 
                    new Rectangle(0, 0, (int)(perc * 50 * scale), (int)(2 * scale)), 
                    new Color(Color.Yellow, 0.3f));
            }
        }

        /// <summary>
        /// Render tooltips on hover
        /// </summary>
        private void RenderTooltips(SpriteBatch b, int offsetX, int offsetY, float scale)
        {
            int mouseX = Game1.getOldMouseX();
            int mouseY = Game1.getOldMouseY();

            Vector2 hungerPos = new Vector2(offsetX, offsetY);
            Vector2 thirstPos = new Vector2(offsetX, offsetY + assetLoader.HungerBar.Height * scale * 1);
            Vector2 envTempPos = new Vector2(offsetX, offsetY + assetLoader.HungerBar.Height * scale * 2);
            Vector2 bodyTempPos = new Vector2(offsetX, offsetY + assetLoader.HungerBar.Height * scale * 3);

            Rectangle hungerHover = new Rectangle((int)hungerPos.X, (int)hungerPos.Y, 
                (int)(assetLoader.HungerBar.Width * scale), (int)(assetLoader.HungerBar.Height * scale));
            Rectangle thirstHover = new Rectangle((int)thirstPos.X, (int)thirstPos.Y, 
                (int)(assetLoader.ThirstBar.Width * scale), (int)(assetLoader.ThirstBar.Height * scale));
            Rectangle envTempHover = new Rectangle((int)envTempPos.X, (int)envTempPos.Y, 
                (int)(assetLoader.EnvTempBar.Width * scale), (int)(assetLoader.EnvTempBar.Height * scale));
            Rectangle bodyTempHover = new Rectangle((int)bodyTempPos.X, (int)bodyTempPos.Y, 
                (int)(assetLoader.BodyTempBar.Width * scale), (int)(assetLoader.BodyTempBar.Height * scale));

            if (hungerHover.Contains(mouseX, mouseY))
                Game1.drawWithBorder(gameState.getPlayerHungerStat(), Color.Black * 0.0f, Color.White, new Vector2(mouseX, mouseY - 32));

            if (thirstHover.Contains(mouseX, mouseY))
                Game1.drawWithBorder(gameState.getPlayerThirstStat(), Color.Black * 0.0f, Color.White, new Vector2(mouseX, mouseY - 32));

            if (bodyTempHover.Contains(mouseX, mouseY))
                Game1.drawWithBorder(gameState.getPlayerBodyTempString(), Color.Black * 0.0f, Color.White, new Vector2(mouseX, mouseY - 32));

            if (envTempHover.Contains(mouseX, mouseY))
                Game1.drawWithBorder(gameState.getEnvTempString(), Color.Black * 0.0f, Color.White, new Vector2(mouseX, mouseY - 32));

            // Mood tooltip
            if (ModConfig.GetInstance().UseSanityModule)
            {
                Vector2 moodPos = new Vector2(offsetX, offsetY + assetLoader.HungerBar.Height * scale * 4);
                Rectangle moodHover = new Rectangle((int)moodPos.X, (int)moodPos.Y, 
                    (int)(assetLoader.MoodIcons[0].Width * scale), (int)(assetLoader.MoodIcons[0].Height * scale));

                if (moodHover.Contains(mouseX, mouseY))
                {
                    DrawMoodTooltip(b, mouseX, mouseY);
                }
            }
        }

        /// <summary>
        /// Draw detailed mood tooltip with all modifiers
        /// </summary>
        private void DrawMoodTooltip(SpriteBatch b, int x, int y)
        {
            var player = gameState.GetPlayerModel();
            if (player?.mood == null) return;

            // Prepare tooltip content
            var lines = new List<string>();
            var mood = player.mood;
            
            // Title line
            lines.Add($"Mood: {mood.TotalMood:F1} ({mood.Level})");
            lines.Add($"Base Mood: {mood.Value:F1}");
            
            if (mood.MoodElements.Count > 0)
            {
                lines.Add(""); // Empty line separator
                lines.Add("Active Modifiers:");
                
                // Sort by value (most positive first)
                var sortedElements = mood.MoodElements.OrderByDescending(e => e.Value).ToList();
                
                foreach (var element in sortedElements)
                {
                    string sign = element.Value >= 0 ? "+" : "";
                    string valueLine = $"  {sign}{element.Value:F1} {element.Name}";
                    lines.Add(valueLine);
                    
                    // Add description on next line if it differs from name
                    if (!string.IsNullOrEmpty(element.Description) && element.Description != element.Name)
                    {
                        lines.Add($"    {element.Description}");
                    }
                }
            }
            else
            {
                lines.Add("");
                lines.Add("No active modifiers");
            }

            // Calculate tooltip dimensions
            SpriteFont font = Game1.smallFont;
            int maxWidth = 0;
            int totalHeight = 0;
            var lineHeights = new List<int>();

            foreach (var line in lines)
            {
                Vector2 size = font.MeasureString(line);
                maxWidth = Math.Max(maxWidth, (int)size.X);
                int lineHeight = (int)size.Y;
                lineHeights.Add(lineHeight);
                totalHeight += lineHeight;
            }

            // Add padding
            int padding = 16;
            int tooltipWidth = maxWidth + padding * 2;
            int tooltipHeight = totalHeight + padding * 2;

            // Position tooltip (avoid going off screen)
            int tooltipX = x + 16;
            int tooltipY = y - tooltipHeight - 16;

            // Adjust if going off right edge
            if (tooltipX + tooltipWidth > Game1.viewport.Width)
                tooltipX = x - tooltipWidth - 16;

            // Adjust if going off top edge
            if (tooltipY < 0)
                tooltipY = y + 16;

            // Draw tooltip background (using game's standard tooltip style)
            StardewValley.Menus.IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                tooltipX,
                tooltipY,
                tooltipWidth,
                tooltipHeight,
                Color.White,
                1f,
                false
            );

            // Draw text lines
            int currentY = tooltipY + padding;
            for (int i = 0; i < lines.Count; i++)
            {
                Color textColor = Color.Black;
                
                // Color code based on content
                if (lines[i].Contains("+") && lines[i].Contains("  +"))
                    textColor = new Color(0, 150, 0); // Green for positive
                else if (lines[i].Contains("-") && lines[i].Contains("  -"))
                    textColor = new Color(150, 0, 0); // Red for negative
                else if (i == 0) // Title
                    textColor = new Color(86, 22, 12); // Dark brown title
                
                b.DrawString(font, lines[i], new Vector2(tooltipX + padding, currentY), textColor);
                currentY += lineHeights[i];
            }
        }
    }
}
