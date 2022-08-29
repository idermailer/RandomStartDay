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
using xTile.Tiles;

namespace RandomStartDay
{
    
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private ModConfig config;

        private int dayOfMonth;
        private String currentSeason = "spring";

        private bool introEnd = true; // for asset replacing

        public override void Entry(IModHelper helper)
        {
            this.config = this.Helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += this.GameLoop_GameLaunched;
            helper.Events.Specialized.LoadStageChanged += this.Specialized_LoadStageChanged;
            helper.Events.Content.AssetRequested += this.Content_AssetRequested;

        }

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            introEnd = true;
        }

        private void Specialized_LoadStageChanged(object sender, LoadStageChangedEventArgs e)
        {
            if (e.NewStage == LoadStage.CreatedBasicInfo)
            {
                verification();
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

            // Once the save file has been created, make the asset is no longer replaced.
            if (e.NewStage == LoadStage.CreatedSaveFile)
            {
                introEnd = true;
                // spring_outdoorsTileSheet invalidation
                Helper.GameContent.InvalidateCache("Maps/spring_outdoorsTileSheet");
            }

        }

        private void Content_AssetRequested(object sender, AssetRequestedEventArgs e)
        {
            // excute only introEnd is false
            if (introEnd != false)
                return;

            if (!config.useSeasonalTilesetInBusScene)
                return;

            /*  ★ minigames/currentseason_intro 이미지 만들 것
            if (e.NameWithoutLocale.IsEquivalentTo("Minigames/Intro"))
            {
                e.LoadFromModFile<Texture2D>("assets/" + currentSeason + "Intro.png", AssetLoadPriority.Low);
            }
            */
            // outdoortiles, fixed on spring to seasonal
            if (e.NameWithoutLocale.IsEquivalentTo("Maps/spring_outdoorsTileSheet"))
            {
                // load asset from game folder
                e.LoadFromModFile<Texture2D>("../../Content/Maps/" + currentSeason + "_outdoorsTileSheet.xnb", AssetLoadPriority.Low);
            }
        }

        //
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
            dayOfMonth = random.Next(config.MaxOfDayOfMonth) + 1;
            currentSeason = config.allowedSeasons[random.Next(config.allowedSeasons.Length)];
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
                int dom = random.Next(config.MaxOfDayOfMonth) + 1;
                string cs = config.allowedSeasons[random.Next(config.allowedSeasons.Length)];

                // apply
                Game1.dayOfMonth = dom;
                Game1.currentSeason = cs;
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
            //method for test
        }
    }
}
