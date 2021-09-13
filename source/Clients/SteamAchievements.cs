using Playnite.SDK;
using Playnite.SDK.Data;
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
using SteamKit2;
using System.Globalization;
using System.Threading.Tasks;
using CommonPluginsPlaynite.Common.Web;
using SuccessStory.Services;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using CommonPluginsStores;
using CommonPluginsShared.Models;

namespace SuccessStory.Clients
{
    class SteamAchievements : GenericAchievements
    {
        private IHtmlDocument HtmlDocument { get; set; } = null;
        private bool IsLocal { get; set; } = false;
        private bool IsManual { get; set; } = false;

        private string SteamId { get; set; } = string.Empty;
        private string SteamApiKey { get; set; } = string.Empty;
        private string SteamUser { get; set; } = string.Empty;

        private readonly string UrlProfilById = @"https://steamcommunity.com/profiles/{0}/stats/{1}?tab=achievements&l={2}";
        private readonly string UrlProfilByName = @"https://steamcommunity.com/id/{0}/stats/{1}?tab=achievements&l={2}";

        private readonly string UrlAchievements = @"https://steamcommunity.com/stats/{0}/achievements/?l={1}";


        public SteamAchievements() : base()
        {
            LocalLang = CodeLang.GetSteamLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language);
        }


