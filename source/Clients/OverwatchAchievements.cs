using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
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
using System.Threading.Tasks;
using static CommonPluginsShared.PlayniteTools;

namespace SuccessStory.Clients
{
    // TODO KO
    internal class OverwatchAchievements : BattleNetAchievements
    {
        private static string UrlOverwatchProfil => @"https://playoverwatch.com";
        private static string UrlOverwatchLogin => $"{UrlOverwatchProfil}/login";
        private static string UrlOverwatchProfilLocalised { get; set; } = $"{UrlOverwatchProfil}/" + "{0}";

        private string UrlProfil { get; set; } = @"https://overwatch.blizzard.com/career";
        private List<ColorElement> OverwatchColor => new List<ColorElement>
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


        public OverwatchAchievements() : base("Overwatch", CodeLang.GetEpicLang(API.Instance.ApplicationSettings.Language))
        {
            UrlOverwatchProfilLocalised = string.Format(UrlOverwatchProfilLocalised, LocalLang);
        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> AllAchievements = new List<Achievement>();
            List<GameStats> AllStats = new List<GameStats>();


            //string UrlProfil = string.Empty;
            if (IsConnected())
            {
                if (!UrlProfil.IsNullOrEmpty())
                {
                    string data = Web.DownloadStringData(UrlOverwatchProfilLocalised + UrlProfil, GetCookies()).GetAwaiter().GetResult();
                    string dataEn = data;
                    if (!UrlOverwatchProfilLocalised.Contains("en-US"))
                    {
                        dataEn = Web.DownloadStringData(UrlOverwatchProfil + "en-US" + UrlProfil + UrlProfil, GetCookies()).GetAwaiter().GetResult();
                    }

                    HtmlParser parser = new HtmlParser();
                    IHtmlDocument htmlDocument = parser.Parse(data);
                    IHtmlDocument htmlDocumentEn = parser.Parse(dataEn);

                    IElement SectionAchievements = htmlDocument.QuerySelector("#achievements-section");

                    foreach (IElement SearchCategory in SectionAchievements.QuerySelectorAll("div.toggle-display"))
                    {
                        string Category = SearchCategory.GetAttribute("data-category-id");

                        foreach (IElement SearchAchievements in SearchCategory.QuerySelectorAll("div.achievement-card-container"))
                        {
                            try
                            {
                                string Id = SearchAchievements.QuerySelector("div.tooltip-handle").GetAttribute("data-tooltip");
                                IElement dataApi = htmlDocumentEn.QuerySelector($"#{Id} h6.h5");
                                string ApiName = string.Empty;
                                if (dataApi != null)
                                {
                                    ApiName = WebUtility.HtmlDecode(dataApi.InnerHtml);
                                }

                                bool IsUnlocked = SearchAchievements.QuerySelector("div.m-disabled") == null;

                                string UrlImage = SearchAchievements.QuerySelector("img.media-card-fill").GetAttribute("src");
                                string Name = WebUtility.HtmlDecode(SearchAchievements.QuerySelector("div.media-card-title").InnerHtml);
                                string Description = WebUtility.HtmlDecode(SearchAchievements.QuerySelector("div.tooltip-tip p.h6").InnerHtml);

                                AllAchievements.Add(new Achievement
                                {
                                    ApiName = ApiName,
                                    Name = Name,
                                    Description = Description,
                                    UrlUnlocked = UrlImage,
                                    DateUnlocked = (IsUnlocked) ? new DateTime(1982, 12, 15, 0, 0, 0, 0) : default(DateTime),

                                    Category = Category
                                });
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                            }
                        }
                    }

                    try
                    {
                        AllStats = GetUsersStats(htmlDocument);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error on GetUsersStats({game.Name})", true, PluginDatabase.PluginName);
                    }
                }
                else
                {
                    ShowNotificationPluginNoAuthenticate(ExternalPlugin.BattleNetLibrary);
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(ExternalPlugin.BattleNetLibrary);
            }


            gameAchievements.Items = AllAchievements;
            gameAchievements.ItemsStats = AllStats;


            // Set source link
            if (gameAchievements.HasAchievements)
            {
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = "Overwatch",
                    Name = "Battle.net",
                    Url = UrlOverwatchProfilLocalised + UrlProfil
                };
            }

            // Set rarity from Exophase
            if (gameAchievements.HasAchievements)
            {
                SuccessStory.ExophaseAchievements.SetRarety(gameAchievements, Services.SuccessStoryDatabase.AchievementSource.Overwatch);
            }

            gameAchievements.SetRaretyIndicator();
            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            return false;
            /*
            if (PlayniteTools.IsDisabledPlaynitePlugins("BattleNetLibrary"))
            {
                ShowNotificationPluginDisable(ResourceProvider.GetString("LOCSuccessStoryNotificationsBattleNetDisabled"));
                return false;
            }

            if (CachedConfigurationValidationResult == null)
            {
                CachedConfigurationValidationResult = IsConnected();

                if (!(bool)CachedConfigurationValidationResult)
                {
                    ShowNotificationPluginNoAuthenticate(ResourceProvider.GetString("LOCSuccessStoryNotificationsBattleNetNoAuthenticate"), ExternalPlugin.BattleNetLibrary);
                }
            }
            else if (!(bool)CachedConfigurationValidationResult)
            {
                ShowNotificationPluginErrorMessage();
            }

            return (bool)CachedConfigurationValidationResult;
            */
        }


