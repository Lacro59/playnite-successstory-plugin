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
using System.Diagnostics;

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

        public static CumulErrors ListErrors = new CumulErrors();


        public AchievementsDatabase(IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            this.PlayniteApi = PlayniteApi;
            this.PluginUserDataPath = PluginUserDataPath;
            PluginDatabasePath = PluginUserDataPath + "\\achievements\\";

            if (!Directory.Exists(PluginDatabasePath))
                Directory.CreateDirectory(PluginDatabasePath);
        }

        /// <summary>
        /// Initialize database / create directory.
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="PluginUserDataPath"></param>
        public void Initialize()
        {
            PluginDatabase = new ConcurrentDictionary<Guid, GameAchievements>();

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
                catch (Exception ex)
                {
                    var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                    PlayniteApi.Dialogs.ShowErrorMessage(ex.Message, $"SuccessStory error [{LineNumber}]");
                    logger.Error(ex, $"SuccessStory - Failed to load item from {objectFile}");
                }
            });
        }

        /// <summary>
        /// Get number achievements unlock by month.
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<string, int> GetCountByMonth()
        {
            ConcurrentDictionary<string, int> CountByMonth = new ConcurrentDictionary<string, int>();
            for (int i = 11; i >= 0 ; i--)
            {
                CountByMonth.TryAdd(DateTime.Now.AddMonths(-i).ToString("yyyy-MM"), 0);
            }

            foreach (var item in PluginDatabase)
            {
                List<Achievements> temp = item.Value.Achievements;
                foreach (Achievements itemAchievements in temp)
                {
                    if (itemAchievements.DateUnlocked != null && itemAchievements.DateUnlocked != default(DateTime)) {
                        string tempDate = ((DateTime)itemAchievements.DateUnlocked).ToLocalTime().ToString("yyyy-MM");

                        if (CountByMonth.ContainsKey(tempDate))
                        {
                            CountByMonth[tempDate] = CountByMonth[tempDate] + 1;
                        }
                    }
                }
            }
            return CountByMonth;
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

            if (settings.enableSteam && GameSourceName.ToLower() == "steam")
            {
                return true;
            }
            if (settings.enableGog && GameSourceName.ToLower() == "gog")
            {
                return true;
            }
            if (settings.enableOrigin && GameSourceName.ToLower() == "origin")
            {
                return true;
            }

            return Result;
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
                        GogAchievements gogAPI = new GogAchievements();
                        GameAchievements = gogAPI.GetAchievements(PlayniteApi, GameId);
                    }

                    if (GameSourceName.ToLower() == "steam")
                    {
                        SteamAchievements steamAPI = new SteamAchievements();
                        GameAchievements = steamAPI.GetAchievements(PlayniteApi, GameId, PluginUserDataPath);
                    }

                    if (GameSourceName.ToLower() == "origin")
                    {
                        OriginAchievements originAPI = new OriginAchievements();
                        GameAchievements = originAPI.GetAchievements(PlayniteApi, GameId);
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
}
