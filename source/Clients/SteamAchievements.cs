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
using CommonPlayniteShared.Common.Web;
using System.Text.RegularExpressions;
using CommonPluginsShared.Models;
using PlayniteTools = CommonPluginsShared.PlayniteTools;
using CommonPluginsShared.Extensions;
using System.Threading;
using CommonPluginsStores.Steam;
using CommonPluginsStores.Steam.Models;
using CommonPlayniteShared.Common;
using AngleSharp.Dom;
using CommonPluginsStores.Models;
using System.Collections.ObjectModel;

namespace SuccessStory.Clients
{
    public class SteamAchievements : GenericAchievements
    {
        private SteamApi SteamApi => SuccessStory.SteamApi;

        private IHtmlDocument HtmlDocument { get; set; } = null;

        private bool IsLocal { get; set; } = false;
        private bool IsManual { get; set; } = false;

        #region Url
        private static string UrlBase => @"https://steamcommunity.com";
        private static string UrlProfil => UrlBase + @"/my/profile";
        private static string UrlProfilById => UrlBase + @"/profiles/{0}/stats/{1}?tab=achievements&l={2}";
        private static string UrlProfilByName => UrlBase + @"/id/{0}/stats/{1}?tab=achievements&l={2}";

        private static string UrlAchievements => UrlBase + @"/stats/{0}/achievements/?l={1}";

        private static string UrlSearch => @"https://store.steampowered.com/search/?term={0}&ignore_preferences=1&category1=998&ndl=1";
        #endregion


        public SteamAchievements() : base("Steam", CodeLang.GetSteamLang(API.Instance.ApplicationSettings.Language))
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();
            List<GameStats> AllStats = new List<GameStats>();

            uint appId = 0;


            // Get Steam configuration if exist.
            if (!IsConfigured())
            {
                return gameAchievements;
            }

            Common.LogDebug(true, $"Steam.GetAchievements() - IsLocal : {IsLocal}, IsManual : {IsManual}, HasApiKey: {!SteamApi.CurrentAccountInfos.ApiKey.IsNullOrEmpty()}, IsPrivate: {SteamApi.CurrentAccountInfos.IsPrivate}");
            if (!IsLocal)
            {
                ObservableCollection<GameAchievement> steamAchievements = SteamApi.GetAchievements(game.GameId, SteamApi.CurrentAccountInfos);
                if (steamAchievements?.Count > 0 && uint.TryParse(game.GameId, out appId))
                {
                    // Check private game
                    if (steamAchievements.Count(x => !(x.DateUnlocked == default || x.DateUnlocked == null || x.DateUnlocked.ToString().Contains("0001"))) == 0 && !PluginDatabase.PluginSettings.Settings.SteamApiSettings.UseAuth)
                    {
                        Logger.Info($"No unlocked achievement, check if the game is private - {game.Name} - {game.GameId}");
                        bool gameIsPrivate = SteamApi.CheckGameIsPrivate(appId, SteamApi.CurrentAccountInfos);
                        if (gameIsPrivate)
                        {
                            API.Instance.Notifications.Add(new NotificationMessage(
                                $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-PrivateGame-{game.GameId}",
                                $"{PluginDatabase.PluginName}\r\n{string.Format(ResourceProvider.GetString("LOCSuccessStoryNotificationsSteamGamePrivate"), game.Name, ResourceProvider.GetString("LOCCommonIsPrivate"))}",
                                NotificationType.Error,
                                () => PlayniteTools.ShowPluginSettings(PlayniteTools.ExternalPlugin.SuccessStory)
                            ));
                            Logger.Warn($"Steam game is private - {game.Name} - {game.GameId}");
                        }
                    }

                    Logger.Info($"SteamApi.GetAchievements({game.Name}, {game.GameId})");

                    AllAchievements = steamAchievements.Select(x => new Achievements
                    {
                        ApiName = x.Id,
                        Name = x.Name,
                        CategoryIcon = x.CategoryIcon,
                        CategoryOrder = x.CategoryOrder,
                        Category = x.Category.IsNullOrEmpty() ? ResourceProvider.GetString("LOCSuccessStoryBaseGame") : x.Category,
                        Description = x.Description,
                        UrlUnlocked = x.UrlUnlocked,
                        UrlLocked = x.UrlLocked,
                        DateUnlocked = x.DateUnlocked,
                        IsHidden = x.IsHidden,
                        Percent = x.Percent,
                        GamerScore = x.GamerScore
                    }).ToList();

                    gameAchievements.Items = AllAchievements;
                    gameAchievements.ItemsStats = SteamApi.GetUsersStats(appId, SteamApi.CurrentAccountInfos)?.Select(x => new GameStats
                    {
                        Name = x.Name,
                        Value = double.Parse(x.Value
                                .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                                .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator))
                    })?.ToList() ?? new List<GameStats>();
                }

