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

    }
}
