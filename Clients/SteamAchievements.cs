using Newtonsoft.Json.Linq;
using Playnite.Common.Web;
using Playnite.SDK;
using PluginCommon;
using SuccessStory.Database;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using AchievementsLocal;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Playnite.SDK.Models;
using Newtonsoft.Json;
using SteamKit2;
using System.Globalization;

namespace SuccessStory.Clients
{
    // https://partner.steamgames.com/doc/home
    class SteamAchievements : GenericAchievements
    {
        private IHtmlDocument HtmlDocument { get; set; } = null;
        private bool IsLocal { get; set; } = false;

        private string SteamId { get; set; } = string.Empty;
        private string SteamApiKey { get; set; } = string.Empty;
        private string SteamUser { get; set; } = string.Empty;

        private string UrlProfilById = @"https://steamcommunity.com/profiles/{0}/stats/{1}/?tab=achievements";
        private string UrlProfilByName = @"https://steamcommunity.com/id/{0}/stats/{1}/?tab=achievements";


        public SteamAchievements(IPlayniteAPI PlayniteApi, SuccessStorySettings settings, string PluginUserDataPath) : base(PlayniteApi, settings, PluginUserDataPath)
        {
            LocalLang = CodeLang.GetSteamLang(Localization.GetPlayniteLanguageConfiguration(_PlayniteApi.Paths.ConfigurationPath));
        }

        public override GameAchievements GetAchievements(Game game)
        {
            int AppId = int.Parse(game.GameId);
            List<Achievements> AllAchievements = new List<Achievements>();
            GameAchievements Result = new GameAchievements
            {
                Name = game.Name,
                HaveAchivements = false,
                Total = 0,
                Unlocked = 0,
                Locked = 0,
                Progression = 0,
                Achievements = AllAchievements
            };


            // Get Steam configuration if exist.
            if (!GetSteamConfig())
            {
                return Result;
            }


            if (!IsLocal)
            {
                VerifSteamUser();
                if (SteamUser.IsNullOrEmpty())
                {
                    logger.Warn("SuccessStory - No Steam user");
                }

                AllAchievements = GetPlayerAchievements(AppId);

                if (AllAchievements.Count > 0)
                {
                    AllAchievements = GetSchemaForGame(AppId, AllAchievements);


                    Result.HaveAchivements = true;
                    Result.Total = AllAchievements.Count;
                    Result.Unlocked = AllAchievements.FindAll(x => x.DateUnlocked != null && x.DateUnlocked != default(DateTime)).Count;
                    Result.Locked = Result.Total - Result.Locked;
                    Result.Progression = (Result.Total != 0) ? (int)Math.Ceiling((double)(Result.Unlocked * 100 / Result.Total)) : 0;
                    Result.Achievements = AllAchievements;
                }
            }
            else
            {
                SteamEmulators se = new SteamEmulators(_PlayniteApi, _PluginUserDataPath);
                AppId = se.GetSteamId();

                var temp = se.GetAchievementsLocal(game.Name, SteamApiKey);

                if (temp.Achievements.Count > 0)
                {
                    Result.HaveAchivements = true;
                    Result.Total = temp.Total;
                    Result.Locked = temp.Locked;
                    Result.Unlocked = temp.Unlocked;
                    Result.Progression = temp.Progression;

                    for (int i = 0; i < temp.Achievements.Count; i++)
                    {
                        Result.Achievements.Add(new Achievements
                        {
                            Name = temp.Achievements[i].Name,
                            Description = temp.Achievements[i].Description,
                            UrlUnlocked = temp.Achievements[i].UrlUnlocked,
                            UrlLocked = temp.Achievements[i].UrlLocked,
                            DateUnlocked = temp.Achievements[i].DateUnlocked
                        });
                    }
                }
            }


            if (Result.Achievements.Count > 0)
            {
                Result.Achievements = GetGlobalAchievementPercentagesForApp(AppId, Result.Achievements);
            }

            return Result;
        }

