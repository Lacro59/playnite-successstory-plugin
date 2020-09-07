using Newtonsoft.Json.Linq;
using Playnite.Common.Web;
using Playnite.SDK;
using PluginCommon;
using SuccessStory.Database;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using AchievementsLocal;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;

namespace SuccessStory.Clients
{
    // https://partner.steamgames.com/doc/home
    class SteamAchievements
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private IHtmlDocument htmlDocument { get; set; } = null; 


        /// <summary>
        /// Get all achievements for a Steam game.
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="Id"></param>
        /// <param name="PluginUserDataPath"></param>
        /// <returns></returns>
        public GameAchievements GetAchievements(IPlayniteAPI PlayniteApi, Guid Id, string PluginUserDataPath, bool isLocal = false)
        {
            List<Achievements> Achievements = new List<Achievements>();
            string GameName = PlayniteApi.Database.Games.Get(Id).Name;
            string ClientId = PlayniteApi.Database.Games.Get(Id).GameId;
            bool HaveAchivements = false;
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            GameAchievements Result = new GameAchievements
            {
                Name = GameName,
                HaveAchivements = HaveAchivements,
                Total = Total,
                Unlocked = Unlocked,
                Locked = Locked,
                Progression = 0,
                Achievements = Achievements
            };

            var url = "";
            string ResultWeb = "";

            JObject resultObj = new JObject();
            JArray resultItems = new JArray();

            // Get Steam configuration if exist.
            string userId = "";
            string apiKey = "";
            string SteamUser = "";
            try
            {
                JObject SteamConfig = JObject.Parse(File.ReadAllText(PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json"));
                userId = (string)SteamConfig["UserId"];
                apiKey = (string)SteamConfig["ApiKey"];
                SteamUser = (string)SteamConfig["UserName"];
            }
            catch
            {
            }

            if (userId == "" || apiKey == "")
            {
                logger.Error($"SuccessStory - No Steam configuration.");
                AchievementsDatabase.ListErrors.Add($"Error on SteamAchievements: no Steam configuration and/or API key in settings menu for Steam Library.");
                return null;
            }

            if (!isLocal)
            {
                // Get player info
                url = string.Format(@"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamid={1}",
                    apiKey, userId);
                ResultWeb = "";
                try
                {
                    ResultWeb = HttpDownloader.DownloadString(url, Encoding.UTF8);

                    if (ResultWeb != "")
                    {
                        try
                        {
                            resultObj = JObject.Parse(ResultWeb);

                            if (!((string)resultObj["response"]["players"]["personaname"]).IsNullOrEmpty())
                            {
                                SteamUser = (string)resultObj["response"]["players"]["personaname"];
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, "SuccessStory", $"[{ClientId}] Failed to parse {ResultWeb}");
                        }
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                    {
                        var resp = (HttpWebResponse)ex.Response;
                        switch (resp.StatusCode)
                        {
                            case HttpStatusCode.BadRequest: // HTTP 400
                                break;
                            case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                break;
                            default:
                                Common.LogError(ex, "SuccessStory", $"Failed to load from {url}. ");
                                break;
                        }
                        return Result;
                    }
                }




                string lang = CodeLang.GetSteamLang(Localization.GetPlayniteLanguageConfiguration(PlayniteApi.Paths.ConfigurationPath));

                // List acheviements (default return in english)
                url = string.Format(@"https://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?appid={0}&key={1}&steamid={2}&l={3}",
                    ClientId, apiKey, userId, lang);

                ResultWeb = "";
                try
                {
                    ResultWeb = HttpDownloader.DownloadString(url, Encoding.UTF8);
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                    {
                        var resp = (HttpWebResponse)ex.Response;
                        switch (resp.StatusCode)
                        {
                            case HttpStatusCode.BadRequest: // HTTP 400
                                break;
                            case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                break;
                            default:
                                Common.LogError(ex, "SuccessStory", $"Failed to load from {url}. ");
                                break;
                        }
                        return Result;
                    }
                }

                if (ResultWeb != "")
                {
                    try
                    {
                        resultObj = JObject.Parse(ResultWeb);

                        if ((bool)resultObj["playerstats"]["success"])
                        {
                            resultItems = (JArray)resultObj["playerstats"]["achievements"];
                            if (resultItems.Count > 0)
                            {
                                HaveAchivements = true;

                                for (int i = 0; i < resultItems.Count; i++)
                                {
                                    Achievements temp = new Achievements
                                    {
                                        Name = (string)resultItems[i]["name"],
                                        ApiName = (string)resultItems[i]["apiname"],
                                        Description = (string)resultItems[i]["description"],
                                        UrlUnlocked = "",
                                        UrlLocked = "",
                                        DateUnlocked = ((int)resultItems[i]["unlocktime"] == 0) ? default(DateTime) : new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((int)resultItems[i]["unlocktime"])
                                    };

                                    // Achievement without unlocktime but achieved = 1
                                    if ((int)resultItems[i]["achieved"] == 1 && temp.DateUnlocked == new DateTime(1970, 1, 1, 0, 0, 0, 0))
                                    {
                                        temp.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                    }

                                    Total += 1;
                                    if ((int)resultItems[i]["unlocktime"] == 0)
                                        Locked += 1;
                                    else
                                        Unlocked += 1;

                                    Achievements.Add(temp);
                                }
                            }
                            else
                            {
                                logger.Info($"SuccessStory - No list achievement for {ClientId}. ");
                                return Result;
                            }
                        }
                        else
                        {
                            logger.Info($"SuccessStory - No Succes for {ClientId}. ");
                            return Result;
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "SuccessStory", $"[{ClientId}] Failed to parse {ResultWeb}");
                        return Result;
                    }


                    // List details acheviements
                    url = string.Format(@"https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key={0}&appid={1}&l={2}",
                        apiKey, ClientId, lang);

                    ResultWeb = "";
                    try
                    {
                        ResultWeb = HttpDownloader.DownloadString(url, Encoding.UTF8);
                    }
                    catch (WebException ex)
                    {
                        if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                        {
                            var resp = (HttpWebResponse)ex.Response;
                            switch (resp.StatusCode)
                            {
                                case HttpStatusCode.BadRequest: // HTTP 400
                                    break;
                                case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                    break;
                                default:
                                    Common.LogError(ex, "SuccessStory", $"Failed to load from {url}");
                                    break;
                            }
                            return Result;
                        }
                    }

                    if (ResultWeb != "")
                    {
                        resultObj = JObject.Parse(ResultWeb);

                        try
                        {
                            resultItems = (JArray)resultObj["game"]["availableGameStats"]["achievements"];

                            for (int i = 0; i < resultItems.Count; i++)
                            {
                                for (int j = 0; j < Achievements.Count; j++)
                                {
                                    if (Achievements[j].ApiName.ToLower() == ((string)resultItems[i]["name"]).ToLower())
                                    {
                                        Achievements temp = new Achievements
                                        {
                                            Name = (string)resultItems[i]["displayName"],
                                            ApiName = Achievements[j].ApiName,
                                            Description = Achievements[j].Description,
                                            UrlUnlocked = (string)resultItems[i]["icon"],
                                            UrlLocked = (string)resultItems[i]["icongray"],
                                            DateUnlocked = Achievements[j].DateUnlocked
                                        };

                                        if ((int)resultItems[i]["hidden"] == 1)
                                        {
                                            temp.Description = FindHiddenDescription(SteamUser, userId, ClientId, temp.Name, lang);
                                        }

                                        Achievements[j] = temp;
                                        j = Achievements.Count;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, "SuccessStory", $"Failed to parse");
                            return Result;
                        }
                    }
                }

                //logger.Info($"SuccessStory - No data for {ClientId}. ");

                Result = new GameAchievements
                {
                    Name = GameName,
                    HaveAchivements = HaveAchivements,
                    Total = Total,
                    Unlocked = Unlocked,
                    Locked = Locked,
                    Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0,
                    Achievements = Achievements
                };
            }
            else
            {
                SteamEmulators se = new SteamEmulators(PlayniteApi, PluginUserDataPath);
                ClientId = se.GetSteamId().ToString();

                var temp = se.GetAchievementsLocal(GameName, apiKey);

                if (temp.Achievements.Count > 0)
                {
                    Result.HaveAchivements = true;
                    Result.Total = temp.Total;
                    Result.Locked = temp.Locked;
                    Result.Unlocked = temp.Unlocked;
                    Result.Progression = temp.Progression;

                    for (int i = 0; i < temp.Achievements.Count; i++)
                    {
                        Result.Achievements.Add(new Achievements
                        {
                            Name = temp.Achievements[i].Name,
                            Description = temp.Achievements[i].Description,
                            UrlUnlocked = temp.Achievements[i].UrlUnlocked,
                            UrlLocked = temp.Achievements[i].UrlLocked,
                            DateUnlocked = temp.Achievements[i].DateUnlocked
                        });
                    }
                }
            }

            // Percentages
            url = string.Format(@"http://api.steampowered.com/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v0002/?gameid={0}&format=json",
                ClientId);
            ResultWeb = "";
            try
            {
                ResultWeb = HttpDownloader.DownloadString(url, Encoding.UTF8);
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.BadRequest: // HTTP 400
                            break;
                        case HttpStatusCode.ServiceUnavailable: // HTTP 503
                            break;
                        default:
                            Common.LogError(ex, "SuccessStory", $"Failed to load from {url}. ");
                            break;
                    }
                    return Result;
                }
            }

            if (ResultWeb != "")
            {
                JObject resultObj = new JObject();
                JArray resultItems = new JArray();

                try
                {
                    resultObj = JObject.Parse(ResultWeb);

                    if (resultObj["achievementpercentages"]["achievements"] != null)
                    {
                        foreach(JObject data in resultObj["achievementpercentages"]["achievements"])
                        {
                            for(int i = 0; i < Result.Achievements.Count; i++)
                            {
                                if (Result.Achievements[i].ApiName == (string)data["name"])
                                {
                                    Result.Achievements[i].Percent = (float)data["percent"];
                                    i = Result.Achievements.Count;
                                }
                            }
                        }
                    }
                    else
                    {
                        logger.Info($"SuccessStory - No percentages for {ClientId}. ");
                        return Result;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", $"[{ClientId}] Failed to parse {ResultWeb}");
                    return Result;
                }
            }


            return Result;
        }

        private string FindHiddenDescription(string SteamUser, string userId, string AppId, string DisplayName, string Lang)
        {
            if (htmlDocument == null)
            {
                logger.Debug($"SuccessStory - Load profil data for {SteamUser} - {AppId}");
                string url = string.Format(@"https://steamcommunity.com/id/{0}/stats/{1}/?tab=achievements",
                SteamUser, AppId);
                string ResultWeb = "";
                try
                {
                    var cookieLang = new Cookie("Steam_Language", Lang);
                    var cookies = new List<Cookie>();
                    cookies.Add(cookieLang);
                    ResultWeb = HttpDownloader.DownloadString(url, cookies, Encoding.UTF8);
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                    {
                        var resp = (HttpWebResponse)ex.Response;
                        switch (resp.StatusCode)
                        {
                            case HttpStatusCode.BadRequest: // HTTP 400
                                break;
                            case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                break;
                            default:
                                Common.LogError(ex, "SuccessStory", $"Failed to load from {url}. ");
                                break;
                        }
                    }
                }

                if (!ResultWeb.IsNullOrEmpty())
                {
                    HtmlParser parser = new HtmlParser();
                    htmlDocument = parser.Parse(ResultWeb);

                    if (htmlDocument.QuerySelectorAll("div.achieveRow").Length == 0)
                    {
                        logger.Debug($"SuccessStory - Load profil data for {userId} - {AppId}");
                        url = string.Format(@"https://steamcommunity.com/profiles/{0}/stats/{1}/?tab=achievements",
                        userId, AppId);
                        ResultWeb = "";
                        try
                        {
                            var cookieLang = new Cookie("Steam_Language", Lang);
                            var cookies = new List<Cookie>();
                            cookies.Add(cookieLang);
                            ResultWeb = HttpDownloader.DownloadString(url, cookies, Encoding.UTF8);
                        }
                        catch (WebException ex)
                        {
                            if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                            {
                                var resp = (HttpWebResponse)ex.Response;
                                switch (resp.StatusCode)
                                {
                                    case HttpStatusCode.BadRequest: // HTTP 400
                                        break;
                                    case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                        break;
                                    default:
                                        Common.LogError(ex, "SuccessStory", $"Failed to load from {url}. ");
                                        break;
                                }
                            }
                        }
                    }

                    if (!ResultWeb.IsNullOrEmpty())
                    {
                        parser = new HtmlParser();
                        htmlDocument = parser.Parse(ResultWeb);
                    }
                }
            }

            if (htmlDocument != null)
            {
                foreach (var achieveRow in htmlDocument.QuerySelectorAll("div.achieveRow"))
                {
                    //logger.Debug($"SuccessStory - {DisplayName.Trim().ToLower()} - {achieveRow.QuerySelector("h3").InnerHtml.Trim().ToLower()}");
                    if (achieveRow.QuerySelector("h3").InnerHtml.Trim().ToLower() == DisplayName.Trim().ToLower())
                    {
                        return achieveRow.QuerySelector("h5").InnerHtml;
                    }
                }
            }

            return "";
        }
    }
}