                // Set source link
                if (gameAchievements.HasAchievements)
                {
                    gameAchievements.SourcesLink = new SourceLink
                    {
                        GameName = SteamApi.GetGameName(appId),
                        Name = ClientName,
                        Url = string.Format(UrlProfilById, SteamApi.CurrentAccountInfos.UserId, game.GameId, LocalLang)
                    };
                }

                // Set progression
                if (gameAchievements.HasAchievements && gameAchievements.Items.Where(x => x.Progression?.Max != 0)?.Count() != 0)
                {
                    gameAchievements.Items = GetProgressionByWeb(gameAchievements.Items, string.Format(UrlProfilById, SteamApi.CurrentAccountInfos.UserId, game.GameId, LocalLang));
                }
            }
            else
            {
                if (IsManual)
                {
                    appId = SteamApi.GetAppId(game.Name);
                    gameAchievements = GetManual(appId, game);
                }

                if (!gameAchievements.HasAchievements)
                {
                    SteamEmulators se = new SteamEmulators(PluginDatabase.PluginSettings.Settings.LocalPath);
                    GameAchievements temp = se.GetAchievementsLocal(game, SteamApi.CurrentAccountInfos?.ApiKey, 0, IsManual);
                    appId = se.GetAppId();

                    if (temp.Items.Count > 0)
                    {
                        for (int i = 0; i < temp.Items.Count; i++)
                        {
                            AllAchievements.Add(new Achievements
                            {
                                Name = temp.Items[i].Name,
                                ApiName = temp.Items[i].ApiName,
                                Description = temp.Items[i].Description,
                                UrlUnlocked = temp.Items[i].UrlUnlocked,
                                UrlLocked = temp.Items[i].UrlLocked,
                                DateUnlocked = temp.Items[i].DateUnlocked
                            });
                        }

                        gameAchievements.Items = AllAchievements;
                        gameAchievements.ItemsStats = temp.ItemsStats;
                    }
                }

                // Set source link
                if (gameAchievements.HasAchievements)
                {
                    gameAchievements.SourcesLink = new SourceLink
                    {
                        GameName = SteamApi.GetGameName(appId),
                        Name = ClientName,
                        Url = UrlBase + $"/stats/{appId}/achievements"
                    };
                }
            }

            SetRarity(appId, gameAchievements);
            //SetMissingDescription(appId, gameAchievements);
            gameAchievements.SetRaretyIndicator();