        public override bool IsConnected()
        {
            return false;
            /*
            if (CachedIsConnectedResult == null)
            {
                CachedIsConnectedResult = false;
                string data = string.Empty;
                List<HttpCookie> cookies = null;
                using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                {
                    WebViewOffscreen.NavigateAndWait(UrlOverwatchProfil);
                    _ = WebViewOffscreen.GetPageSource();

                    cookies = WebViewOffscreen.GetCookies().Where(
                            x => x.Domain.Contains("playoverwatch.com")
                                        || x.Domain.Contains("blizzard.com", StringComparison.OrdinalIgnoreCase)
                                        || x.Domain.Contains("battle.net", StringComparison.OrdinalIgnoreCase)
                    ).ToList();

                    data = Web.DownloadStringData("https://overwatch.blizzard.com/nav/authenticate/", cookies).GetAwaiter().GetResult();
                    if (Serialization.TryFromJson(data, out BattleNetUser battleNetSc2User))
                    {
                        CachedIsConnectedResult = true;
                        SetCookies(cookies);
                    }
                }
            }

            return (bool)CachedIsConnectedResult;
            */
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableOverwatchAchievements;
        }
        #endregion


        #region Battle.net - Overwatch
        private List<GameStats> GetUsersStats(IHtmlDocument htmlDocument)
        {
            List<GameStats> AllStats = new List<GameStats>();

            string MatchWin = htmlDocument.QuerySelector("p.masthead-detail span").InnerHtml;
            AllStats.Add(new GameStats
            {
                Name = "MatchWin",
                Value = double.Parse(Regex.Replace(MatchWin, "[^0-9]", ""))
            });

            AllStats.Add(new GameStats
            {
                Name = "PlayerPortrait",
                ImageUrl = htmlDocument.QuerySelector(".player-portrait").GetAttribute("src")
            });

            AllStats.Add(new GameStats
            {
                Name = "PlayerName",
                DisplayName = htmlDocument.QuerySelector(".header-masthead").InnerHtml
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

            AllStats = ParseDateMode(htmlDocument, AllStats, "QuickPlay");
            AllStats = ParseDateMode(htmlDocument, AllStats, "Competitive");

            return AllStats;
        }

        private List<GameStats> ParseDateMode(IHtmlDocument htmlDocument, List<GameStats> AllStats, string Mode)
        {
            #region TopHero
            var TopHeroSection = htmlDocument.QuerySelectorAll($"#{Mode.ToLower()} .career-section")[0];
            var DataProgressAll = TopHeroSection.QuerySelectorAll(".progress-category");

            //foreach (var item in TopHeroSection.QuerySelectorAll(".dropdown-select-element option").Select((ElementSection, i) => new { i, ElementSection }))
            foreach (var item in TopHeroSection.QuerySelectorAll("select option").Select((ElementSection, i) => new { i, ElementSection }))
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
                    Common.LogError(ex, false, $"Error on CareerStats for {Mode}", true, PluginDatabase.PluginName);
                }
            }
            #endregion

            #region Stats
            var CareerStatsSection = htmlDocument.QuerySelectorAll($"#{Mode.ToLower()} .career-section")[1];
            var DataTableSection = CareerStatsSection.QuerySelectorAll("div.row div.row");

            //foreach (var item in CareerStatsSection.QuerySelectorAll(".dropdown-select-element option").Select((ElementSection, i) => new { i, ElementSection }))
            foreach (var item in CareerStatsSection.QuerySelectorAll("select option").Select((ElementSection, i) => new { i, ElementSection }))
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
                    Common.LogError(ex, false, $"Error on CareerStats for {Mode}", true, PluginDatabase.PluginName);
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
                    string Name = element.QuerySelector(".ProgressBar-title")?.InnerHtml;
                    string ImageUrl = element.QuerySelector("img")?.GetAttribute("src");

                    string stringClass = element.QuerySelector(".ProgressBar-bar")?.GetAttribute("class")
                        .Replace("ProgressBar-bar", string.Empty).Replace("velocity-animating", string.Empty).Trim();
                    string Color = OverwatchColor.Find(x => x.Name == stringClass)?.Color;

                    double Value = 0;
                    TimeSpan Time = default(TimeSpan);

                    string ValueData = element.QuerySelector(".ProgressBar-description")?.InnerHtml;

                    string DisplayName = string.Empty;
                    if (ValueData?.IndexOf("%") > -1)
                    {
                        DisplayName = ValueData;
                    }

                    double.TryParse(ValueData?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Replace("%", string.Empty), out Value);

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
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
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
                    double.TryParse(ValueData.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator), out Value);

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
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }

            return AllStats;
        }
        #endregion


        #region Errors
        public override void ShowNotificationPluginNoAuthenticate(ExternalPlugin PluginSource)
        {
            string message = string.Format(ResourceProvider.GetString("LOCCommonStoresNoAuthenticate"), ClientName);
            LastErrorId = $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-noauthenticate";
            LastErrorMessage = message;
            Logger.Warn($"{ClientName} user is not authenticated");

            API.Instance.Notifications.Add(new NotificationMessage(
                $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-disabled",
                $"{PluginDatabase.PluginName}\r\n{message}",
                NotificationType.Error,
                () =>
                {
                    using (var WebView = API.Instance.WebViews.CreateView(400, 600))
                    {
                        WebView.LoadingChanged += (s, e) =>
                        {
                            string address = WebView.GetCurrentAddress();
                            if (!address.Contains(UrlLogin) && !address.Contains(UrlOverwatchLogin))
                            {
                                ResetCachedConfigurationValidationResult();
                                ResetCachedIsConnectedResult();
                                WebView.Close();
                            }
                        };

                        WebView.Navigate(UrlOverwatchLogin);
                        WebView.OpenDialog();
                    }
                }
            ));
        }
        #endregion
    }


    public class ColorElement
    {
        public string Name { get; set; }
        public string Color { get; set; }
    }
}
