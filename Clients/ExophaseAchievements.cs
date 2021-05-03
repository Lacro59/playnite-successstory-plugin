using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    class ExophaseAchievements : GenericAchievements
    {
        private IWebView WebView;

        private const string UrlExophaseSearch = @"https://www.exophase.com/games/?q={0}";
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

        public GameAchievements GetAchievements(Game game, SearchResult searchResult, bool IsTwo = false)
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
                var SectionAchievements = htmlDocument.QuerySelector("ul.achievement");
                if (SectionAchievements == null)
                {
                    SectionAchievements = htmlDocument.QuerySelector("ul.trophy");
                }

                if (SectionAchievements == null)
                {
                    logger.Warn($"Problem with {searchResult.Url}");
                    if (!IsTwo)
                    {
                        return GetAchievements(game, searchResult, true);
                    }
                }
                else
                {
                    foreach (var SearchAchievements in SectionAchievements.QuerySelectorAll("li"))
                    {
                        try
                        {
                            float.TryParse(SearchAchievements.GetAttribute("data-average").Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator), out float Percent);

                            string UrlUnlocked = SearchAchievements.QuerySelector("img").GetAttribute("src");
                            string Name = SearchAchievements.QuerySelector("a").InnerHtml;
                            string Description = SearchAchievements.QuerySelector("div.award-description p").InnerHtml;
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

            return Result;
        }


        public List<SearchResult> SearchGame(string Name, bool IsTwo = false)
        {
            string UrlSearch = string.Format(UrlExophaseSearch, WebUtility.UrlEncode(Name));

            WebView.NavigateAndWait(UrlSearch);
            string DataExophaseSearch = WebView.GetPageSource();

            HtmlParser parser = new HtmlParser();
            IHtmlDocument htmlDocument = parser.Parse(DataExophaseSearch);

            List<SearchResult> ListSearchGames = new List<SearchResult>();
            var SectionGames = htmlDocument.QuerySelector("ul.list-unordered-base");
            if (SectionGames == null)
            {
                Common.LogDebug(true, $"Error on SectionGames({Name}) for Exophase search");
                if (!IsTwo)
                {
                    return SearchGame(Name, true);
                }
                else
                {
                    return ListSearchGames;
                }
            }

            var SectionLi = SectionGames.QuerySelectorAll("li");
            if (SectionLi == null)
            {
                Common.LogDebug(true, $"Error on SectionGames({Name}) for Exophase search");
                if (!IsTwo)
                {
                    return SearchGame(Name, true);
                }
                else
                {
                    return ListSearchGames;
                }
            }

            foreach (var SearchGame in SectionGames.QuerySelectorAll("li"))
            {
                try
                {
                    var aElement = SearchGame.QuerySelectorAll("a");

                    string GameUrl = aElement[1].GetAttribute("href");
                    string GameName = aElement[1].InnerHtml;

                    string GameImage = SearchGame.QuerySelector("img").GetAttribute("src");
                    string GamePlatform = SearchGame.QuerySelector("div.platforms span").InnerHtml;

                    int.TryParse(SearchGame.QuerySelector("div.col-4 span span").InnerHtml, out int AchievementsCount);

                    ListSearchGames.Add(new SearchResult
                    {
                        Url = GameUrl,
                        Name = GameName,
                        UrlImage = GameImage,
                        Platform = GamePlatform,
                        AchievementsCount = AchievementsCount
                    });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }

            return ListSearchGames;
        }


        public override bool IsConfigured()
        {
            throw new NotImplementedException();
        }

        public override bool IsConnected()
        {
            throw new NotImplementedException();
        }


        public void Login()
        {
            var view = PluginDatabase.PlayniteApi.WebViews.CreateView(600, 600);

            logger.Info("Login()");

            view.LoadingChanged += (s, e) =>
            {
                if (view.GetCurrentAddress().IndexOf("https://www.exophase.com/account/") > -1 && view.GetCurrentAddress().IndexOf(UrlExophaseLogout) == -1)
                {
                    view.Close();
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
                logger.Warn("User is not connected");
                return false;
            }
            logger.Info("User is connected");
            return true;
        }
    }
}
