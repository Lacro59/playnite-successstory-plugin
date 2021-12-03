using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
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


    class ExophaseAchievements : GenericAchievements
    {
        private const string UrlExophase = @"https://www.exophase.com/";

        private const string UrlExophaseSearch = @"https://api.exophase.com/public/archive/games?q={0}&sort=added";

        private const string UrlExophaseLogin = @"https://www.exophase.com/login";
        private const string UrlExophaseLogout = @"https://www.exophase.com/logout";
        private const string UrlExophaseAccount = @"https://www.exophase.com/account";


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


            try
            {
                WebViewOffscreen.NavigateAndWait(searchResult.Url);
                WebViewOffscreen.NavigateAndWait(searchResult.Url);
                string DataExophase = WebViewOffscreen.GetPageSource();

                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(DataExophase);

                AllAchievements = new List<Achievements>();
                var SectionAchievements = htmlDocument.QuerySelectorAll("ul.achievement, ul.trophy, ul.challenge");

                if (SectionAchievements == null || SectionAchievements.Count() == 0)
                {
                    logger.Warn($"Problem with {searchResult.Url}");
                    if (!IsRetry)
                    {
                        return GetAchievements(game, searchResult, true);
                    }
                }
                else
                {
                    foreach (var Section in SectionAchievements)
                    {
                        foreach (var SearchAchievements in Section.QuerySelectorAll("li"))
                        {
                            try
                            {
                                string sFloat = SearchAchievements.GetAttribute("data-average")
                                    .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                                    .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

                                float.TryParse(sFloat, out float Percent);

                                string UrlUnlocked = SearchAchievements.QuerySelector("img").GetAttribute("src");
                                string Name = WebUtility.HtmlDecode(SearchAchievements.QuerySelector("a").InnerHtml);
                                string Description = WebUtility.HtmlDecode(SearchAchievements.QuerySelector("div.award-description p").InnerHtml);
                                bool IsHidden = SearchAchievements.GetAttribute("class").IndexOf("secret") > -1;

                                AllAchievements.Add(new Achievements
                                {
                                    Name = Name,
                                    UrlUnlocked = UrlUnlocked,
                                    Description = Description,
                                    DateUnlocked = default(DateTime),
                                    Percent = Percent
                                });
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, true, "SuccessStory");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "SuccessStory");
            }


            gameAchievements.Items = AllAchievements;


            // Set source link
            if (gameAchievements.HasAchievements)
            {
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = searchResult.Name,
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
            ResetCachedIsConnectedResult();

            using (var WebView = PluginDatabase.PlayniteApi.WebViews.CreateView(600, 600))
            {
                WebView.LoadingChanged += (s, e) =>
                {
                    string address = WebView.GetCurrentAddress();
                    if (address.Contains(UrlExophaseAccount) && !address.Contains(UrlExophaseLogout))
                    {
                        CachedIsConnectedResult = true;
                        WebView.Close();
                    }
                };

                WebView.LoadingChanged += (s, e) =>
                {
                    if (WebView.GetCurrentAddress() == UrlExophase)
                    {
                        WebView.Navigate(UrlExophaseLogin);
                    }
                };

                WebView.Navigate(UrlExophaseLogout);
                WebView.OpenDialog();
            }
        }

        private bool GetIsUserLoggedIn()
        {
            WebViewOffscreen.NavigateAndWait(UrlExophaseLogin);

            if (!WebViewOffscreen.GetCurrentAddress().StartsWith(UrlExophaseAccount))
            {
                logger.Warn("Exophase user is not connected");
                return false;
            }
            logger.Info("Exophase user is connected");
            return true;
        }


        public List<SearchResult> SearchGame(string Name)
        {
            List<SearchResult> ListSearchGames = new List<SearchResult>();

            try
            {
                string UrlSearch = string.Format(UrlExophaseSearch, WebUtility.UrlEncode(Name));

                string StringJsonResult = Web.DownloadStringData(UrlSearch).GetAwaiter().GetResult();
                if (StringJsonResult == "{\"success\":true,\"games\":false}")
                {
                    logger.Warn($"No Exophase result for {Name}");
                    return ListSearchGames;
                }

                ExophaseSearchResult exophaseScheachResult = Serialization.FromJson<ExophaseSearchResult>(StringJsonResult);

                var ListExophase = exophaseScheachResult?.games?.list;
                if (ListExophase != null)
                {
                    ListSearchGames = ListExophase.Select(x => new SearchResult
                    {
                        Url = x.endpoint_awards,
                        Name = x.title,
                        UrlImage = x.images.o,
                        Platforms = x.platforms.Select(p => p.name).ToList(),
                        AchievementsCount = x.total_awards
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "SuccessStory");
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

            var searchResults = SearchGame(gameAchievements.Name);
            if (searchResults.Count == 0)
            {
                logger.Warn($"No game found for {gameAchievements.Name} in GetAchievementsPageUrl()");

                searchResults = SearchGame(PlayniteTools.NormalizeGameName(gameAchievements.Name));
                if (searchResults.Count == 0)
                {
                    logger.Warn($"No game found for {PlayniteTools.NormalizeGameName(gameAchievements.Name)} in GetAchievementsPageUrl()");

                    searchResults = SearchGame(Regex.Match(gameAchievements.Name, @"^.*(?=[:-])").Value);
                    UsedSplit = true;
                    if (searchResults.Count == 0)
                    {
                        logger.Warn($"No game found for {Regex.Match(gameAchievements.Name, @"^.*(?=[:-])").Value} in GetAchievementsPageUrl()");
                        return null;
                    }                    
                }                    
            }

            string normalizedGameName = UsedSplit ? PlayniteTools.NormalizeGameName(Regex.Match(gameAchievements.Name, @"^.*(?=[:-])").Value) : PlayniteTools.NormalizeGameName(gameAchievements.Name);
            var searchResult = searchResults.Find(x => PlayniteTools.NormalizeGameName(x.Name) == normalizedGameName && PlatformAndProviderMatch(x, gameAchievements, source));

            if (searchResult == null)
            {
                logger.Warn($"No matching game found for {gameAchievements.Name} in GetAchievementsPageUrl()");
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
                logger.Warn($"No Exophase (rarity) url find for {gameAchievements.Name} - {gameAchievements.Id}");
                return;
            }

            try
            {
                GameAchievements exophaseAchievements = GetAchievements(
                    PluginDatabase.PlayniteApi.Database.Games.Get(gameAchievements.Id),
                    achievementsUrl
                );

                exophaseAchievements.Items.ForEach(y =>
                {
                    var achievement = gameAchievements.Items.Find(x => x.Name.Equals(y.Name, StringComparison.InvariantCultureIgnoreCase));
                    if (achievement == null)
                    {
                        achievement = gameAchievements.Items.Find(x => x.ApiName.Equals(y.Name, StringComparison.InvariantCultureIgnoreCase));
                    }

                    if (achievement != null)
                    {
                        achievement.Percent = y.Percent;
                    }
                    else
                    {
                        logger.Warn($"No Exophase (rarity) matching achievements found for {gameAchievements.Name} - {gameAchievements.Id} - {y.Name} in {achievementsUrl}");
                    }
                });

                PluginDatabase.AddOrUpdate(gameAchievements);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "SuccessStory");
            }
        }


        public void SetMissingDescription(GameAchievements gameAchievements, Services.SuccessStoryDatabase.AchievementSource source)
        {
            string achievementsUrl = GetAchievementsPageUrl(gameAchievements, source);
            if (achievementsUrl.IsNullOrEmpty())
            {
                logger.Warn($"No Exophase (description) url find for {gameAchievements.Name} - {gameAchievements.Id}");
                return;
            }

            try
            {
                GameAchievements exophaseAchievements = GetAchievements(
                    PluginDatabase.PlayniteApi.Database.Games.Get(gameAchievements.Id),
                    achievementsUrl
                );

                exophaseAchievements.Items.ForEach(y => 
                {
                    var achievement = gameAchievements.Items.Find(x => x.Name.Equals(y.Name, StringComparison.InvariantCultureIgnoreCase));
                    if (achievement == null)
                    {
                        achievement = gameAchievements.Items.Find(x => x.ApiName.Equals(y.Name, StringComparison.InvariantCultureIgnoreCase));
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
                        logger.Warn($"No Exophase (description) matching achievements found for {gameAchievements.Name} - {gameAchievements.Id} - {y.Name} in {achievementsUrl}");
                    }
                });

                PluginDatabase.AddOrUpdate(gameAchievements);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "SuccessStory");
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
                    return exophaseGame.Platforms.Contains("Origin", StringComparer.InvariantCultureIgnoreCase);
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
            foreach (var playnitePlatform in playniteGame.Platforms)
            {
                string[] exophasePlatformNames;
                string sourceName = PluginDatabase.PlayniteApi.Database.Games.Get(playniteGame.Id).Source?.Name;
                if (sourceName == "Xbox Game Pass")
                {
                    if (!PlaynitePlatformSpecificationIdToExophasePlatformName.TryGetValue("xbox_game_pass", out exophasePlatformNames))
                        continue;
                }
                else
                {
                    if (!PlaynitePlatformSpecificationIdToExophasePlatformName.TryGetValue(playnitePlatform.SpecificationId, out exophasePlatformNames))
                        continue; //there are no natural matches between default Playnite platform name and Exophase platform name, so give up if it's not in the dictionary
                }

                if (exophaseGame.Platforms.IntersectsExactlyWith(exophasePlatformNames))
                    return true;
            }
            return false;
        }
        #endregion
    }
}
