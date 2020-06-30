using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SuccessStory.Database;
using SuccessStory.Clients;
using PluginCommon;
using LiveCharts;
using PluginCommon.LiveChartsCommon;


namespace SuccessStory.Models
{
    public class AchievementsDatabase
    {
        // Variable Playnite
        private static ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI PlayniteApi { get; set; }

        SuccessStorySettings Settings { get; set; }

        // Variable AchievementsCollection
        private ConcurrentDictionary<Guid, GameAchievements> PluginDatabase { get; set; }
        private string PluginUserDataPath { get; set; }
        private string PluginDatabasePath { get; set; }

        public static CumulErrors ListErrors = new CumulErrors();



        public bool VerifAchievementsLoad(Guid gameID)
        {
            return File.Exists(PluginDatabasePath + gameID.ToString() + ".json");
        }



        public AchievementsDatabase(IPlayniteAPI PlayniteApi, SuccessStorySettings Settings, string PluginUserDataPath)
        {
            this.PlayniteApi = PlayniteApi;
            this.Settings = Settings;
            this.PluginUserDataPath = PluginUserDataPath;
            PluginDatabasePath = PluginUserDataPath + "\\achievements\\";

            if (!Directory.Exists(PluginDatabasePath))
            {
                Directory.CreateDirectory(PluginDatabasePath);
            }
        }

        /// <summary>
        /// Initialize database / create directory.
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="PluginUserDataPath"></param>
        public void Initialize()
        {
            PluginDatabase = new ConcurrentDictionary<Guid, GameAchievements>();
            ListErrors = new CumulErrors();

            Parallel.ForEach(Directory.EnumerateFiles(PluginDatabasePath, "*.json"), (objectFile) =>
            {
                try
                {
                    // Get game achievements.
                    Guid gameId = Guid.Parse(objectFile.Replace(PluginDatabasePath, "").Replace(".json", ""));

                    bool IncludeGame = true;
                    if (!Settings.IncludeHiddenGames)
                    {
                        Game tempGame = PlayniteApi.Database.Games.Get(gameId);                       
                        
                        if (tempGame != null)
                        {
                            IncludeGame = !tempGame.Hidden;
                        }
                        else
                        {
                            IncludeGame = false;
                            logger.Info($"SuccessStory - {gameId} is null");
                        }
                    }

                    if (IncludeGame)
                    {
                        GameAchievements objGameAchievements = JsonConvert.DeserializeObject<GameAchievements>(File.ReadAllText(objectFile));

                        // Set game achievements in database.
                        PluginDatabase.TryAdd(gameId, objGameAchievements);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", $"Failed to load item from { objectFile}");
                    ListErrors.Add($"SuccessStory - Failed to load item from {objectFile}");
                }
            });

            if (ListErrors.Get() != "")
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ListErrors.Get(), "SuccesStory errors");
            }

            ListErrors = new CumulErrors();
        }