        public override GameAchievements GetAchievements(Game game)
        {
            bool GetByWeb = false;

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

                if (PluginDatabase.PluginSettings.Settings.EnableSteamWithoutWebApi || PluginDatabase.PluginSettings.Settings.SteamIsPrivate)
                {
                    AllAchievements = GetAchievementsByWeb(AppId);
                    GetByWeb = true;
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

                    bool IsOK = GetByWeb ? GetByWeb : Web.DownloadFileImageTest(AllAchievements[0].UrlLocked).GetAwaiter().GetResult();
                    if (IsOK)
                    {                        
                        AllAchievements = DataCompleted.Item1;
                        AllStats = DataCompleted.Item2;

                        Result.HaveAchivements = true;
                        Result.Total = AllAchievements.Count;
                        Result.Unlocked = AllAchievements.FindAll(x => x.DateUnlocked != null && x.DateUnlocked != default(DateTime)).Count;
                        Result.Locked = Result.Total - Result.Unlocked;
                        Result.Progression = (Result.Total != 0) ? (int)Math.Ceiling((double)(Result.Unlocked * 100 / Result.Total)) : 0;
                        Result.Items = AllAchievements;
                        Result.ItemsStats = AllStats;

                        if (Result.HaveAchivements)
                        {
                            SteamApi steamApi = new SteamApi();

                            Result.SourcesLink = new SourceLink
                            {
                                GameName = steamApi.GetGameName(AppId),
                                Name = "Steam",
                                Url = string.Format(UrlProfilById, SteamId, AppId, LocalLang)
                            };
                        }
                    }
                }
            }
            else
            {
                Common.LogDebug(true, $"GetAchievementsLocal()");

                if (PluginDatabase.PluginSettings.Settings.EnableSteamWithoutWebApi)
                {
                    logger.Warn($"Option without API key is enbaled");
                }
                else if (SteamApiKey.IsNullOrEmpty())
                {
                    logger.Warn($"No Steam API key");
                }
                else
                {
                    SteamEmulators se = new SteamEmulators(PluginDatabase.PluginSettings.Settings.LocalPath);
                    var temp = se.GetAchievementsLocal(game.Name, SteamApiKey, 0, IsManual);
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

                        if (Result.HaveAchivements)
                        {
                            SteamApi steamApi = new SteamApi();

                            Result.SourcesLink = new SourceLink
                            {
                                GameName = steamApi.GetGameName(AppId),
                                Name = "Steam",
                                Url = $"https://steamcommunity.com/stats/{AppId}/achievements"
                            };
                        }
                    }
                }
            }


            if (Result.Items.Count > 0)
            {
                if (!IsLocal && (PluginDatabase.PluginSettings.Settings.EnableSteamWithoutWebApi || PluginDatabase.PluginSettings.Settings.SteamIsPrivate))
                {
                    try
                    {
                        Result.Items = GetGlobalAchievementPercentagesForAppByWeb(AppId, Result.Items);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                    }
                }
                else
                {
                    try
                    {
                        Result.Items = GetGlobalAchievementPercentagesForApp(AppId, Result.Items);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                    }
                }
            }

            return Result;
        }

        public GameAchievements GetAchievements(Game game, int AppId)
        {
            List<Achievements> AllAchievements = new List<Achievements>();
            List<GameStats> AllStats = new List<GameStats>();
            GameAchievements Result = SuccessStory.PluginDatabase.GetDefault(game);
            Result.Items = AllAchievements;


            // Get Steam configuration if exist.
            if (!GetSteamConfig())
            {
                return Result;
            }


            if (IsLocal)
            {
                Common.LogDebug(true, $"GetAchievementsLocal()");

                if (PluginDatabase.PluginSettings.Settings.EnableSteamWithoutWebApi)
                {
                    logger.Warn($"Option without API key is enbaled");
                }
                else if (SteamApiKey.IsNullOrEmpty())
                {
                    logger.Warn($"No Steam API key");
                }
                else
                {
                    SteamEmulators se = new SteamEmulators(PluginDatabase.PluginSettings.Settings.LocalPath);
                    var temp = se.GetAchievementsLocal(game.Name, SteamApiKey, AppId, IsManual);

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
            }


            if (Result.Items.Count > 0)
            {
                Result.Items = GetGlobalAchievementPercentagesForApp(AppId, Result.Items);
            }

            return Result;
        }

        private List<Achievements> GetAchievementsByWeb(int AppId)
        {
            Common.LogDebug(true, $"GetAchievementsByWeb() for {SteamId} - {AppId}");

            List<Achievements> achievements = new List<Achievements>();
            string url = string.Empty;

            // Get data
            url = string.Format(UrlProfilById, SteamId, AppId, LocalLang);
            achievements = GetAchievementsByWeb(achievements, url);

            if (achievements.Count == 0)
            {
                url = string.Format(UrlProfilByName, SteamUser, AppId, LocalLang);
                achievements = GetAchievementsByWeb(achievements, url);
            }

            return achievements;
        }

        private List<Achievements> GetAchievementsByWeb(List<Achievements> Achievements, string Url)
        {
            Common.LogDebug(true, $"GetAchievementsHiddenByWeb()");

            string ResultWeb = string.Empty;

            try
            {
                using (var WebView = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView())
                {
                    Url = Url + "&panorama=please";
                    WebView.NavigateAndWait(Url);
                    ResultWeb = WebView.GetPageSource();


                    string CurrentUrl = WebView.GetCurrentAddress();
                    if (CurrentUrl != Url)
                    {
                        var urlParams = Url.Split('?').ToList();
                        if (urlParams.Count == 2)
                        {
                            Url = CurrentUrl + "?" + urlParams[1];
                        }

                        WebView.NavigateAndWait(Url);
                        ResultWeb = WebView.GetPageSource();
                    }


                    int index = ResultWeb.IndexOf("var g_rgAchievements = ");
                    if (index > -1)
                    {
                        ResultWeb = ResultWeb.Substring(index + "var g_rgAchievements = ".Length);

                        index = ResultWeb.IndexOf("var g_rgLeaderboards");
                        ResultWeb = ResultWeb.Substring(0, index).Trim();

                        ResultWeb = ResultWeb.Substring(0, ResultWeb.Length - 1).Trim();

                        dynamic data = Serialization.FromJson<dynamic>(ResultWeb);

                        dynamic OpenData = data["open"];
                        foreach (dynamic dd in OpenData)
                        {
                            string stringData = Serialization.ToJson(dd.Value);
                            SteamAchievementData steamAchievementData = Serialization.FromJson<SteamAchievementData>(stringData);
                            Achievements.Add(new Achievements
                            {
                                Name = WebUtility.HtmlDecode(steamAchievementData.Name.Trim()),
                                ApiName = steamAchievementData.RawName.Trim(),
                                Description = WebUtility.HtmlDecode(steamAchievementData.Desc.Trim()),
                                UrlUnlocked = steamAchievementData.IconClosed.Trim(),
                                UrlLocked = steamAchievementData.IconClosed.Trim(),
                                DateUnlocked = default(DateTime),
                                IsHidden = steamAchievementData.Hidden,
                                Percent = 100
                            });
                        }

                        dynamic ClosedData = data["closed"];
                        foreach (dynamic dd in ClosedData)
                        {
                            string stringData = Serialization.ToJson(dd.Value);
                            SteamAchievementData steamAchievementData = Serialization.FromJson<SteamAchievementData>(stringData);
                            Achievements.Add(new Achievements
                            {
                                Name = WebUtility.HtmlDecode(steamAchievementData.Name.Trim()),
                                ApiName = steamAchievementData.RawName.Trim(),
                                Description = WebUtility.HtmlDecode(steamAchievementData.Desc.Trim()),
                                UrlUnlocked = steamAchievementData.IconClosed.Trim(),
                                UrlLocked = steamAchievementData.IconClosed.Trim(),
                                DateUnlocked = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(steamAchievementData.UnlockTime),
                                IsHidden = steamAchievementData.Hidden,
                                Percent = 100
                            });
                        }
                    }
                    else
                    {
                        Common.LogDebug(true, $"No achievement data on {Url}");
                    }
                }
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false);
            }

            return Achievements;
        }


        public List<SearchResult> SearchGame(string Name)
        {
            List<SearchResult> ListSearchGames = new List<SearchResult>();

            try
            {
                string UrlSearch = @"https://store.steampowered.com/search/?term={0}";
                var DataSteamSearch = Web.DownloadStringData(string.Format(UrlSearch, WebUtility.UrlEncode(Name))).GetAwaiter().GetResult();

                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(DataSteamSearch);

                int index = 0;
                foreach (var gameElem in htmlDocument.QuerySelectorAll(".search_result_row"))
                {
                    if (index == 10)
                    {
                        break;
                    }

                    var url = gameElem.GetAttribute("href");
                    var title = gameElem.QuerySelector(".title").InnerHtml;
                    var img = gameElem.QuerySelector(".search_capsule img").GetAttribute("src");
                    var releaseDate = gameElem.QuerySelector(".search_released").InnerHtml;
                    if (gameElem.HasAttribute("data-ds-packageid"))
                    {
                        continue;
                    }

                    int gameId = 0;
                    int.TryParse(gameElem.GetAttribute("data-ds-appid"), out gameId);

                    int AchievementsCount = 0;
                    if (!PluginDatabase.PluginSettings.Settings.EnableSteamWithoutWebApi & GetSteamConfig())
                    {
                        if (gameId != 0)
                        {
                            using (dynamic steamWebAPI = WebAPI.GetInterface("ISteamUserStats", SteamApiKey))
                            {
                                KeyValue SchemaForGame = steamWebAPI.GetSchemaForGame(appid: gameId, l: LocalLang);
                                AchievementsCount = SchemaForGame.Children?.Find(x => x.Name == "availableGameStats")?.Children?.Find(x => x.Name == "achievements")?.Children?.Count ?? 0;
                            }
                        }
                    }
                    else
                    {
                        DataSteamSearch = Web.DownloadStringData(string.Format(url, WebUtility.UrlEncode(Name))).GetAwaiter().GetResult();
                        IHtmlDocument htmlDocumentDetails = parser.Parse(DataSteamSearch);

                        var AchievementsInfo = htmlDocumentDetails.QuerySelector("#achievement_block .block_title");
                        if (AchievementsInfo != null)
                        {
                            int.TryParse(Regex.Replace(AchievementsInfo.InnerHtml, "[^0-9]", ""), out AchievementsCount);
                        }
                    }

                    if (gameId != 0)
                    {
                        ListSearchGames.Add(new SearchResult
                        {
                            Name = WebUtility.HtmlDecode(title),
                            Url = url,
                            UrlImage = img,
                            AppId = gameId,
                            AchievementsCount = AchievementsCount
                        });
                    }

                    index++;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return ListSearchGames;
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

        public void SetManual()
        {
            IsManual = true;
        }


        private bool GetSteamConfig()
        {
            try
            {
                if (File.Exists(PluginDatabase.Paths.PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json"))
                {
                    dynamic SteamConfig = Serialization.FromJsonFile<dynamic>(PluginDatabase.Paths.PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json");
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


        public bool CheckIsPublic()
        {
            GetSteamConfig();

            string ProfileById = $"https://steamcommunity.com/profiles/{SteamId}/";
            string ProfileByName = $"https://steamcommunity.com/id/{SteamUser}";

            return IsProfilePublic(ProfileById) || IsProfilePublic(ProfileByName);
        }

        private static bool IsProfilePublic(string profilePageUrl)
        {
            try
            {
                string ResultWeb = HttpDownloader.DownloadString(profilePageUrl);
                IHtmlDocument HtmlDoc = new HtmlParser().Parse(ResultWeb);

                //this finds the Games link on the right side of the profile page. If that's public then so are achievements.
                var gamesPageLink = HtmlDoc.QuerySelector(@".profile_item_links a[href$=""/games/?tab=all""]");
                return gamesPageLink != null;
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false);
                return false;
            }
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
                        foreach (KeyValue AchievementsData in SchemaForGame.Children?.Find(x => x.Name == "availableGameStats").Children?.Find(x => x.Name == "achievements").Children)
                        {
                            AllAchievements.Find(x => x.ApiName.ToLower() == AchievementsData.Name.ToLower()).IsHidden = AchievementsData.Children?.Find(x => x.Name == "hidden").Value == "1";
                            AllAchievements.Find(x => x.ApiName.ToLower() == AchievementsData.Name.ToLower()).UrlUnlocked = AchievementsData.Children?.Find(x => x.Name == "icon").Value;
                            AllAchievements.Find(x => x.ApiName.ToLower() == AchievementsData.Name.ToLower()).UrlLocked = AchievementsData.Children?.Find(x => x.Name == "icongray").Value;

                            if (AllAchievements.Find(x => x.ApiName.ToLower() == AchievementsData.Name.ToLower()).IsHidden)
                            {
                                AllAchievements.Find(x => x.ApiName.ToLower() == AchievementsData.Name.ToLower()).Description = FindHiddenDescription(AppId, AllAchievements.Find(x => x.ApiName.ToLower() == AchievementsData.Name.ToLower()).Name);
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
                                logger.Info($"No Steam stats for {AppId}");
                            }
                        }
                        else
                        {
                            logger.Info($"No Steam stats for {AppId}");
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
        // TODO Utility after updated GetAchievementsByWeb() 
        private string FindHiddenDescription(int AppId, string DisplayName, bool TryByName = false)
        {
            string url = string.Empty;
            string ResultWeb = string.Empty;
            bool noData = true;

            // Get data
            if (HtmlDocument == null)
            {
                if (!TryByName)
                {
                    Common.LogDebug(true, $"FindHiddenDescription() for {SteamId} - {AppId}");

                    url = string.Format(UrlProfilById, SteamId, AppId, LocalLang);
                    try
                    {
                        using (var WebView = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView())
                        {
                            WebView.NavigateAndWait(url);
                            ResultWeb = WebView.GetPageSource();
                        }
                    }
                    catch (WebException ex)
                    {
                        Common.LogError(ex, false, $"Error on FindHiddenDescription()");
                    }
                }
                else
                {
                    Common.LogDebug(true, $"FindHiddenDescription() for {SteamUser} - {AppId}");

                    url = string.Format(UrlProfilByName, SteamUser, AppId, LocalLang);
                    try
                    {
                        using (var WebView = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView())
                        {
                            WebView.NavigateAndWait(url);
                            ResultWeb = WebView.GetPageSource();
                        }
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
                    try
                    {
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

        private List<Achievements> GetGlobalAchievementPercentagesForAppByWeb(int AppId, List<Achievements> AllAchievements)
        {
            string url = string.Empty;
            string ResultWeb = string.Empty;
            bool noData = true;
            HtmlDocument = null;

            // Get data
            if (HtmlDocument == null)
            {
                Common.LogDebug(true, $"GetGlobalAchievementPercentagesForAppByWeb() for {SteamId} - {AppId}");

                url = string.Format(UrlAchievements, AppId, LocalLang);
                try
                {
                    ResultWeb = HttpDownloader.DownloadString(url, Encoding.UTF8);
                }
                catch (WebException ex)
                {
                    Common.LogError(ex, false);
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

                if (noData)
                {
                    return AllAchievements;
                }
            }


            // Find the achievement description
            if (HtmlDocument != null)
            {
                foreach (var achieveRow in HtmlDocument.QuerySelectorAll("div.achieveRow"))
                {
                    try
                    {
                        string Name = string.Empty;
                        if (achieveRow.QuerySelector("h3") != null)
                        {
                            Name = WebUtility.HtmlDecode(achieveRow.QuerySelector("h3").InnerHtml.Trim());
                        }

                        float Percent = 0;
                        if (achieveRow.QuerySelector(".achievePercent") != null)
                        {
                            Percent = float.Parse(achieveRow.QuerySelector(".achievePercent").InnerHtml.Replace("%", string.Empty).Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));
                        }

                        AllAchievements.Find(x => x.Name.ToLower() == Name.ToLower()).Percent = Percent;
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                    }
                }
            }

            return AllAchievements;
        }
    }
}
