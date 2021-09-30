using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using CommonPluginsShared.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

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
        protected static IWebView _WebViewOffscreen;
        internal static IWebView WebViewOffscreen
        {
            get
            {
                if (_WebViewOffscreen == null)
                {
                    _WebViewOffscreen = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView();
                }
                return _WebViewOffscreen;
            }

            set
            {
                _WebViewOffscreen = value;
            }
        }

        private const string UrlExophaseSearch = @"https://api.exophase.com/public/archive/games?q={0}&sort=added";
        private const string UrlExophaseLogin = @"https://www.exophase.com/login/";
        private const string UrlExophaseLogout = @"https://www.exophase.com/logout/";


        public ExophaseAchievements() : base()
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            throw new NotImplementedException();
        }

        public GameAchievements GetAchievements(Game game, SearchResult searchResult, bool IsRetry = false)
        {
            List<Achievements> AllAchievements = new List<Achievements>();
            string GameName = game.Name;
            bool HaveAchivements = false;
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            GameAchievements Result = SuccessStory.PluginDatabase.GetDefault(game);
            Result.Items = AllAchievements;


            try
            {
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
                                float.TryParse(SearchAchievements.GetAttribute("data-average").Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator), out float Percent);

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
                                Common.LogError(ex, false);
                            }
                        }
                    }

                    HaveAchivements = true;
                    Total = AllAchievements.Count;
                    Locked = AllAchievements.Count;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }


            Result.Name = GameName;
            Result.HaveAchivements = HaveAchivements;
            Result.Total = Total;
            Result.Unlocked = Unlocked;
            Result.Locked = Locked;
            Result.Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;
            Result.Items = AllAchievements;

            if (Result.HaveAchivements)
            {
                Result.SourcesLink = new SourceLink
                {
                    GameName = searchResult.Name,
                    Name = "Exophase",
                    Url = searchResult.Url
                };
            }

            return Result;
        }


        public List<SearchResult> SearchGame(string Name, bool IsTwo = false)
        {
            List<SearchResult> ListSearchGames = new List<SearchResult>();

            try
            {
                string UrlSearch = string.Format(UrlExophaseSearch, WebUtility.UrlEncode(Name));

                string StringJsonResult = Web.DownloadStringData(UrlSearch).GetAwaiter().GetResult();
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
                Common.LogError(ex, false);
            }

            return ListSearchGames;
        }

        private string GetAchievementsPageUrl(GameAchievements gameAchievements, Services.SuccessStoryDatabase.AchievementSource source)
        {
            string sourceLinkName = gameAchievements.SourcesLink?.Name;
            if (sourceLinkName == "Exophase")
                return gameAchievements.SourcesLink.Url;

            var searchResults = SearchGame(gameAchievements.Name);
            if (searchResults.Count == 0)
            {
                logger.Warn($"No game found for {gameAchievements.Name} in GetAchievementsPageUrl()");
                return null;
            }

            string normalizedGameName = PlayniteTools.NormalizeGameName(gameAchievements.Name);
            var searchResult = searchResults.Find(x => PlayniteTools.NormalizeGameName(x.Name) == normalizedGameName && PlatformAndProviderMatch(x, gameAchievements, source));

            if (searchResult == null)
                logger.Warn($"No matching game found for {gameAchievements.Name} in GetAchievementsPageUrl()");

            return searchResult?.Url;
        }

        /// <summary>
        /// Set achievement rarity via Exophase web scraping
        /// </summary>
        /// <param name="gameAchievements"></param>
        /// <param name="source"></param>
        public void SetRarety(GameAchievements gameAchievements, Services.SuccessStoryDatabase.AchievementSource source)
        {
            string achievementsUrl = GetAchievementsPageUrl(gameAchievements, source);
            if (achievementsUrl == null)
                return;

            try
            {
                WebViewOffscreen.NavigateAndWait(achievementsUrl);
                string DataExophase = WebViewOffscreen.GetPageSource();

                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(DataExophase);

                var SectionAchievements = htmlDocument.QuerySelectorAll("ul.achievement, ul.trophy, ul.challenge");

                if (SectionAchievements == null || SectionAchievements.Count() == 0)
                {
                    logger.Warn($"No achievements list found in {achievementsUrl}");
                    return;
                }

                foreach (var Section in SectionAchievements)
                {
                    foreach (var SearchAchievements in Section.QuerySelectorAll("li"))
                    {
                        try
                        {
                            string achievementName = WebUtility.HtmlDecode(SearchAchievements.QuerySelector("a").InnerHtml).Trim();
                            float.TryParse(SearchAchievements.GetAttribute("data-average")
                                .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                                .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator), out float Percent);

                            var achievement = gameAchievements.Items.Find(x => x.Name.Equals(achievementName, StringComparison.InvariantCultureIgnoreCase));
                            if (achievement == null)
                            {
                                achievement = gameAchievements.Items.Find(x => x.ApiName.Equals(achievementName, StringComparison.InvariantCultureIgnoreCase));
                            }

                            if (achievement != null)
                            {
                                achievement.Percent = Percent;
                            }
                            else
                            {
                                logger.Warn($"No matching achievements found for {achievementName} in {achievementsUrl}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false);
                        }
                    }
                }
                PluginDatabase.Update(gameAchievements);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
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
                if (!PlaynitePlatformSpecificationIdToExophasePlatformName.TryGetValue(playnitePlatform.SpecificationId, out string[] exophasePlatformNames))
                    continue; //there are no natural matches between default Playnite platform name and Exophase platform name, so give up if it's not in the dictionary

                if (exophaseGame.Platforms.IntersectsExactlyWith(exophasePlatformNames))
                    return true;
            }
            return false;
        }


        public override bool IsConfigured()
        {
            throw new NotImplementedException();
        }

        public override bool IsConnected()
        {
            return GetIsUserLoggedIn();
        }


        public void Login()
        {
            var view = PluginDatabase.PlayniteApi.WebViews.CreateView(600, 600);

            logger.Info("Login()");

            view.LoadingChanged += (s, e) =>
            {
                string address = view.GetCurrentAddress();
                if (address.Contains("https://www.exophase.com/account/") && !address.Contains(UrlExophaseLogout))
                {
                    view.Close();
                }
            };

            view.LoadingChanged += (s, e) =>
            {
                if (view.GetCurrentAddress() == "https://www.exophase.com/")
                {
                    view.Navigate(UrlExophaseLogin);
                }
            };

            view.Navigate(UrlExophaseLogout);
            view.OpenDialog();
        }

        public bool GetIsUserLoggedIn()
        {
            WebViewOffscreen.NavigateAndWait(UrlExophaseLogin);

            if (WebViewOffscreen.GetCurrentAddress().StartsWith(UrlExophaseLogin))
            {
                logger.Warn("Exophase user is not connected");
                return false;
            }
            logger.Info("Exophase user is connected");
            return true;
        }


        public override bool ValidateConfiguration(IPlayniteAPI playniteAPI, Plugin plugin, SuccessStorySettings settings)
        {
            throw new NotImplementedException();
        }

        public override bool EnabledInSettings(SuccessStorySettings settings)
        {
            return true; //not sure about this one
        }
    }
}
