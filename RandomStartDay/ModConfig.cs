using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;
using System.Threading;

class ModConfig
{
    public bool disableAll { get; set; }

    public bool isRandomSeedUsed { get; set; }

    // reset other randomize options only when isRandomSeedUsed is TRUE
    public string[] allowedSeasons { get; set; }
    public bool avoidFestivalDay { get; set; }
    public bool alwaysStartAt1st { get; set; }

    public bool useSeasonalSeeds { get; set; }
    public bool useWinter28toYear1 { get; set; }

    public ModConfig()
    {
        this.disableAll = false;
        this.isRandomSeedUsed = true;
        this.allowedSeasons = new string[] { "spring", "summer", "fall", "winter" };
        this.avoidFestivalDay = false;
        this.alwaysStartAt1st = false;

        this.useSeasonalSeeds = true;
        this.useWinter28toYear1 = true;
    }
}