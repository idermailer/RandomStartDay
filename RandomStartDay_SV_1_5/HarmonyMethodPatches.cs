using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Quests;
using xTile;
using xTile.Dimensions;

namespace RandomStartDay
{
    internal class HarmonyMethodPatches
    {
        private static IModHelper Helper;
        private static IMonitor Monitor;
        private static string resultString = "";

        public static void Initialize(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;
        }
        public static void ChangeTodaysTip(ref string __result)
        {
            try
            {
                Dictionary<string, string> dic = Game1.temporaryContent.Load<Dictionary<string, string>>("Data/TV/TipChannel");
                var date = SDate.Now();
                int year = (date.Year + 1) % 2;
                int season = date.SeasonIndex;
                int day = date.Day;
                int todayNumber = 112 * year + (28 * season) + day;
                if (dic.ContainsKey(todayNumber.ToString()))
                {
                    resultString = dic[todayNumber.ToString()];
                }
                else
                {
                    dic = Game1.temporaryContent.Load<Dictionary<string, string>>("Strings/StringsFromCSFiles");
                    resultString = dic["TV.cs.13148"];

                }
                __result = resultString;
                return;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(ChangeTodaysTip)}:\n{ex}", LogLevel.Error);
                Dictionary<string, string> dic = Game1.temporaryContent.Load<Dictionary<string, string>>("Strings/StringsFromCSFiles");
                resultString = dic["TV.cs.13148"];
                __result = resultString;
                return;
            }
        }

        public static void Harmony_Quest6ToWheatQuest(ref int questID)
        {
            if (ModEntry.config.DisableAll) { return; }

            if ((ModEntry.seedSeason == "summer" || ModEntry.seedSeason == "fall") && questID == 6)
            {
                Quest questFromId = Quest.getQuestFromId(13225001);
                if (questFromId != null)
                {
                    Game1.player.questLog.Add(questFromId);
                    Game1.player.removeQuest(6);
                }
            }
        }

        public static void Harmony_ChangeIntroSeason(ref Texture2D ___roadsideTexture)
        {
            if (ModEntry.config.DisableAll) { return; }

            if (ModEntry.currentSeason != "spring")
            {
                ___roadsideTexture = Game1.content.Load<Texture2D>("Maps/" + ModEntry.currentSeason + "_outdoorsTileSheet");
                Game1.changeMusicTrack(ModEntry.currentSeason + "_day_ambient");
            }
        }
    }
}