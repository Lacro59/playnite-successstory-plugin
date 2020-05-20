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
using SuccessStory.Clients;

namespace SuccessStory.Models
{
    class AchievementsDatabase
    {
        // Variable Playnite
        private static ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI PlayniteApi { get; set; }

        // Variable AchievementsCollection
        private ConcurrentDictionary<Guid, GameAchievements> PluginDatabase { get; set; }
        private string PluginUserDataPath { get; set; }
        private string PluginDatabasePath { get; set; }


        public AchievementsDatabase(IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            this.PlayniteApi = PlayniteApi;
            this.PluginUserDataPath = PluginUserDataPath;
            PluginDatabasePath = PluginUserDataPath + "\\achievements\\";

            if (!Directory.Exists(PluginDatabasePath))
                Directory.CreateDirectory(PluginDatabasePath);

            PluginDatabase = new ConcurrentDictionary<Guid, GameAchievements>();
        }

        public void ResetData()
        {
            Parallel.ForEach(Directory.EnumerateFiles(PluginDatabasePath, "*.json"), (objectFile) =>
            {
                File.Delete(objectFile);
            });
        }


        /// <summary>
        /// Initialize database / create directory.
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="PluginUserDataPath"></param>
        public void Initialize()
        {
            Parallel.ForEach(Directory.EnumerateFiles(PluginDatabasePath, "*.json"), (objectFile) =>
            {
                try
                {
                    // Get game achievements.
                    Guid gameId = Guid.Parse(objectFile.Replace(PluginDatabasePath, "").Replace(".json", ""));
                    GameAchievements objGameAchievements = JsonConvert.DeserializeObject<GameAchievements>(File.ReadAllText(objectFile));

                    // Set game achievements in database.
                    PluginDatabase.TryAdd(gameId, objGameAchievements);
                }
                catch (Exception e)
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error");
                    logger.Error(e, $"SuccessStory - Failed to load item from {objectFile}");
                }
            });
        }

