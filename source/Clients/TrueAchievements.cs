using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    class TrueAchievements
    {
        public static string XboxUrlSearch = @"https://www.trueachievements.com/searchresults.aspx?search={0}";
        public static string SteamUrlSearch = @"https://truesteamachievements.com/searchresults.aspx?search={0}";


        public enum OriginData
        {
            Steam, Xbox
        }


        public static List<TrueAchievementSearch> SearchGame(Game game, OriginData originData)
        {
            List<TrueAchievementSearch> ListSearchGames = new List<TrueAchievementSearch>();

            string Url = string.Empty;
            string UrlBase = string.Empty;
            if (originData == OriginData.Steam)
            {
                Url = string.Format(SteamUrlSearch, WebUtility.UrlEncode(game.Name));
                UrlBase = @"https://truesteamachievements.com";
            }
            else
            {
                Url = string.Format(XboxUrlSearch, WebUtility.UrlEncode(game.Name));
                UrlBase = @"https://www.trueachievements.com";
            }


            try
            {
                string WebData = Web.DownloadStringData(Url).GetAwaiter().GetResult();

                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(WebData);

                if (WebData.IndexOf("There are no matching search results, please change your search terms") > -1)
                {
                    return ListSearchGames;
                }

                var SectionGames = htmlDocument.QuerySelector("#oSearchResults");

                if (SectionGames == null)
                {
                    string GameUrl = htmlDocument.QuerySelector("link[rel=\"canonical\"]").GetAttribute("href");
                    string GameImage = htmlDocument.QuerySelector("div.info img").GetAttribute("src");

                    ListSearchGames.Add(new TrueAchievementSearch
                    {
                        GameUrl = GameUrl,
                        GameName = game.Name,
                        GameImage = GameImage
                    });
                }
                else
                {
                    foreach (var SearchGame in SectionGames.QuerySelectorAll("tr"))
                    {
                        try
                        {
                            var GameInfos = SectionGames.QuerySelectorAll("td");

                            string GameUrl = UrlBase + GameInfos[0].QuerySelector("a").GetAttribute("href");
                            string GameName = GameInfos[1].QuerySelector("a").InnerHtml;
                            string GameImage = UrlBase + GameInfos[0].QuerySelector("a img").GetAttribute("src");

                            string ItemType = GameInfos[2].InnerHtml;

                            if (ItemType.ToLower() == "game")
                            {
                                ListSearchGames.Add(new TrueAchievementSearch
                                {
                                    GameUrl = GameUrl,
                                    GameName = GameName,
                                    GameImage = GameImage
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return ListSearchGames;
        }

        public static EstimateTimeToUnlock GetEstimateTimeToUnlock(string UrlTrueAchievement)
        {
            EstimateTimeToUnlock EstimateTimeToUnlock = new EstimateTimeToUnlock();


            try
            {
                string WebData = Web.DownloadStringData(UrlTrueAchievement).GetAwaiter().GetResult();


                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(WebData);


                int NumberDataCount = 0;
                foreach (var SearchElement in htmlDocument.QuerySelectorAll("div.game div.l1 div"))
                {
                    var Title = SearchElement.GetAttribute("title");

                    if (Title != null && (Title == "Maximum TrueAchievement" || Title == "Maximum TrueSteamAchievement"))
                    {
                        var data = SearchElement.InnerHtml;
                        int.TryParse(Regex.Replace(data, "[^0-9]", ""), out NumberDataCount);
                        break;
                    }
                }

                foreach (var SearchElement in htmlDocument.QuerySelectorAll("div.game div.l2 a"))
                {
                    var Title = SearchElement.GetAttribute("title");

                    if (Title != null && Title == "Estimated time to unlock all achievements")
                    {
                        string EstimateTime = SearchElement.InnerHtml.Replace("<i class=\"fa fa-hourglass-end\"></i>", string.Empty).Trim();

                        int EstimateTimeMin = 0;
                        int EstimateTimeMax = 0;
                        int index = 0;
                        foreach (var item in EstimateTime.Replace("h", string.Empty).Split('-'))
                        {
                            if (index == 0)
                            {
                                int.TryParse(item, out EstimateTimeMin);
                            }
                            else
                            {
                                int.TryParse(item, out EstimateTimeMax);
                            }

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
                Common.LogError(ex, false);
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
