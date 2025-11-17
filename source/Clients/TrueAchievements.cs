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

        internal static SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;

        private static string XboxUrlSearch => @"https://www.trueachievements.com/searchresults.aspx?search={0}";
        private static string SteamUrlSearch => @"https://truesteamachievements.com/searchresults.aspx?search={0}";

        public enum OriginData { Steam, Xbox }


        /// <summary>
        /// Search list game on truesteamachievements or trueachievements.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="originData"></param>
        /// <returns></returns>
        public static List<TrueAchievementSearch> SearchGame(Game game, OriginData originData)
        {
            List<TrueAchievementSearch> listSearchGames = new List<TrueAchievementSearch>();
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
                var sourceData = Web.DownloadSourceDataWebView(url).GetAwaiter().GetResult();
                string response = sourceData.Item1;

                if (response.IsNullOrEmpty())
                {
                    Logger.Warn($"No data from {url}");
                    return listSearchGames;
                }

                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(response);

                if (response.IndexOf("There are no matching search results, please change your search terms") > -1)
                {
                    return listSearchGames;
                }

                IElement SectionGames = htmlDocument.QuerySelector("#oSearchResults");
                if (SectionGames == null)
                {
                    string gameUrl = htmlDocument.QuerySelector("link[rel=\"canonical\"]")?.GetAttribute("href");
                    string gameImage = htmlDocument.QuerySelector("div.info img")?.GetAttribute("src");

                    listSearchGames.Add(new TrueAchievementSearch
                    {
                        GameUrl = gameUrl,
                        GameName = game.Name,
                        GameImage = gameImage
                    });
                }
                else
                {
                    foreach (IElement searchGame in SectionGames.QuerySelectorAll("tr"))
                    {
                        try
                        {
                            IHtmlCollection<IElement> gameInfos = searchGame.QuerySelectorAll("td");
                            if (gameInfos.Count() > 2)
                            {
                                string gameUrl = urlBase + gameInfos[0].QuerySelector("a")?.GetAttribute("href");
                                string gameName = gameInfos[1].QuerySelector("a")?.InnerHtml;
                                string gameImage = urlBase + gameInfos[0].QuerySelector("a img")?.GetAttribute("src");

                                string itemType = gameInfos[2].InnerHtml;

                                if (itemType.IsEqual("game"))
                                {
                                    listSearchGames.Add(new TrueAchievementSearch
                                    {
                                        GameUrl = gameUrl,
                                        GameName = gameName,
                                        GameImage = gameImage
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

            return listSearchGames;
        }


        /// <summary>
        /// Get the estimate time from game url on truesteamachievements or trueachievements.
        /// </summary>
        /// <param name="urlTrueAchievement"></param>
        /// <returns></returns>
        public static EstimateTimeToUnlock GetEstimateTimeToUnlock(string urlTrueAchievement)
        {
            EstimateTimeToUnlock estimateTimeToUnlock = new EstimateTimeToUnlock();

            if (urlTrueAchievement.IsNullOrEmpty())
            {
                Logger.Warn($"No url for GetEstimateTimeToUnlock()");
                return estimateTimeToUnlock;
            }

            try
            {
                var sourceData = Web.DownloadSourceDataWebView(urlTrueAchievement).GetAwaiter().GetResult();
                string response = sourceData.Item1;

                if (response.IsNullOrEmpty())
                {
                    Logger.Warn($"No data from {urlTrueAchievement}");
                    return estimateTimeToUnlock;
                }


                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(response);

                int numberDataCount = 0;
                foreach (IElement SearchElement in htmlDocument.QuerySelectorAll("div.game div.l1 div"))
                {
                    string title = SearchElement.GetAttribute("title");
                    if (!title.IsNullOrEmpty() && (title == "Maximum TrueAchievement" || title == "Maximum TrueSteamAchievement"))
                    {
                        string data = SearchElement.InnerHtml;
                        _ = int.TryParse(Regex.Replace(data, "[^0-9]", ""), out numberDataCount);
                        break;
                    }
                }

                foreach (IElement SearchElement in htmlDocument.QuerySelectorAll("div.game div.l2 a"))
                {
                    string title = SearchElement.GetAttribute("title");
                    if (!title.IsNullOrEmpty() && title == "Estimated time to unlock all achievements")
                    {
                        string estimateTime = SearchElement.InnerHtml
                            .Replace("<i class=\"fa fa-hourglass-end\"></i>", string.Empty)
                            .Replace("<i class=\"fa fa-clock-o\"></i>", string.Empty)
                            .Trim();

                        int estimateTimeMin = 0;
                        int estimateTimeMax = 0;
                        int index = 0;
                        foreach (string item in estimateTime.Replace("h", string.Empty).Split('-'))
                        {
                            _ = index == 0 ? int.TryParse(item.Replace("+", string.Empty), out estimateTimeMin) : int.TryParse(item, out estimateTimeMax);
                            index++;
                        }

                        estimateTimeToUnlock = new EstimateTimeToUnlock
                        {
                            DataCount = numberDataCount,
                            EstimateTime = estimateTime,
                            EstimateTimeMin = estimateTimeMin,
                            EstimateTimeMax = estimateTimeMax
                        };
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            if (estimateTimeToUnlock.EstimateTimeMin == 0)
            {
                Logger.Warn($"No {(urlTrueAchievement.ToLower().Contains("truesteamachievements") ? "TrueSteamAchievements" : "TrueAchievements")} data found");
            }

            return estimateTimeToUnlock;
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