        /// <summary>
        /// Get Config and Achivements for a game.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public GameAchievements Get(Guid id)
        {
            if (PluginDatabase.TryGetValue(id, out var item))
            {
                return item;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Generate database achivements for the game if achievement exist and game not exist in database.
        /// </summary>
        /// <param name="GameAdded"></param>
        public void Add(Game GameAdded)
        {
            GameAchievements GameAchievements = new GameAchievements();

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

            string PluginDatabaseGamePath = PluginDatabasePath + GameId.ToString() + ".json";

            bool HaveAchivements = false;
            int Total = 0;            
            int Unlocked = 0;            
            int Locked = 0;            
            List<Achievements> Achievements = new List<Achievements>();

            // Generate database only this source
            if (GameSourceId != Guid.Parse("00000000-0000-0000-0000-000000000000") && (GameSourceName.ToLower() == "origin" || GameSourceName.ToLower() == "gog" || GameSourceName.ToLower() == "steam"))
            {
                // Generate only not exist
                if (!File.Exists(PluginDatabaseGamePath))
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
                                catch (WebException e)
                                {
                                    if (e.Status == WebExceptionStatus.ProtocolError && e.Response != null)
                                    {
                                        var resp = (HttpWebResponse)e.Response;
                                        switch (resp.StatusCode)
                                        {
                                            case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                                logger.Error(e, $"HTTP 503 to load from {url}");
                                                return;
                                            default:
                                                logger.Error(e, $"Failed to load from {url}");
                                                PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error");
                                                break;
                                        }
                                    }
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

                                            Total += 1;
                                            if ((string)resultItems[i]["date_unlocked"] == null)
                                                Locked += 1;
                                            else
                                                Unlocked += 1;

                                            Achievements.Add(temp);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    logger.Error(e, $"Failed to parse.");
                                    PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error");
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
                                    switch (resp.StatusCode)
                                    {
                                        case HttpStatusCode.BadRequest: // HTTP 400
                                            break;
                                        case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                            return;
                                        default:
                                            logger.Error(e, $"Failed to load from {url}");
                                            PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error");
                                            break;
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
                                PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error");
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
                                logger.Error(e, $"Failed to parse.");
                                PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error");
                            }


                            // List details acheviements
                            url = "https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key=" + apiKey + "&appid=" + ClientId;

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
                                        switch (resp.StatusCode)
                                        {
                                            case HttpStatusCode.BadRequest: // HTTP 400
                                                break;
                                            case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                                return;
                                            default:
                                                logger.Error(e, $"Failed to load from {url}");
                                                PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error");
                                                break;
                                        }
                                    }
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
                                catch (Exception e)
                                {
                                    logger.Error(e, $"Failed to parse.");
                                    PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error");
                                }
                            }
                        }
                    }

                    if (GameSourceName.ToLower() == "origin")
                    {
                        Origin originAPI = new Origin();
                        GameAchievements = originAPI.GetAchievements(PlayniteApi, GameId);

                        if (Achievements.Count > 0)
                        {
                            HaveAchivements = true;
                        }
                    }
                    else
                    {
                        GameAchievements = new GameAchievements
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


                    File.WriteAllText(PluginDatabaseGamePath, JsonConvert.SerializeObject(GameAchievements));
                }
            }
        }


        public void Remove(Game GameRemoved)
        {
            Guid GameId = GameRemoved.Id;
            string PluginDatabaseGamePath = PluginDatabasePath + GameId.ToString() + ".json";

            if (File.Exists(PluginDatabaseGamePath))
            {
                File.Delete(PluginDatabaseGamePath);
            }
        }



        public ProgressionAchievements Progession()
        {
            ProgressionAchievements Result = new ProgressionAchievements();
            int Total = 0;
            int Locked = 0;
            int Unlocked = 0;

            foreach(var item in PluginDatabase)
            {
                GameAchievements GameAchievements = item.Value;

                if (GameAchievements.HaveAchivements)
                {
                    Total += GameAchievements.Total;
                    Locked += GameAchievements.Locked;
                    Unlocked += GameAchievements.Unlocked;
                }
            }

            Result.Total = Total;
            Result.Locked = Locked;
            Result.Unlocked = Unlocked;
            Result.Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;

            return Result;
        }

        public ProgressionAchievements ProgessionGame(Guid GameId)
        {
            ProgressionAchievements Result = new ProgressionAchievements();
            int Total = 0;
            int Locked = 0;
            int Unlocked = 0;

            foreach (var item in PluginDatabase)
            {
                Guid Id = item.Key;
                GameAchievements GameAchievements = item.Value;

                if (GameAchievements.HaveAchivements && Id == GameId)
                {
                    Total += GameAchievements.Total;
                    Locked += GameAchievements.Locked;
                    Unlocked += GameAchievements.Unlocked;
                }
            }

            Result.Total = Total;
            Result.Locked = Locked;
            Result.Unlocked = Unlocked;
            Result.Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;

            return Result;
        }

        public ProgressionAchievements ProgessionSource(Guid GameSourceId)
        {
            ProgressionAchievements Result = new ProgressionAchievements();
            int Total = 0;
            int Locked = 0;
            int Unlocked = 0;

            foreach (var item in PluginDatabase)
            {
                Guid Id = item.Key;
                Game Game = PlayniteApi.Database.Games.Get(Id);
                GameAchievements GameAchievements = item.Value;

                if (GameAchievements.HaveAchivements && Game.SourceId == GameSourceId)
                {
                    Total += GameAchievements.Total;
                    Locked += GameAchievements.Locked;
                    Unlocked += GameAchievements.Unlocked;
                }
            }

            Result.Total = Total;
            Result.Locked = Locked;
            Result.Unlocked = Unlocked;
            Result.Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;

            return Result;
        }

        /// <summary>
        /// Control game have achieveements.
        /// </summary>
        /// <param name="GameId"></param>
        /// <returns></returns>
        public bool HaveAchievements(Guid GameId)
        {
            if (Get(GameId) != null)
                return Get(GameId).HaveAchivements;
            else
                return false;
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
                            PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error");
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
                            PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error");
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
                //{"playerstats":{"SuccessStory error":"Requested app has no stats","success":false}}

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
                                PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error");
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
                        PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error");
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
                            PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error");
                        }
                    }
                }
            }

            return data;
        }
    }
}
