using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
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

namespace SuccessStory.Clients
{
    public enum ExophasePlatform
    {
        Google_Play,
        Steam,
        PS3, PS4, PS5, PS_Vita,
        Retro,
        Xbox_One, Xbox_360, Xbox_Series, Windows_8, Windows_10, WP,
        Stadia,
        Origin,
        Blizzard,
        GOG,
        Ubisoft,
    }


    public class ExophaseAchievements : GenericAchievements
    {
        private string UrlExophaseSearch => @"https://api.exophase.com/public/archive/games?q={0}&sort=added";
        private string UrlExophase => @"https://www.exophase.com";
        private string UrlExophaseLogin => $"{UrlExophase}/login";
        private string UrlExophaseLogout => $"{UrlExophase}/logout";
        private string UrlExophaseAccount => $"{UrlExophase}/account";

        

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

        public GameAchievements GetAchievements(Game game, SearchResult searchResult, bool IsRetry = false)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();
            string gameName = string.Empty;

            try
            {
                string dataExophase = Web.DownloadStringData(searchResult.Url, GetCookies()).GetAwaiter().GetResult();

                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(dataExophase);

                AllAchievements = new List<Achievements>();
                IHtmlCollection<IElement> sectionAchievements = htmlDocument.QuerySelectorAll("ul.achievement, ul.trophy, ul.challenge");
                gameName = htmlDocument.QuerySelector("h2.me-2 a")?.GetAttribute("title");

                if (sectionAchievements == null || sectionAchievements.Count() == 0)
                {
                    Logger.Warn($"Problem with {searchResult.Url}");
                    if (!IsRetry)
                    {
                        return GetAchievements(game, searchResult, true);
                    }
                }
                else
                {
                    foreach (IElement section in sectionAchievements)
                    {
                        foreach (IElement SearchAchievements in section.QuerySelectorAll("li"))
                        {
                            try
                            {
                                string sFloat = SearchAchievements.GetAttribute("data-average")
                                    .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                                    .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

                                float.TryParse(sFloat, out float Percent);

                                string urlUnlocked = SearchAchievements.QuerySelector("img").GetAttribute("src");
                                string name = WebUtility.HtmlDecode(SearchAchievements.QuerySelector("a").InnerHtml);
                                string description = WebUtility.HtmlDecode(SearchAchievements.QuerySelector("div.award-description p").InnerHtml);
                                bool isHidden = SearchAchievements.GetAttribute("class").IndexOf("secret") > -1;

                                AllAchievements.Add(new Achievements
                                {
                                    Name = name,
                                    UrlUnlocked = urlUnlocked,
                                    Description = description,
                                    DateUnlocked = default(DateTime),
                                    Percent = Percent
                                });
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }


            gameAchievements.Items = AllAchievements;


            // Set source link
            if (gameAchievements.HasAchievements)
            {
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = searchResult.Name.IsNullOrEmpty() ? gameName : searchResult.Name,
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

            using (IWebView WebView = API.Instance.WebViews.CreateView(600, 600))
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
                WebView.OpenDialog();
            }

            List<HttpCookie> httpCookies = WebViewOffscreen.GetCookies().Where(x => x.Domain.IsEqual(".exophase.com")).ToList();
            SetCookies(httpCookies);
            WebViewOffscreen.DeleteDomainCookies(".exophase.com");
            WebViewOffscreen.Dispose();
        }

        private bool GetIsUserLoggedIn()
        {
            string DataExophase = Web.DownloadStringData(UrlExophaseAccount, GetCookies()).GetAwaiter().GetResult();
            bool isConnected = DataExophase.Contains("column-username", StringComparison.InvariantCultureIgnoreCase);
            if (isConnected)
            {
                SetCookies(GetCookies());
            }
            return isConnected;
        }


        public List<SearchResult> SearchGame(string Name)
        {
            List<SearchResult> ListSearchGames = new List<SearchResult>();
            try
            {
                string UrlSearch = string.Format(UrlExophaseSearch, WebUtility.UrlEncode(Name));

                string StringJsonResult = Web.DownloadStringData(UrlSearch).GetAwaiter().GetResult();
                if (!Serialization.TryFromJson(StringJsonResult, out ExophaseSearchResult exophaseScheachResult))
                {
                    Logger.Warn($"No Exophase result for {Name}");
                    return ListSearchGames;
                }

                List<List> ListExophase = exophaseScheachResult?.games?.list;
                if (ListExophase != null)
                {
                    ListSearchGames = ListExophase.Select(x => new SearchResult
                    {
                        Url = x.endpoint_awards,
                        Name = x.title,
                        UrlImage = x.images.o ?? x.images.l ?? x.images.m,
                        Platforms = x.platforms.Select(p => p.name).ToList(),
                        AchievementsCount = x.total_awards
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on SearchGame({Name})", true, PluginDatabase.PluginName);
            }

            return ListSearchGames;
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

                searchResults = SearchGame(PlayniteTools.NormalizeGameName(gameAchievements.Name));
                if (searchResults.Count == 0)
                {
                    Logger.Warn($"No game found for {PlayniteTools.NormalizeGameName(gameAchievements.Name)} in GetAchievementsPageUrl()");

                    searchResults = SearchGame(Regex.Match(gameAchievements.Name, @"^.*(?=[:-])").Value);
                    UsedSplit = true;
                    if (searchResults.Count == 0)
                    {
                        Logger.Warn($"No game found for {Regex.Match(gameAchievements.Name, @"^.*(?=[:-])").Value} in GetAchievementsPageUrl()");
                        return null;
                    }
                }
            }

            string normalizedGameName = UsedSplit ? PlayniteTools.NormalizeGameName(Regex.Match(gameAchievements.Name, @"^.*(?=[:-])").Value) : PlayniteTools.NormalizeGameName(gameAchievements.Name);
            SearchResult searchResult = searchResults.Find(x => PlayniteTools.NormalizeGameName(x.Name) == normalizedGameName && PlatformAndProviderMatch(x, gameAchievements, source));

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
                Logger.Warn($"No Exophase (rarity) url find for {gameAchievements.Name} - {gameAchievements.Id}");
                return;
            }

            try
            {
                GameAchievements exophaseAchievements = GetAchievements(
                    API.Instance.Database.Games.Get(gameAchievements.Id),
                    achievementsUrl
                );

                exophaseAchievements.Items.ForEach(y =>
                {
                    Achievements achievement = gameAchievements.Items.Find(x => x.Name.IsEqual(y.Name));
                    if (achievement == null)
                    {
                        achievement = gameAchievements.Items.Find(x => x.ApiName.IsEqual(y.Name));
                    }

                    if (achievement != null)
                    {
                        achievement.Percent = y.Percent;
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


        public void SetMissingDescription(GameAchievements gameAchievements, Services.SuccessStoryDatabase.AchievementSource source)
        {
            string achievementsUrl = GetAchievementsPageUrl(gameAchievements, source);
            if (achievementsUrl.IsNullOrEmpty())
            {
                Logger.Warn($"No Exophase (description) url find for {gameAchievements.Name} - {gameAchievements.Id}");
                return;
            }

            try
            {
                GameAchievements exophaseAchievements = GetAchievements(
                    API.Instance.Database.Games.Get(gameAchievements.Id),
                    achievementsUrl
                );

                exophaseAchievements.Items.ForEach(y => 
                {
                    Achievements achievement = gameAchievements.Items.Find(x => x.Name.IsEqual(y.Name));
                    if (achievement == null)
                    {
                        achievement = gameAchievements.Items.Find(x => x.ApiName.IsEqual(y.Name));
                    }

                    if (achievement != null)
                    {
                        if (achievement.Description.IsNullOrEmpty())
                        {
                            achievement.Description = y.Description;
                        }
                    }
                    else
                    {
                        Logger.Warn($"No Exophase (description) matching achievements found for {gameAchievements.Name} - {gameAchievements.Id} - {y.Name} in {achievementsUrl}");
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

                case Services.SuccessStoryDatabase.AchievementSource.None:
                case Services.SuccessStoryDatabase.AchievementSource.Local:
                default:
                    return false;
            }
        }

        private static Dictionary<string, string[]> PlaynitePlatformSpecificationIdToExophasePlatformName = new Dictionary<string, string[]>
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
            foreach (Playnite.SDK.Models.Platform playnitePlatform in playniteGame.Platforms)
            {
                string[] exophasePlatformNames;
                string sourceName = API.Instance.Database.Games.Get(playniteGame.Id).Source?.Name;
                if (sourceName == "Xbox Game Pass")
                {
                    if (!PlaynitePlatformSpecificationIdToExophasePlatformName.TryGetValue("xbox_game_pass", out exophasePlatformNames))
                    {
                        continue;
                    }
                }
                else
                {
                    if (!PlaynitePlatformSpecificationIdToExophasePlatformName.TryGetValue(playnitePlatform.SpecificationId, out exophasePlatformNames))
                    {
                        continue; //there are no natural matches between default Playnite platform name and Exophase platform name, so give up if it's not in the dictionary
                    }
                }

                if (exophaseGame.Platforms.IntersectsExactlyWith(exophasePlatformNames))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
