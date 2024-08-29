using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace SuccessStory.Clients
{
    public class TrueAchievements
    {
        private static ILogger Logger => LogManager.GetLogger();

        private static SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;

        public static string XboxUrlSearch => @"https://www.trueachievements.com/searchresults.aspx?search={0}";
        public static string SteamUrlSearch => @"https://truesteamachievements.com/searchresults.aspx?search={0}";

        public enum OriginData { Steam, Xbox }


        /// <summary>
        /// Search list game on truesteamachievements or trueachievements.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="originData"></param>
        /// <returns></returns>
        public static List<TrueAchievementSearch> SearchGame(Game game, OriginData originData)
        {
            List<TrueAchievementSearch> ListSearchGames = new List<TrueAchievementSearch>();
            string url;
            string urlBase;
            if (originData == OriginData.Steam)
            {
                //TODO: Decide if editions should be removed here
                url = string.Format(SteamUrlSearch, WebUtility.UrlEncode(PlayniteTools.NormalizeGameName(game.Name, true)));
                urlBase = @"https://truesteamachievements.com";
            }
            else
            {
                //TODO: Decide if editions should be removed here
                url = string.Format(XboxUrlSearch, WebUtility.UrlEncode(PlayniteTools.NormalizeGameName(game.Name, true)));
                urlBase = @"https://www.trueachievements.com";
            }


            try
            {
                string WebData = string.Empty;
                using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                {
                    WebViewOffscreen.NavigateAndWait(url);
                    WebData = WebViewOffscreen.GetPageSource();
                }

                if (WebData.IsNullOrEmpty())
                {
                    Logger.Warn($"No data from {url}");
                    return ListSearchGames;
                }

                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(WebData);

                if (WebData.IndexOf("There are no matching search results, please change your search terms") > -1)
                {
                    return ListSearchGames;
                }

                IElement SectionGames = htmlDocument.QuerySelector("#oSearchResults");
                if (SectionGames == null)
                {
                    string GameUrl = htmlDocument.QuerySelector("link[rel=\"canonical\"]")?.GetAttribute("href");
                    string GameImage = htmlDocument.QuerySelector("div.info img")?.GetAttribute("src");

                    ListSearchGames.Add(new TrueAchievementSearch
                    {
                        GameUrl = GameUrl,
                        GameName = game.Name,
                        GameImage = GameImage
                    });
                }
                else
                {
                    foreach (IElement SearchGame in SectionGames.QuerySelectorAll("tr"))
                    {
                        try
                        {
                            IHtmlCollection<IElement> GameInfos = SearchGame.QuerySelectorAll("td");
                            if (GameInfos.Count() > 2)
                            {
                                string GameUrl = urlBase + GameInfos[0].QuerySelector("a")?.GetAttribute("href");
                                string GameName = GameInfos[1].QuerySelector("a")?.InnerHtml;
                                string GameImage = urlBase + GameInfos[0].QuerySelector("a img")?.GetAttribute("src");

                                string ItemType = GameInfos[2].InnerHtml;

                                if (ItemType.IsEqual("game"))
                                {
                                    ListSearchGames.Add(new TrueAchievementSearch
                                    {
                                        GameUrl = GameUrl,
                                        GameName = GameName,
                                        GameImage = GameImage
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return ListSearchGames;
        }


        /// <summary>
        /// Get the estimate time from game url on truesteamachievements or trueachievements.
        /// </summary>
        /// <param name="UrlTrueAchievement"></param>
        /// <returns></returns>
        public static EstimateTimeToUnlock GetEstimateTimeToUnlock(string UrlTrueAchievement)
        {
            EstimateTimeToUnlock EstimateTimeToUnlock = new EstimateTimeToUnlock();

            if (UrlTrueAchievement.IsNullOrEmpty())
            {
                Logger.Warn($"No url for GetEstimateTimeToUnlock()");
                return EstimateTimeToUnlock;
            }

            try
            {
                string WebData = string.Empty;
                using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                {
                    WebViewOffscreen.NavigateAndWait(UrlTrueAchievement);
                    WebData = WebViewOffscreen.GetPageSource();
                }

                if (WebData.IsNullOrEmpty())
                {
                    Logger.Warn($"No data from {UrlTrueAchievement}");
                    return EstimateTimeToUnlock;
                }


                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(WebData);

                int NumberDataCount = 0;
                foreach (IElement SearchElement in htmlDocument.QuerySelectorAll("div.game div.l1 div"))
                {
                    string Title = SearchElement.GetAttribute("title");
                    if (!Title.IsNullOrEmpty() && (Title == "Maximum TrueAchievement" || Title == "Maximum TrueSteamAchievement"))
                    {
                        string data = SearchElement.InnerHtml;
                        _ = int.TryParse(Regex.Replace(data, "[^0-9]", ""), out NumberDataCount);
                        break;
                    }
                }

                foreach (IElement SearchElement in htmlDocument.QuerySelectorAll("div.game div.l2 a"))
                {
                    string Title = SearchElement.GetAttribute("title");
                    if (!Title.IsNullOrEmpty() && Title == "Estimated time to unlock all achievements")
                    {
                        string EstimateTime = SearchElement.InnerHtml
                            .Replace("<i class=\"fa fa-hourglass-end\"></i>", string.Empty)
                            .Replace("<i class=\"fa fa-clock-o\"></i>", string.Empty)
                            .Trim();

                        int EstimateTimeMin = 0;
                        int EstimateTimeMax = 0;
                        int index = 0;
                        foreach (string item in EstimateTime.Replace("h", string.Empty).Split('-'))
                        {
                            _ = index == 0 ? int.TryParse(item.Replace("+", string.Empty), out EstimateTimeMin) : int.TryParse(item, out EstimateTimeMax);
                            index++;
                        }

                        EstimateTimeToUnlock = new EstimateTimeToUnlock
                        {
                            DataCount = NumberDataCount,
                            EstimateTime = EstimateTime,
                            EstimateTimeMin = EstimateTimeMin,
                            EstimateTimeMax = EstimateTimeMax
                        };
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            if (EstimateTimeToUnlock.EstimateTimeMin == 0)
            {
                Logger.Warn($"No {(UrlTrueAchievement.ToLower().Contains("truesteamachievements") ? "TrueSteamAchievements" : "TrueAchievements")} data found");
            }

            return EstimateTimeToUnlock;
        }
    }


    public class TrueAchievementSearch
    {
        public string GameUrl { get; set; }
        public string GameName { get; set; }
        public string GameImage { get; set; }
    }

    public class EstimateTimeToUnlock
    {
        public int DataCount { get; set; }
        public string EstimateTime { get; set; }
        public int EstimateTimeMin { get; set; }
        public int EstimateTimeMax { get; set; }
    }
}
