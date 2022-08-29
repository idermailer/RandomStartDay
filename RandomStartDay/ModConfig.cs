using StardewModdingAPI.Utilities;

class ModConfig
{
    public bool isRandomSeedUsed { get; set; }
    public string[] allowedSeasons { get; set;}
    public int MaxOfDayOfMonth { get; set;}
    public bool useSeasonalTilesetInBusScene { get; set; }
    public ModConfig()
    {
        this.isRandomSeedUsed = true;
        this.allowedSeasons = new string[] {"spring", "summer", "fall", "winter"};
        this.MaxOfDayOfMonth = 28;

        this.useSeasonalTilesetInBusScene = true;
    }
}