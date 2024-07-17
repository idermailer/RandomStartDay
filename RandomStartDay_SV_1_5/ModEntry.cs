using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Minigames;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RandomStartDay
{

    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        internal static IMonitor monitor;
        internal static ModConfig config;
        internal static int dayOfMonth;
        internal static string currentSeason = "spring";
        internal static string[] allowedSeasons = Array.Empty<string>();

        internal static bool winter28 = false;
        internal static string seedSeason = "spring";

        public override void Entry(IModHelper helper)
        {
            monitor = this.Monitor;
            config = this.Helper.ReadConfig<ModConfig>();
            var harmony = new Harmony(this.ModManifest.UniqueID);

            helper.Events.GameLoop.GameLaunched += this.GameLoop_GameLaunched;

            // run when disableAll is false
            if (Verification())
            {
                helper.Events.Specialized.LoadStageChanged += this.Specialized_LoadStageChanged;
                helper.Events.Content.AssetRequested += this.Content_AssetRequested;
                helper.Events.GameLoop.DayEnding += this.GameLoop_DayEnding;

                HarmonyMethodPatches.Initialize(helper, Monitor);


                harmony.Patch(
                    original: AccessTools.Method(typeof(Farmer), nameof(Farmer.addQuest)),
                    postfix: new HarmonyMethod(typeof(HarmonyMethodPatches), nameof(HarmonyMethodPatches.Harmony_Quest6ToWheatQuest))
                    );
                harmony.Patch(
                    original: AccessTools.Method(typeof(Intro), nameof(Intro.createBeginningOfLevel)),
                    prefix: new HarmonyMethod(typeof(HarmonyMethodPatches), nameof(HarmonyMethodPatches.Harmony_ChangeIntroSeason))
                    );
            }

            helper.Events.GameLoop.DayStarted += this.GameLoop_DayStarted;
            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Objects.TV), "getTodaysTip"),
                postfix: new HarmonyMethod(typeof(HarmonyMethodPatches), nameof(HarmonyMethodPatches.ChangeTodaysTip))
                );
        }

        // EVENTS
        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // disable all
            if (config.DisableAll)
            {
                Monitor.Log("DISABLED", LogLevel.Debug);
                return;
            }

            winter28 = false;
            // if unique id is used, other random options are disabled
            if (config.IsRandomSeedUsed)
            {
                Monitor.Log("ENABLED, using unique ID(9digit number)", LogLevel.Debug);
                config.AllowSpringSummerFallWinter = new bool[4] { true, true, true, true };
                config.AvoidFestivalDay = false;
                config.AlwaysStartAt1st = false;
            }
            else
            {
                Monitor.Log("ENABLED, using default random seed", LogLevel.Debug);
            }
        }

        private void Specialized_LoadStageChanged(object sender, LoadStageChangedEventArgs e)
        {
            if (e.NewStage == LoadStage.CreatedBasicInfo)
            {
                // for prevent tilesheet to be fixed to spring
                if (config.IsRandomSeedUsed)
                {
                    Random random = new((int)Game1.uniqueIDForThisGame);
                    Randomize(random);
                }
                else
                {
                    Random random = new();
                    Randomize(random);
                }

                // check if the date is winter 28th, if the option is used
                if (currentSeason == "winter" && dayOfMonth == 28 && config.UseWinter28toYear1)
                {
                    winter28 = true;
                }
                else
                {
                    winter28 = false;
                }
                SetSeedSeason();
            }

            if (e.NewStage == LoadStage.CreatedInitialLocations)
            {
                Apply();
                if (config.UseWheatSeeds)
                {
                    PutSeasonalSeeds();
                }
            }
        }

        private void GameLoop_DayEnding(object sender, DayEndingEventArgs e)
        {
            // if player moves on winter 28th(=starts on spring 1), return to year 1
            if (winter28)
            {
                --Game1.year;
                winter28 = false;
            }
        }

        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            // problem fix: first day, clear mailbox and add willy's mail to tomorrow's mail
            if (Game1.stats.daysPlayed == 1)
            {
                Game1.mailbox.Clear();
                Game1.addMailForTomorrow("spring_2_1");
            }

            // When an email that should be received last year exists and not received yet
            Dictionary<string, string> mailData = Game1.content.Load<Dictionary<string, string>>("Data/mail");
            if (mailData.ContainsKey(currentSeason + "_" + Game1.dayOfMonth.ToString() + "_" + (Game1.year - 1).ToString()))
            {
                if (!Game1.player.hasOrWillReceiveMail(currentSeason + "_" + Game1.dayOfMonth.ToString() + "_" + (Game1.year - 1).ToString()))
                {
                    // add last year mail and remove this year mail
                    Game1.mailbox.Add(currentSeason + "_" + Game1.dayOfMonth.ToString() + "_" + (Game1.year - 1).ToString());
                    Game1.mailbox.Remove(currentSeason + "_" + Game1.dayOfMonth.ToString() + "_" + (Game1.year).ToString());
                }
            }
        }

        private void Content_AssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Minigames/Intro"))
            {
                if (currentSeason != "spring")
                {
                    e.Edit(asset =>
                    {
                        var editor = asset.AsImage();
                        Texture2D sourceImage = this.Helper.ModContent.Load<Texture2D>("assets/intro_" + currentSeason + ".png");

                        editor.PatchImage(sourceImage, targetArea: (new Rectangle(0, 176, 48, 80)), sourceArea: (new Rectangle(0, 0, 48, 80)));
                        editor.PatchImage(sourceImage, targetArea: (new Rectangle(48, 224, 64, 16)), sourceArea: (new Rectangle(48, 48, 64, 16)));
                    });
                }
            }
        }

        // METHODS
        private bool Verification()
        {
            // if AllowSpringSummerFallWinter.Length is not 4
            if (config.AllowSpringSummerFallWinter.Length != 4)
            {
                Monitor.Log("Length of \"AllowSpringSummerFallWinter\" is not 4. Randomize will be disabled.", LogLevel.Error);
                return false;
            }

            if (config.AllowSpringSummerFallWinter[0])
                allowedSeasons = allowedSeasons.AddToArray("spring");
            if (config.AllowSpringSummerFallWinter[1])
                allowedSeasons = allowedSeasons.AddToArray("summer");
            if (config.AllowSpringSummerFallWinter[2])
                allowedSeasons = allowedSeasons.AddToArray("fall");
            if (config.AllowSpringSummerFallWinter[3])
                allowedSeasons = allowedSeasons.AddToArray("winter");

            // if allowed seasons not have true value
            if (allowedSeasons.Length == 0)
            {
                Monitor.Log("No Seasons are allowed. Randomize will be disabled. Set true at least one item in AllowSpringSummerFallWinter.", LogLevel.Error);
                return false;
            }

            return true;
        }

        private static void Randomize(Random random)
        {
            string[] seasonNames = new string[4] { "spring", "summer", "fall", "winter" };
            if (config.IsRandomSeedUsed)
            {
                currentSeason = seasonNames[random.Next(seasonNames.Length)];
                dayOfMonth = random.Next(28) + 1;
            }
            else
            {
                bool conflicts;
                SDate tomorrow;

                do
                {
                    conflicts = false;
                    currentSeason = seasonNames[random.Next(seasonNames.Length)];
                    if (!config.AlwaysStartAt1st)
                        dayOfMonth = random.Next(28) + 1;
                    else
                        dayOfMonth = 28; // set dayOfMonth to 28 to make next day to 1st
                    tomorrow = new SDate(dayOfMonth, currentSeason).AddDays(1);

                    // conflict check
                    if (!allowedSeasons.Contains(tomorrow.Season))
                        conflicts = true;

                    // if next day is festival day, randomize one more time
                    else if (config.AvoidFestivalDay && Utility.isFestivalDay(tomorrow.Day, tomorrow.Season))
                        conflicts = true;

                    random = new Random();
                } while (conflicts);

                monitor.Log("Randomized: Season: " + currentSeason + ", Day: " + dayOfMonth + ", as moving day");
                // set seedSeason
                seedSeason = tomorrow.AddDays(4).Season;
                // check if the date is winter 28th, if the option is used
                if (currentSeason == "winter" && dayOfMonth == 28 && config.UseWinter28toYear1)
                    winter28 = true;
            }
        }

        private static void Apply()
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

        private static void PutSeasonalSeeds()
        {
            if (seedSeason == "spring" || seedSeason == "winter")
                return;

            // detect property
            Farm farm = Game1.getFarm();
            string seedBoxLocationString = farm.getMapProperty("FarmHouseStarterSeedsPosition");
            Vector2 seedBoxLocation = new(0f, 0f);

            bool foundPropertyLocation = false;
            if (seedBoxLocationString != "" && farm.getMapProperty("FarmHouseFurniture") != null)
            {
                try
                {
                    string[] location = seedBoxLocationString.Split(' ');
                    seedBoxLocation = new Vector2(int.Parse(location[0]), int.Parse(location[1]));
                }
                catch
                {
                }
            }

            if (!foundPropertyLocation)
            {
                switch (Game1.whichFarm)
                {
                    case 0:
                    case 5:
                        seedBoxLocation = new Vector2(3f, 7f);
                        break;
                    case 1:
                    case 2:
                    case 4:
                        seedBoxLocation = new Vector2(4f, 7f);
                        break;
                    case 3:
                        seedBoxLocation = new Vector2(2f, 9f);
                        break;
                    case 6:
                        seedBoxLocation = new Vector2(8f, 6f);
                        break;
                }
            }


            // change seed chest
            GameLocation farmHouse = Game1.getLocationFromName("FarmHouse");
            farmHouse.objects.Remove(seedBoxLocation);
            farmHouse.objects.Add(seedBoxLocation, new Chest(0, new List<Item>()
            {
                (Item)new StardewValley.Object(483, 18)
            }, seedBoxLocation, true));
        }

        public static void SetSeedSeason()
        {

            if ((currentSeason == "spring" && dayOfMonth >= 24) || (currentSeason == "summer" && dayOfMonth < 24))
            {
                seedSeason = "summer";
            }
            // summer 22 ~ fall 23
            else if (currentSeason == "summer" || (currentSeason == "fall" && dayOfMonth < 24))
            {
                seedSeason = "fall";
            }
            // fall 24 ~ spring 23
            else
            {
                seedSeason = "spring";
            }
        }
    }
}