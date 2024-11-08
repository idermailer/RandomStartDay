﻿using HarmonyLib;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RandomStartDay
{
    internal static class FixAftermath
    {
        internal static void SubtractYearWhenWinter28()
        {
            // if player moves on winter 28th(=starts on spring 1), return to year 1
            if (Randomize.isWinter28 && ModEntry.config.UseWinter28toYear1)
            {
                ModEntry.monitor.Log("(Randomizing.isWinter28 && config.UseWinter28toYear1) is true, subtract Game1.year");
                Game1.year -= 1;
            }
        }

        internal static void SendTodaysMails()
        {
            // replace last year's mail to this year's mail
            ThisYearsMailToLastYearsMail();

            // little compatibility support for Serfdom
            Compatibility_Serfdom(ModEntry.modHelper.ModRegistry.IsLoaded("DaLion.Taxes"));
        }

        private static void ThisYearsMailToLastYearsMail()
        {
            SDate today = SDate.Now();
            string s = today.SeasonKey.ToLower();
            int d = today.Day;
            int y = today.Year;
            Dictionary<string, string> mailData = Game1.content.Load<Dictionary<string, string>>("Data/mail");

            // When an email that should be received last year exists and not received yet
            string lastYearMailName = s + "_" + d + "_" + (y - 1);
            if (mailData.ContainsKey(lastYearMailName))
            {
                if (!Game1.player.hasOrWillReceiveMail(lastYearMailName))
                {
                    // add last year mail and remove this year mail
                    Game1.mailbox.Remove(s + "_" + d + "_" + y);
                    Game1.mailbox.Add(lastYearMailName);
                }
            }
        }

        // harmony patches
        internal static void Harmony_ChangeTodaysTip(ref string __result)
        {
            string resultString;
            Dictionary<string, string> tips = Game1.content.Load<Dictionary<string, string>>("Data/TV/TipChannel");
            int todayNumber = Game1.Date.TotalDays + 1;
            if (tips.ContainsKey(todayNumber.ToString()))
            {
                resultString = tips[todayNumber.ToString()];
            }
            else
            {
                Dictionary<string, string> CSStrings = Game1.content.Load<Dictionary<string, string>>("Strings/StringsFromCSFiles");
                CSStrings.TryGetValue("TV.cs.13148", out resultString);
            }

            if (resultString != null)
                __result = resultString;
            else
                __result = "Strings\\StringsFromCSFiles:TV.cs.13148"; // path as is
            return;
        }

        internal static void Harmony_ChangeCookingChannel(ref string[] __result, ref StardewValley.Objects.TV __instance)
        {
            if (!ModEntry.config.TVRecipeWithSeasonContext) { return; }

            // only affected on Sunday
            if (Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth) != "Sun")
                return;

            Dictionary<string, string> cookingData = Game1.content.Load<Dictionary<string, string>>("Data/TV/CookingChannel");
            int todayNumber = Game1.Date.TotalDays + 1;

            int recipeNum = todayNumber % 224 / 7;
            if (todayNumber % 224 == 0)
                recipeNum = 32;
            MethodInfo m = AccessTools.Method(typeof(StardewValley.Objects.TV), "getWeeklyRecipe", new Type[] { typeof(Dictionary<string, string>), typeof(string) });
            try
            {
                __result = (string[])m.Invoke(__instance, new object[] { cookingData, recipeNum.ToString() });
            }
            catch
            {
                __result = (string[])m.Invoke(__instance, new object[] { cookingData, "1" });
            }
        }

        private static void Compatibility_Serfdom(bool installed)
        {
            // https://www.nexusmods.com/stardewvalley/mods/24357
            if (!installed)
                return;

            Dictionary<string, string> mailData = Game1.content.Load<Dictionary<string, string>>("Data/mail");
            if (mailData == null) { return; }
            if (mailData.ContainsKey("DaLion.Taxes/FrsIntro"))
                Game1.player.mailbox.Add("DaLion.Taxes/FrsIntro");
            if (mailData.ContainsKey("DaLion.Taxes/LewisIntro"))
                Game1.player.mailbox.Add("DaLion.Taxes/LewisIntro");
        }
    }
}