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

namespace SuccessStory.Clients
{
    // https://partner.steamgames.com/doc/home
    class SteamAchievements
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();


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


            string ResultWeb = "";

            // Get Steam configuration if exist.
            string userId = "";
            string apiKey = "";
            try
            {
                JObject SteamConfig = JObject.Parse(File.ReadAllText(PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json"));
                userId = (string)SteamConfig["UserId"];
                apiKey = (string)SteamConfig["ApiKey"];
            }
            catch
            {
            }

            if (userId == "" || apiKey == "")
            {
                logger.Error($"SuccessStory - No Steam configuration.");
                AchievementsDatabase.ListErrors.Add($"Error on SteamAchievements: no Steam configuration.");
                return Result;
            }

            if (!isLocal)
            {
                // List acheviements (default return in english)
                var url = string.Format(@"https://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?appid={0}&key={1}&steamid={2}",
                    ClientId, apiKey, userId);

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
                    string lang = CodeLang.GetSteamLang(Localization.GetPlayniteLanguageConfiguration(PlayniteApi.Paths.ConfigurationPath));
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
                        catch (Exception ex)
                        {
                            Common.LogError(ex, "SuccessStory", $"Failed to parse");
                            return Result;
                        }
                    }
                }

                logger.Info($"SuccessStory - No data for {ClientId}. ");

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
            else
            {
                SteamEmulators se = new SteamEmulators(PlayniteApi);

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

                    return Result;
                }
                else
                {
                    return Result;
                }
            }
        }
    }
}
