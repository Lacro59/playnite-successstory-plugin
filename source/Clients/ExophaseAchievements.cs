using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace SuccessStory.Clients
{
    public class ExophaseAchievements : GenericAchievements
    {
        #region Url
        private string UrlApi => @"https://api.exophase.com";
        private string UrlExophaseSearch => UrlApi + "/public/archive/games?q={0}&sort=added";
        private string UrlExophaseSearchPlatform => UrlApi + "/public/archive/platform/{1}?q={0}&sort=added";

        private string UrlExophase => @"https://www.exophase.com";
        private string UrlExophaseLogin => $"{UrlExophase}/login";
        private string UrlExophaseLogout => $"{UrlExophase}/logout";
        private string UrlExophaseAccount => $"{UrlExophase}/account";
        #endregion

        public static List<string> Platforms = new List<string>
        {
            ResourceProvider.GetString("LOCAll"),
            "Apple",
            "Blizzard",
            "Electronic Arts",
            "Epic",
            "GOG",
            "Google Play",
            "Nintendo",
            "PSN",
            "Retro",
            "Stadia",
            "Steam",
            "Ubisoft",
            "Xbox"
        };


        public ExophaseAchievements() : base("Exophase")
        {
        }


        public override GameAchievements GetAchievements(Game game)
        {
            throw new NotImplementedException();
        }

        public GameAchievements GetAchievements(Game game, string url)
        {
            return GetAchievements(game, new SearchResult { Name = game.Name, Url = url });
        }

        public GameAchievements GetAchievements(Game game, SearchResult searchResult)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> allAchievements = new List<Achievement>();

            try
            {
                string dataExophaseLocalised = string.Empty;
                string dataExophase = string.Empty;

                WebViewSettings webViewSettings = new WebViewSettings
                {
                    UserAgent = Web.UserAgent
                };

                using (IWebView webView = API.Instance.WebViews.CreateOffscreenView(webViewSettings))
                {
                    webView.DeleteDomainCookies(".exophase.com");
                    webView.NavigateAndWait(searchResult.Url);
                    dataExophase = webView.GetPageSource();
                }

                if (PluginDatabase.PluginSettings.Settings.UseLocalised && !IsConnected())
                {
                    Logger.Warn($"Exophase is disconnected");
                    string message = string.Format(ResourceProvider.GetString("LOCCommonStoresNoAuthenticate"), ClientName);
                    API.Instance.Notifications.Add(new NotificationMessage(
                        $"{PluginDatabase.PluginName}-Exophase-disconnected",
                        $"{PluginDatabase.PluginName}\r\n{message}",
                        NotificationType.Error,
                        () => PluginDatabase.Plugin.OpenSettingsView()
                    ));
                }
                else if (PluginDatabase.PluginSettings.Settings.UseLocalised)
                {
                    using (IWebView webView = API.Instance.WebViews.CreateOffscreenView(webViewSettings))
                    {
                        GetCookies()?.ForEach(x => 
                        {
                            string domain = x.Domain.StartsWith(".") ? x.Domain.Substring(1) : x.Domain;
                            webView.SetCookies("https://" + domain, x);
                        });
                        webView.NavigateAndWait(searchResult.Url);
                        dataExophaseLocalised = webView.GetPageSource();
                        webView.DeleteDomainCookies(".exophase.com");
                    }
                }

                List<Achievement> All = ParseData(dataExophase);
                List<Achievement> AllLocalised = dataExophaseLocalised.IsNullOrEmpty() ? new List<Achievement>() : ParseData(dataExophaseLocalised);

                for (int i = 0; i < All?.Count; i++)
                {
                    allAchievements.Add(new Achievement
                    {
                        Name = AllLocalised.Count > 0 ? AllLocalised[i].Name : All[i].Name,
                        ApiName = All[i].Name,
                        UrlUnlocked = All[i].UrlUnlocked,
                        Description = AllLocalised.Count > 0 ? AllLocalised[i].Description : All[i].Description,
                        DateUnlocked = All[i].DateUnlocked,
                        Percent = All[i].Percent,
                        GamerScore = All[i].GamerScore
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            gameAchievements.Items = allAchievements;

            // Set source link
            if (gameAchievements.HasAchievements)
            {
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = searchResult.Name.IsNullOrEmpty() ? searchResult.Name : searchResult.Name,
                    Name = "Exophase",
                    Url = searchResult.Url
                };
            }

            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            // The authentification is only for localised achievement
            return true;
        }


        public override bool IsConnected()
        {
            if (CachedIsConnectedResult == null)
            {
                CachedIsConnectedResult = GetIsUserLoggedIn();
            }

            return (bool)CachedIsConnectedResult;
        }

        public override bool EnabledInSettings()
        {
            // No necessary activation
            return true;
        }
        #endregion


        #region Exophase
        public void Login()
        {
            FileSystem.DeleteFile(CookiesPath);
            ResetCachedIsConnectedResult();

            WebViewSettings webViewSettings = new WebViewSettings
            {
                WindowWidth = 580,
                WindowHeight = 700,
                JavaScriptEnabled = true,
                // This is needed otherwise captcha won't pass
                UserAgent = Web.UserAgent
            };

            using (IWebView WebView = API.Instance.WebViews.CreateView(webViewSettings))
            {
                WebView.LoadingChanged += (s, e) =>
                {
                    string address = WebView.GetCurrentAddress();
                    if (address.StartsWith(UrlExophaseAccount, StringComparison.InvariantCultureIgnoreCase) && !address.StartsWith(UrlExophaseLogout, StringComparison.InvariantCultureIgnoreCase))
                    {
                        CachedIsConnectedResult = true;
                        WebView.Close();
                    }
                };

                WebView.DeleteDomainCookies(".exophase.com");
                WebView.Navigate(UrlExophaseLogin);
                _ = WebView.OpenDialog();
            }

            List<HttpCookie> httpCookies = WebViewOffscreen.GetCookies().Where(x => x.Domain.IsEqual(".exophase.com")).ToList();
            SetCookies(httpCookies);
            WebViewOffscreen.DeleteDomainCookies(".exophase.com");
            WebViewOffscreen.Dispose();
        }

        private bool GetIsUserLoggedIn()
        {
            WebViewSettings webViewSettings = new WebViewSettings
            {
                UserAgent = Web.UserAgent,
                JavaScriptEnabled = true
            };

            using (IWebView webViewOffscreen = API.Instance.WebViews.CreateOffscreenView(webViewSettings))
            {
                try
                {
                    // 1. Set cookies
                    var cookies = GetCookies();
                    if (cookies == null || cookies.Count == 0)
                    {
                        Logger.Warn("Exophase: no cookies present – user considered disconnected.");
                        return false;
                    }
                    cookies.ForEach(cookie => webViewOffscreen.SetCookies(UrlExophaseAccount, cookie));

                    // 2. Prepare asynchronous wait
                    using (var loadingCompleted = new ManualResetEventSlim(false))
                    {
                        webViewOffscreen.LoadingChanged += (s, e) =>
                        {
                            if (!e.IsLoading)
                            {
                                loadingCompleted.Set();
                            }
                        };

                        // 3. Navigate and wait for page to be fully loaded
                        webViewOffscreen.Navigate(UrlExophaseAccount);
                        TimeSpan waitTimeout = TimeSpan.FromSeconds(30);
                        if (!loadingCompleted.Wait(waitTimeout))
                        {
                            Logger.Error($"Timeout during authentication status check after {waitTimeout.TotalSeconds} seconds.");
                            return false;
                        }
                    }

                    // 4. Get content and check login
                    string dataExophase = webViewOffscreen.GetPageSource();
                    bool isConnected = dataExophase.Contains("column-username", StringComparison.InvariantCultureIgnoreCase);

                    if (isConnected)
                    {
                        var refreshedCookies = (webViewOffscreen.GetCookies() ?? cookies ?? new List<HttpCookie>())
                            .Where(c => c.Domain.IsEqual(".exophase.com"))
                            .ToList();
                        SetCookies(refreshedCookies);
                    }

                    return isConnected;
                }
                finally
                {
                    webViewOffscreen.DeleteDomainCookies(".exophase.com");
                }
            }
        }


        public List<SearchResult> SearchGame(string name, string platforms = "")
        {
            List<SearchResult> listSearchGames = new List<SearchResult>();
            try
            {
                WebViewSettings webViewSettings = new WebViewSettings
                {
                    UserAgent = Web.UserAgent
                };

                string json = string.Empty;
                using (IWebView webView = API.Instance.WebViews.CreateOffscreenView(webViewSettings))
                {
                    string urlSearch = platforms.IsNullOrEmpty() || platforms.IsEqual(ResourceProvider.GetString("LOCAll"))
                        ? string.Format(UrlExophaseSearch, WebUtility.UrlEncode(name))
                        : string.Format(UrlExophaseSearchPlatform, WebUtility.UrlEncode(name), platforms);

                    webView.NavigateAndWait(urlSearch);
                    json = webView.GetPageText();
                }

                if (!Serialization.TryFromJson(json, out ExophaseSearchResult exophaseScheachResult))
                {
                    Logger.Warn($"No Exophase result for {name}");
                    Logger.Warn($"{json}");
                    return listSearchGames;
                }

                List<List> ListExophase = exophaseScheachResult?.Games?.List;
                if (ListExophase != null)
                {
                    listSearchGames = ListExophase.Select(x => new SearchResult
                    {
                        Url = x.EndpointAwards,
                        Name = x.Title,
                        UrlImage = x.Images.O ?? x.Images.L ?? x.Images.M,
                        Platforms = x.Platforms.Select(p => p.Name).ToList(),
                        AchievementsCount = x.TotalAwards ?? 0
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on SearchGame({name})", true, PluginDatabase.PluginName);
            }

            return listSearchGames;
        }


        private string GetAchievementsPageUrl(GameAchievements gameAchievements, Services.SuccessStoryDatabase.AchievementSource source)
        {
            bool UsedSplit = false;

            string sourceLinkName = gameAchievements.SourcesLink?.Name;
            if (sourceLinkName == "Exophase")
            {
                return gameAchievements.SourcesLink.Url;
            }

            List<SearchResult> searchResults = SearchGame(gameAchievements.Name);
            if (searchResults.Count == 0)
            {
                Logger.Warn($"No game found for {gameAchievements.Name} in GetAchievementsPageUrl()");

                Thread.Sleep(1000);
                searchResults = SearchGame(CommonPluginsShared.PlayniteTools.NormalizeGameName(gameAchievements.Name));
                if (searchResults.Count == 0)
                {
                    Logger.Warn($"No game found for {CommonPluginsShared.PlayniteTools.NormalizeGameName(gameAchievements.Name)} in GetAchievementsPageUrl()");

                    Thread.Sleep(1000);
                    searchResults = SearchGame(Regex.Match(gameAchievements.Name, @"^.*(?=[:-])").Value);
                    UsedSplit = true;
                    if (searchResults.Count == 0)
                    {
                        Logger.Warn($"No game found for {Regex.Match(gameAchievements.Name, @"^.*(?=[:-])").Value} in GetAchievementsPageUrl()");
                        return null;
                    }
                }
            }

            string normalizedGameName = UsedSplit ? CommonPluginsShared.PlayniteTools.NormalizeGameName(Regex.Match(gameAchievements.Name, @"^.*(?=[:-])").Value) : CommonPluginsShared.PlayniteTools.NormalizeGameName(gameAchievements.Name);
            SearchResult searchResult = searchResults.Find(x => CommonPluginsShared.PlayniteTools.NormalizeGameName(x.Name) == normalizedGameName && PlatformAndProviderMatch(x, gameAchievements, source));

            if (searchResult == null)
            {
                Logger.Warn($"No matching game found for {gameAchievements.Name} in GetAchievementsPageUrl()");
            }

            return searchResult?.Url;
        }


        /// <summary>
        /// Set achievement rarity via Exophase web scraping.
        /// </summary>
        /// <param name="gameAchievements"></param>
        /// <param name="source"></param>
        public void SetRarety(GameAchievements gameAchievements, Services.SuccessStoryDatabase.AchievementSource source)
        {
            string achievementsUrl = GetAchievementsPageUrl(gameAchievements, source);
            if (achievementsUrl.IsNullOrEmpty())
            {
                Logger.Warn($"No Exophase (rarity) url found for {gameAchievements.Name} - {gameAchievements.Id}");
                return;
            }

            try
            {
                GameAchievements exophaseAchievements = GetAchievements(gameAchievements.Game, achievementsUrl);
                exophaseAchievements.Items.ForEach(y =>
                {
                    Achievement achievement = gameAchievements.Items.Find(x => x.ApiName.IsEqual(y.ApiName));
                    if (achievement == null)
                    {
                        achievement = gameAchievements.Items.Find(x => x.Name.IsEqual(y.Name));
                        if (achievement == null)
                        {
                            achievement = gameAchievements.Items.Find(x => x.Name.IsEqual(y.ApiName));
                        }
                    }

                    if (achievement != null)
                    {
                        achievement.ApiName = y.ApiName;
                        achievement.Percent = y.Percent;
                        achievement.GamerScore = StoreApi.CalcGamerScore(y.Percent);

                        if (PluginDatabase.PluginSettings.Settings.UseLocalised && IsConnected())
                        {
                            achievement.Name = y.Name;
                            achievement.Description = y.Description;
                        }
                    }
                    else
                    {
                        Logger.Warn($"No Exophase (rarity) matching achievements found for {gameAchievements.Name} - {gameAchievements.Id} - {y.Name} in {achievementsUrl}");
                    }
                });

                PluginDatabase.AddOrUpdate(gameAchievements);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private static bool PlatformAndProviderMatch(SearchResult exophaseGame, GameAchievements playniteGame, Services.SuccessStoryDatabase.AchievementSource achievementSource)
        {
            switch (achievementSource)
            {
                //PC: match service
                case Services.SuccessStoryDatabase.AchievementSource.Steam:
                    return exophaseGame.Platforms.Contains("Steam", StringComparer.InvariantCultureIgnoreCase);

                case Services.SuccessStoryDatabase.AchievementSource.GOG:
                    return exophaseGame.Platforms.Contains("GOG", StringComparer.InvariantCultureIgnoreCase);

                case Services.SuccessStoryDatabase.AchievementSource.Origin:
                    return exophaseGame.Platforms.Contains("Electronic Arts", StringComparer.InvariantCultureIgnoreCase);

                case Services.SuccessStoryDatabase.AchievementSource.RetroAchievements:
                    return exophaseGame.Platforms.Contains("Retro", StringComparer.InvariantCultureIgnoreCase);

                case Services.SuccessStoryDatabase.AchievementSource.Overwatch:
                case Services.SuccessStoryDatabase.AchievementSource.Starcraft2:
                case Services.SuccessStoryDatabase.AchievementSource.Wow:
                    return exophaseGame.Platforms.Contains("Blizzard", StringComparer.InvariantCultureIgnoreCase);

                //Console: match platform
                case Services.SuccessStoryDatabase.AchievementSource.Playstation:
                case Services.SuccessStoryDatabase.AchievementSource.Xbox:
                case Services.SuccessStoryDatabase.AchievementSource.RPCS3:
                    return PlatformsMatch(exophaseGame, playniteGame);

                case Services.SuccessStoryDatabase.AchievementSource.Epic:
                case Services.SuccessStoryDatabase.AchievementSource.GenshinImpact:
                case Services.SuccessStoryDatabase.AchievementSource.GuildWars2:
                case Services.SuccessStoryDatabase.AchievementSource.None:
                case Services.SuccessStoryDatabase.AchievementSource.Local:
                default:
                    return false;
            }
        }

        private static Dictionary<string, string[]> PlaynitePlatformSpecificationIdToExophasePlatformName => new Dictionary<string, string[]>
        {
            { "xbox360", new[]{"Xbox 360"} },
            { "xbox_one", new[]{"Xbox One"} },
            { "xbox_series", new[]{"Xbox Series"} },
            { "xbox_game_pass", new []{"Windows 8", "Windows 10", "Windows 11", "GFWL", "Xbox 360", "Xbox One", "Xbox Series" } },
            { "pc_windows", new []{"Windows 8", "Windows 10", "Windows 11" /* future proofing */, "GFWL"} },
            { "sony_playstation3", new[]{"PS3"} },
            { "sony_playstation4", new[]{"PS4"} },
            { "sony_playstation5", new[]{"PS5"} },
            { "sony_vita", new[]{"PS Vita"} },
        };

        private static bool PlatformsMatch(SearchResult exophaseGame, GameAchievements playniteGame)
        {
            foreach (Platform playnitePlatform in playniteGame.Platforms)
            {
                string sourceName = string.Empty;
                string key = string.Empty;
                try
                {
                    sourceName = API.Instance.Database.Games.Get(playniteGame.Id)?.Source?.Name;
                    key = sourceName == "Xbox Game Pass" ? "xbox_game_pass" : playnitePlatform.SpecificationId;
                    if (!PlaynitePlatformSpecificationIdToExophasePlatformName.TryGetValue(key, out string[] exophasePlatformNames))
                    {
                        continue;
                    }

                    if (exophaseGame?.Platforms?.IntersectsExactlyWith(exophasePlatformNames) ?? false)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on PlatformsMatch with {sourceName} - {key}");
                }
            }
            return false;
        }
        #endregion


        private List<Achievement> ParseData(string data)
        {
            HtmlParser parser = new HtmlParser();
            IHtmlDocument htmlDocument = parser.Parse(data);

            List<Achievement> allAchievements = new List<Achievement>();
            IHtmlCollection<IElement> sectionAchievements = htmlDocument.QuerySelectorAll("ul.achievement, ul.trophy, ul.challenge");
            string gameName = htmlDocument.QuerySelector("h2.me-2 a")?.GetAttribute("title");

            if (sectionAchievements == null || sectionAchievements.Count() == 0)
            {
                return null;
            }
            else
            {
                foreach (IElement section in sectionAchievements)
                {
                    foreach (IElement searchAchievements in section.QuerySelectorAll("li"))
                    {
                        try
                        {
                            string sFloat = searchAchievements.GetAttribute("data-average")
                                ?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                                ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

                            _ = float.TryParse(sFloat, out float Percent);

                            string urlUnlocked = searchAchievements.QuerySelector("img")?.GetAttribute("src");
                            string name = WebUtility.HtmlDecode(searchAchievements.QuerySelector("a")?.InnerHtml);
                            string description = WebUtility.HtmlDecode(searchAchievements.QuerySelector("div.award-description p")?.InnerHtml);
                            bool isHidden = searchAchievements.GetAttribute("class").IndexOf("secret") > -1;

                            allAchievements.Add(new Achievement
                            {
                                Name = name,
                                UrlUnlocked = urlUnlocked,
                                Description = description,
                                DateUnlocked = default(DateTime),
                                Percent = Percent,
                                GamerScore = StoreApi.CalcGamerScore(Percent)
                            });
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        }
                    }
                }
            }

            return allAchievements;
        }
    }
}
