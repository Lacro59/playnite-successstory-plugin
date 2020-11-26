using AchievementsLocal.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
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

        private ConcurrentDictionary<Guid, GameAchievements> Items { get; set; } = new ConcurrentDictionary<Guid, GameAchievements>();


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
                    GameAchievements objGameAchievements = JsonConvert.DeserializeObject<GameAchievements>(File.ReadAllText(objectFile));
                    IsOld = true;
                    return;
                }
                catch (Exception ex)
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

                    GameAchievements objGameAchievements = JsonConvert.DeserializeObject<GameAchievements>(File.ReadAllText(objectFile));

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

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                logger.Info($"CheckLocalizations - ConvertDB()");

                int Converted = 0;

                foreach (var item in Items)
                {
                    try
                    {
                        if (PlayniteApi.Database.Games.Get(item.Key) != null)
                        {
                            SuccessStories successStories = SuccessStory.PluginDatabase.Get(item.Key, true);

                            successStories.HaveAchivements = item.Value.HaveAchivements;
                            successStories.IsEmulators = item.Value.IsEmulators;
                            successStories.Total = item.Value.Total;
                            successStories.Unlocked = item.Value.Unlocked;
                            successStories.Locked = item.Value.Locked;
                            successStories.Progression = item.Value.Progression;
                            successStories.Items = item.Value.Achievements;

                            Thread.Sleep(10);
                            SuccessStory.PluginDatabase.Update(successStories);
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
                logger.Info($"SuccessStory - Migration - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }, globalProgressOptions);

            IsOld = false;
        }
    }


    public class GameAchievements
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
