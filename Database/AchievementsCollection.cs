using GogLibrary.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SuccessStory.Database;

namespace SuccessStory.Models
{
    class AchievementsCollection
    {
        private static ILogger logger = LogManager.GetLogger();

        private ConcurrentDictionary<Guid, List<Achievements>> Database { get; set; }
        private string DatabasePath { get; set; }



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





        /// <summary>
        /// Generate database for the game.
        /// </summary>
        /// <param name="GameAdded"></param>
        /// <param name="PlayniteApi"></param>
        /// <param name="PluginUserDataPath"></param>
        public static void AddAchievements(Game GameAdded, IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            string ResultWeb = "";
            string ClientId = GameAdded.GameId;
            Guid GameId = GameAdded.Id;
            string GameName = GameAdded.Name;
            Guid GameSourceId = GameAdded.SourceId;
            string GameSourceName = "";

            if (GameSourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                GameSourceName = GameAdded.Source.Name;
            else
                GameSourceName = "Playnite";

            string PathPluginDb = PluginUserDataPath + "\\achievements\\";
            string PathPluginGameDb = PathPluginDb + GameId.ToString() + ".json";

            bool HaveAchivements = false;

            if (GameSourceId != Guid.Parse("00000000-0000-0000-0000-000000000000") && (GameSourceName.ToLower() == "gog" || GameSourceName.ToLower() == "steam"))
            {
                if (!Directory.Exists(PathPluginDb))
                    Directory.CreateDirectory(PathPluginDb);

                if (!File.Exists(PathPluginGameDb))
                {
                    // TODO one func
                    if (GameSourceName.ToLower() == "gog")
                    {
                        var view = PlayniteApi.WebViews.CreateOffscreenView();
                        var gogAPI = new GogAccountClient(view);

                        if (gogAPI.GetIsUserLoggedIn())
                        {
                            string accessToken = gogAPI.GetAccountInfo().accessToken;
                            string userId = gogAPI.GetAccountInfo().userId;
                            string url = "https://gameplay.gog.com/clients/" + ClientId + "/users/" + userId + "/achievements";

                            using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
                            {
                                try
                                {
                                    webClient.Headers.Add("Authorization: Bearer " + accessToken);
                                    ResultWeb = webClient.DownloadString(url);
                                }
                                catch (Exception e)
                                {
                                    logger.Error(e, $"Failed to load from {url}");
                                    PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "error");
                                }
                            }

                            if (ResultWeb != "")
                            {
                                JObject resultObj = JObject.Parse(ResultWeb);
                                try
                                {
                                    JArray resultItems = (JArray)resultObj["items"];
                                    if (resultItems.Count > 0)
                                    {
                                        HaveAchivements = true;
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

                    if (GameSourceName.ToLower() == "steam")
                    {
                        JObject SteamConfig = JObject.Parse(File.ReadAllText(PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json"));
                        string userId = (string)SteamConfig["UserId"];
                        string apiKey = (string)SteamConfig["ApiKey"];

                        // List acheviements
                        string url = "http://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?appid=" + ClientId + "&key=" + apiKey + "&steamid=" + userId;

                        using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
                        {
                            try
                            {
                                webClient.Headers["Content-Type"] = "application/json;charset=UTF-8";
                                ResultWeb = webClient.DownloadString(url);
                            }
                            catch (WebException e)
                            {
                                if (e.Status == WebExceptionStatus.ProtocolError && e.Response != null)
                                {
                                    var resp = (HttpWebResponse)e.Response;
                                    if (resp.StatusCode != HttpStatusCode.BadRequest) // HTTP 404
                                    {
                                        logger.Error(e, $"Failed to load from {url}");
                                    }
                                }
                            }
                        }

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
                                logger.Error(e, $"Failed to parse.");
                                PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "error");
                            }
                        }
                    }

                    GameAchievements GameAchievements = new GameAchievements
                    {
                        Name = GameName,
                        HaveAchivements = HaveAchivements
                    };
                    File.WriteAllText(PathPluginGameDb, JsonConvert.SerializeObject(GameAchievements));
                }
            }
        }

        /// <summary>
        /// Control game have achieveements.
        /// </summary>
        /// <param name="GameId"></param>
        /// <param name="PluginUserDataPath"></param>
        /// <returns></returns>
        public static bool HaveAchievements(Guid GameId, string PluginUserDataPath)
        {
            bool Result = false;
            string PathPluginDb = PluginUserDataPath + "\\achievements\\";
            string PathPluginGameDb = PathPluginDb + GameId.ToString() + ".json";

            if (!Directory.Exists(PathPluginDb))
                Directory.CreateDirectory(PathPluginDb);

            if (File.Exists(PathPluginGameDb)) {
                GameAchievements GameAchievements = JsonConvert.DeserializeObject<GameAchievements>(File.ReadAllText(PathPluginGameDb));
                Result = GameAchievements.HaveAchivements;
            }

            return Result;
        }

        /// <summary>
        /// Get Achievements for a game on the web.
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="PlayniteApi"></param>
        /// <param name="PluginUserDataPath"></param>
        /// <returns></returns>
        public static List<Achievements> GetAchievementsListWEB(Guid gameId, IPlayniteAPI PlayniteApi, string PluginUserDataPath = "")
        {
            List<Achievements> data = new List<Achievements>();
            string resultWeb = "";

            Game gameData = PlayniteApi.Database.Games.Get(gameId);

            string clientId = gameData.GameId;
            string gameName = gameData.Name;
            string gameSource = gameData.Source.Name;

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

                    using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
                    {
                        try
                        {
                            webClient.Headers.Add("Authorization: Bearer " + accessToken);
                            webClient.Headers["Content-Type"] = "application/json;charset=UTF-8";
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
                                    Achievements temp = new Achievements
                                    {
                                        Name = (string)resultItems[i]["name"],
                                        Description = (string)resultItems[i]["description"],
                                        UrlUnlocked = (string)resultItems[i]["image_url_unlocked"],
                                        UrlLocked = (string)resultItems[i]["image_url_locked"],
                                        DateUnlocked = ((string)resultItems[i]["date_unlocked"] == null) ? default(DateTime) : (DateTime)resultItems[i]["date_unlocked"]
                                    };

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


                //error 400
                //{"playerstats":{"error":"Requested app has no stats","success":false}}

                // List acheviements
                string url = "http://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?appid=" + clientId + "&key=" + apiKey + "&steamid=" + userId;

                using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
                {
                    try
                    {
                        webClient.Headers["Content-Type"] = "application/json;charset=UTF-8";
                        resultWeb = webClient.DownloadString(url);
                    }
                    catch (WebException e)
                    {
                        if (e.Status == WebExceptionStatus.ProtocolError && e.Response != null)
                        {
                            var resp = (HttpWebResponse)e.Response;
                            if (resp.StatusCode != HttpStatusCode.BadRequest) // HTTP 404
                            {
                                logger.Error(e, $"Failed to load from {url}");
                                PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "error");
                            }
                        }
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
                                Achievements temp = new Achievements
                                {
                                    Name = (string)resultItems[i]["apiname"],
                                    Description = "",
                                    UrlUnlocked = "",
                                    UrlLocked = "",
                                    DateUnlocked = ((int)resultItems[i]["unlocktime"] == 0) ? default(DateTime) : new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((int)resultItems[i]["unlocktime"])
                                };

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

                    using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
                    {
                        webClient.Headers["Content-Type"] = "application/json;charset=UTF-8";
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
                                        Achievements temp = new Achievements
                                        {
                                            Name = (string)resultItems[i]["displayName"],
                                            Description = (string)resultItems[i]["description"],
                                            UrlUnlocked = (string)resultItems[i]["icon"],
                                            UrlLocked = (string)resultItems[i]["icongray"],
                                            DateUnlocked = data[j].DateUnlocked
                                        };

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
    }
}
