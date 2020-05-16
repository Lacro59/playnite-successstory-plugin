using GogLibrary.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SteamLibrary;

namespace SuccessStory.Models
{
    class AchievementsCollection : IAchievements
    {
        private ILogger logger = LogManager.GetLogger();

        private ConcurrentDictionary<Guid, List<Achievements>> Database { get; set; }
        private string DatabasePath { get; set; }





        public void AddAchievements(Guid gameId, string data)
        {
            
        }

        public List<Achievements> GetAchievementsList(Guid gameId)
        {
            if (Database.TryGetValue(gameId, out var item))
            {
                return item;
            }
            else
            {
                return null;
            }
        }

        public List<Achievements> GetAchievementsListWEB(Guid gameId, IPlayniteAPI PlayniteApi, string PluginUserDataPath = "")
        {
            List<Achievements> data = new List<Achievements>();
            string resultWeb = "";

            Game gameData = PlayniteApi.Database.Games.Get(gameId);

            string clientId = gameData.GameId;
            string gameName = gameData.Name;
            string gameSource = gameData.Source.Name;

            logger.Info("gameSource: " + gameSource);

            if (gameSource.ToLower() == "gog")
            {
                var view = PlayniteApi.WebViews.CreateOffscreenView();
                var gogAPI = new GogAccountClient(view);

                if (gogAPI.GetIsUserLoggedIn())
                {
                    string accessToken = gogAPI.GetAccountInfo().accessToken;
                    string userId = gogAPI.GetAccountInfo().userId;

                    //https://gogapidocs.readthedocs.io/en/latest/galaxy.html?highlight=achievement#get--clients-(int-product_id)-users-(int-user_id)-achievements
                    string url = "https://gameplay.gog.com/clients/" + clientId + "/users/" + userId + "/achievements";

                    logger.Info("url: " + url);

                    using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
                    {
                        try
                        { 
                            webClient.Headers.Add("Authorization: Bearer " + accessToken);
                            resultWeb = webClient.DownloadString(url);
                        }
                        catch (Exception e)
                        {
                            logger.Error(e, $"Failed to load from {url}");
                            PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "error");
                        }
                }

                    //    "total_count": 0,
                    //    "limit": 1000,
                    //    "page_token": "0",
                    //    "items": [],
                    //    "achievements_mode": "all_visible"

                    //    "total_count": 50,
                    //    "limit": 1000,
                    //    "page_token": "0",
                    //    "items": [
                    //        {
                    //            "achievement_id": "51602177717540236",
                    //            "achievement_key": "Agressor",
                    //            "visible": true,
                    //            "name": "Aggressor",
                    //            "description": "Deal a total of 30 damage to units in one multiplayer game.",
                    //            "image_url_unlocked": "https:\/\/images.gog.com\/42e0314cecdd452a1f7876fdd44db0e5eb4671ea0a25989d0506eceebeb7e3f6_gac_60.png",
                    //            "image_url_locked": "https:\/\/images.gog.com\/b96c30d5a668e49d97897dcaa70f4849c20b602052b80db6ddd4025be0fab9b2_gac_60.png",
                    //            "rarity": 35.4,
                    //            "date_unlocked": null,
                    //            "rarity_level_description": "Common",
                    //            "rarity_level_slug": "common"
                    //        }
                    //    ]
                    if (resultWeb != "")
                    {
                        JObject resultObj = JObject.Parse(resultWeb);
                        try
                        {
                            JArray resultItems = (JArray)resultObj["items"];
                            if (resultItems.Count > 0)
                            {
                                for (int i = 0; i < resultItems.Count; i++)
                                {
                                    Achievements temp = new Achievements();
                                    temp.Name = (string)resultItems[i]["name"];
                                    temp.Description = (string)resultItems[i]["description"];
                                    temp.UrlUnlocked = (string)resultItems[i]["image_url_unlocked"];
                                    temp.UrlLocked = (string)resultItems[i]["image_url_locked"];
                                    temp.DateUnlocked = ((string)resultItems[i]["date_unlocked"] == null) ? default(DateTime) : (DateTime)resultItems[i]["date_unlocked"];

                                    data.Add(temp);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Error(e, $"Failed to parse.");
                            PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "error");
                        }
                    }
                        
                }
            }

            //CB91DFC9-B977-43BF-8E70-55F46E410FAB
            if (gameSource.ToLower() == "steam")
            {
                JObject SteamConfig = JObject.Parse(File.ReadAllText(PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json"));
                string userId = (string)SteamConfig["UserId"];
                string apiKey = (string)SteamConfig["ApiKey"];

                //logger.Info("UserId: " + userId);
                //logger.Info("ApiKey: " + apiKey);

                //http://api.steampowered.com/ISteamUserStats/GetUserStatsForGame/v0002/?appid=34330&key=14F11749C321B8A396A9864BD82BBB1C&steamid=76561198003215440
                //ACHIEVEMENT_B_4_ASHIGARU_UNITS_WHOLE_BATTLE
                //v2
                //{
                //"name": "ACHIEVEMENT_B_4_ASHIGARU_UNITS_WHOLE_BATTLE",
                //"percent": 53.2000007629394531
                //}

                //http://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?appid=34330&key=14F11749C321B8A396A9864BD82BBB1C&steamid=76561198003215440
                //v1 - 5/5/2020 à 16:49:57	
                //{
                //"apiname": "ACHIEVEMENT_B_4_ASHIGARU_UNITS_WHOLE_BATTLE",
                //"achieved": 1,
                //"unlocktime": 1588697397
                //}

                // List image acheviement
                //https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key=14F11749C321B8A396A9864BD82BBB1C&appid=34330&l=french

                // List acheviements
                string url = "http://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?appid=" + clientId + "&key=" + apiKey + "&steamid=" + userId;

                logger.Info("url: " + url);

                using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
                {
                    try
                    {
                        resultWeb = webClient.DownloadString(url);
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, $"Failed to load from {url}");
                        PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "error");
                    }
                }

                if (resultWeb != "")
                {
                    JObject resultObj = JObject.Parse(resultWeb);
                    JArray resultItems = new JArray();

                    try
                    {
                        resultItems = (JArray)resultObj["playerstats"]["achievements"];
                        if (resultItems.Count > 0)
                        {
                            for (int i = 0; i < resultItems.Count; i++)
                            {
                                Achievements temp = new Achievements();
                                temp.Name = (string)resultItems[i]["apiname"];
                                temp.Description = "";
                                temp.UrlUnlocked = "";
                                temp.UrlLocked = "";
                                temp.DateUnlocked = ((int)resultItems[i]["unlocktime"] == 0) ? default(DateTime) : new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((int)resultItems[i]["unlocktime"]);

                                data.Add(temp);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, $"Failed to parse.");
                        PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "error");
                    }


                    // List details acheviements
                    url = "https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key=" + apiKey + "&appid=" + clientId;

                    logger.Info("url: " + url);

                    using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
                    {
                        resultWeb = webClient.DownloadString(url);
                    }

                    if (resultWeb != "")
                    {
                        resultObj = JObject.Parse(resultWeb);

                        try
                        {
                            resultItems = (JArray)resultObj["game"]["availableGameStats"]["achievements"];

                            for (int i = 0; i < resultItems.Count; i++)
                            {
                                for (int j = 0; j < data.Count; j++)
                                {
                                    if (data[j].Name.ToLower() == ((string)resultItems[i]["name"]).ToLower())
                                    {
                                        Achievements temp = new Achievements();
                                        temp.Name = (string)resultItems[i]["displayName"];
                                        temp.Description = (string)resultItems[i]["description"];
                                        temp.UrlUnlocked = (string)resultItems[i]["icon"];
                                        temp.UrlLocked = (string)resultItems[i]["icongray"];
                                        temp.DateUnlocked = data[j].DateUnlocked;

                                        data[j] = temp;
                                        j = data.Count;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Error(e, $"Failed to parse.");
                            PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "error");
                        }
                    }
                }
            }

            return data;
        }

        public void Load(string Path)
        {
            DatabasePath = Path + "\\achievements\\";

            if (!Directory.Exists(DatabasePath))
                Directory.CreateDirectory(DatabasePath);

            Parallel.ForEach(Directory.EnumerateFiles(DatabasePath, "*.json"), (objectFile) =>
            {
                try
                {
                    // Get game achievements.
                    Guid gameId = Guid.Parse(objectFile.Replace(DatabasePath, "").Replace(".json", ""));
                    List<Achievements> objGameAchievements = JsonConvert.DeserializeObject<List<Achievements>>(File.ReadAllText(objectFile));

                    // Set game achievements in database.
                    Database.TryAdd(gameId, objGameAchievements);
                }
                catch (Exception e)
                {
                    logger.Error(e, $"Failed to load item from {objectFile}");
                }
            });
        }

        public void Save()
        {
            foreach (var gameAchievements in Database)
            {
                string gameID = ((Guid)gameAchievements.Key).ToString();
                List<Achievements> Achievements = gameAchievements.Value;

                File.WriteAllText(DatabasePath + gameID + ".json", JsonConvert.SerializeObject(Achievements));
            }

        }
    }
}
