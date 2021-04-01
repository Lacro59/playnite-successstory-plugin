using LiveCharts;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsControls.LiveChartsCommon;
using SuccessStory.Clients;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonPluginsShared.Interfaces;

namespace SuccessStory.Services
{
    public class SuccessStoryDatabase : PluginDatabaseObject<SuccessStorySettingsViewModel, SuccessStoryCollection, GameAchievements>
    {
        public SuccessStory Plugin;

        private GogAchievements GogAPI { get; set; }
        private OriginAchievements OriginAPI { get; set; }
        private XboxAchievements XboxAPI { get; set; }

        public static bool? VerifToAddOrShowGog = null;
        public static bool? VerifToAddOrShowOrigin = null;
        public static bool? VerifToAddOrShowRetroAchievements = null;
        public static bool? VerifToAddOrShowSteam = null;
        public static bool? VerifToAddOrShowXbox = null;
        public static bool? VerifToAddOrShowRpcs3 = null;

        private bool _isRetroachievements { get; set; }

        public static CumulErrors ListErrors = new CumulErrors();


        public SuccessStoryDatabase(IPlayniteAPI PlayniteApi, SuccessStorySettingsViewModel PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, "SuccessStory", PluginUserDataPath)
        {

        }


        public void InitializeClient(SuccessStory Plugin)
        {
            this.Plugin = Plugin;
        }


        protected override bool LoadDatabase()
        {
            Database = new SuccessStoryCollection(Paths.PluginDatabasePath);
            Database.SetGameInfo<Achievements>(PlayniteApi);

            GetPluginTags();

            return true;
        }


        public GameAchievements GetManual(Game game)
        {
            GameAchievements gameAchievements = GetDefault(game);

            SteamAchievements steamAPI = new SteamAchievements(PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath);
            steamAPI.SetLocal();
            gameAchievements = steamAPI.GetAchievements(game);
            gameAchievements.IsManual = true;

            Add(gameAchievements);

            Common.LogDebug(true, $"GetManual({game.Id.ToString()}) - gameAchievements: {JsonConvert.SerializeObject(gameAchievements)}");

            return gameAchievements;
        }

        public override GameAchievements Get(Guid Id, bool OnlyCache = false, bool Force = false)
        {
            GameAchievements gameAchievements = base.GetOnlyCache(Id);

            // Get from web
            if ((gameAchievements == null && !OnlyCache) || Force)
            {
                gameAchievements = GetWeb(Id);
                AddOrUpdate(gameAchievements);
            }
            else if (gameAchievements == null)
            {
                Game game = PlayniteApi.Database.Games.Get(Id);
                gameAchievements = GetDefault(game);
                Add(gameAchievements);
            }
            
            return gameAchievements;
        }


