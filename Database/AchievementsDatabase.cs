using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SuccessStory.Database;
using SuccessStory.Clients;
using PluginCommon;
using LiveCharts;
using PluginCommon.LiveChartsCommon;
using Newtonsoft.Json.Linq;

namespace SuccessStory.Models
{
    public class AchievementsDatabase : ICloneable
    {
        // Variable Playnite
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private IPlayniteAPI _PlayniteApi { get; set; }

        SuccessStory _plugin { get; set; }
        SuccessStorySettings _settings { get; set; }

        // Variable AchievementsCollection
        private ConcurrentDictionary<Guid, GameAchievements> PluginDatabase { get; set; } = new ConcurrentDictionary<Guid, GameAchievements>();
        private string _PluginUserDataPath { get; set; }
        private string PluginDatabasePath { get; set; }
        private bool _isRetroachievements { get; set; }

        private static bool? VerifToAddOrShowGog = null;
        private static bool? VerifToAddOrShowOrigin = null;
        private static bool? VerifToAddOrShowRetroAchievements = null;
        private static bool? VerifToAddOrShowSteam = null;
        private static bool? VerifToAddOrShowXbox = null;


        public static CumulErrors ListErrors = new CumulErrors();

        public ConcurrentDictionary<Guid, GameAchievements> gameAchievements
        {
            get
            {
                return PluginDatabase;
            }
        }

        public bool VerifAchievementsLoad(Guid gameID)
        {
            return File.Exists(PluginDatabasePath + gameID.ToString() + ".json");
        }