            return gameAchievements;
        }

        public GameAchievements GetAchievements(Game game, uint appId)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();

            // Get Steam configuration if exist.
            if (!IsConfigured())
            {
                return gameAchievements;
            }
            Common.LogDebug(true, $"Steam.GetAchievements() - IsLocal : {IsLocal}, IsManual : {IsManual}, HasApiKey: {!SteamApi.CurrentAccountInfos.ApiKey.IsNullOrEmpty()}, IsPrivate: {SteamApi.CurrentAccountInfos.IsPrivate}");
            Logger.Info($"GetAchievements({game.Name}, {appId})");

            if (IsManual)
            {
                gameAchievements = GetManual(appId, game);
            }

            if (IsLocal && !gameAchievements.HasAchievements)
            {
                if (SteamApi.CurrentAccountInfos.ApiKey.IsNullOrEmpty())
                {
                    Logger.Warn($"No Steam API key");
                }

                SteamEmulators se = new SteamEmulators(PluginDatabase.PluginSettings.Settings.LocalPath);
                GameAchievements temp = se.GetAchievementsLocal(game, SteamApi.CurrentAccountInfos.ApiKey, appId, IsManual);

                if (temp.Items.Count > 0)
                {
                    for (int i = 0; i < temp.Items.Count; i++)
                    {
                        AllAchievements.Add(new Achievements
                        {
                            Name = temp.Items[i].Name,
                            ApiName = temp.Items[i].ApiName,
                            Description = temp.Items[i].Description,
                            UrlUnlocked = temp.Items[i].UrlUnlocked,
                            UrlLocked = temp.Items[i].UrlLocked,
                            DateUnlocked = temp.Items[i].DateUnlocked
                        });
                    }

                    gameAchievements.Items = AllAchievements;
                }
            }

            // Set source link
            if (gameAchievements.HasAchievements)
            {
                string gameName = SteamApi.GetGameName(appId);
                if (gameName.IsNullOrEmpty())
                {
                    gameName = SteamApi.GetGameInfos(appId.ToString(), null)?.Name;
                }

                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = gameName,
                    Name = "Steam",
                    Url = UrlBase + $"/stats/{appId}/achievements"
                };
            }

            SetRarity(appId, gameAchievements);
            //SetMissingDescription(appId, gameAchievements);
            gameAchievements.SetRaretyIndicator();

            return gameAchievements;
        }


        private GameAchievements GetManual(uint appId, Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();

            if (appId == 0)
            {
                appId = SteamApi.GetAppId(game.Name);
            }
            ObservableCollection<GameAchievement> steamAchievements = SteamApi.GetAchievements(appId.ToString(), null);

            if (steamAchievements?.Count > 0)
            {
                Logger.Info($"SteamApi.GetAchievements()");

                AllAchievements = steamAchievements.Select(x => new Achievements
                {
                    ApiName = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    UrlUnlocked = x.UrlUnlocked,
                    UrlLocked = x.UrlLocked,
                    DateUnlocked = x.DateUnlocked,
                    IsHidden = x.IsHidden,
                    Percent = x.Percent
                }).ToList();
                gameAchievements.Items = AllAchievements;
                gameAchievements.IsManual = true;
            }

            return gameAchievements;
        }


        public void SetRarity(uint appId, GameAchievements gameAchievements)
        {
            ObservableCollection<GameAchievement> steamAchievements = SteamApi.GetAchievements(appId.ToString(), null);
            steamAchievements.ForEach(x =>
            {
                Achievements found = gameAchievements.Items?.Find(y => y.ApiName.IsEqual(x.Id));
                if (found != null)
                {
                    found.GamerScore = x.GamerScore;
                }
                else
                {

                }
            });
        }

        /*
        private void SetMissingDescription(uint appId, GameAchievements gameAchievements)
        {
            if (gameAchievements.HasAchievements && gameAchievements.Items?.Where(x => !x.Description.IsNullOrEmpty())?.Count() == 0)
            {
                gameAchievements.Items.ForEach(x =>
                {
                    if (x.IsHidden && x.Description.IsNullOrEmpty())
                    {
                        x.Description = FindHiddenDescription(appId, x.Name);
                    }
                });

                ExophaseAchievements exophaseAchievements = new ExophaseAchievements();
                exophaseAchievements.SetMissingDescription(gameAchievements, Services.SuccessStoryDatabase.AchievementSource.Steam);
            }
        }
        */


        #region Configuration
        // TODO Rewrite
        public override bool ValidateConfiguration()
        {
            if (PlayniteTools.IsDisabledPlaynitePlugins("SteamLibrary"))
            {
                ShowNotificationPluginDisable(ResourceProvider.GetString("LOCSuccessStoryNotificationsSteamDisabled"));
                return false;
            }
            else
            {
                if (CachedConfigurationValidationResult == null)
                {
                    if (!IsConfigured())
                    {
                        ShowNotificationPluginNoConfiguration(ResourceProvider.GetString("LOCSuccessStoryNotificationsSteamBadConfig"));
                        CachedConfigurationValidationResult = false;
                    }

                    if (SteamApi.CurrentAccountInfos.IsPrivate && !IsConnected())
                    {
                        ResetCachedIsConnectedResult();
                        Thread.Sleep(2000);
                        if (SteamApi.CurrentAccountInfos.IsPrivate && !IsConnected())
                        {
                            ShowNotificationPluginNoAuthenticate(ResourceProvider.GetString("LOCSuccessStoryNotificationsSteamNoAuthenticate"), PlayniteTools.ExternalPlugin.SuccessStory);
                            CachedConfigurationValidationResult = false;
                        }
                    }


                    if (CachedConfigurationValidationResult == null)
                    {
                        CachedConfigurationValidationResult = true;
                    }

                    if (!(bool)CachedConfigurationValidationResult)
                    {
                        ShowNotificationPluginErrorMessage();
                    }
                }
                else if (!(bool)CachedConfigurationValidationResult)
                {
                    ShowNotificationPluginErrorMessage();
                }

                return (bool)CachedConfigurationValidationResult;
            }
        }

        public override bool IsConnected()
        {
            if (CachedIsConnectedResult == null)
            {
                if (IsConfigured())
                {
                    CachedIsConnectedResult = SteamApi.IsUserLoggedIn;
                }
            }

            return (bool)CachedIsConnectedResult;
        }

        public override bool IsConfigured()
        {
            return SteamApi.IsConfigured();
        }

        public override bool EnabledInSettings()
        {
            return IsLocal ? PluginDatabase.PluginSettings.Settings.EnableLocal : PluginDatabase.PluginSettings.Settings.EnableSteam;
        }
        #endregion


        public void SetLocal()
        {
            IsLocal = true;
        }

        public void SetManual()
        {
            IsManual = true;
        }


        #region Steam
        public List<SearchResult> SearchGame(string Name)
        {
            List<SearchResult> ListSearchGames = new List<SearchResult>();

            string Url = string.Empty;
            try
            {
                Url = string.Format(UrlSearch, WebUtility.UrlEncode(Name));
                string DataSteamSearch = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                IHtmlDocument htmlDocument = new HtmlParser().Parse(DataSteamSearch);

                int index = 0;
                foreach (IElement gameElem in htmlDocument.QuerySelectorAll(".search_result_row"))
                {
                    if (index == 10)
                    {
                        break;
                    }

                    _ = uint.TryParse(gameElem.GetAttribute("data-ds-appid"), out uint appId);
                    string url = gameElem.GetAttribute("href");
                    string title = gameElem.QuerySelector(".title").InnerHtml;
                    string img = gameElem.QuerySelector(".search_capsule img").GetAttribute("src");
                    string releaseDate = gameElem.QuerySelector(".search_released").InnerHtml;
                    if (gameElem.HasAttribute("data-ds-packageid"))
                    {
                        continue;
                    }

                    ObservableCollection<GameAchievement> steamAchievements = SteamApi.GetAchievementsSchema(appId);
                    int AchievementsCount = steamAchievements?.Count() ?? 0;

                    if (appId > 0)
                    {
                        ListSearchGames.Add(new SearchResult
                        {
                            Name = WebUtility.HtmlDecode(title),
                            Url = url,
                            UrlImage = img,
                            AppId = appId,
                            AchievementsCount = AchievementsCount
                        });
                    }

                    index++;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error with SearchGame{Name} on {Url}", true, PluginDatabase.PluginName);
            }

            return ListSearchGames;
        }


        /*
        // TODO Use "profileurl" in "ISteamUser"
        // TODO Utility after updated GetAchievementsByWeb()
        private string FindHiddenDescription(uint AppId, string DisplayName, bool TryByName = false)
        {
            string url = string.Empty;
            string ResultWeb = string.Empty;
            bool noData = true;

            // Get data
            if (HtmlDocument == null)
            {
                if (!TryByName)
                {
                    Common.LogDebug(true, $"FindHiddenDescription() for {SteamApi.CurrentAccountInfos.UserId} - {AppId}");
                    url = string.Format(UrlProfilById, SteamApi.CurrentAccountInfos.UserId, AppId, LocalLang);
                    try
                    {
                        List<HttpCookie> cookies = SteamApi.GetStoredCookies();
                        ResultWeb = Web.DownloadStringData(url, cookies).GetAwaiter().GetResult();
                    }
                    catch (WebException ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }
                else
                {
                    Common.LogDebug(true, $"FindHiddenDescription() for {SteamApi.CurrentAccountInfos.Pseudo} - {AppId}");
                    url = string.Format(UrlProfilByName, SteamApi.CurrentAccountInfos.Pseudo, AppId, LocalLang);
                    try
                    {
                        List<HttpCookie> cookies = SteamApi.GetStoredCookies();
                        ResultWeb = Web.DownloadStringData(url, cookies).GetAwaiter().GetResult();
                    }
                    catch (WebException ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
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
                foreach (IElement achieveRow in HtmlDocument.QuerySelectorAll("div.achieveRow"))
                {
                    try
                    {
                        if (achieveRow.QuerySelector("h3").InnerHtml.IsEqual(DisplayName))
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
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }
            }

            return string.Empty;
        }
        */


        private List<Achievements> GetProgressionByWeb(List<Achievements> Achievements, string Url, bool isRetry = false)
        {
            string ResultWeb = string.Empty;
            try
            {
                Url += "&panorama=please";
                List<HttpCookie> cookies = SteamApi.GetStoredCookies();
                ResultWeb = Web.DownloadStringData(Url, cookies, string.Empty, true).GetAwaiter().GetResult();

                int index = ResultWeb.IndexOf("var g_rgAchievements = ");
                if (index > -1)
                {
                    ResultWeb = ResultWeb.Substring(index + "var g_rgAchievements = ".Length);

                    index = ResultWeb.IndexOf("var g_rgLeaderboards");
                    ResultWeb = ResultWeb.Substring(0, index).Trim();

                    ResultWeb = ResultWeb.Substring(0, ResultWeb.Length - 1).Trim();

                    dynamic dataByWeb = Serialization.FromJson<dynamic>(ResultWeb);
                    if (dataByWeb == null)
                    {
                        Logger.Warn($"No g_rgAchievements data");
                        return Achievements;
                    }

                    dynamic OpenData = dataByWeb["open"];
                    foreach (dynamic dd in OpenData)
                    {
                        string stringData = Serialization.ToJson(dd.Value);
                        SteamAchievementData steamAchievementData = Serialization.FromJson<SteamAchievementData>(stringData);

                        if (!(steamAchievementData.Progress is string))
                        {
                            double.TryParse(steamAchievementData.Progress["min_val"].ToString(), out double min);
                            double.TryParse(steamAchievementData.Progress["max_val"].ToString(), out double max);
                            double.TryParse(steamAchievementData.Progress["currentVal"].ToString(), out double val);

                            Achievements found = Achievements.Find(x => x.ApiName.IsEqual(steamAchievementData.RawName));
                            if (found != null)
                            {
                                found.Progression = new AchProgression
                                {
                                    Min = min,
                                    Max = max,
                                    Value = val,
                                };
                            }
                        }
                    }

                    dynamic ClosedData = dataByWeb["closed"];
                    foreach (dynamic dd in ClosedData)
                    {
                        string stringData = Serialization.ToJson(dd.Value);
                        SteamAchievementData steamAchievementData = Serialization.FromJson<SteamAchievementData>(stringData);

                        if (!(steamAchievementData.Progress is string))
                        {
                            double.TryParse(steamAchievementData.Progress["min_val"].ToString(), out double min);
                            double.TryParse(steamAchievementData.Progress["max_val"].ToString(), out double max);
                            double.TryParse(steamAchievementData.Progress["currentVal"].ToString(), out double val);

                            Achievements found = Achievements.Find(x => x.ApiName.IsEqual(steamAchievementData.RawName));
                            if (found != null)
                            {
                                found.Progression = new AchProgression
                                {
                                    Min = min,
                                    Max = max,
                                    Value = val,
                                };
                            }
                        }
                    }
                }
                else if (ResultWeb.IndexOf("achieveRow") > -1)
                {
                    IHtmlDocument htmlDocument = new HtmlParser().Parse(ResultWeb);

                    htmlDocument = new HtmlParser().Parse(ResultWeb);
                    foreach (IElement el in htmlDocument.QuerySelectorAll(".achieveRow"))
                    {
                        string UrlUnlocked = el.QuerySelector(".achieveImgHolder img")?.GetAttribute("src") ?? string.Empty;
                        string Name = el.QuerySelector(".achieveTxtHolder h3").InnerHtml;
                        string Description = el.QuerySelector(".achieveTxtHolder h5").InnerHtml;

                        foreach (IElement progress in el.QuerySelectorAll(".progressText"))
                        {
                            string[] data = progress.InnerHtml.Split('/');
                            double min = 0;
                            _ = double.TryParse(data[1].Trim().Replace(",", string.Empty), out double max);
                            _ = double.TryParse(data[0].Trim().Replace(",", string.Empty), out double val);

                            Achievements found = Achievements.Find(x => x.Name.IsEqual(Name));
                            if (found != null)
                            {
                                found.Progression = new AchProgression
                                {
                                    Min = min,
                                    Max = max,
                                    Value = val,
                                };
                            }
                        }
                    }
                }
                else
                {
                    Common.LogDebug(true, $"No achievement data on {Url}");
                    if (!isRetry)
                    {
                        return GetProgressionByWeb(Achievements, Url, true);
                    }
                }
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return Achievements;
        }


        public static SteamAchievements GetLocalSteamAchievementsProvider()
        {
            SteamAchievements provider = new SteamAchievements();
            provider.SetLocal();
            return provider;
        }
        #endregion


        #region Errors
        public virtual void ShowNotificationPluginNoPublic(string Message)
        {
            Logger.Warn($"{ClientName} user is not public");

            API.Instance.Notifications.Add(new NotificationMessage(
                $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-nopublic",
                $"{PluginDatabase.PluginName}\r\n{Message}",
                NotificationType.Error,
                () =>
                {
                    ResetCachedConfigurationValidationResult();
                    ResetCachedIsConnectedResult();
                    _ = PluginDatabase.Plugin.OpenSettingsView();
                }
            ));
        }
        #endregion
    }
}
