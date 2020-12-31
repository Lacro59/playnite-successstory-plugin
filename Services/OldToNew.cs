using Newtonsoft.Json;
using Playnite.SDK;
using CommonPluginsShared;
using SuccessStory.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Achievements = SuccessStory.Models.Achievements;

namespace SuccessStory.Services
{
    public class OldToNew
    {
        private ILogger logger = LogManager.GetLogger();

        public bool IsOld = false;

        private string PathActivityDB = "achievements";

        private ConcurrentDictionary<Guid, GameAchievementsOld> Items { get; set; } = new ConcurrentDictionary<Guid, GameAchievementsOld>();


        public OldToNew(string PluginUserDataPath)
        {
            PathActivityDB = Path.Combine(PluginUserDataPath, PathActivityDB);

            if (Directory.Exists(PathActivityDB))
            {
                // Test is old
                CheckIsOld();

                if (IsOld)
                {
                    Directory.Move(PathActivityDB, PathActivityDB + "_old");

                    PathActivityDB += "_old";

                    LoadOldDB();
                }
            }
        }

        public void CheckIsOld()
        {
            Parallel.ForEach(Directory.EnumerateFiles(PathActivityDB, "*.json"), (objectFile) =>
            {
                string objectFileManual = string.Empty;

                try
                {
                    var JsonStringData = File.ReadAllText(objectFile);
                    IsOld = !JsonStringData.Contains("\"Items\"");
                    return;
                }
                catch
                {
                    IsOld = false;
                    return;
                }
            });
        }

        public void LoadOldDB()
        {
            logger.Info($"CheckLocalizations - LoadOldDB()");

            Parallel.ForEach(Directory.EnumerateFiles(PathActivityDB, "*.json"), (objectFile) =>
            {
                string objectFileManual = string.Empty;

                try
                {
                    var JsonStringData = File.ReadAllText(objectFile);

#if DEBUG
                    logger.Debug(objectFile.Replace(PathActivityDB, "").Replace(".json", "").Replace("\\", ""));
#endif
                    Guid gameId = Guid.Parse(objectFile.Replace(PathActivityDB, "").Replace(".json", "").Replace("\\", ""));

                    GameAchievementsOld objGameAchievements = JsonConvert.DeserializeObject<GameAchievementsOld>(File.ReadAllText(objectFile));

                    Items.TryAdd(gameId, objGameAchievements);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "CheckLocalizations", $"Failed to load item from {objectFile} or {objectFileManual}");
                }
            });

            logger.Info($"CheckLocalizations - Find {Items.Count} items");
        }

        public void ConvertDB(IPlayniteAPI PlayniteApi)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                "SuccessStory - Database migration",
                false
            );
            globalProgressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((Action<GlobalProgressActionArgs>)((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                logger.Info($"SuccessStory - ConvertDB()");

                int Converted = 0;

                foreach (var item in Items)
                {
                    try
                    {
                        if (PlayniteApi.Database.Games.Get(item.Key) != null)
                        {
                            GameAchievements gameAchievements = SuccessStory.PluginDatabase.Get(item.Key, true);

                            gameAchievements.HaveAchivements = item.Value.HaveAchivements;
                            gameAchievements.IsEmulators = item.Value.IsEmulators;
                            gameAchievements.Total = item.Value.Total;
                            gameAchievements.Unlocked = item.Value.Unlocked;
                            gameAchievements.Locked = item.Value.Locked;
                            gameAchievements.Progression = item.Value.Progression;
                            gameAchievements.Items = item.Value.Achievements;

                            Thread.Sleep(10);
                            SuccessStory.PluginDatabase.Update(gameAchievements);
                            Converted++;
                        }
                        else
                        {
                            logger.Warn($"SuccessStory - Game is deleted - {item.Key.ToString()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "SuccessStory", $"Failed to load ConvertDB from {item.Key.ToString()}");
                    }
                }

                logger.Info($"SuccessStory - Converted {Converted} / {Items.Count}");

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Info($"SuccessStory - Migration - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }), globalProgressOptions);

            IsOld = false;
        }
    }


    public class GameAchievementsOld
    {
        /// <summary>
        /// Game Name in the Playnite database.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool HaveAchivements { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsEmulators { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public bool Is100Percent
        {
            get
            {
                return Total == Unlocked;
            }
        }
        /// <summary>
        /// Total achievements for the game.
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Unlocked { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Locked { get; set; }
        /// <summary>
        /// Percentage
        /// </summary>
        public int Progression { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<Achievements> Achievements { get; set; }
    }
}