        /// <summary>
        /// Generate database achivements for the game if achievement exist and game not exist in database.
        /// </summary>
        /// <param name="game"></param>
        public override GameAchievements GetWeb(Guid Id)
        {
            Game game = PlayniteApi.Database.Games.Get(Id);
            GameAchievements gameAchievements = GetDefault(game);

            Guid GameId = game.Id;
            Guid GameSourceId = game.SourceId;
            string GameSourceName = PlayniteTools.GetSourceName(PlayniteApi, game);

            List<Achievements> Achievements = new List<Achievements>();

            // Generate database only this source
            if (VerifToAddOrShow(Plugin, PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath, GameSourceName))
            {
                Common.LogDebug(true, $"VerifToAddOrShow({game.Name}, {GameSourceName}) - OK");

                // TODO one func
                if (GameSourceName.ToLower() == "gog")
                {
                    if (GogAPI == null)
                    {
                        GogAPI = new GogAchievements(PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath);
                    }
                    gameAchievements = GogAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "steam")
                {
                    SteamAchievements steamAPI = new SteamAchievements(PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath);
                    gameAchievements = steamAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "origin")
                {
                    if (OriginAPI == null)
                    {
                        OriginAPI = new OriginAchievements(PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath);
                    }
                    gameAchievements = OriginAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "xbox")
                {
                    if (XboxAPI == null)
                    {
                        XboxAPI = new XboxAchievements(PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath);
                    }
                    gameAchievements = XboxAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "playnite" || GameSourceName.ToLower() == "hacked")
                {
                    SteamAchievements steamAPI = new SteamAchievements(PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath);
                    steamAPI.SetLocal();
                    gameAchievements = steamAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "retroachievements")
                {
                    RetroAchievements retroAchievementsAPI = new RetroAchievements(PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath);
                    gameAchievements = retroAchievementsAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "rpcs3")
                {
                    Rpcs3Achievements rpcs3Achievements = new Rpcs3Achievements(PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath);
                    gameAchievements = rpcs3Achievements.GetAchievements(game);
                }

                Common.LogDebug(true, $"Achievements for {game.Name} - {GameSourceName} - {JsonConvert.SerializeObject(gameAchievements)}");
            }
            else
            {
                Common.LogDebug(true, $"VerifToAddOrShow({game.Name}, {GameSourceName}) - KO");
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
                    GraphicsAchievementsLabels[(limit - i)] = (string)localDateYMConverter.Convert(DateTime.Now.AddMonths(-i), null, null, null);
                    SourceAchievementsSeries.Add(new CustomerForSingle
                    {
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
                    Common.LogError(ex, false, "Error in load GetCountByMonth()");
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
                    Common.LogError(ex, false, $"Error in load GetCountByMonth({GameID.ToString()})");
                }
            }

            return new AchievementsGraphicsDataCount { Labels = GraphicsAchievementsLabels, Series = SourceAchievementsSeries };
        }

        public AchievementsGraphicsDataCountSources GetCountBySources()
        {
            List<string> tempSourcesLabels = new List<string>();

            if (PluginSettings.Settings.EnableRetroAchievementsView && PluginSettings.Settings.EnableRetroAchievements)
            {
                if (_isRetroachievements)
                {
                    if (PluginSettings.Settings.EnableRetroAchievements)
                    {
                        tempSourcesLabels.Add("RetroAchievements");
                    }
                }
                else
                {
                    if (PluginSettings.Settings.EnableGog)
                    {
                        tempSourcesLabels.Add("GOG");
                    }
                    if (PluginSettings.Settings.EnableSteam)
                    {
                        tempSourcesLabels.Add("Steam");
                    }
                    if (PluginSettings.Settings.EnableOrigin)
                    {
                        tempSourcesLabels.Add("Origin");
                    }
                    if (PluginSettings.Settings.EnableXbox)
                    {
                        tempSourcesLabels.Add("Xbox");
                    }
                    if (PluginSettings.Settings.EnableLocal)
                    {
                        tempSourcesLabels.Add("Playnite");
                        tempSourcesLabels.Add("Hacked");
                    }
                    if (PluginSettings.Settings.EnableRpcs3Achievements)
                    {
                        tempSourcesLabels.Add("RPCS3");
                    }
                }
            }
            else
            {
                if (PluginSettings.Settings.EnableGog)
                {
                    tempSourcesLabels.Add("GOG");
                }
                if (PluginSettings.Settings.EnableSteam)
                {
                    tempSourcesLabels.Add("Steam");
                }
                if (PluginSettings.Settings.EnableOrigin)
                {
                    tempSourcesLabels.Add("Origin");
                }
                if (PluginSettings.Settings.EnableXbox)
                {
                    tempSourcesLabels.Add("Xbox");
                }
                if (PluginSettings.Settings.EnableRetroAchievements)
                {
                    tempSourcesLabels.Add("RetroAchievements");
                }
                if (PluginSettings.Settings.EnableRpcs3Achievements)
                {
                    tempSourcesLabels.Add("RPCS3");
                }
                if (PluginSettings.Settings.EnableLocal)
                {
                    tempSourcesLabels.Add("Playnite");
                    tempSourcesLabels.Add("Hacked");
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
                    string SourceName = PlayniteTools.GetSourceName(PlayniteApi, item.Key);

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
                    Common.LogError(ex, false, $"Error on GetCountBySources() for {item.Key}");
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
                    Common.LogError(ex, false, $"Error in load GetCountByDay()");
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
                    Common.LogError(ex, false, $"Error in load GetCountByDay({GameID.ToString()})");
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

                    Common.LogDebug(true, $"VerifToAddOrShowXbox: {VerifToAddOrShowXbox}");

                    if (VerifToAddOrShowXbox == null)
                    {
                        VerifToAddOrShowXbox = xboxAchievements.IsConnected();
                    }

                    Common.LogDebug(true, $"VerifToAddOrShowXbox: {VerifToAddOrShowXbox}");

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

            logger.Warn($"SuccessStory - VerifToAddOrShow() find no action for {GameSourceName}");
            return false;
        }

        public static bool IsAddOrShowManual(Game game, string GameSourceName)
        {
            if (game.PluginId.ToString() != "00000000-0000-0000-0000-000000000000")
            {
                return
                (
                    GameSourceName.ToLower().IndexOf("steam") == -1 &&
                    GameSourceName.ToLower().IndexOf("gog") == -1 &&
                    GameSourceName.ToLower().IndexOf("origin") == -1 &&
                    GameSourceName.ToLower().IndexOf("xbox") == -1 &&
                    GameSourceName.ToLower().IndexOf("playnite") == -1 &&
                    GameSourceName.ToLower().IndexOf("hacked") == -1 &&
                    GameSourceName.ToLower().IndexOf("retroachievements") == -1 &&
                    GameSourceName.ToLower().IndexOf("rpcs3") == -1
                );
            }

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
                    if (!PlayniteTools.IsDisabledPlaynitePlugins("GogLibrary", PlayniteApi.Paths.ConfigurationPath) && PluginSettings.Settings.EnableGog && GogAPI == null)
                    {
                        GogAPI = new GogAchievements(PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath);
                    }
                    break;

                case "origin":
                    if (!PlayniteTools.IsDisabledPlaynitePlugins("OriginLibrary", PlayniteApi.Paths.ConfigurationPath) && OriginAPI == null)
                    {
                        OriginAPI = new OriginAchievements(PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath);
                    }
                    break;

                case "Xbox":
                    if (!PlayniteTools.IsDisabledPlaynitePlugins("XboxLibrary", PlayniteApi.Paths.ConfigurationPath) && XboxAPI == null)
                    {
                        XboxAPI = new XboxAchievements(PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath);
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


        public override void SetThemesResources(Game game)
        {
            GameAchievements gameAchievements = Get(game, true);

            PluginSettings.Settings.HasData = gameAchievements.HasData;

            PluginSettings.Settings.Is100Percent = gameAchievements.Is100Percent;
            PluginSettings.Settings.Unlocked = gameAchievements.Unlocked;
            PluginSettings.Settings.Locked = gameAchievements.Locked;
            PluginSettings.Settings.Total = gameAchievements.Total;
        }

        public override void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            foreach (var GameUpdated in e.UpdatedItems)
            {
                Database.SetGameInfo<Achievements>(PlayniteApi, GameUpdated.NewData.Id);
            }
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
                Common.LogError(ex, false, $"Error on Progession()");
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

                    if (PlayniteApi.Database.Games.Get(item.Key) != null)
                    {
                        if (GameAchievements.HaveAchivements && PlayniteApi.Database.Games.Get(item.Key).Playtime > 0)
                        {
                            Total += GameAchievements.Total;
                            Locked += GameAchievements.Locked;
                            Unlocked += GameAchievements.Unlocked;
                        }
                    }
                    else
                    {
                        logger.Warn($"SuccessStory - Achievements data without game for {GameAchievements.Name} & {GameAchievements.Id.ToString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on ProgessionLaunched()");
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
                Common.LogError(ex, false, $"Error on ProgessionGame()");
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
                    Game Game = PlayniteApi.Database.Games.Get(Id);
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
                Common.LogError(ex, false, $"Error on ProgessionSource()");
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