        /// <summary>
        /// Get number achievements unlock by month for a game or not.
        /// </summary>
        /// <param name="GameID"></param>
        /// <returns></returns>
        public AchievementsGraphicsDataCount GetCountByMonth(Guid? GameID = null)
        {
            string[] GraphicsAchievementsLabels = new string[12];
            ChartValues<CustomerForSingle> SourceAchievementsSeries = new ChartValues<CustomerForSingle>();

            // All achievements
            if (GameID == null)
            {
                for (int i = 11; i >= 0; i--)
                {
                    GraphicsAchievementsLabels[(11 - i)] = DateTime.Now.AddMonths(-i).ToString("yyyy-MM");
                    SourceAchievementsSeries.Add(new CustomerForSingle
                    {
                        Name = DateTime.Now.AddMonths(-i).ToString("yyyy-MM"),
                        Values = 0
                    });
                }

                foreach (var item in PluginDatabase)
                {
                    List<Achievements> temp = item.Value.Achievements;
                    foreach (Achievements itemAchievements in temp)
                    {
                        if (itemAchievements.DateUnlocked != null && itemAchievements.DateUnlocked != default(DateTime))
                        {
                            string tempDate = ((DateTime)itemAchievements.DateUnlocked).ToLocalTime().ToString("yyyy-MM");
                            int index = Array.IndexOf(GraphicsAchievementsLabels, tempDate);

                            if (index >= 0 && index < 12)
                            {
                                SourceAchievementsSeries[index].Values += 1;
                            }
                        }
                    }
                }
            }
            // Achievement for a game
            else
            {
                List<Achievements> Achievements = Get((Guid)GameID).Achievements;

                if (Achievements != null && Achievements.Count > 0)
                {
                    Achievements.Sort((x, y) => ((DateTime)y.DateUnlocked).CompareTo((DateTime)x.DateUnlocked));
                    DateTime TempDateTime = DateTime.Now;
                    if (((DateTime)Achievements[0].DateUnlocked).ToLocalTime().ToString("yyyy-MM") != "0001-01")
                    {
                        TempDateTime = ((DateTime)Achievements[0].DateUnlocked).ToLocalTime();
                    }

                    for (int i = 11; i >= 0; i--)
                    {
                        GraphicsAchievementsLabels[(11 - i)] = TempDateTime.AddMonths(-i).ToString("yyyy-MM");
                        SourceAchievementsSeries.Add(new CustomerForSingle
                        {
                            Name = TempDateTime.AddMonths(-i).ToString("yyyy-MM"),
                            Values = 0
                        });
                    }

                    for (int i = 0; i < Achievements.Count; i++)
                    {
                        string tempDate = ((DateTime)Achievements[i].DateUnlocked).ToLocalTime().ToString("yyyy-MM");
                        int index = Array.IndexOf(GraphicsAchievementsLabels, tempDate);

                        if (index >= 0 && index < 12)
                        {
                            SourceAchievementsSeries[index].Values += 1;
                        }
                    }
                }
            }

            return new AchievementsGraphicsDataCount { Labels = GraphicsAchievementsLabels, Series = SourceAchievementsSeries };
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
        /// 
        /// </summary>
        /// <param name="GameSourceName"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static bool VerifToAddOrShow(string GameSourceName, SuccessStorySettings settings)
        {
            bool Result = false;

            if (settings.EnableSteam && GameSourceName.ToLower() == "steam")
            {
                return true;
            }
            if (settings.EnableGog && GameSourceName.ToLower() == "gog")
            {
                return true;
            }
            if (settings.EnableOrigin && GameSourceName.ToLower() == "origin")
            {
                return true;
            }
            if (settings.EnableLocal && GameSourceName.ToLower() == "playnite")
            {
                return true;
            }

            return Result;
        }


        private GogAchievements gogAPI { get; set; }
        private OriginAchievements originAPI { get; set; }

        public void InitializeMultipleAdd(SuccessStorySettings settings, string GameSourceName = "all")
        {
            switch (GameSourceName.ToLower())
            {
                case "all":
                    InitializeMultipleAdd(settings, "Steam");
                    InitializeMultipleAdd(settings, "GOG");
                    InitializeMultipleAdd(settings, "Origin");
                    break;

                case "steam":
                    break;

                case "gog":
                    if (settings.EnableGog && gogAPI == null)
                    {
                        gogAPI = new GogAchievements(PlayniteApi);
                    }
                    break;

                case "origin":
                    if (settings.EnableOrigin && originAPI == null)
                    {
                        originAPI = new OriginAchievements(PlayniteApi);
                    }
                    break;

                case "playnite":

                    break;
            }
        }



        /// <summary>
        /// Generate database achivements for the game if achievement exist and game not exist in database.
        /// </summary>
        /// <param name="GameAdded"></param>
        public void Add(Game GameAdded, SuccessStorySettings settings)
        {
            GameAchievements GameAchievements = new GameAchievements();

            Guid GameId = GameAdded.Id;
            Guid GameSourceId = GameAdded.SourceId;
            string GameSourceName = "";

            if (GameSourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
            {
                GameSourceName = GameAdded.Source.Name;
            }
            else
            {
                GameSourceName = "Playnite";
            }

            string PluginDatabaseGamePath = PluginDatabasePath + GameId.ToString() + ".json";
      
            List<Achievements> Achievements = new List<Achievements>();

            // Generate database only this source
            if (VerifToAddOrShow(GameSourceName, settings))
            {
                // Generate only not exist
                if (!File.Exists(PluginDatabaseGamePath))
                {
                    // TODO one func
                    if (GameSourceName.ToLower() == "gog")
                    {
                        if (gogAPI == null)
                        {
                            gogAPI = new GogAchievements(PlayniteApi);
                        }
                        GameAchievements = gogAPI.GetAchievements(PlayniteApi, GameId);
                    }

                    if (GameSourceName.ToLower() == "steam")
                    {
                        SteamAchievements steamAPI = new SteamAchievements();
                        GameAchievements = steamAPI.GetAchievements(PlayniteApi, GameId, PluginUserDataPath);
                    }

                    if (GameSourceName.ToLower() == "origin")
                    {
                        if (originAPI == null)
                        {
                            originAPI = new OriginAchievements(PlayniteApi);
                        }
                        GameAchievements = originAPI.GetAchievements(PlayniteApi, GameId);
                    }

                    if (GameSourceName.ToLower() == "playnite")
                    {
                        SteamAchievements steamAPI = new SteamAchievements();
                        GameAchievements = steamAPI.GetAchievements(PlayniteApi, GameId, PluginUserDataPath, settings.EnableLocal);
                    }

                    File.WriteAllText(PluginDatabaseGamePath, JsonConvert.SerializeObject(GameAchievements));
                }
            }
        }

        /// <summary>
        /// Remove game achievements in database for a game.
        /// </summary>
        /// <param name="GameRemoved"></param>
        public void Remove(Game GameRemoved)
        {
            Guid GameId = GameRemoved.Id;
            string PluginDatabaseGamePath = PluginDatabasePath + GameId.ToString() + ".json";

            if (File.Exists(PluginDatabaseGamePath))
            {
                File.Delete(PluginDatabaseGamePath);
            }
        }

        /// <summary>
        /// Delete achievements database.
        /// </summary>
        public void ResetData()
        {
            Parallel.ForEach(Directory.EnumerateFiles(PluginDatabasePath, "*.json"), (objectFile) =>
            {
                File.Delete(objectFile);
            });
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
            Result.Progression = (Total != 0) ? (int)Math.Round((double)(Unlocked * 100 / Total)) : 0;

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
    }

    public class AchievementsGraphicsDataCount
    {
        public string[] Labels { get; set; }
        public ChartValues<CustomerForSingle> Series { get; set; }
    }
}
