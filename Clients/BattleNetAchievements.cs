using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPlayniteShared.PluginLibrary.BattleNetLibrary.Models;
using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;

namespace SuccessStory.Clients
{
    class BattleNetAchievements : GenericAchievements
    {
        private IWebView WebViewOffscreen;

        private const string apiStatusUrl = @"https://account.blizzard.com/api/";
        private const string UrlProfil = @"https://playoverwatch.com/";
        private string UrlProfilLocalised { get; set; }


        public BattleNetAchievements() : base()
        {
            WebViewOffscreen = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView();

            string Lang = CodeLang.GetEpicLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language);
            UrlProfilLocalised = UrlProfil + Lang;
        }


        public override GameAchievements GetAchievements(Game game)
        {
            try
            {
                if (game.Name.ToLower() == "overwatch")
                {
                    return GetAchievementsOverwatch(game);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }


            List<Achievements> AllAchievements = new List<Achievements>();
            string GameName = game.Name;
            bool HaveAchivements = false;
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            GameAchievements Result = SuccessStory.PluginDatabase.GetDefault(game);
            Result.Items = AllAchievements;
            Result.Name = GameName;
            Result.HaveAchivements = HaveAchivements;
            Result.Total = Total;
            Result.Unlocked = Unlocked;
            Result.Locked = Locked;
            Result.Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;
            Result.Items = AllAchievements;

            return Result;
        }

        private GameAchievements GetAchievementsOverwatch(Game game)
        {
            List<Achievements> AllAchievements = new List<Achievements>();
            string GameName = game.Name;
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            GameAchievements Result = SuccessStory.PluginDatabase.GetDefault(game);
            Result.Items = AllAchievements;


            if (IsConnected())
            {
                WebViewOffscreen.NavigateAndWait(UrlProfilLocalised);
                string data = WebViewOffscreen.GetPageSource();

                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(data);

                string UrlProfil = string.Empty;
                foreach (var SearchElement in htmlDocument.QuerySelectorAll("a.ow-SiteNavLogin-profile"))
                {
                    UrlProfil = SearchElement.GetAttribute("href");
                }

                if (!UrlProfil.IsNullOrEmpty())
                {
                    WebViewOffscreen.NavigateAndWait(UrlProfilLocalised + UrlProfil);
                    data = WebViewOffscreen.GetPageSource();

                    htmlDocument = parser.Parse(data);

                    var SectionAchievements = htmlDocument.QuerySelector("#achievements-section");

                    foreach (var SearchCategory in SectionAchievements.QuerySelectorAll("div.toggle-display"))
                    {
                        string Category = SearchCategory.GetAttribute("data-category-id");

                        foreach (var SearchAchievements in SearchCategory.QuerySelectorAll("div.achievement-card-container"))
                        {
                            try
                            {
                                bool IsUnlocked = SearchAchievements.QuerySelector("div.m-disabled") == null;

                                string UrlImage = SearchAchievements.QuerySelector("img.media-card-fill").GetAttribute("src");
                                string Name = SearchAchievements.QuerySelector("div.media-card-title").InnerHtml;

                                AllAchievements.Add(new Achievements
                                {
                                    Name = Name,
                                    UrlUnlocked = UrlImage,
                                    DateUnlocked = (IsUnlocked) ? new DateTime(1982, 12, 15, 0, 0, 0, 0) : default(DateTime),

                                    Category = Category
                                });

                                Total++;
                                if (IsUnlocked)
                                {
                                    Unlocked++;
                                }
                                else
                                {
                                    Locked++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false);
                            }
                        }
                    }
                }
            }

            Result.Name = GameName;
            Result.HaveAchivements = Total > 0;
            Result.Total = Total;
            Result.Unlocked = Unlocked;
            Result.Locked = Locked;
            Result.Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;
            Result.Items = AllAchievements;

            return Result;
        }


        public override bool IsConfigured()
        {
            throw new NotImplementedException();
        }

        public override bool IsConnected()
        {
            return GetApiStatus().authenticated;
        }


        private BattleNetApiStatus GetApiStatus()
        {
            // This refreshes authentication cookie
            WebViewOffscreen.NavigateAndWait("https://account.blizzard.com:443/oauth2/authorization/account-settings");
            WebViewOffscreen.NavigateAndWait(apiStatusUrl);
            var textStatus = WebViewOffscreen.GetPageText();
            return Serialization.FromJson<BattleNetApiStatus>(textStatus);
        }
    }
}
