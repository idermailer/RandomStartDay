using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection.Emit;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Minigames;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using xTile;
using xTile.Dimensions;
using xTile.Tiles;

namespace RandomStartDay
{
    
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private IModHelper helper;
        private ModConfig config;

        private int dayOfMonth;
        private String currentSeason = "spring";

        private bool introEnd = true; // for asset replacing
        private IAssetName originalSpringTileName;

        public override void Entry(IModHelper helper)
        {
            this.config = this.Helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += this.GameLoop_GameLaunched;
            helper.Events.Specialized.LoadStageChanged += this.Specialized_LoadStageChanged;
            helper.Events.Content.AssetRequested += this.Content_AssetRequested;
            helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
        }

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            introEnd = true;
            verification();
            // if unique id is used, other random options are disabled
            if (config.isRandomSeedUsed)
            {
                config.allowedSeasons = new String[] { "spring", "summer", "fall", "winter" };
                config.avoidFestivalDay = false;
            }
        }

        private void Specialized_LoadStageChanged(object sender, LoadStageChangedEventArgs e)
        {
            Monitor.Log("e.NewStage = " + e.NewStage.ToString(), LogLevel.Debug);

            if (e.NewStage == LoadStage.CreatedBasicInfo)
            {
                Monitor.Log(config.allowedSeasons.ToString() + " / " + config.avoidFestivalDay, LogLevel.Debug);
                // make introEnd to false because asset is loaded before createdInitialLocations
                introEnd = false;
                // for prevent tilesheet to be fixed to spring
                if (config.isRandomSeedUsed)
                {
                    Random random = new((int)Game1.uniqueIDForThisGame);
                    randomize(random);
                }
                else
                {
                    Random random = new();
                    randomize(random);
                }
            }

            // Main method
            if (e.NewStage == LoadStage.CreatedInitialLocations) // new game
            {
                apply();
                problemFix();
            }
        }

        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            // GameLoop.SaveCreated not worked so I use DayStarted I don't know why
            if (introEnd == false)
            {
                Helper.GameContent.InvalidateCache(originalSpringTileName);
                introEnd = true;
            }
        }

        private void Content_AssetRequested(object sender, AssetRequestedEventArgs e)
        {

            if (!config.useSeasonalTilesetInBusScene)
                return;

            /*  ★ minigames/currentseason_intro 이미지 만들 것
            if (e.NameWithoutLocale.IsEquivalentTo("Minigames/Intro"))
            {
                e.LoadFromModFile<Texture2D>("assets/" + currentSeason + "Intro.png", AssetLoadPriority.Low);
            }
            */
            // outdoortiles, fixed on spring to seasonal, when introEnd is false
            if (e.NameWithoutLocale.IsEquivalentTo("Maps/spring_outdoorsTileSheet"))
            {
                if (introEnd == false)
                // load asset from game folder
                {
                    originalSpringTileName = e.Name;
                    e.LoadFromModFile<Texture2D>("../../Content/Maps/" + currentSeason + "_outdoorsTileSheet.xnb", AssetLoadPriority.Low);
                }
            }
        }

        private void verification()
            {
            // if allowed seasons have invalid value (other than spring, summer, fall, winter)
                for (int i = 0; i < config.allowedSeasons.Length; i++)
                {
                    switch (config.allowedSeasons[i])
                    {
                        case "spring":
                            break;
                        case "summer":
                            break;
                        case "fall":
                            break;
                        case "winter":
                            break;
                        default:
                        {
                            this.Monitor.Log("array \"allowedSeasons\" contains invalid value(s). Valid values are: \"spring\", \"summer\", \"fall\", \"winter\". This mod did NOT work.", LogLevel.Error);
                            introEnd = true;
                            return;
                        
                        }
                        
                    }
                }
        }

        private void randomize (Random random)
        {
            do
            {
                dayOfMonth = random.Next(28) + 1;
                currentSeason = config.allowedSeasons[random.Next(config.allowedSeasons.Length)];
                // if next day is festival day, randomize one more time
                if (!Utility.isFestivalDay(dayOfMonth + 1, currentSeason))
                    break;
                random = new Random();
            } while (true);
        }

        private void apply()
        {
            Game1.dayOfMonth = dayOfMonth;
            Game1.currentSeason = currentSeason;

            // refresh all locations
            foreach (GameLocation location in (IEnumerable<GameLocation>)Game1.locations)
                {
                    // this is initial objects, so call seasonal method
                    location.seasonUpdate(currentSeason);
                }
            // make sure outside not dark, for Dynamic Night Time
            Game1.timeOfDay = 1200;
        }

        private void problemFix()
        {
            // FIX: if player arrived after year1 spring 1st, assume that willy's first mail has been received.
            // so player can see event getting fishig rod, and enter willy's shop
            if (!(currentSeason == "spring" && dayOfMonth == 1))
            {
                Game1.MasterPlayer.mailReceived.Add("spring_2_1");
            }
        }

        private void test________()
        {
            Monitor.Log("Test Method called!!!!!!", LogLevel.Warn);
            //method for test
        }
    }
}
