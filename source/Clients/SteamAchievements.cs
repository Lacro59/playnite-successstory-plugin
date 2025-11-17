using Playnite.SDK;
using CommonPluginsShared;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Playnite.SDK.Models;
using System.Globalization;
using CommonPluginsShared.Models;
using PlayniteTools = CommonPluginsShared.PlayniteTools;
using CommonPluginsShared.Extensions;
using System.Threading;
using CommonPluginsStores.Steam;
using AngleSharp.Dom;
using CommonPluginsStores.Models;
using System.Collections.ObjectModel;
using static CommonPluginsShared.PlayniteTools;

namespace SuccessStory.Clients
{
    public class SteamAchievements : GenericAchievements
    {
        /// <summary>
        /// Gets the Steam API instance from the plugin.
        /// </summary>
        private SteamApi SteamApi => SuccessStory.SteamApi;

        /// <summary>
        /// Gets or sets the last loaded HTML document (if any).
        /// </summary>
        private IHtmlDocument HtmlDocument { get; set; } = null;

        /// <summary>
        /// Indicates whether local achievement data should be used.
        /// </summary>
        private bool IsLocal { get; set; } = false;

        /// <summary>
        /// Indicates whether manual achievement data loading is enabled.
        /// </summary>
        private bool IsManual { get; set; } = false;

        #region Urls

        private static string UrlBase => @"https://steamcommunity.com";
        private static string UrlProfilById => UrlBase + @"/profiles/{0}/stats/{1}?tab=achievements&l={2}";
        private static string UrlSearch => @"https://store.steampowered.com/search/?term={0}&ignore_preferences=1&category1=998&ndl=1";

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SteamAchievements"/> class.
        /// </summary>
        public SteamAchievements() : base("Steam", CodeLang.GetSteamLang(API.Instance.ApplicationSettings.Language))
        {
        }

        /// <summary>
        /// Retrieves the achievements for the specified game.
        /// This may involve using the Steam API or loading local/manual data.
        /// </summary>
        /// <param name="game">The game for which achievements are retrieved.</param>
        /// <returns>A <see cref="GameAchievements"/> object containing the achievements.</returns>
        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Models.Achievement> allAchievements = new List<Models.Achievement>();
            List<GameStats> allStats = new List<GameStats>();

            uint appId = 0;

            // Get Steam configuration if exist.
            if (!IsConfigured())
            {
                return gameAchievements;
            }

            Common.LogDebug(true, $"Steam.GetAchievements() - IsLocal : {IsLocal}, IsManual : {IsManual}, HasApiKey: {!SteamApi.CurrentAccountInfos.ApiKey.IsNullOrEmpty()}, IsPrivate: {SteamApi.CurrentAccountInfos.IsPrivate}");
            if (!IsLocal)
            {
                Logger.Info($"SteamApi.GetAchievements({game.Name}, {game.GameId})");
                ObservableCollection<GameAchievement> steamAchievements = SteamApi.GetAchievements(game.GameId, SteamApi.CurrentAccountInfos);

                if (steamAchievements?.Count > 0 && uint.TryParse(game.GameId, out appId))
                {
                    // Check private game
                    if (steamAchievements.Count(x => !(x.DateUnlocked == default || x.DateUnlocked == null || x.DateUnlocked.ToString().Contains("0001"))) == 0 && !PluginDatabase.PluginSettings.Settings.SteamStoreSettings.UseAuth)
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

                    allAchievements = steamAchievements.Select(x => new Models.Achievement
                    {
                        ApiName = x.Id,
                        Name = x.Name,
                        CategoryIcon = x.CategoryIcon,
                        CategoryOrder = x.CategoryOrder,
                        Category = x.Category.IsNullOrEmpty() ? ResourceProvider.GetString("LOCSuccessStoryBaseGame") : x.Category,
                        Description = x.Description,
                        UrlUnlocked = x.UrlUnlocked,
                        UrlLocked = x.UrlLocked,
                        DateUnlocked = x.DateUnlocked.ToString().Contains(default(DateTime).ToString()) ? (DateTime?) null : x.DateUnlocked,
                        IsHidden = x.IsHidden,
                        Percent = x.Percent,
                        GamerScore = x.GamerScore
                    }).ToList();

                    gameAchievements.Items = allAchievements;
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
                    gameAchievements.Items = GetProgressionByWeb(gameAchievements.Items, game);
                }
            }
            else
            {
                if (IsManual)
                {
                    appId = SteamApi.GetAppId(game);
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
                            allAchievements.Add(new Models.Achievement
                            {
                                Name = temp.Items[i].Name,
                                ApiName = temp.Items[i].ApiName,
                                Description = temp.Items[i].Description,
                                UrlUnlocked = temp.Items[i].UrlUnlocked,
                                UrlLocked = temp.Items[i].UrlLocked,
                                DateUnlocked = temp.Items[i].DateUnlocked
                            });
                        }

                        gameAchievements.Items = allAchievements;
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
            gameAchievements.SetRaretyIndicator();

            return gameAchievements;
        }