        public void SetLocal()
        {
            IsLocal = true;
        }

        private bool GetSteamConfig()
        {
            try
            {
                if (File.Exists(_PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json"))
                {
                    JObject SteamConfig = JObject.Parse(File.ReadAllText(_PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json"));
                    SteamId = (string)SteamConfig["UserId"];
                    SteamApiKey = (string)SteamConfig["ApiKey"];
                    SteamUser = (string)SteamConfig["UserName"];
                }
                else
                {
                    logger.Error($"SuccessStory - No Steam configuration find");
                    AchievementsDatabase.ListErrors.Add($"Error on SteamAchievements: no Steam configuration and/or API key in settings menu for Steam Library.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", "Error on GetSteamConfig");
            }

            if (SteamId.IsNullOrEmpty() || SteamApiKey.IsNullOrEmpty())
            {
                logger.Error($"SuccessStory - No Steam configuration");
                AchievementsDatabase.ListErrors.Add($"Error on SteamAchievements: no Steam configuration and/or API key in settings menu for Steam Library.");
                return false;
            }

            return true;
        }

        private void VerifSteamUser()
        {
            try
            {
                using (dynamic steamWebAPI = WebAPI.GetInterface("ISteamUser", SteamApiKey))
                {
                    KeyValue PlayerSummaries = steamWebAPI.GetPlayerSummaries(steamids: SteamId);
                    string personaname = (string)PlayerSummaries["players"]["player"].Children[0].Children.Find(x => x.Name == "personaname").Value;

                    if (personaname != SteamUser)
                    {
                        logger.Warn($"SuccessStory - SteamUser is different {SteamUser} != {personaname}");
                        SteamUser = personaname;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", "Error on VerifSteamUser()");
            }
        }

        private List<Achievements> GetPlayerAchievements(int AppId)
        {
            List<Achievements> AllAchievements = new List<Achievements>();

            try
            {
                using (dynamic steamWebAPI = WebAPI.GetInterface("ISteamUserStats", SteamApiKey))
                {
                    KeyValue PlayerAchievements = steamWebAPI.GetPlayerAchievements(steamid: SteamId, appid: AppId, l: LocalLang);
                    foreach(KeyValue AchievementsData in PlayerAchievements.Children.Find(x => x.Name == "achievements").Children)
                    {
                        int.TryParse(AchievementsData.Children.Find(x => x.Name == "unlocktime").Value, out int unlocktime);

                        AllAchievements.Add(new Achievements
                        {
                            ApiName = AchievementsData.Children.Find(x => x.Name == "apiname").Value,
                            Name = AchievementsData.Children.Find(x => x.Name == "name").Value,
                            Description = AchievementsData.Children.Find(x => x.Name == "description").Value,
                            DateUnlocked = (unlocktime == 0) ? default(DateTime) : new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(unlocktime)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", $"Error on GetPlayerAchievements({SteamId}, {AppId}, {LocalLang})");
            }

            return AllAchievements;
        }

        private List<Achievements> GetSchemaForGame(int AppId, List<Achievements> AllAchievements)
        {
            try
            {
                using (dynamic steamWebAPI = WebAPI.GetInterface("ISteamUserStats", SteamApiKey))
                {
                    KeyValue SchemaForGame = steamWebAPI.GetSchemaForGame(appid: AppId, l: LocalLang);
                    foreach (KeyValue AchievementsData in SchemaForGame.Children.Find(x => x.Name == "availableGameStats").Children.Find(x => x.Name == "achievements").Children)
                    {
                        AllAchievements.Find(x => x.ApiName == AchievementsData.Name).IsHidden = AchievementsData.Children.Find(x => x.Name == "hidden").Value == "1";
                        AllAchievements.Find(x => x.ApiName == AchievementsData.Name).UrlUnlocked = AchievementsData.Children.Find(x => x.Name == "icon").Value;
                        AllAchievements.Find(x => x.ApiName == AchievementsData.Name).UrlLocked = AchievementsData.Children.Find(x => x.Name == "icongray").Value;

                        if (AllAchievements.Find(x => x.ApiName == AchievementsData.Name).IsHidden)
                        {
                            AllAchievements.Find(x => x.ApiName == AchievementsData.Name).Description = FindHiddenDescription(AppId, AllAchievements.Find(x => x.ApiName == AchievementsData.Name).Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", $"Error on GetPlayerAchievements({SteamId}, {AppId}, {LocalLang})");
            }

            return AllAchievements;
        }

        private string FindHiddenDescription(int AppId, string DisplayName, bool TryByName = false)
        {
            string url = string.Empty;
            string ResultWeb = string.Empty;
            bool noData = true;

            // Get data
            if (HtmlDocument == null)
            {
                var cookieLang = new Cookie("Steam_Language", LocalLang);
                var cookies = new List<Cookie>();
                cookies.Add(cookieLang);

                if (!TryByName)
                {
                    logger.Debug($"SuccessStory - FindHiddenDescription() for {SteamId} - {AppId}");
                    url = string.Format(UrlProfilById, SteamId, AppId);
                    try
                    {
                        ResultWeb = HttpDownloader.DownloadString(url, cookies, Encoding.UTF8);
                    }
                    catch (WebException ex)
                    {
                        Common.LogError(ex, "SuccessStory", $"Error on FindHiddenDescription()");
                    }
                }
                else
                {
                    logger.Debug($"SuccessStory - FindHiddenDescription() for {SteamUser} - {AppId}");
                    url = string.Format(UrlProfilByName, SteamUser, AppId);
                    try
                    {
                        ResultWeb = HttpDownloader.DownloadString(url, cookies, Encoding.UTF8);
                    }
                    catch (WebException ex)
                    {
                        Common.LogError(ex, "SuccessStory", $"Error on FindHiddenDescription()");
                    }
                }

                if (!ResultWeb.IsNullOrEmpty())
                {
                    HtmlParser parser = new HtmlParser();
                    HtmlDocument = parser.Parse(ResultWeb);

                    if (HtmlDocument.QuerySelectorAll("div.achieveRow").Length != 0)
                    {
                        noData = false;
                    }
                }
                
                if (!TryByName && noData)
                {
                    HtmlDocument = null;
                    return FindHiddenDescription(AppId, DisplayName, TryByName = true);
                }
                else if (noData)
                {
                    return string.Empty;
                }
            }

            // Find the achievement description
            if (HtmlDocument != null)
            {
                foreach (var achieveRow in HtmlDocument.QuerySelectorAll("div.achieveRow"))
                {
                    if (achieveRow.QuerySelector("h3").InnerHtml.Trim().ToLower() == DisplayName.Trim().ToLower())
                    {
                        return achieveRow.QuerySelector("h5").InnerHtml;
                    }
                }
            }

            return string.Empty;
        }

        private List<Achievements> GetGlobalAchievementPercentagesForApp(int AppId, List<Achievements> AllAchievements)
        {
            try
            {
                using (dynamic steamWebAPI = WebAPI.GetInterface("ISteamUserStats", SteamApiKey))
                {
                    KeyValue GlobalAchievementPercentagesForApp = steamWebAPI.GetGlobalAchievementPercentagesForApp(gameid: AppId);
                    foreach (KeyValue AchievementPercentagesData in GlobalAchievementPercentagesForApp["achievements"]["achievement"].Children)
                    {
                        string ApiName = AchievementPercentagesData.Children.Find(x => x.Name == "name").Value;
                        float Percent = float.Parse(AchievementPercentagesData.Children.Find(x => x.Name == "percent").Value.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));

                        AllAchievements.Find(x => x.ApiName == ApiName).Percent = Percent;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", $"Error on GetPlayerAchievements({SteamId}, {AppId}, {LocalLang})");
            }

            return AllAchievements;
        }
    }
}
