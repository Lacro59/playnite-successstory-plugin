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
        private IWebView WebView;

        private const string UrlExophaseSearch = @"https://api.exophase.com/public/archive/games?q={0}&sort=added";
        private const string UrlExophaseLogin = @"https://www.exophase.com/login/";
        private const string UrlExophaseLogout = @"https://www.exophase.com/logout/";


        public ExophaseAchievements() : base()
        {
            WebView = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView();
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
                WebView.NavigateAndWait(searchResult.Url);
                string DataExophase = WebView.GetPageSource();

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
                        Platform = x.platforms.FirstOrDefault()?.name,
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


        public void SetRarety(GameAchievements gameAchievements, bool IsRefresh = false)
        {
            List<SearchResult> SearchResults = new List<SearchResult>();
            if (!IsRefresh)
            {
                SearchResults = SearchGame(gameAchievements.Name);
            }
            else
            {
                SearchResults = new List<SearchResult>
                {
                    new SearchResult
                    {
                        Url = gameAchievements.SourcesLink.Url
                    }
                };
            }

            if (SearchResults.Count == 0)
            {
                logger.Warn($"No game found for {gameAchievements.Name} in SetRarety()");
                return;
            }

            // Find good game
            string SourceName = PlayniteTools.GetSourceName(PluginDatabase.PlayniteApi, gameAchievements.Id);
            SearchResult searchResult;

            if (!IsRefresh)
            {
                string normalizedGameName = gameAchievements.Name.NormalizeTitleForComparison();
                searchResult = SearchResults.Find(x => x.Name.NormalizeTitleForComparison().Equals(normalizedGameName, StringComparison.InvariantCultureIgnoreCase) && IsSamePlatform(x.Platform, SourceName));
            }
            else
            {
                searchResult = SearchResults.Find(x => x.Url == gameAchievements.SourcesLink.Url);
            }

            if (searchResult == null)
            {
                logger.Warn($"No matching game found for {gameAchievements.Name} in SetRarety()");
                return;
            }

            try
            {
                WebView.NavigateAndWait(searchResult.Url);
                string DataExophase = WebView.GetPageSource();

                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(DataExophase);

                var SectionAchievements = htmlDocument.QuerySelectorAll("ul.achievement, ul.trophy, ul.challenge");

                if (SectionAchievements == null || SectionAchievements.Count() == 0)
                {
                    logger.Warn($"No achievements list found in {searchResult.Url}");
                    return;
                }

                foreach (var Section in SectionAchievements)
                {
                    foreach (var SearchAchievements in Section.QuerySelectorAll("li"))
                    {
                        try
                        {
                            string achievementName = WebUtility.HtmlDecode(SearchAchievements.QuerySelector("a").InnerHtml).TrimWhitespace();
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
                                logger.Warn($"No matching achievements found for {achievementName} in {searchResult.Url}");
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


        private bool IsSamePlatform(string ExophasePlatformName, string PlaynitePlatformName)
        {
            int.TryParse(Regex.Replace(ExophasePlatformName, "[^0-9]", ""), out int ExophaseNumber);
            int.TryParse(Regex.Replace(PlaynitePlatformName, "[^0-9]", ""), out int PlayniteNumber);


            if (ExophasePlatformName.ToLower() == PlaynitePlatformName.ToLower())
            {
                return true;
            }

            if (PlaynitePlatformName.ToLower().IndexOf(ExophasePlatformName.ToLower()) > -1 && ExophasePlatformName.ToLower() == "ubisoft")
            {
                return true;
            }

            if (PlaynitePlatformName.ToLower() == "retroachievements" && ExophasePlatformName.ToLower() == "retro")
            {
                return true;
            }

            if (PlaynitePlatformName.ToLower().IndexOf("playstation") > -1 && ExophaseNumber == PlayniteNumber)
            {
                return true;
            }

            if (PlaynitePlatformName.ToLower().IndexOf("battle.net") > -1 && ExophasePlatformName.ToLower() == "blizzard")
            {
                return true;
            }

            if (PlaynitePlatformName.ToLower() == "xbox" && ExophasePlatformName.ToLower() == "windows 10")
            {
                return true;
            }

            if (PlaynitePlatformName.ToLower() == "xbox" && ExophasePlatformName.ToLower() == "windows 8")
            {
                return true;
            }

            if (PlaynitePlatformName.ToLower() == "xbox" && ExophasePlatformName.ToLower() == "xbox one")
            {
                return true;
            }

            if (PlaynitePlatformName.ToLower() == "xbox" && ExophasePlatformName.ToLower() == "xbox 360")
            {
                return true;
            }


            Common.LogDebug(true, $"No similar in IsSamePlatform({ExophasePlatformName}, {PlaynitePlatformName})");
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
            WebView.NavigateAndWait(UrlExophaseLogin);

            if (WebView.GetCurrentAddress().StartsWith(UrlExophaseLogin))
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
