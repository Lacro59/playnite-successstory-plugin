using Newtonsoft.Json.Linq;
using Playnite.SDK;
using CommonPluginsShared;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Playnite.SDK.Models;
using Newtonsoft.Json;
using SteamKit2;
using System.Globalization;
using System.Threading.Tasks;
using CommonPluginsPlaynite.Common.Web;
using SuccessStory.Services;

namespace SuccessStory.Clients
{
    class SteamAchievements : GenericAchievements
    {
        private IHtmlDocument HtmlDocument { get; set; } = null;
        private bool IsLocal { get; set; } = false;

        private string SteamId { get; set; } = string.Empty;
        private string SteamApiKey { get; set; } = string.Empty;
        private string SteamUser { get; set; } = string.Empty;

        private readonly string UrlProfilById = @"https://steamcommunity.com/profiles/{0}/stats/{1}/?tab=achievements";
        private readonly string UrlProfilByName = @"https://steamcommunity.com/id/{0}/stats/{1}/?tab=achievements";


        public SteamAchievements() : base()
        {
            LocalLang = CodeLang.GetSteamLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language);
        }


        public override GameAchievements GetAchievements(Game game)
        {
            int AppId = 0;
            List<Achievements> AllAchievements = new List<Achievements>();
            List<GameStats> AllStats = new List<GameStats>();
            GameAchievements Result = SuccessStory.PluginDatabase.GetDefault(game);
            Result.Items = AllAchievements;


            // Get Steam configuration if exist.
            if (!GetSteamConfig())
            {
                return Result;
            }


            if (!IsLocal)
            {
                Common.LogDebug(true, $"GetAchievements()");

                int.TryParse(game.GameId, out AppId);

                if (PluginDatabase.PluginSettings.Settings.EnableSteamWithoutWebApi)
                {
                    AllAchievements = GetAchievementsInPublic(AppId);
                }
                else
                {
                    VerifSteamUser();
                    if (SteamUser.IsNullOrEmpty())
                    {
                        logger.Warn("No Steam user");
                    }

                    AllAchievements = GetPlayerAchievements(AppId);
                    AllStats = GetUsersStats(AppId);
                }

                if (AllAchievements.Count > 0)
                {
                    var DataCompleted = GetSchemaForGame(AppId, AllAchievements, AllStats);
                    AllAchievements = DataCompleted.Item1;
                    AllStats = DataCompleted.Item2;

                    Result.HaveAchivements = true;
                    Result.Total = AllAchievements.Count;
                    Result.Unlocked = AllAchievements.FindAll(x => x.DateUnlocked != null && x.DateUnlocked != default(DateTime)).Count;
                    Result.Locked = Result.Total - Result.Unlocked;
                    Result.Progression = (Result.Total != 0) ? (int)Math.Ceiling((double)(Result.Unlocked * 100 / Result.Total)) : 0;
                    Result.Items = AllAchievements;
                    Result.ItemsStats = AllStats;
                }
            }
            else
            {
                Common.LogDebug(true, $"GetAchievementsLocal()");

                SteamEmulators se = new SteamEmulators();
                var temp = se.GetAchievementsLocal(game.Name, SteamApiKey);
                AppId = se.GetSteamId();

                if (temp.Items.Count > 0)
                {
                    Result.HaveAchivements = true;
                    Result.Total = temp.Total;
                    Result.Locked = temp.Locked;
                    Result.Unlocked = temp.Unlocked;
                    Result.Progression = temp.Progression;

                    for (int i = 0; i < temp.Items.Count; i++)
                    {
                        Result.Items.Add(new Achievements
                        {
                            Name = temp.Items[i].Name,
                            ApiName = temp.Items[i].ApiName,
                            Description = temp.Items[i].Description,
                            UrlUnlocked = temp.Items[i].UrlUnlocked,
                            UrlLocked = temp.Items[i].UrlLocked,
                            DateUnlocked = temp.Items[i].DateUnlocked
                        });
                    }
                }
            }


            if (Result.Items.Count > 0)
            {
                Result.Items = GetGlobalAchievementPercentagesForApp(AppId, Result.Items);
            }

            return Result;
        }


