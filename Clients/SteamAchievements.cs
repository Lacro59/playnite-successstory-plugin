using Newtonsoft.Json.Linq;
using Playnite.Common.Web;
using Playnite.SDK;
using SuccessStory.Database;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace SuccessStory.Clients
{
    // https://partner.steamgames.com/doc/home
    class SteamAchievements
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        public GameAchievements GetAchievements(IPlayniteAPI PlayniteApi, Guid Id, string PluginUserDataPath)
        {
            GameAchievements Result = new GameAchievements();

            List<Achievements> Achievements = new List<Achievements>();
            string GameName = PlayniteApi.Database.Games.Get(Id).Name;
            string ClientId = PlayniteApi.Database.Games.Get(Id).GameId;
            bool HaveAchivements = false;
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            string ResultWeb = "";


            JObject SteamConfig = JObject.Parse(File.ReadAllText(PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json"));
            string userId = (string)SteamConfig["UserId"];
            string apiKey = (string)SteamConfig["ApiKey"];

            if (userId == "" || apiKey == "")
            {
                logger.Debug($"SuccessStory - No Steam configuration.");
                return Result;
            }

            // List acheviements
            var url = string.Format(@"http://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?appid={0}&key={1}&steamid={2}",
                ClientId, apiKey, userId);

            //if (HttpDownloader.GetResponseCode(url) == System.Net.HttpStatusCode.OK)
            //{
            //    var stringData = Encoding.UTF8.GetString(HttpDownloader.DownloadData(url));
            //    return JsonConvert.DeserializeObject<StorePageMetadata>(stringData);
            //}
            //else
            //{
            //    return null;
            //}

            //using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
            //{
                try
                {
                    //webClient.Headers["Content-Type"] = "application/json;charset=UTF-8";
                    //ResultWeb = webClient.DownloadString(url);

                    ResultWeb = HttpDownloader.DownloadString(url);
                }
                catch (WebException e)
                {
                    if (e.Status == WebExceptionStatus.ProtocolError && e.Response != null)
                    {
                        var resp = (HttpWebResponse)e.Response;
                        switch (resp.StatusCode)
                        {
                            case HttpStatusCode.BadRequest: // HTTP 400
                                break;
                            case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                break;
                            default:
                                logger.Error(e, $"SuccessStory - Failed to load from {url}");
                                //PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error on SteamAchievements");
                                AchievementsDatabase.ListErrors.Add("Error on SteamAchievements: " + e.Message);
                                break;
                        }
                    }
                }
            //}

            if (ResultWeb != "")
            {
                JObject resultObj = JObject.Parse(ResultWeb);
                JArray resultItems = new JArray();

                try
                {
                    resultItems = (JArray)resultObj["playerstats"]["achievements"];
                    if (resultItems.Count > 0)
                    {
                        HaveAchivements = true;
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e, $"SuccessStory - Failed to parse.");
                    //PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error on SteamAchievements");
                    AchievementsDatabase.ListErrors.Add("Error on SteamAchievements: " + e.Message);
                }


                try
                {
                    resultItems = (JArray)resultObj["playerstats"]["achievements"];
                    if (resultItems.Count > 0)
                    {
                        HaveAchivements = true;

                        for (int i = 0; i < resultItems.Count; i++)
                        {
                            Achievements temp = new Achievements
                            {
                                Name = (string)resultItems[i]["apiname"],
                                Description = "",
                                UrlUnlocked = "",
                                UrlLocked = "",
                                DateUnlocked = ((int)resultItems[i]["unlocktime"] == 0) ? default(DateTime) : new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((int)resultItems[i]["unlocktime"])
                            };

                            Total += 1;
                            if ((int)resultItems[i]["unlocktime"] == 0)
                                Locked += 1;
                            else
                                Unlocked += 1;

                            Achievements.Add(temp);
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e, $"SuccessStory - Failed to parse.");
                    //PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error on SteamAchievements");
                    AchievementsDatabase.ListErrors.Add("Error on SteamAchievements: " + e.Message);
                }


                // List details acheviements
                string lang = resources.GetString("LOCLanguageNameEnglish");
                url = string.Format(@"https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key={0}&appid={1}&l={2}",
                    apiKey, ClientId, lang);

                logger.Debug($"SuccessStory - Steam.GetAchievements {url}");


                //using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
                //{
                    try
                    {
                        //webClient.Headers["Content-Type"] = "application/json;charset=UTF-8";
                        //ResultWeb = webClient.DownloadString(url);

                        ResultWeb = HttpDownloader.DownloadString(url);
                    }
                    catch (WebException e)
                    {
                        if (e.Status == WebExceptionStatus.ProtocolError && e.Response != null)
                        {
                            var resp = (HttpWebResponse)e.Response;
                            switch (resp.StatusCode)
                            {
                                case HttpStatusCode.BadRequest: // HTTP 400
                                    break;
                                case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                    break;
                                default:
                                    logger.Error(e, $"SuccessStory - Failed to load from {url}");
                                    //PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error on SteamAchievements");
                                    AchievementsDatabase.ListErrors.Add("Error on SteamAchievements: " + e.Message);
                                    break;
                            }
                        }
                    }
                //}

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
                                if (Achievements[j].Name.ToLower() == ((string)resultItems[i]["name"]).ToLower())
                                {
                                    Achievements temp = new Achievements
                                    {
                                        Name = (string)resultItems[i]["displayName"],
                                        Description = (string)resultItems[i]["description"],
                                        UrlUnlocked = (string)resultItems[i]["icon"],
                                        UrlLocked = (string)resultItems[i]["icongray"],
                                        DateUnlocked = Achievements[j].DateUnlocked
                                    };

                                    Achievements[j] = temp;
                                    j = Achievements.Count;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, $"SuccessStory - Failed to parse.");
                        //PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error on SteamAchievements");
                        AchievementsDatabase.ListErrors.Add("Error on SteamAchievements: " + e.Message);
                    }
                }
            }

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

            return Result;
        }
    }
}
