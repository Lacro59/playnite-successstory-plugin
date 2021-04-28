using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        private const string UrlOverwatchProfil = @"https://playoverwatch.com/";
        private const string UrlOverwatchLogin = @"https://playoverwatch.com/login";
        private string UrlOverwatchProfilLocalised { get; set; }


        public BattleNetAchievements() : base()
        {
            WebViewOffscreen = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView();

            string Lang = CodeLang.GetEpicLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language);
            UrlOverwatchProfilLocalised = UrlOverwatchProfil + Lang;
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
            List<GameStats> AllStats = new List<GameStats>();
            string GameName = game.Name;
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            GameAchievements Result = PluginDatabase.GetDefault(game);
            Result.Items = AllAchievements;


            if (IsConnected())
            {
                WebViewOffscreen.NavigateAndWait(UrlOverwatchProfilLocalised);
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
                    WebViewOffscreen.NavigateAndWait(UrlOverwatchProfilLocalised + UrlProfil);
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
                                string Description = SearchAchievements.QuerySelector("div.tooltip-tip p.h6").InnerHtml;

                                AllAchievements.Add(new Achievements
                                {
                                    Name = Name,
                                    Description = Description,
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

                    try
                    {
                        AllStats = GetUsersStats(game, htmlDocument);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error on GetUsersStats({game.Name})");
                    }
                }
                else
                {
                    logger.Error($"No Overwath profil connected");
                }
            }

            Result.Name = GameName;
            Result.HaveAchivements = Total > 0;
            Result.Total = Total;
            Result.Unlocked = Unlocked;
            Result.Locked = Locked;
            Result.Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;
            Result.Items = AllAchievements;
            Result.ItemsStats = AllStats;

            return Result;
        }



        private List<GameStats> GetUsersStats(Game game, IHtmlDocument htmlDocument)
        {
            try
            {
                if (game.Name.ToLower() == "overwatch")
                {
                    return GetUsersStatsOverwatch(game, htmlDocument);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }


            List<GameStats> AllStats = new List<GameStats>();
            return AllStats;
        }

        private List<GameStats> GetUsersStatsOverwatch(Game game, IHtmlDocument htmlDocument)
        {
            List<GameStats> AllStats = new List<GameStats>();

            string MatchWin = htmlDocument.QuerySelector("p.masthead-detail span").InnerHtml;
            AllStats.Add(new GameStats
            {
                Name = "MatchWin", Value = double.Parse(Regex.Replace(MatchWin, "[^0-9]", ""))
            });

            AllStats.Add(new GameStats
            {
                Name = "PlayerLevel",
                Value = double.Parse(htmlDocument.QuerySelector(".player-level-tooltip .player-level .u-vertical-center").InnerHtml)
            });
            AllStats.Add(new GameStats
            {
                Name = "PlayerLevelFrame",
                ImageUrl = htmlDocument.QuerySelector(".player-level-tooltip .player-level").GetAttribute("style")
                    .Replace("background-image:url(", string.Empty).Replace(")", string.Empty).Trim()
            });
            AllStats.Add(new GameStats
            {
                Name = "PlayerLevelRank",
                ImageUrl = htmlDocument.QuerySelector(".player-level-tooltip .player-level .player-rank").GetAttribute("style")
                    .Replace("background-image:url(", string.Empty).Replace(")", string.Empty).Trim()
            });

            AllStats.Add(new GameStats
            {
                Name = "PlayerEndorsement",
                Value = double.Parse(htmlDocument.QuerySelector(".EndorsementIcon-tooltip .u-center").InnerHtml)
            });
            //AllStats.Add(new GameStats
            //{
            //    Name = "PlayerEndorsementFrame",
            //    ImageUrl = htmlDocument.QuerySelector("").GetAttribute("style")
            //        .Replace("background-image:url(", string.Empty).Replace(")", string.Empty).Trim()
            //});
            //AllStats.Add(new GameStats
            //{
            //    Name = "PlayerEndorsementRank",
            //    ImageUrl = htmlDocument.QuerySelector("").GetAttribute("style")
            //        .Replace("background-image:url(", string.Empty).Replace(")", string.Empty).Trim()
            //});


            //var CareerStatsSection = htmlDocument.QuerySelectorAll("#competitive .career-section")[1];


            AllStats = ParseDateMode(htmlDocument, AllStats, "QuickPlay");
            AllStats = ParseDateMode(htmlDocument, AllStats, "Competitive");

            return AllStats;
        }

        private List<GameStats> ParseDateMode(IHtmlDocument htmlDocument, List<GameStats> AllStats, string Mode)
        {
            #region TopHero
            var TopHeroSection = htmlDocument.QuerySelectorAll($"#{Mode.ToLower()} .career-section")[0];
            var DataProgressAll = TopHeroSection.QuerySelectorAll(".progress-category");

            foreach (var item in TopHeroSection.QuerySelectorAll(".dropdown-select-element option").Select((ElementSection, i) => new { i, ElementSection }))
            {
                try
                {
                    string CareerType = item.ElementSection.InnerHtml;

                    string id = item.ElementSection.GetAttribute("value");
                    var DataPogress = DataProgressAll.Where(x => x.GetAttribute("data-category-id") == id).FirstOrDefault();

                    if (DataPogress != null)
                    {
                        AllStats = ParseDataPogressOverwatch(DataPogress, AllStats, Mode, CareerType);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on CareerStats for {Mode}");
                }
            }
            #endregion

            #region Stats
            var CareerStatsSection = htmlDocument.QuerySelectorAll($"#{Mode.ToLower()} .career-section")[1];
            var DataTableSection = CareerStatsSection.QuerySelectorAll("div.row div.row");

            foreach (var item in CareerStatsSection.QuerySelectorAll(".dropdown-select-element option").Select((ElementSection, i) => new { i, ElementSection }))
            {
                try
                {
                    string CareerType = string.Empty;
                    if (item.i == 0)
                    {
                        CareerType = "AllHero";
                    }
                    else
                    {
                        CareerType = item.ElementSection.InnerHtml;
                    }

                    string id = item.ElementSection.GetAttribute("value");
                    var DataTable = DataTableSection.Where(x => x.GetAttribute("data-category-id") == id).FirstOrDefault();

                    if (DataTable != null)
                    {
                        AllStats = ParseDataTableOverwatch(DataTable, AllStats, Mode, CareerType);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on CareerStats for {Mode}");
                }
            }
            #endregion

            return AllStats;
        }

        private List<GameStats> ParseDataPogressOverwatch(AngleSharp.Dom.IElement DataPogress, List<GameStats> AllStats, string Mode, string CareerType)
        {
            foreach (var element in DataPogress.QuerySelectorAll(".ProgressBar"))
            {

                try
                {
                    string Name = element.QuerySelector(".ProgressBar-title").InnerHtml;
                    string ImageUrl = element.QuerySelector("img").GetAttribute("src");

                    double Value = 0;
                    TimeSpan Time = default(TimeSpan);

                    string ValueData = element.QuerySelector(".ProgressBar-description").InnerHtml;
                    double.TryParse(ValueData.Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator), out Value);

                    if (DateTime.TryParse(ValueData, out DateTime dateTime))
                    {
                        if (ValueData.Length < 6)
                        {
                            ValueData = "00:" + ValueData;
                        }

                        TimeSpan.TryParse(ValueData, out Time);
                    }

                    AllStats.Add(new GameStats
                    {
                        Name = Name,
                        Value = Value,

                        ImageUrl = ImageUrl,
                        Mode = Mode,
                        CareerType = CareerType,
                        Time = Time
                    });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }

            return AllStats;
        }

        private List<GameStats> ParseDataTableOverwatch(AngleSharp.Dom.IElement DataTable, List<GameStats> AllStats, string Mode, string CareerType)
        {
            string Category = DataTable.QuerySelector("th.DataTable-tableHeading h5").InnerHtml;

            foreach (var tr in DataTable.QuerySelectorAll("tr.DataTable-tableRow"))
            {
                try
                {
                    var td = tr.QuerySelectorAll("td");
                    string Name = td[0].InnerHtml;

                    double Value = 0;
                    TimeSpan Time = default(TimeSpan);

                    string ValueData = td[1].InnerHtml;
                    double.TryParse(ValueData.Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator), out Value);

                    if (DateTime.TryParse(ValueData, out DateTime dateTime))
                    {
                        if (ValueData.Length < 6)
                        {
                            ValueData = "00:" + ValueData;
                        }

                        TimeSpan.TryParse(ValueData, out Time);
                    }

                    AllStats.Add(new GameStats
                    {
                        Name = Name,
                        Value = Value,
                        Time = Time,

                        Mode = Mode,
                        CareerType = CareerType,
                        Category = Category
                    });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }

            return AllStats;
        }



        public override bool IsConfigured()
        {
            throw new NotImplementedException();
        }

        public override bool IsConnected()
        {
            var ApiStatus = GetApiStatus();

            if (ApiStatus == null)
            {
                return false;
            }

            if (ApiStatus.authenticated)
            {
                WebViewOffscreen.Navigate(UrlOverwatchProfil);
            }

            return ApiStatus.authenticated;
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