        /// <summary>
        /// Retrieves achievements for a specific game and app ID.
        /// Used for cases where the game ID may be missing or ambiguous.
        /// </summary>
        /// <param name="game">The Playnite game object.</param>
        /// <param name="appId">The Steam App ID of the game.</param>
        /// <returns>Game achievements for the provided App ID.</returns>
        public GameAchievements GetAchievements(Game game, uint appId)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Models.Achievement> allAchievements = new List<Models.Achievement>();

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
                        allAchievements.Add(new Models.Achievement
                        {
                            Name = temp.Items[i].Name,
                            ApiName = temp.Items[i].ApiName,
                            Description = temp.Items[i].Description,
                            UrlUnlocked = temp.Items[i].UrlUnlocked,
                            UrlLocked = temp.Items[i].UrlLocked,
                            DateUnlocked = temp.Items[i].DateUnlocked
                        });
                    }

                    gameAchievements.Items = allAchievements;
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
                if (gameName.IsNullOrEmpty())
                {
                    gameName = SteamApi.GetGameName(appId);
                }

                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = gameName,
                    Name = "Steam",
                    Url = UrlBase + $"/stats/{appId}/achievements"
                };
            }

            SetRarity(appId, gameAchievements);
            gameAchievements.SetRaretyIndicator();

            return gameAchievements;
        }

        /// <summary>
        /// Loads achievements from Steam without authentication (manual schema parsing).
        /// </summary>
        /// <param name="appId">Steam App ID of the game.</param>
        /// <param name="game">The game object.</param>
        /// <returns>Game achievements parsed manually.</returns>
        private GameAchievements GetManual(uint appId, Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Models.Achievement> allAchievements = new List<Models.Achievement>();

            if (appId == 0)
            {
                appId = SteamApi.GetAppId(game);
            }
            ObservableCollection<GameAchievement> steamAchievements = SteamApi.GetAchievementsSchema(appId.ToString()).Item2;

            if (steamAchievements?.Count > 0)
            {
                Logger.Info($"SteamApi.GetAchievements()");

                allAchievements = steamAchievements.Select(x => new Models.Achievement
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
                gameAchievements.Items = allAchievements;
                gameAchievements.IsManual = true;
            }

            return gameAchievements;
        }

        /// <summary>
        /// Sets rarity information and gamer score for the given achievements.
        /// </summary>
        /// <param name="appId">The Steam App ID.</param>
        /// <param name="gameAchievements">The achievements to update.</param>
        public void SetRarity(uint appId, GameAchievements gameAchievements)
        {
            ObservableCollection<GameAchievement> steamAchievements = SteamApi.GetAchievementsSchema(appId.ToString()).Item2;
            steamAchievements.ForEach(x =>
            {
                Models.Achievement found = gameAchievements.Items?.Find(y => y.ApiName.IsEqual(x.Id));
                if (found != null)
                {
                    found.Percent = x.Percent;
                    found.GamerScore = x.GamerScore;
                }
            });

            PluginDatabase.AddOrUpdate(gameAchievements);
        }

        #region Configuration

        /// <summary>
        /// Validates that the plugin is properly configured and the Steam account is connected.
        /// </summary>
        /// <returns>True if valid, otherwise false.</returns>
        // TODO Rewrite
        public override bool ValidateConfiguration()
        {
            if (!PluginDatabase.PluginSettings.Settings.PluginState.SteamIsEnabled)
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
                        ShowNotificationPluginNoConfiguration();
                        CachedConfigurationValidationResult = false;
                    }

                    if (SteamApi.CurrentAccountInfos.IsPrivate && !IsConnected())
                    {
                        ResetCachedIsConnectedResult();
                        Thread.Sleep(2000);
                        if (SteamApi.CurrentAccountInfos.IsPrivate && !IsConnected())
                        {
                            ShowNotificationPluginNoAuthenticate(ExternalPlugin.SuccessStory);
                            CachedConfigurationValidationResult = false;
                        }
                    }


                    if (CachedConfigurationValidationResult == null)
                    {
                        CachedConfigurationValidationResult = true;
                    }

                    if (!(bool)CachedConfigurationValidationResult)
                    {
                        ShowNotificationPluginErrorMessage(ExternalPlugin.SuccessStory);
                    }
                }
                else if (!(bool)CachedConfigurationValidationResult)
                {
                    ShowNotificationPluginErrorMessage(ExternalPlugin.SuccessStory);
                }

                return (bool)CachedConfigurationValidationResult;
            }
        }

        /// <summary>
        /// Indicates whether the user is currently connected to their Steam account.
        /// </summary>
        /// <returns>True if connected, otherwise false.</returns>
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

        /// <summary>
        /// Indicates whether the plugin has been configured for Steam usage.
        /// </summary>
        /// <returns>True if configured, otherwise false.</returns>
        public override bool IsConfigured()
        {
            return SteamApi.IsConfigured();
        }

        /// <summary>
        /// Returns whether Steam achievements are enabled in plugin settings.
        /// </summary>
        /// <returns>True if enabled, otherwise false.</returns>
        public override bool EnabledInSettings()
        {
            return IsLocal ? PluginDatabase.PluginSettings.Settings.EnableLocal : PluginDatabase.PluginSettings.Settings.EnableSteam;
        }

        #endregion

        /// <summary>
        /// Forces achievement loading to use local files (for emulators or offline usage).
        /// </summary>
        public void SetLocal()
        {
            IsLocal = true;
        }

        /// <summary>
        /// Enables manual parsing of achievement schemas.
        /// </summary>
        public void SetManual()
        {
            IsManual = true;
        }

        #region Steam

        /// <summary>
        /// Searches the Steam store for a game by name.
        /// Returns a maximum of 10 results.
        /// </summary>
        /// <param name="name">Game name to search.</param>
        /// <returns>List of search results containing app ID and metadata.</returns>
        public List<SearchResult> SearchGame(string name)
        {
            List<SearchResult> searchGames = new List<SearchResult>();

            string searchUrl = string.Empty;
            try
            {
                searchUrl = string.Format(UrlSearch, WebUtility.UrlEncode(name));
                string response = Web.DownloadStringData(searchUrl).GetAwaiter().GetResult();
                IHtmlDocument htmlDocument = new HtmlParser().Parse(response);

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

                    ObservableCollection<GameAchievement> steamAchievements = SteamApi.GetAchievementsSchema(appId.ToString()).Item2;
                    int AchievementsCount = steamAchievements?.Count() ?? 0;

                    if (appId > 0)
                    {
                        searchGames.Add(new SearchResult
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
                Common.LogError(ex, false, $"Error with SearchGame {name} on {searchUrl}", true, PluginDatabase.PluginName);
            }

            return searchGames;
        }

        /// <summary>
        /// Tries to enrich achievements with progression info scraped from the Steam community web page.
        /// </summary>
        /// <param name="achievements">Achievements to enrich.</param>
        /// <param name="game">The game being processed.</param>
        /// <returns>List of updated achievements.</returns>
        private List<Models.Achievement> GetProgressionByWeb(List<Models.Achievement> achievements, Game game)
        {
            var achievementsProgression = SteamApi.GetProgressionByWeb(uint.Parse(game.GameId), SteamApi.CurrentAccountInfos);
            foreach(var achievement in achievements)
            {
                var achievementProgression = achievementsProgression.FirstOrDefault(x => x.Id.IsEqual(achievement.ApiName));
                if (achievementProgression != null)
                {
                    achievement.Progression = new AchProgression
                    {
                        Value = achievementProgression.Value,
                        Max = achievementProgression.Max
                    };
                }
            }

            return achievements;
        }

        /// <summary>
        /// Gets a Steam achievements provider pre-configured for local usage.
        /// </summary>
        /// <returns>A <see cref="SteamAchievements"/> instance set to local mode.</returns>
        public static SteamAchievements GetLocalSteamAchievementsProvider()
        {
            SteamAchievements provider = new SteamAchievements();
            provider.SetLocal();
            return provider;
        }

        #endregion
    }
}