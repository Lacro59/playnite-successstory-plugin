using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPlayniteShared.PluginLibrary.BattleNetLibrary.Models;
using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SuccessStory.Clients
{
    class BattleNetAchievements : GenericAchievements
    {
        private IWebView WebViewOffscreen;

        private const string apiStatusUrl = @"https://account.blizzard.com/api/";


        private const string UrlOverwatchProfil = @"https://playoverwatch.com/";
        private const string UrlOverwatchLogin = @"https://playoverwatch.com/login";
        private string UrlOverwatchProfilLocalised { get; set; }

        private List<ColorElement> OverwatchColor = new List<ColorElement>
        {
            new ColorElement { Name = "ow-ana-color", Color = "#9c978a" },
            new ColorElement { Name = "ow-ashe-color", Color = "#b3a05f" },
            new ColorElement { Name = "ow-bastion-color", Color = "#24f9f8" },
            new ColorElement { Name = "ow-baptiste-color", Color = "#2892a8" },
            new ColorElement { Name = "ow-brigitte-color", Color = "#efb016" },
            new ColorElement { Name = "ow-doomfist-color", Color = "#762c21" },
            new ColorElement { Name = "ow-dva-color", Color = "#ee4bb5" },
            new ColorElement { Name = "ow-echo-color", Color = "#89c8ff" },
            new ColorElement { Name = "ow-genji-color", Color = "#abe50b" },
            new ColorElement { Name = "ow-hanzo-color", Color = "#837c46" },
            new ColorElement { Name = "ow-junkrat-color", Color = "#fbd73a" },
            new ColorElement { Name = "ow-lucio-color", Color = "#aaf531" },
            new ColorElement { Name = "ow-mccree-color", Color = "#c23f46" },
            new ColorElement { Name = "ow-mei-color", Color = "#87d7f6" },
            new ColorElement { Name = "ow-mercy-color", Color = "#fcd849" },
            new ColorElement { Name = "ow-moira-color", Color = "#7112f4" },
            new ColorElement { Name = "ow-orisa-color", Color = "#ccb370" },
            new ColorElement { Name = "ow-pharah-color", Color = "#3461a4" },
            new ColorElement { Name = "ow-reaper-color", Color = "#333" },
            new ColorElement { Name = "ow-reinhardt-color", Color = "#b9b5ad" },
            new ColorElement { Name = "ow-roadhog-color", Color = "#54515a" },
            new ColorElement { Name = "ow-sigma-color", Color = "#3ba" },
            new ColorElement { Name = "ow-soldier-76-color", Color = "#525d9b" },
            new ColorElement { Name = "ow-sombra-color", Color = "#9762ec" },
            new ColorElement { Name = "ow-symmetra-color", Color = "#3e90b5" },
            new ColorElement { Name = "ow-torbjorn-color", Color = "#b04a33" },
            new ColorElement { Name = "ow-tracer-color", Color = "#ffcf35" },
            new ColorElement { Name = "ow-widowmaker-color", Color = "#af5e9e" },
            new ColorElement { Name = "ow-winston-color", Color = "#595959" },
            new ColorElement { Name = "ow-wrecking-ball-color", Color = "#4a575f" },
            new ColorElement { Name = "ow-zarya-color", Color = "#ff73c1" },
            new ColorElement { Name = "ow-zenyatta-color", Color = "#e1c931" }
        };


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

                    WebViewOffscreen.NavigateAndWait(UrlOverwatchProfil);
                    WebViewOffscreen.NavigateAndWait(UrlOverwatchProfil + "en-US" + UrlProfil);
                    string dataEn = WebViewOffscreen.GetPageSource();

                    htmlDocument = parser.Parse(data);
                    IHtmlDocument htmlDocumentEn = parser.Parse(dataEn);

                    var SectionAchievements = htmlDocument.QuerySelector("#achievements-section");

                    foreach (var SearchCategory in SectionAchievements.QuerySelectorAll("div.toggle-display"))
                    {
                        string Category = SearchCategory.GetAttribute("data-category-id");

                        foreach (var SearchAchievements in SearchCategory.QuerySelectorAll("div.achievement-card-container"))
                        {
                            try
                            {
                                string Id = SearchAchievements.QuerySelector("div.tooltip-handle").GetAttribute("data-tooltip");
                                var dataApi = htmlDocumentEn.QuerySelector($"#{Id} h6.h5");
                                string ApiName = string.Empty;
                                if (dataApi != null)
                                {
                                    ApiName = dataApi.InnerHtml;
                                }

                                bool IsUnlocked = SearchAchievements.QuerySelector("div.m-disabled") == null;

                                string UrlImage = SearchAchievements.QuerySelector("img.media-card-fill").GetAttribute("src");
                                string Name = SearchAchievements.QuerySelector("div.media-card-title").InnerHtml;
                                string Description = SearchAchievements.QuerySelector("div.tooltip-tip p.h6").InnerHtml;

                                AllAchievements.Add(new Achievements
                                {
                                    ApiName = ApiName,
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

            if (Result.HaveAchivements)
            {
                ExophaseAchievements exophaseAchievements = new ExophaseAchievements();
                exophaseAchievements.SetRarety(Result);
            }

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
                Name = "PlayerPortrait", ImageUrl = htmlDocument.QuerySelector(".player-portrait").GetAttribute("src")
            });

            AllStats.Add(new GameStats
            {
                Name = "PlayerName", DisplayName = htmlDocument.QuerySelector(".header-masthead").InnerHtml
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
                    var DataTables = DataTableSection.Where(x => x.GetAttribute("data-category-id") == id).FirstOrDefault();

                    if (DataTables != null)
                    {
                        foreach (var DataTable in DataTables.QuerySelectorAll("table.DataTable"))
                        {
                            AllStats = ParseDataTableOverwatch(DataTable, AllStats, Mode, CareerType);
                        }
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

                    string stringClass = element.QuerySelector(".ProgressBar-bar").GetAttribute("class")
                        .Replace("ProgressBar-bar", string.Empty).Replace("velocity-animating", string.Empty).Trim();
                    string Color = OverwatchColor.Find(x => x.Name == stringClass).Color;

                    double Value = 0;
                    TimeSpan Time = default(TimeSpan);

                    string ValueData = element.QuerySelector(".ProgressBar-description").InnerHtml;

                    string DisplayName = string.Empty;
                    if (ValueData.IndexOf("%") > -1)
                    {
                        DisplayName = ValueData;
                    }

                    double.TryParse(ValueData.Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Replace("%", string.Empty), out Value);

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
                        DisplayName = DisplayName,
                        Value = Value,

                        ImageUrl = ImageUrl,
                        Mode = Mode,
                        CareerType = CareerType,
                        Category = "TopHero",
                        Color = Color,
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
            string SubCategory = DataTable.QuerySelector("th.DataTable-tableHeading h5").InnerHtml;

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
                        Category = "CarrerStats",
                        SubCategory = SubCategory
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
            try
            {
                // This refreshes authentication cookie
                WebViewOffscreen.NavigateAndWait("https://account.blizzard.com:443/oauth2/authorization/account-settings");
                WebViewOffscreen.NavigateAndWait(apiStatusUrl);
                var textStatus = WebViewOffscreen.GetPageText();
                return Serialization.FromJson<BattleNetApiStatus>(textStatus);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }


    class ColorElement
    {
        public string Name { get; set; }
        public string Color { get; set; }
    } 
}
