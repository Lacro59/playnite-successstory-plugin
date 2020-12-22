using LiveCharts;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using PluginCommon.Collections;
using PluginCommon.LiveChartsCommon;
using SuccessStory.Clients;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Services
{
    public class SuccessStoryDatabase : PluginDatabaseObject<SuccessStorySettings, SuccessStoryCollection, GameAchievements>
    {
        private SuccessStory _plugin;

        private GogAchievements gogAPI { get; set; }
        private OriginAchievements originAPI { get; set; }
        private XboxAchievements xboxAPI { get; set; }

        public static bool? VerifToAddOrShowGog = null;
        public static bool? VerifToAddOrShowOrigin = null;
        public static bool? VerifToAddOrShowRetroAchievements = null;
        public static bool? VerifToAddOrShowSteam = null;
        public static bool? VerifToAddOrShowXbox = null;
        public static bool? VerifToAddOrShowRpcs3 = null;

        private bool _isRetroachievements { get; set; }

        public static CumulErrors ListErrors = new CumulErrors();


        public SuccessStoryDatabase(SuccessStory plugin, IPlayniteAPI PlayniteApi, SuccessStorySettings PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, PluginUserDataPath)
        {
            _plugin = plugin;

            PluginName = "SuccessStory";

            ControlAndCreateDirectory(PluginUserDataPath, "Achievements");
        }


        protected override bool LoadDatabase()
        {
            IsLoaded = false;
            Database = new SuccessStoryCollection(PluginDatabaseDirectory);
            Database.SetGameInfo<Achievements>(_PlayniteApi);

            GameSelectedData = new GameAchievements();
            GetPluginTags();

            IsLoaded = true;
            return true;
        }


        public override GameAchievements Get(Guid Id, bool OnlyCache = false)
        {
            GameIsLoaded = false;
            GameAchievements gameAchievements = base.GetOnlyCache(Id);
#if DEBUG
            logger.Debug($"{PluginName} - GetFromDb({Id.ToString()}) - gameAchievements: {JsonConvert.SerializeObject(gameAchievements)}");
#endif

            // Get from web
            if (gameAchievements == null && !OnlyCache)
            {
                gameAchievements = GetFromWeb(_PlayniteApi.Database.Games.Get(Id));
                Add(gameAchievements);

#if DEBUG
                logger.Debug($"{PluginName} - GetFromWeb({Id.ToString()}) - gameAchievements: {JsonConvert.SerializeObject(gameAchievements)}");
#endif
            }

            if (gameAchievements == null)
            {
                Game game = _PlayniteApi.Database.Games.Get(Id);
                gameAchievements = GetDefault(game);
                Add(gameAchievements);
            }

            GameIsLoaded = true;
            return gameAchievements;
        }


        /// <summary>
        /// Generate database achivements for the game if achievement exist and game not exist in database.
        /// </summary>
        /// <param name="game"></param>
        public GameAchievements GetFromWeb(Game game)
        {
            GameAchievements gameAchievements = GetDefault(game);

            Guid GameId = game.Id;
            Guid GameSourceId = game.SourceId;
            string GameSourceName = PlayniteTools.GetSourceName(_PlayniteApi, game);

            List<Achievements> Achievements = new List<Achievements>();

            // Generate database only this source
            if (VerifToAddOrShow(_plugin, _PlayniteApi, PluginSettings, PluginUserDataPath, GameSourceName))
            {
#if DEBUG
                logger.Debug($"SuccessStory - VerifToAddOrShow({game.Name}, {GameSourceName}) - OK");
#endif

                // TODO one func
                if (GameSourceName.ToLower() == "gog")
                {
                    if (gogAPI == null)
                    {
                        gogAPI = new GogAchievements(_PlayniteApi, PluginSettings, PluginUserDataPath);
                    }
                    gameAchievements = gogAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "steam")
                {
                    SteamAchievements steamAPI = new SteamAchievements(_PlayniteApi, PluginSettings, PluginUserDataPath);
                    gameAchievements = steamAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "origin")
                {
                    if (originAPI == null)
                    {
                        originAPI = new OriginAchievements(_PlayniteApi, PluginSettings, PluginUserDataPath);
                    }
                    gameAchievements = originAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "xbox")
                {
                    if (xboxAPI == null)
                    {
                        xboxAPI = new XboxAchievements(_PlayniteApi, PluginSettings, PluginUserDataPath);
                    }
                    gameAchievements = xboxAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "playnite" || GameSourceName.ToLower() == "hacked")
                {
                    SteamAchievements steamAPI = new SteamAchievements(_PlayniteApi, PluginSettings, PluginUserDataPath);
                    steamAPI.SetLocal();
                    gameAchievements = steamAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "retroachievements")
                {
                    RetroAchievements retroAchievementsAPI = new RetroAchievements(_PlayniteApi, PluginSettings, PluginUserDataPath);
                    gameAchievements = retroAchievementsAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "rpcs3")
                {
                    Rpcs3Achievements rpcs3Achievements = new Rpcs3Achievements(_PlayniteApi, PluginSettings, PluginUserDataPath);
                    gameAchievements = rpcs3Achievements.GetAchievements(game);
                }

#if DEBUG
                logger.Debug($"SuccessStory - Achievements for {game.Name} - {GameSourceName} - {JsonConvert.SerializeObject(gameAchievements)}");
#endif
            }
            else
            {
#if DEBUG
                logger.Debug($"SuccessStory - VerifToAddOrShow({game.Name}, {GameSourceName}) - KO");
#endif
            }

            return gameAchievements;
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
                    foreach (var item in Database.Items)
                    {
                        if (!item.Value.HaveAchivements || item.Value.IsDeleted)
                        {
                            continue;
                        }

                        List<Achievements> temp = item.Value.Items;
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
                    List<Achievements> Achievements = GetOnlyCache((Guid)GameID).Items;

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

            if (PluginSettings.EnableRetroAchievementsView && PluginSettings.EnableRetroAchievements)
            {
                if (_isRetroachievements)
                {
                    if (PluginSettings.EnableRetroAchievements)
                    {
                        tempSourcesLabels.Add("RetroAchievements");
                    }
                }
                else
                {
                    if (PluginSettings.EnableGog)
                    {
                        tempSourcesLabels.Add("GOG");
                    }
                    if (PluginSettings.EnableSteam)
                    {
                        tempSourcesLabels.Add("Steam");
                    }
                    if (PluginSettings.EnableOrigin)
                    {
                        tempSourcesLabels.Add("Origin");
                    }
                    if (PluginSettings.EnableXbox)
                    {
                        tempSourcesLabels.Add("Xbox");
                    }
                    if (PluginSettings.EnableLocal)
                    {
                        tempSourcesLabels.Add("Playnite");
                    }
                    if (PluginSettings.EnableRpcs3Achievements)
                    {
                        tempSourcesLabels.Add("RPCS3");
                    }
                }
            }
            else
            {
                if (PluginSettings.EnableGog)
                {
                    tempSourcesLabels.Add("GOG");
                }
                if (PluginSettings.EnableSteam)
                {
                    tempSourcesLabels.Add("Steam");
                }
                if (PluginSettings.EnableOrigin)
                {
                    tempSourcesLabels.Add("Origin");
                }
                if (PluginSettings.EnableXbox)
                {
                    tempSourcesLabels.Add("Xbox");
                }
                if (PluginSettings.EnableRetroAchievements)
                {
                    tempSourcesLabels.Add("RetroAchievements");
                }
                if (PluginSettings.EnableRpcs3Achievements)
                {
                    tempSourcesLabels.Add("RPCS3");
                }
                if (PluginSettings.EnableLocal)
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


            foreach (var item in Database.Items)
            {
                if (!item.Value.HaveAchivements || item.Value.IsDeleted)
                {
                    continue;
                }

                try
                {
                    string SourceName = PlayniteTools.GetSourceName(_PlayniteApi, item.Key);

                    foreach (Achievements achievements in item.Value.Items)
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
                    foreach (var item in Database.Items)
                    {
                        if (!item.Value.HaveAchivements || item.Value.IsDeleted == false)
                        {
                            continue;
                        }

                        List<Achievements> temp = item.Value.Items;
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
                try
                {
                    List<Achievements> Achievements = GetOnlyCache((Guid)GameID).Items;

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
        /// 
        /// </summary>
        /// <param name="GameSourceName"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static bool VerifToAddOrShow(SuccessStory plugin, IPlayniteAPI PlayniteApi, SuccessStorySettings settings, string PluginUserDataPath, string GameSourceName)
        {
            if (settings.EnableSteam && GameSourceName.ToLower() == "steam")
            {
                if (PlayniteTools.IsDisabledPlaynitePlugins("SteamLibrary", PlayniteApi.Paths.ConfigurationPath))
                {
                    logger.Warn("SuccessStory - Steam is enable then disabled");
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "SuccessStory-Steam-disabled",
                        $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsSteamDisabled")}",
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
                            $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsSteamBadConfig")}",
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
                        $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsGogDisabled")}",
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
                            $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsGogNoAuthenticate")}",
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
                        $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsOriginDisabled")}",
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
                            $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsOriginNoAuthenticate")}",
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
                        $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsXboxDisabled")}",
                        NotificationType.Error,
                        () => plugin.OpenSettingsView()
                    ));
                    return false;
                }
                else
                {
                    XboxAchievements xboxAchievements = new XboxAchievements(PlayniteApi, settings, PluginUserDataPath);

                    //#if DEBUG
                    logger.Debug($"SuccessStory - VerifToAddOrShowXbox: {VerifToAddOrShowXbox}");
                    //#endif
                    if (VerifToAddOrShowXbox == null)
                    {
                        VerifToAddOrShowXbox = xboxAchievements.IsConnected();
                    }
                    //#if DEBUG
                    logger.Debug($"SuccessStory - VerifToAddOrShowXbox: {VerifToAddOrShowXbox}");
                    //#endif

                    if (!(bool)VerifToAddOrShowXbox)
                    {
                        logger.Warn("SuccessStory - Xbox user is not authenticated");
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "SuccessStory-Xbox-NoAuthenticate",
                            $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsXboxNotAuthenticate")}",
                            NotificationType.Error,
                            () => plugin.OpenSettingsView()
                        ));
                        return false;
                    }
                }
                return true;
            }

            if (settings.EnableLocal && (GameSourceName.ToLower() == "playnite" || GameSourceName.ToLower() == "hacked"))
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
                        $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsRetroAchievementsBadConfig")}",
                        NotificationType.Error,
                        () => plugin.OpenSettingsView()
                    ));
                    return false;
                }
                return true;
            }

            if (settings.EnableRpcs3Achievements && GameSourceName.ToLower() == "rpcs3")
            {
                Rpcs3Achievements rpcs3Achievements = new Rpcs3Achievements(PlayniteApi, settings, PluginUserDataPath);
                if (!rpcs3Achievements.IsConfigured())
                {
                    logger.Warn("SuccessStory - Bad RPCS3 configuration");
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "SuccessStory-Rpcs3-NoConfig",
                        $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsRpcs3BadConfig")}",
                        NotificationType.Error,
                        () => plugin.OpenSettingsView()
                    ));
                    return false;
                }
                return true;
            }

            logger.Warn($"SuccessStory - VerifToAddOrShow() find no action");
            return false;
        }


        public void InitializeMultipleAdd(string GameSourceName = "all")
        {
            switch (GameSourceName.ToLower())
            {
                case "all":
                    InitializeMultipleAdd("Steam");
                    InitializeMultipleAdd("GOG");
                    InitializeMultipleAdd("Origin");
                    break;

                case "steam":
                    break;

                case "gog":
                    if (!PlayniteTools.IsDisabledPlaynitePlugins("GogLibrary", _PlayniteApi.Paths.ConfigurationPath) && PluginSettings.EnableGog && gogAPI == null)
                    {
                        gogAPI = new GogAchievements(_PlayniteApi, PluginSettings, PluginUserDataPath);
                    }
                    break;

                case "origin":
                    if (!PlayniteTools.IsDisabledPlaynitePlugins("OriginLibrary", _PlayniteApi.Paths.ConfigurationPath) && originAPI == null)
                    {
                        originAPI = new OriginAchievements(_PlayniteApi, PluginSettings, PluginUserDataPath);
                    }
                    break;

                case "Xbox":
                    if (!PlayniteTools.IsDisabledPlaynitePlugins("XboxLibrary", _PlayniteApi.Paths.ConfigurationPath) && xboxAPI == null)
                    {
                        xboxAPI = new XboxAchievements(_PlayniteApi, PluginSettings, PluginUserDataPath);
                    }
                    break;

                case "playnite":
                    break;
            }
        }


        public bool VerifAchievementsLoad(Guid gameID)
        {
            return GetOnlyCache(gameID) != null;
        }


        public ProgressionAchievements Progession()
        {
            ProgressionAchievements Result = new ProgressionAchievements();
            int Total = 0;
            int Locked = 0;
            int Unlocked = 0;

            try
            {
                foreach (var item in Database.Items)
                {
                    var GameAchievements = item.Value;

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
                foreach (var item in Database.Items)
                {
                    var GameAchievements = item.Value;

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
                Common.LogError(ex, "SuccessStroy", $"Error on ProgessionLaunched()");
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

            try
            {
                foreach (var item in Database.Items)
                {
                    Guid Id = item.Key;
                    var GameAchievements = item.Value;

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

            try
            {
                foreach (var item in Database.Items)
                {
                    Guid Id = item.Key;
                    Game Game = _PlayniteApi.Database.Games.Get(Id);
                    var GameAchievements = item.Value;

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