        public override bool IsConfigured()
        {
            return GetSteamConfig();
        }

        public override bool IsConnected()
        {
            throw new NotImplementedException();
        }


        public void SetLocal()
        {
            IsLocal = true;
        }

        private bool GetSteamConfig()
        {
            try
            {
                if (File.Exists(PluginDatabase.Paths.PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json"))
                {
                    JObject SteamConfig = JObject.Parse(File.ReadAllText(PluginDatabase.Paths.PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json"));
                    SteamId = (string)SteamConfig["UserId"];
                    SteamApiKey = (string)SteamConfig["ApiKey"];
                    SteamUser = (string)SteamConfig["UserName"];
                }
                else
                {
                    logger.Error($"No Steam configuration find");
                    SuccessStoryDatabase.ListErrors.Add($"Error on SteamAchievements: no Steam configuration and/or API key in settings menu for Steam Library.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "Error on GetSteamConfig");
            }


            if (PluginDatabase.PluginSettings.Settings.EnableSteamWithoutWebApi)
            {
                if (SteamUser.IsNullOrEmpty())
                {
                    logger.Error($"No Steam user configuration");
                    SuccessStoryDatabase.ListErrors.Add($"Error on SteamAchievements: no Steam user in settings menu for Steam Library.");
                    return false;
                }
            }
            else
            {
                if (SteamId.IsNullOrEmpty() || SteamApiKey.IsNullOrEmpty())
                {
                    logger.Error($"No Steam configuration");
                    SuccessStoryDatabase.ListErrors.Add($"Error on SteamAchievements: no Steam configuration and/or API key in settings menu for Steam Library.");
                    return false;
                }
            }

            return true;
        }

        private void VerifSteamUser()
        {
            if (PluginDatabase.PluginSettings.Settings.EnableSteamWithoutWebApi)
            {
                return;
            }

            if (SteamApiKey.IsNullOrEmpty())
            {
                logger.Warn($"No Steam API key");
                return;
            }

            try
            {
                using (dynamic steamWebAPI = WebAPI.GetInterface("ISteamUser", SteamApiKey))
                {
                    KeyValue PlayerSummaries = steamWebAPI.GetPlayerSummaries(steamids: SteamId);
                    string personaname = (string)PlayerSummaries["players"]["player"].Children[0].Children.Find(x => x.Name == "personaname").Value;

                    if (personaname != SteamUser)
                    {
                        logger.Warn($"Steam user is different {SteamUser} != {personaname}");
                        SteamUser = personaname;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "Error on VerifSteamUser()");
            }
        }

        public bool CheckIsPublic(int AppId)
        {
            GetSteamConfig();

            if (PluginDatabase.PluginSettings.Settings.EnableSteamWithoutWebApi)
            {
                string ProfilById = @"https://steamcommunity.com/profiles/{0}/";
                string ProfilByName = @"https://steamcommunity.com/id/{0}";

                ProfilById = string.Format(ProfilById, SteamId);
                ProfilByName = string.Format(ProfilByName, SteamUser);

                string ResultWeb = string.Empty;
                HtmlParser parser = new HtmlParser();
                IHtmlDocument HtmlDoc = null;

                try
                {
                    ResultWeb = HttpDownloader.DownloadString(ProfilById);
                    HtmlDoc = parser.Parse(ResultWeb);
                    if (HtmlDocument.QuerySelectorAll("div.achieveRow").Length > 0)
                    {
                        return true;
                    }
                }
                catch (WebException ex)
                {
                    Common.LogError(ex, false);
                    return false;
                }

                try
                {
                    ResultWeb = HttpDownloader.DownloadString(ProfilByName);
                    HtmlDoc = parser.Parse(ResultWeb);
                    if (HtmlDocument.QuerySelectorAll("div.achieveRow").Length > 0)
                    {
                        return true;
                    }
                }
                catch (WebException ex)
                {
                    Common.LogError(ex, false);
                    return false;
                }
            }
            else
            {
                if (SteamApiKey.IsNullOrEmpty())
                {
                    logger.Warn($"No Steam API key");
                    return false;
                }

                try
                {
                    using (dynamic steamWebAPI = WebAPI.GetInterface("ISteamUserStats", SteamApiKey))
                    {
                        KeyValue PlayerAchievements = steamWebAPI.GetPlayerAchievements(steamid: SteamId, appid: AppId, l: LocalLang);
                        return true;
                    }
                }
                // TODO With recent SteamKit
                //catch (WebAPIRequestException wex)
                //{
                //    if (wex.StatusCode == HttpStatusCode.Forbidden)
                //    {
                //        _PlayniteApi.Notifications.Add(new NotificationMessage(
                //            $"SuccessStory-Steam-PrivateProfil",
                //            "SuccessStory - Steam profil is private",
                //            NotificationType.Error
                //        ));
                //        logger.Warn("Steam profil is private");
                //    }
                //    else
                //    {
                //        Common.LogError(wex, false, $"Error on GetPlayerAchievements({SteamId}, {AppId}, {LocalLang})");
                //    }
                //}
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        if (ex.Response is HttpWebResponse response)
                        {
                            if (response.StatusCode == HttpStatusCode.Forbidden)
                            {
                                PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                                    "SuccessStory-Steam-PrivateProfil",
                                    $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsSteamPrivate")}",
                                    NotificationType.Error,
                                    () => Process.Start(@"https://steamcommunity.com/my/edit/settings")
                                ));
                                logger.Warn("Steam profil is private");

                                // TODO https://github.com/Lacro59/playnite-successstory-plugin/issues/76
                                Common.LogError(ex, false, "Error on CheckIsPublic()");

                                return false;
                            }
                        }
                        else
                        {
                            // no http status code available
                            Common.LogError(ex, false, $"Error on CheckIsPublic({AppId})");
                        }
                    }
                    else
                    {
                        // no http status code available
                        Common.LogError(ex, false, $"Error on CheckIsPublic({AppId})");
                    }

                    return true;
                }
            }

            return false;
        }


        private List<GameStats> GetUsersStats(int AppId)
        {
            List<GameStats> AllStats = new List<GameStats>();

            if (PluginDatabase.PluginSettings.Settings.EnableSteamWithoutWebApi)
            {
                return AllStats;
            }

            if (SteamApiKey.IsNullOrEmpty())
            {
                logger.Warn($"No Steam API key");
                return AllStats;
            }

            try
            {
                using (dynamic steamWebAPI = WebAPI.GetInterface("ISteamUserStats", SteamApiKey))
                {
                    KeyValue UserStats = steamWebAPI.GetUserStatsForGame(steamid: SteamId, appid: AppId, l: LocalLang);

                    if (UserStats != null && UserStats.Children != null)
                    {
                        var UserStatsData = UserStats.Children.Find(x => x.Name == "stats");
                        if (UserStatsData != null)
                        {
                            foreach (KeyValue StatsData in UserStatsData.Children)
                            {
                                double.TryParse(StatsData.Children.First().Value.Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator), out double ValueStats);

                                AllStats.Add(new GameStats
                                {
                                    Name = StatsData.Name,
                                    DisplayName = string.Empty,
                                    Value = ValueStats
                                });
                            }
                        }
                    }
                }
            }
            // TODO With recent SteamKit
            //catch (WebAPIRequestException wex)
            //{
            //    if (wex.StatusCode == HttpStatusCode.Forbidden)
            //    {
            //        _PlayniteApi.Notifications.Add(new NotificationMessage(
            //            $"SuccessStory-Steam-PrivateProfil",
            //            "SuccessStory - Steam profil is private",
            //            NotificationType.Error
            //        ));
            //        logger.Warn("Steam profil is private");
            //    }
            //    else
            //    {
            //        Common.LogError(wex, false, $"Error on GetUsersStats({SteamId}, {AppId}, {LocalLang})");
            //    }
            //}
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    if (ex.Response is HttpWebResponse response)
                    {
                        if (response.StatusCode == HttpStatusCode.Forbidden)
                        {
                            PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                                "SuccessStory-Steam-PrivateProfil",
                                $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsSteamPrivate")}",
                                NotificationType.Error,
                                () => Process.Start(@"https://steamcommunity.com/my/edit/settings")
                            ));
                            logger.Warn("Steam profil is private");

                            // TODO https://github.com/Lacro59/playnite-successstory-plugin/issues/76
                            Common.LogError(ex, false, $"Error on GetUsersStats({SteamId}, {AppId}, {LocalLang})");
                        }
                    }
                    else
                    {
                        // no http status code available
                        Common.LogError(ex, false, $"Error on GetUsersStats({SteamId}, {AppId}, {LocalLang})");
                    }
                }
                else
                {
                    // no http status code available
                    Common.LogError(ex, false, $"Error on GetUsersStats({SteamId}, {AppId}, {LocalLang})");
                }
            }

            return AllStats;
        }

        private List<Achievements> GetPlayerAchievements(int AppId)
        {
            List<Achievements> AllAchievements = new List<Achievements>();

            if (PluginDatabase.PluginSettings.Settings.EnableSteamWithoutWebApi)
            {
                return AllAchievements;
            }

            if (SteamApiKey.IsNullOrEmpty())
            {
                logger.Warn($"No Steam API key");
                return AllAchievements;
            }

            try
            {
                using (dynamic steamWebAPI = WebAPI.GetInterface("ISteamUserStats", SteamApiKey))
                {
                    KeyValue PlayerAchievements = steamWebAPI.GetPlayerAchievements(steamid: SteamId, appid: AppId, l: LocalLang);

                    if (PlayerAchievements != null && PlayerAchievements.Children != null)
                    {
                        var PlayerAchievementsData = PlayerAchievements.Children.Find(x => x.Name == "achievements");
                        if (PlayerAchievementsData != null)
                        {
                            foreach (KeyValue AchievementsData in PlayerAchievementsData.Children)
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
                }
            }
            // TODO With recent SteamKit
            //catch (WebAPIRequestException wex)
            //{
            //    if (wex.StatusCode == HttpStatusCode.Forbidden)
            //    {
            //        _PlayniteApi.Notifications.Add(new NotificationMessage(
            //            $"SuccessStory-Steam-PrivateProfil",
            //            "SuccessStory - Steam profil is private",
            //            NotificationType.Error
            //        ));
            //        logger.Warn("Steam profil is private");
            //    }
            //    else
            //    {
            //        Common.LogError(wex, false, $"Error on GetPlayerAchievements({SteamId}, {AppId}, {LocalLang})");
            //    }
            //}
            catch (WebException ex)
            {
                if (ex != null && ex.Status == WebExceptionStatus.ProtocolError)
                {
                    if (ex.Response is HttpWebResponse response)
                    {
                        if (response.StatusCode == HttpStatusCode.Forbidden)
                        {
                            PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                                "SuccessStory-Steam-PrivateProfil",
                                $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsSteamPrivate")}",
                                NotificationType.Error,
                                () => Process.Start(@"https://steamcommunity.com/my/edit/settings")
                            ));
                            logger.Warn("Steam profil is private");

                            // TODO https://github.com/Lacro59/playnite-successstory-plugin/issues/76
                            Common.LogError(ex, false, $"Error on GetPlayerAchievements({SteamId}, {AppId}, {LocalLang})");
                        }
                    }
                    else
                    {
                        // no http status code available
                        Common.LogError(ex, false, $"Error on GetPlayerAchievements({SteamId}, {AppId}, {LocalLang})");
                    }
                }
                else
                {
                    // no http status code available
                    Common.LogError(ex, false, $"Error on GetPlayerAchievements({SteamId}, {AppId}, {LocalLang})");
                }
            }

            return AllAchievements;
        }

        private Tuple<List<Achievements>, List<GameStats>> GetSchemaForGame(int AppId, List<Achievements> AllAchievements, List<GameStats> AllStats)
        {
            try
            {
                if (PluginDatabase.PluginSettings.Settings.EnableSteamWithoutWebApi)
                {
                    return Tuple.Create(AllAchievements, AllStats);
                }

                if (SteamApiKey.IsNullOrEmpty())
                {
                    logger.Warn($"No Steam API key");
                    return Tuple.Create(AllAchievements, AllStats);
                }

                using (dynamic steamWebAPI = WebAPI.GetInterface("ISteamUserStats", SteamApiKey))
                {
                    KeyValue SchemaForGame = steamWebAPI.GetSchemaForGame(appid: AppId, l: LocalLang);

                    try
                    {
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
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error on AchievementsData({AppId}, {LocalLang})");
                    }

                    try
                    {
                        var availableGameStats = SchemaForGame.Children.Find(x => x.Name == "availableGameStats");

                        if (availableGameStats != null)
                        {
                            var stats = availableGameStats.Children.Find(x => x.Name == "stats");

                            if (stats != null)
                            {
                                var ListStatsData = stats.Children;
                                foreach (KeyValue StatsData in ListStatsData)
                                {
                                    if (AllStats.Find(x => x.Name == StatsData.Name) == null)
                                    {
                                        double.TryParse(StatsData.Children.Find(x => x.Name == "defaultvalue").Value, out double ValueStats);

                                        AllStats.Add(new GameStats
                                        {
                                            Name = StatsData.Name,
                                            DisplayName = StatsData.Children.Find(x => x.Name == "displayName").Value,
                                            Value = ValueStats
                                        });
                                    }
                                    else
                                    {
                                        AllStats.Find(x => x.Name == StatsData.Name).DisplayName = StatsData.Children.Find(x => x.Name == "displayName").Value;
                                    }
                                }
                            }
                            else
                            {
                                logger.Warn($"No Steam stats for {AppId}");
                            }
                        }
                        else
                        {
                            logger.Warn($"No Steam stats for {AppId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error on AvailableGameStats({AppId}, {LocalLang})");
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on GetSchemaForGame({AppId}, {LocalLang})");
            }

            return Tuple.Create(AllAchievements, AllStats);
        }


        // TODO Use "profileurl" in "ISteamUser"
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
                    Common.LogDebug(true, $"FindHiddenDescription() for {SteamId} - {AppId}");

                    url = string.Format(UrlProfilById, SteamId, AppId);
                    try
                    {
                        ResultWeb = HttpDownloader.DownloadString(url, cookies, Encoding.UTF8);
                    }
                    catch (WebException ex)
                    {
                        Common.LogError(ex, false, $"Error on FindHiddenDescription()");
                    }
                }
                else
                {
                    Common.LogDebug(true, $"FindHiddenDescription() for {SteamUser} - {AppId}");

                    url = string.Format(UrlProfilByName, SteamUser, AppId);
                    try
                    {
                        ResultWeb = HttpDownloader.DownloadString(url, cookies, Encoding.UTF8);
                    }
                    catch (WebException ex)
                    {
                        Common.LogError(ex, false, $"Error on FindHiddenDescription()");
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
                    try { 
                        if (achieveRow.QuerySelector("h3").InnerHtml.Trim().ToLower() == DisplayName.Trim().ToLower())
                        {
                            string TempDescription = achieveRow.QuerySelector("h5").InnerHtml;

                            if (TempDescription.Contains("steamdb_achievement_spoiler"))
                            {
                                TempDescription = achieveRow.QuerySelector("h5 span").InnerHtml;
                                return WebUtility.HtmlDecode(TempDescription.Trim());
                            }
                            else
                            {
                                return WebUtility.HtmlDecode(TempDescription.Trim());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                    }
                }
            }

            return string.Empty;
        }

        private List<Achievements> GetAchievementsInPublic(int AppId, bool TryByName = false)
        {
            List<Achievements> achievements = new List<Achievements>();

            string url = string.Empty;
            string ResultWeb = string.Empty;
            bool noData = true;

            // Get data
            if (HtmlDocument == null)
            {
                var cookieLang = new Cookie("Steam_Language", "en_US");
                var cookies = new List<Cookie>();
                cookies.Add(cookieLang);

                if (!TryByName)
                {
                    Common.LogDebug(true, $"GetAchievementsInPublic() for {SteamId} - {AppId}");

                    url = string.Format(UrlProfilById, SteamId, AppId);
                    try
                    {
                        ResultWeb = HttpDownloader.DownloadString(url, cookies, Encoding.UTF8);
                    }
                    catch (WebException ex)
                    {
                        Common.LogError(ex, false);
                    }
                }
                else
                {
                    Common.LogDebug(true, $"GetAchievementsInPublic() for {SteamUser} - {AppId}");

                    url = string.Format(UrlProfilByName, SteamUser, AppId);
                    try
                    {
                        ResultWeb = HttpDownloader.DownloadString(url, cookies, Encoding.UTF8);
                    }
                    catch (WebException ex)
                    {
                        Common.LogError(ex, false);
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
                    return GetAchievementsInPublic(AppId, TryByName = true);
                }
                else if (noData)
                {
                    return achievements;
                }
            }


            // Find the achievement description
            if (HtmlDocument != null)
            {
                foreach (var achieveRow in HtmlDocument.QuerySelectorAll("div.achieveRow"))
                {
                    try
                    {
                        string UrlUnlocked = achieveRow.QuerySelector(".achieveImgHolder img").GetAttribute("src");

                        DateTime DateUnlocked = default(DateTime);
                        string TempDate = string.Empty;
                        if (achieveRow.QuerySelector(".achieveUnlockTime") != null)
                        {
                            TempDate = achieveRow.QuerySelector(".achieveUnlockTime").InnerHtml.Trim();
                            TempDate = TempDate.ToLower().Replace("unlocked", string.Empty).Replace("@ ", string.Empty).Replace("<br>", string.Empty).Trim();
                            try
                            {
                                DateUnlocked = DateTime.ParseExact(TempDate, "d MMM h:mmtt", CultureInfo.InvariantCulture);
                            }
                            catch
                            {
                            }
                            try
                            {
                                DateUnlocked = DateTime.ParseExact(TempDate, "d MMM, yyyy h:mmtt", CultureInfo.InvariantCulture);
                            }
                            catch
                            {
                            }
                        }

                        string Name = string.Empty;
                        if (achieveRow.QuerySelector("h3") != null)
                        {
                            Name = achieveRow.QuerySelector("h3").InnerHtml.Trim();
                        }

                        string Description = string.Empty;
                        if (achieveRow.QuerySelector("h5") != null)
                        {
                            Description = achieveRow.QuerySelector("h5").InnerHtml;
                            if (Description.Contains("steamdb_achievement_spoiler"))
                            {
                                Description = achieveRow.QuerySelector("h5 span").InnerHtml.Trim();
                            }

                            Description = WebUtility.HtmlDecode(Description);
                        }

                        achievements.Add(new Achievements
                        {
                            Name = Name,
                            ApiName = string.Empty,
                            Description = Description,
                            UrlUnlocked = UrlUnlocked,
                            UrlLocked = string.Empty,
                            DateUnlocked = DateUnlocked,
                            IsHidden = false,
                            Percent = 100
                        });
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                    }
                }
            }

            return achievements;
        }

        private List<Achievements> GetGlobalAchievementPercentagesForApp(int AppId, List<Achievements> AllAchievements)
        {
            if (PluginDatabase.PluginSettings.Settings.EnableSteamWithoutWebApi)
            {
                return AllAchievements;
            }

            if (SteamApiKey.IsNullOrEmpty())
            {
                logger.Warn($"No Steam API key");
                return AllAchievements;
            }

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
                Common.LogError(ex, false, $"Error on GetPlayerAchievements({SteamId}, {AppId}, {LocalLang})");
            }

            return AllAchievements;
        }
    }
}
