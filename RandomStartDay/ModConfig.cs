using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley.ItemTypeDefinitions;
using System;
using System.Threading;

class ModConfig
{
    public bool DisableAll { get; set; }


    // reset other randomize options only when isRandomSeedUsed is TRUE
    public bool[] AllowSpringSummerFallWinter { get; set; }
    public bool AvoidFestivalDay { get; set; }
    public bool AlwaysStartAt1st { get; set; }

    public bool UseWheatSeeds { get; set; }
    public bool UseWinter28toYear1 { get; set; }

    public ModConfig()
    {
        this.DisableAll = false;
        this.AllowSpringSummerFallWinter = new bool[] { true,true,true,true };
        this.AvoidFestivalDay = false;
        this.AlwaysStartAt1st = false;

        this.UseWheatSeeds = true;
        this.UseWinter28toYear1 = true;
    }
}