        public AchievementsDatabase(SuccessStory plugin, IPlayniteAPI PlayniteApi, SuccessStorySettings Settings, string PluginUserDataPath, bool isRetroachievements = false)
        {
            _plugin = plugin;
            _PlayniteApi = PlayniteApi;
            _settings = Settings;
            _PluginUserDataPath = PluginUserDataPath;
            _isRetroachievements = isRetroachievements;
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
        public void Initialize(bool ignore = true)
        {
            ListErrors = new CumulErrors();

            PluginDatabase = new ConcurrentDictionary<Guid, GameAchievements>();

            Parallel.ForEach(Directory.EnumerateFiles(PluginDatabasePath, "*.json"), (objectFile) =>
            {
                try
                {
                    // Get game achievements.
                    Guid gameId = Guid.Parse(objectFile.Replace(PluginDatabasePath, string.Empty).Replace(".json", string.Empty));

                    bool IncludeGame = true;
                    if (!_settings.IncludeHiddenGames)
                    {
                        Game tempGame = _PlayniteApi.Database.Games.Get(gameId);                       
                        
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

            // Filters
            if (_settings.EnableRetroAchievementsView && !ignore)
            {
                if (_isRetroachievements)
                {
                    var a = PluginDatabase.Where(x => IsEmulatedGame(x));
                    var b = a.ToDictionary(x => x.Key, x => x.Value);
                    PluginDatabase = ToConcurrent(b);
                }
                else
                {
                    var a = PluginDatabase.Where(x => !IsEmulatedGame(x));
                    var b = a.ToDictionary(x => x.Key, x => x.Value);
                    PluginDatabase = ToConcurrent(b);
                }
            }

            if (ListErrors.Get() != string.Empty)
            {
                _PlayniteApi.Dialogs.ShowErrorMessage(ListErrors.Get(), "SuccessStory errors");
            }

            ListErrors = new CumulErrors();
        }

        public void Filter(bool ignore = true)
        {
            if (_settings.EnableRetroAchievementsView && !ignore)
            {
                if (_isRetroachievements)
                {
                    var a = PluginDatabase.Where(x => IsEmulatedGame(x));
                    var b = a.ToDictionary(x => x.Key, x => x.Value);
                    PluginDatabase = ToConcurrent(b);
                }
                else
                {
                    var a = PluginDatabase.Where(x => !IsEmulatedGame(x));
                    var b = a.ToDictionary(x => x.Key, x => x.Value);
                    PluginDatabase = ToConcurrent(b);
                }
            }
        }

        private ConcurrentDictionary<TKey, TValue> ToConcurrent<TKey, TValue>(Dictionary<TKey, TValue> dic)
        {
            return new ConcurrentDictionary<TKey, TValue>(dic);
        }

        private bool IsEmulatedGame(KeyValuePair<Guid, GameAchievements> x)
        {
            Game game = _PlayniteApi.Database.Games.Get(x.Key);
            return PlayniteTools.IsGameEmulated(_PlayniteApi, game);
        }



        /// <summary>
        /// Get number achievements unlock by month for a game or not.
        /// </summary>
        /// <param name="GameID"></param>
        /// <returns></returns>
        public AchievementsGraphicsDataCount GetCountByMonth(Guid? GameID = null, int limit = 11)
        {
            string[] GraphicsAchievementsLabels = new string[limit + 1];
            ChartValues<CustomerForSingle> SourceAchievementsSeries = new ChartValues<CustomerForSingle>();

            LocalDateYMConverter localDateYMConverter = new LocalDateYMConverter();

            // All achievements
            if (GameID == null)
            {
                for (int i = limit; i >= 0; i--)
                {
                    //GraphicsAchievementsLabels[(limit - i)] = DateTime.Now.AddMonths(-i).ToString("yyyy-MM");
                    GraphicsAchievementsLabels[(limit - i)] = (string)localDateYMConverter.Convert(DateTime.Now.AddMonths(-i), null, null, null);
                    SourceAchievementsSeries.Add(new CustomerForSingle
                    {
                        //Name = DateTime.Now.AddMonths(-i).ToString("yyyy-MM"),
                        Name = (string)localDateYMConverter.Convert(DateTime.Now.AddMonths(-i), null, null, null),
                        Values = 0
                    });
                }

                try
                {
                    foreach (var item in PluginDatabase)
                    {
                        List<Achievements> temp = item.Value.Achievements;
                        foreach (Achievements itemAchievements in temp)
                        {
                            if (itemAchievements.DateUnlocked != null && itemAchievements.DateUnlocked != default(DateTime))
                            {
                                //string tempDate = ((DateTime)itemAchievements.DateUnlocked).ToLocalTime().ToString("yyyy-MM");
                                string tempDate = (string)localDateYMConverter.Convert(((DateTime)itemAchievements.DateUnlocked).ToLocalTime(), null, null, null);
                                int index = Array.IndexOf(GraphicsAchievementsLabels, tempDate);

                                if (index >= 0 && index < (limit + 1))
                                {
                                    SourceAchievementsSeries[index].Values += 1;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", "Error in load GetCountByMonth()");
                }
            }
            // Achievement for a game
            else
            {
                try
                {
                    List<Achievements> Achievements = Get((Guid)GameID).Achievements;

                    if (Achievements != null && Achievements.Count > 0)
                    {
                        Achievements.Sort((x, y) => ((DateTime)y.DateUnlocked).CompareTo((DateTime)x.DateUnlocked));
                        DateTime TempDateTime = DateTime.Now;

                        // Find last achievement date unlock
                        if (((DateTime)Achievements[0].DateUnlocked).ToLocalTime().ToString("yyyy-MM") != "0001-01" && ((DateTime)Achievements[0].DateUnlocked).ToLocalTime().ToString("yyyy-MM") != "1982-12")
                        {
                            TempDateTime = ((DateTime)Achievements[0].DateUnlocked).ToLocalTime();
                        }

                        for (int i = limit; i >= 0; i--)
                        {
                            //GraphicsAchievementsLabels[(limit - i)] = TempDateTime.AddMonths(-i).ToString("yyyy-MM");
                            GraphicsAchievementsLabels[(limit - i)] = (string)localDateYMConverter.Convert(TempDateTime.AddMonths(-i), null, null, null);
                            SourceAchievementsSeries.Add(new CustomerForSingle
                            {
                                Name = TempDateTime.AddMonths(-i).ToString("yyyy-MM"),
                                Values = 0
                            });
                        }

                        for (int i = 0; i < Achievements.Count; i++)
                        {
                            //string tempDate = ((DateTime)Achievements[i].DateUnlocked).ToLocalTime().ToString("yyyy-MM");
                            string tempDate = (string)localDateYMConverter.Convert(((DateTime)Achievements[i].DateUnlocked).ToLocalTime(), null, null, null);
                            int index = Array.IndexOf(GraphicsAchievementsLabels, tempDate);

                            if (index >= 0 && index < (limit + 1))
                            {
                                SourceAchievementsSeries[index].Values += 1;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", $"Error in load GetCountByMonth({GameID.ToString()})");
                }
            }

            return new AchievementsGraphicsDataCount { Labels = GraphicsAchievementsLabels, Series = SourceAchievementsSeries };
        }

        public AchievementsGraphicsDataCountSources GetCountBySources()
        {
            List<string> tempSourcesLabels = new List<string>();

            if (_settings.EnableRetroAchievementsView && _settings.EnableRetroAchievements)
            {
                if (_isRetroachievements)
                {
                    if (_settings.EnableRetroAchievements)
                    {
                        tempSourcesLabels.Add("RetroAchievements");
                    }
                }
                else
                {
                    if (_settings.EnableGog)
                    {
                        tempSourcesLabels.Add("GOG");
                    }
                    if (_settings.EnableSteam)
                    {
                        tempSourcesLabels.Add("Steam");
                    }
                    if (_settings.EnableOrigin)
                    {
                        tempSourcesLabels.Add("Origin");
                    }
                    if (_settings.EnableXbox)
                    {
                        tempSourcesLabels.Add("Xbox");
                    }
                    if (_settings.EnableLocal)
                    {
                        tempSourcesLabels.Add("Playnite");
                    }
                }
            }
            else
            {
                if (_settings.EnableGog)
                {
                    tempSourcesLabels.Add("GOG");
                }
                if (_settings.EnableSteam)
                {
                    tempSourcesLabels.Add("Steam");
                }
                if (_settings.EnableOrigin)
                {
                    tempSourcesLabels.Add("Origin");
                }
                if (_settings.EnableXbox)
                {
                    tempSourcesLabels.Add("Xbox");
                }
                if (_settings.EnableRetroAchievements)
                {
                    tempSourcesLabels.Add("RetroAchievements");
                }
                if (_settings.EnableLocal)
                {
                    tempSourcesLabels.Add("Playnite");
                }
            }

            tempSourcesLabels.Sort((x, y) => x.CompareTo(y));

            string[] GraphicsAchievementsLabels = new string[tempSourcesLabels.Count];
            List<AchievementsGraphicsDataSources> tempDataUnlocked = new List<AchievementsGraphicsDataSources>();
            List<AchievementsGraphicsDataSources> tempDataLocked = new List<AchievementsGraphicsDataSources>();
            List<AchievementsGraphicsDataSources> tempDataTotal = new List<AchievementsGraphicsDataSources>();
            for (int i = 0; i < tempSourcesLabels.Count; i++)
            {
                GraphicsAchievementsLabels[i] = TransformIcon.Get(tempSourcesLabels[i]);
                tempDataLocked.Add(new AchievementsGraphicsDataSources { source = tempSourcesLabels[i], value = 0 });
                tempDataUnlocked.Add(new AchievementsGraphicsDataSources { source = tempSourcesLabels[i], value = 0 });
                tempDataTotal.Add(new AchievementsGraphicsDataSources { source = tempSourcesLabels[i], value = 0 });
            }


            foreach (var item in PluginDatabase)
            {
                try
                {
                    Game game = _PlayniteApi.Database.Games.Get(item.Key);

                    string SourceName = PlayniteTools.GetSourceName(game, _PlayniteApi);

                    foreach (Achievements achievements in item.Value.Achievements)
                    {
                        for (int i = 0; i < tempDataUnlocked.Count; i++)
                        {
                            if (tempDataUnlocked[i].source == SourceName)
                            {
                                tempDataTotal[i].value += 1;
                                if (achievements.DateUnlocked != default(DateTime))
                                {
                                    tempDataUnlocked[i].value += 1;
                                }
                                if (achievements.DateUnlocked == default(DateTime))
                                {
                                    tempDataLocked[i].value += 1;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", $"Error on GetCountBySources() for {item.Key}");
                }
            }

            ChartValues<CustomerForSingle> SourceAchievementsSeriesUnlocked = new ChartValues<CustomerForSingle>();
            ChartValues<CustomerForSingle> SourceAchievementsSeriesLocked = new ChartValues<CustomerForSingle>();
            ChartValues<CustomerForSingle> SourceAchievementsSeriesTotal = new ChartValues<CustomerForSingle>();
            for (int i = 0; i < tempDataUnlocked.Count; i++)
            {
                SourceAchievementsSeriesUnlocked.Add(new CustomerForSingle
                {
                    Name = TransformIcon.Get(tempDataUnlocked[i].source),
                    Values = tempDataUnlocked[i].value
                });
                SourceAchievementsSeriesLocked.Add(new CustomerForSingle
                {
                    Name = TransformIcon.Get(tempDataLocked[i].source),
                    Values = tempDataLocked[i].value
                });
                SourceAchievementsSeriesTotal.Add(new CustomerForSingle
                {
                    Name = TransformIcon.Get(tempDataTotal[i].source),
                    Values = tempDataTotal[i].value
                });
            }


            return new AchievementsGraphicsDataCountSources
            {
                Labels = GraphicsAchievementsLabels,
                SeriesLocked = SourceAchievementsSeriesLocked,
                SeriesUnlocked = SourceAchievementsSeriesUnlocked,
                SeriesTotal = SourceAchievementsSeriesTotal
            };
        }

        /// <summary>
        /// Get number achievements unlock by month for a game or not.
        /// </summary>
        /// <param name="GameID"></param>
        /// <returns></returns>
        public AchievementsGraphicsDataCount GetCountByDay(Guid? GameID = null, int limit = 11)
        {
            string[] GraphicsAchievementsLabels = new string[limit + 1];
            ChartValues<CustomerForSingle> SourceAchievementsSeries = new ChartValues<CustomerForSingle>();

            LocalDateConverter localDateConverter = new LocalDateConverter();

            // All achievements
            if (GameID == null)
            {
                for (int i = limit; i >= 0; i--)
                {
                    //GraphicsAchievementsLabels[(limit - i)] = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd");
                    GraphicsAchievementsLabels[(limit - i)] = (string)localDateConverter.Convert(DateTime.Now.AddDays(-i), null, null, null);
                    SourceAchievementsSeries.Add(new CustomerForSingle
                    {
                        //Name = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd"),
                        Name = (string)localDateConverter.Convert(DateTime.Now.AddDays(-i), null, null, null),
                        Values = 0
                    });
                }

                try
                { 
                    foreach (var item in PluginDatabase)
                    {
                        List<Achievements> temp = item.Value.Achievements;
                        foreach (Achievements itemAchievements in temp)
                        {
                            if (itemAchievements.DateUnlocked != null && itemAchievements.DateUnlocked != default(DateTime))
                            {
                                //string tempDate = ((DateTime)itemAchievements.DateUnlocked).ToLocalTime().ToString("yyyy-MM-dd");
                                string tempDate = (string)localDateConverter.Convert(((DateTime)itemAchievements.DateUnlocked).ToLocalTime(), null, null, null);
                                int index = Array.IndexOf(GraphicsAchievementsLabels, tempDate);

                                if (index >= 0 && index < (limit + 1))
                                {
                                    SourceAchievementsSeries[index].Values += 1;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", $"Error in load GetCountByDay()");
                }
            }
            // Achievement for a game
            else
            {
                try { 
                    List<Achievements> Achievements = Get((Guid)GameID).Achievements;

                    if (Achievements != null && Achievements.Count > 0)
                    {
                        Achievements.Sort((x, y) => ((DateTime)y.DateUnlocked).CompareTo((DateTime)x.DateUnlocked));
                        DateTime TempDateTime = DateTime.Now;

                        // Find last achievement date unlock
                        if (((DateTime)Achievements[0].DateUnlocked).ToLocalTime().ToString("yyyy-MM-dd") != "0001-01-01" && ((DateTime)Achievements[0].DateUnlocked).ToLocalTime().ToString("yyyy-MM-dd") != "1982-12-15")
                        {
                            TempDateTime = ((DateTime)Achievements[0].DateUnlocked).ToLocalTime();
                        }

                        for (int i = limit; i >= 0; i--)
                        {
                            //GraphicsAchievementsLabels[(limit - i)] = TempDateTime.AddDays(-i).ToString("yyyy-MM-dd");
                            GraphicsAchievementsLabels[(limit - i)] = (string)localDateConverter.Convert(TempDateTime.AddDays(-i), null, null, null);
                            SourceAchievementsSeries.Add(new CustomerForSingle
                            {
                                //Name = TempDateTime.AddDays(-i).ToString("yyyy-MM-dd"),
                                Name = (string)localDateConverter.Convert((TempDateTime.AddDays(-i)), null, null, null),
                                Values = 0
                            });
                        }

                        for (int i = 0; i < Achievements.Count; i++)
                        {
                            //string tempDate = ((DateTime)Achievements[i].DateUnlocked).ToLocalTime().ToString("yyyy-MM-dd");
                            string tempDate = (string)localDateConverter.Convert(((DateTime)Achievements[i].DateUnlocked).ToLocalTime(), null, null, null);
                            int index = Array.IndexOf(GraphicsAchievementsLabels, tempDate);

                            if (index >= 0 && index < (limit + 1))
                            {
                                SourceAchievementsSeries[index].Values += 1;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", $"Error in load GetCountByDay({GameID.ToString()})");
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
        public static bool VerifToAddOrShow(SuccessStory plugin, IPlayniteAPI PlayniteApi, string GameSourceName, SuccessStorySettings settings, string PluginUserDataPath)
        {
            bool Result = false;

            if (settings.EnableSteam && GameSourceName.ToLower() == "steam")
            {
                if (PlayniteTools.IsDisabledPlaynitePlugins("SteamLibrary", PlayniteApi.Paths.ConfigurationPath))
                {
                    logger.Warn("SuccessStory - Steam is enable then disabled");
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "SuccessStory-Steam-disabled",
                        $"SuccessStory - {resources.GetString("LOCSuccessStoryNotificationsSteamDisabled")}",
                        NotificationType.Error,
                        () => plugin.OpenSettingsView()
                    ));
                    return false;
                }
                else
                {
                    SteamAchievements steamAchievements = new SteamAchievements(PlayniteApi, settings, PluginUserDataPath);
                    if (!steamAchievements.IsConfigured())
                    {
                        logger.Warn("SuccessStory - Bad Steam configuration");
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "SuccessStory-Steam-NoConfig",
                            $"SuccessStory - {resources.GetString("LOCSuccessStoryNotificationsSteamBadConfig")}",
                            NotificationType.Error,
                            () => plugin.OpenSettingsView()
                        ));
                        return false;
                    }
                }
                return true;
            }

            if (settings.EnableGog && GameSourceName.ToLower() == "gog")
            {
                if (PlayniteTools.IsDisabledPlaynitePlugins("GogLibrary", PlayniteApi.Paths.ConfigurationPath))
                {
                    logger.Warn("SuccessStory - GOG is enable then disabled");
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "SuccessStory-GOG-disabled",
                        $"SuccessStory - {resources.GetString("LOCSuccessStoryNotificationsGogDisabled")}",
                        NotificationType.Error,
                        () => plugin.OpenSettingsView()
                    ));
                    return false;
                }
                else
                {
                    GogAchievements gogAchievements = new GogAchievements(PlayniteApi, settings, PluginUserDataPath);

                    if (VerifToAddOrShowGog == null)
                    {
                        VerifToAddOrShowGog = gogAchievements.IsConnected();
                    }

                    if (!(bool)VerifToAddOrShowGog)
                    {
                        logger.Warn("SuccessStory - Gog user is not authenticate");
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "SuccessStory-Gog-NoAuthenticated",
                            $"SuccessStory - {resources.GetString("LOCSuccessStoryNotificationsGogNoAuthenticate")}",
                            NotificationType.Error,
                            () => plugin.OpenSettingsView()
                        ));
                        return false;
                    }
                }
                return true;
            }

            if (settings.EnableOrigin && GameSourceName.ToLower() == "origin")
            {
                if (PlayniteTools.IsDisabledPlaynitePlugins("OriginLibrary", PlayniteApi.Paths.ConfigurationPath))
                {
                    logger.Warn("SuccessStory - Origin is enable then disabled");
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "SuccessStory-Origin-disabled",
                        $"SuccessStory - {resources.GetString("LOCSuccessStoryNotificationsOriginDisabled")}",
                        NotificationType.Error,
                        () => plugin.OpenSettingsView()
                    ));
                    return false;
                }
                else
                {
                    OriginAchievements originAchievements = new OriginAchievements(PlayniteApi, settings, PluginUserDataPath);

                    if (VerifToAddOrShowOrigin == null)
                    {
                        VerifToAddOrShowOrigin = originAchievements.IsConnected();
                    }

                    if (!(bool)VerifToAddOrShowOrigin)
                    {
                        logger.Warn("SuccessStory - Origin user is not authenticated");
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "SuccessStory-Origin-NoAuthenticate",
                            $"SuccessStory - {resources.GetString("LOCSuccessStoryNotificationsOriginNoAuthenticate")}",
                            NotificationType.Error,
                            () => plugin.OpenSettingsView()
                        ));
                        return false;
                    }
                }
                return true;
            }

            if (settings.EnableXbox && GameSourceName.ToLower() == "xbox")
            {
                if (PlayniteTools.IsDisabledPlaynitePlugins("XboxLibrary", PlayniteApi.Paths.ConfigurationPath))
                {
                    logger.Warn("SuccessStory - Xbox is enable then disabled");
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "SuccessStory-Xbox-disabled",
                        $"SuccessStory - {resources.GetString("LOCSuccessStoryNotificationsXboxDisabled")}",
                        NotificationType.Error,
                        () => plugin.OpenSettingsView()
                    ));
                    return false;
                }
                else
                {
                    XboxAchievements xboxAchievements = new XboxAchievements(PlayniteApi, settings, PluginUserDataPath);

                    if (VerifToAddOrShowXbox == null)
                    {
                        VerifToAddOrShowXbox = xboxAchievements.IsConnected();
                    }

                    if (!(bool)VerifToAddOrShowXbox)
                    {
                        logger.Warn("SuccessStory - Xbox user is not authenticated");
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "SuccessStory-Xbox-NoAuthenticate",
                            $"SuccessStory - {resources.GetString("LOCSuccessStoryNotificationsXboxNotAuthenticate")}",
                            NotificationType.Error,
                            () => plugin.OpenSettingsView()
                        ));
                        return false;
                    }
                }
                return true;
            }

            if (settings.EnableLocal && GameSourceName.ToLower() == "playnite")
            {
                return true;
            }

            if (settings.EnableRetroAchievements && GameSourceName.ToLower() == "retroachievements")
            {
                RetroAchievements retroAchievements = new RetroAchievements(PlayniteApi, settings, PluginUserDataPath);
                if (!retroAchievements.IsConfigured())
                {
                    logger.Warn("SuccessStory - Bad RetroAchievements configuration");
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "SuccessStory-RetroAchievements-NoConfig",
                        $"SuccessStory - {resources.GetString("LOCSuccessStoryNotificationsRetroAchievementsBadConfig")}",
                        NotificationType.Error,
                        () => plugin.OpenSettingsView()
                    ));
                    return false;
                }
                return true;
            }

            return Result;
        }


        private GogAchievements gogAPI { get; set; }
        private OriginAchievements originAPI { get; set; }
        private XboxAchievements xboxAPI { get; set; }

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
                    if (!PlayniteTools.IsDisabledPlaynitePlugins("GogLibrary", _PlayniteApi.Paths.ConfigurationPath) && settings.EnableGog && gogAPI == null)
                    {
                        gogAPI = new GogAchievements(_PlayniteApi, settings, _PluginUserDataPath);
                    }
                    break;

                case "origin":
                    if (!PlayniteTools.IsDisabledPlaynitePlugins("OriginLibrary", _PlayniteApi.Paths.ConfigurationPath) && originAPI == null)
                    {
                        originAPI = new OriginAchievements(_PlayniteApi, settings, _PluginUserDataPath);
                    }
                    break;

                case "Xbox":
                    if (!PlayniteTools.IsDisabledPlaynitePlugins("XboxLibrary", _PlayniteApi.Paths.ConfigurationPath) && xboxAPI == null)
                    {
                        xboxAPI = new XboxAchievements(_PlayniteApi, settings, _PluginUserDataPath);
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
            string GameSourceName = PlayniteTools.GetSourceName(GameAdded, _PlayniteApi);
            string PluginDatabaseGamePath = PluginDatabasePath + GameId.ToString() + ".json";
      
            List<Achievements> Achievements = new List<Achievements>();

            // Generate database only this source
            if (VerifToAddOrShow(_plugin, _PlayniteApi, GameSourceName, settings, _PluginUserDataPath))
            {
                // Generate only not exist
                if (!File.Exists(PluginDatabaseGamePath))
                {
#if DEBUG
                    logger.Debug($"SuccessStory - VerifToAddOrShow({GameAdded.Name}, {GameSourceName}) - OK");
#endif

                    // TODO one func
                    if (GameSourceName.ToLower() == "gog")
                    {
                        if (gogAPI == null)
                        {
                            gogAPI = new GogAchievements(_PlayniteApi, settings, _PluginUserDataPath);
                        }
                        GameAchievements = gogAPI.GetAchievements(GameAdded);
                    }

                    if (GameSourceName.ToLower() == "steam")
                    {
                        SteamAchievements steamAPI = new SteamAchievements(_PlayniteApi, settings, _PluginUserDataPath);
                        GameAchievements = steamAPI.GetAchievements(GameAdded);
                    }

                    if (GameSourceName.ToLower() == "origin")
                    {
                        if (originAPI == null)
                        {
                            originAPI = new OriginAchievements(_PlayniteApi, settings, _PluginUserDataPath);
                        }
                        GameAchievements = originAPI.GetAchievements(GameAdded);
                    }

                    if (GameSourceName.ToLower() == "xbox")
                    {
                        if (xboxAPI == null)
                        {
                            xboxAPI = new XboxAchievements(_PlayniteApi, settings, _PluginUserDataPath);
                        }
                        GameAchievements = xboxAPI.GetAchievements(GameAdded);
                    }

                    if (GameSourceName.ToLower() == "playnite")
                    {
                        SteamAchievements steamAPI = new SteamAchievements(_PlayniteApi, settings, _PluginUserDataPath);
                        steamAPI.SetLocal();
                        GameAchievements = steamAPI.GetAchievements(GameAdded);
                    }

                    if (GameSourceName.ToLower() == "retroachievements")
                    {
                        RetroAchievements retroAchievementsAPI = new RetroAchievements(_PlayniteApi, settings, _PluginUserDataPath);
                        GameAchievements = retroAchievementsAPI.GetAchievements(GameAdded);
                    }

#if DEBUG
                    logger.Debug($"SuccessStory - Achievements for {GameAdded.Name} - {GameSourceName} - {JsonConvert.SerializeObject(GameAchievements)}");
#endif

                    if (GameAchievements != null)
                    {
                        File.WriteAllText(PluginDatabaseGamePath, JsonConvert.SerializeObject(GameAchievements));
                    }
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"SuccessStory - VerifToAddOrShow({GameAdded.Name}, {GameSourceName}) - KO");
#endif
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

            try
            {
                if (File.Exists(PluginDatabaseGamePath))
                {
                    File.Delete(PluginDatabaseGamePath);
                    PluginDatabase.TryRemove(GameRemoved.Id, out var value);
                }
            }
            catch
            {
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

            try { 
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
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStroy", $"Error on Progession()");
            }

            return Result;
        }

        public ProgressionAchievements ProgessionLaunched()
        {
            ProgressionAchievements Result = new ProgressionAchievements();
            int Total = 0;
            int Locked = 0;
            int Unlocked = 0;

            try
            {
                foreach (var item in PluginDatabase)
                {
                    GameAchievements GameAchievements = item.Value;

                    if (GameAchievements.HaveAchivements && _PlayniteApi.Database.Games.Get(item.Key).Playtime > 0)
                    {
                        Total += GameAchievements.Total;
                        Locked += GameAchievements.Locked;
                        Unlocked += GameAchievements.Unlocked;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStroy",  $"Error on ProgessionLaunched()");
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

            try { 
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
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStroy", $"Error on ProgessionGame()");
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

            try { 
                foreach (var item in PluginDatabase)
                {
                    Guid Id = item.Key;
                    Game Game = _PlayniteApi.Database.Games.Get(Id);
                    GameAchievements GameAchievements = item.Value;

                    if (GameAchievements.HaveAchivements && Game.SourceId == GameSourceId)
                    {
                        Total += GameAchievements.Total;
                        Locked += GameAchievements.Locked;
                        Unlocked += GameAchievements.Unlocked;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStroy", $"Error on ProgessionSource()");
            }

            Result.Total = Total;
            Result.Locked = Locked;
            Result.Unlocked = Unlocked;
            Result.Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;

            return Result;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class AchievementsGraphicsDataCount
    {
        public string[] Labels { get; set; }
        public ChartValues<CustomerForSingle> Series { get; set; }
    }

    public class AchievementsGraphicsDataCountSources
    {
        public string[] Labels { get; set; }
        public ChartValues<CustomerForSingle> SeriesUnlocked { get; set; }
        public ChartValues<CustomerForSingle> SeriesLocked { get; set; }
        public ChartValues<CustomerForSingle> SeriesTotal { get; set; }
    }

    public class AchievementsGraphicsDataSources
    {
        public string source { get; set; }
        public int value { get; set; }
    }
}
