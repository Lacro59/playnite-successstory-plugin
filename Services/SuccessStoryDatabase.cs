using LiveCharts;
using Playnite.SDK;
using Playnite.SDK.Data;
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
using static SuccessStory.Clients.TrueAchievements;
using System.Windows.Threading;
using System.Windows;
using System.Threading;
using SuccessStory.Views;
using CommonPluginsShared.Converters;
using CommonPluginsControls.Controls;
using System.Diagnostics;

namespace SuccessStory.Services
{
    public class SuccessStoryDatabase : PluginDatabaseObject<SuccessStorySettingsViewModel, SuccessStoryCollection, GameAchievements>
    {
        public SuccessStory Plugin;

        private GogAchievements GogAPI { get; set; }
        private OriginAchievements OriginAPI { get; set; }
        private XboxAchievements XboxAPI { get; set; }
        private PSNAchievements PsnAPI { get; set; }

        public static bool? VerifToAddOrShowPsn = null;
        public static bool? VerifToAddOrShowGog = null;
        public static bool? VerifToAddOrShowOrigin = null;
        public static bool? VerifToAddOrShowRetroAchievements = null;
        public static bool? VerifToAddOrShowSteam = null;
        public static bool? VerifToAddOrShowXbox = null;
        public static bool? VerifToAddOrShowRpcs3 = null;
        public static bool? VerifToAddOrShowOverwatch = null;
        public static bool? VerifToAddOrShowSc2 = null;

        private bool _isRetroachievements { get; set; }

        public static CumulErrors ListErrors = new CumulErrors();


        public SuccessStoryDatabase(IPlayniteAPI PlayniteApi, SuccessStorySettingsViewModel PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, "SuccessStory", PluginUserDataPath)
        {

        }


        public void InitializeClient(SuccessStory Plugin)
        {
            this.Plugin = Plugin;

            string SourceName = string.Empty;
            string GameName = string.Empty;

            Task.Run(() =>
            {
                SourceName = "GOG";
                //VerifToAddOrShowGog = false;
                VerifToAddOrShow(Plugin, PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath, SourceName, GameName);
            });

            Task.Run(() =>
            {
                SourceName = "Origin";
                //VerifToAddOrShowOrigin = false;
                VerifToAddOrShow(Plugin, PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath, SourceName, GameName);
            });

            Task.Run(() =>
            {
                SourceName = "Retroachievements";
                //VerifToAddOrShowRetroAchievements = false;
                VerifToAddOrShow(Plugin, PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath, SourceName, GameName);
            });

            Task.Run(() =>
            {
                SourceName = "Steam";
                //VerifToAddOrShowSteam = false;
                VerifToAddOrShow(Plugin, PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath, SourceName, GameName);
            });

            Task.Run(() =>
            {
                SourceName = "Xbox";
                //VerifToAddOrShowXbox = false;
                VerifToAddOrShow(Plugin, PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath, SourceName, GameName);
            });

            Task.Run(() =>
            {
                SourceName = "Rpcs3";
                //VerifToAddOrShowRpcs3 = false;
                VerifToAddOrShow(Plugin, PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath, SourceName, GameName);
            });

            Task.Run(() =>
            {
                SourceName = "Battle.net";
                GameName = "Overwatch";
                //VerifToAddOrShowOverwatch = false;
                VerifToAddOrShow(Plugin, PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath, SourceName, GameName);
            });

            Task.Run(() =>
            {
                SourceName = "Battle.net";
                GameName = "StarCraft II";
                //VerifToAddOrShowSc2 = false;
                VerifToAddOrShow(Plugin, PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath, SourceName, GameName);
            });

            Task.Run(() =>
            {
                SourceName = "Playstation";
                //VerifToAddOrShowPsn = false;
                VerifToAddOrShow(Plugin, PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath, SourceName, GameName);
            });
        }


        protected override bool LoadDatabase()
        {
            Database = new SuccessStoryCollection(Paths.PluginDatabasePath);
            Database.SetGameInfo<Achievements>(PlayniteApi);

            return true;
        }


        public void GetManual(Game game)
        {
            try
            {
                GameAchievements gameAchievements = GetDefault(game);

                var ViewExtension = new SuccessStoreGameSelection(game);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension);
                windowExtension.ShowDialog();

                if (ViewExtension.gameAchievements != null)
                {
                    gameAchievements = ViewExtension.gameAchievements;
                    gameAchievements.IsManual = true;
                }

                AddOrUpdate(gameAchievements);

                SetEstimateTimeToUnlock(game, gameAchievements);

                Common.LogDebug(true, $"GetManual({game.Id.ToString()}) - gameAchievements: {Serialization.ToJson(gameAchievements)}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                PlayniteApi.Notifications.Add(new NotificationMessage(
                    "SuccessStory-error-manual",
                    $"SuccessStory\r\n{ex.Message}",
                    NotificationType.Error
                ));
            }
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
            string GameName = game.Name;

            List<Achievements> Achievements = new List<Achievements>();


            // Generate database only this source
            if (VerifToAddOrShow(Plugin, PlayniteApi, PluginSettings.Settings, Paths.PluginUserDataPath, GameSourceName, GameName))
            {
                Common.LogDebug(true, $"VerifToAddOrShow({game.Name}, {GameSourceName}) - OK");

                // TODO one func
                if (GameSourceName.ToLower() == "gog")
                {
                    if (GogAPI == null)
                    {
                        GogAPI = new GogAchievements();
                    }
                    gameAchievements = GogAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "steam")
                {
                    SteamAchievements steamAPI = new SteamAchievements();
                    gameAchievements = steamAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "origin")
                {
                    if (OriginAPI == null)
                    {
                        OriginAPI = new OriginAchievements();
                    }
                    gameAchievements = OriginAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "xbox")
                {
                    if (XboxAPI == null)
                    {
                        XboxAPI = new XboxAchievements();
                    }
                    gameAchievements = XboxAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "playstation")
                {
                    if (PsnAPI == null)
                    {
                        PsnAPI = new PSNAchievements();
                    }
                    gameAchievements = PsnAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "playnite" || GameSourceName.ToLower() == "hacked")
                {
                    SteamAchievements steamAPI = new SteamAchievements();
                    steamAPI.SetLocal();
                    gameAchievements = steamAPI.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "retroachievements")
                {
                    RetroAchievements retroAchievementsAPI = new RetroAchievements();

                    if (!SuccessStory.IsFromMenu)
                    {
                        GameAchievements TEMPgameAchievements = Get(game, true);
                        retroAchievementsAPI.gameID = TEMPgameAchievements.RAgameID;
                    }

                    gameAchievements = retroAchievementsAPI.GetAchievements(game);
                    gameAchievements.RAgameID = retroAchievementsAPI.gameID;
                }

                if (GameSourceName.ToLower() == "rpcs3")
                {
                    Rpcs3Achievements rpcs3Achievements = new Rpcs3Achievements();
                    gameAchievements = rpcs3Achievements.GetAchievements(game);
                }

                if (GameSourceName.ToLower() == "battle.net")
                {
                    BattleNetAchievements battleNetAchievements = new BattleNetAchievements();
                    gameAchievements = battleNetAchievements.GetAchievements(game);
                }

                Common.LogDebug(true, $"Achievements for {game.Name} - {GameSourceName} - {Serialization.ToJson(gameAchievements)}");
            }
            else
            {
                Common.LogDebug(true, $"VerifToAddOrShow({game.Name}, {GameSourceName}) - KO");
            }


            SetEstimateTimeToUnlock(game, gameAchievements);


            return gameAchievements;
        }


        private GameAchievements SetEstimateTimeToUnlock(Game game, GameAchievements gameAchievements)
        {
            if (gameAchievements.HaveAchivements)
            {
                try
                {
                    EstimateTimeToUnlock EstimateTimeSteam = new EstimateTimeToUnlock();
                    EstimateTimeToUnlock EstimateTimeXbox = new EstimateTimeToUnlock();

                    List<TrueAchievementSearch> ListGames = TrueAchievements.SearchGame(game, OriginData.Steam);
                    if (ListGames.Count > 0)
                    {
                        EstimateTimeSteam = TrueAchievements.GetEstimateTimeToUnlock(ListGames[0].GameUrl);
                    }

                    ListGames = TrueAchievements.SearchGame(game, OriginData.Xbox);
                    if (ListGames.Count > 0)
                    {
                        EstimateTimeXbox = TrueAchievements.GetEstimateTimeToUnlock(ListGames[0].GameUrl);
                    }

                    if (EstimateTimeSteam.DataCount >= EstimateTimeXbox.DataCount)
                    {
                        Common.LogDebug(true, $"Get EstimateTimeSteam for {game.Name}");
                        gameAchievements.EstimateTime = EstimateTimeSteam;
                    }
                    else
                    {
                        Common.LogDebug(true, $"Get EstimateTimeXbox for {game.Name}");
                        gameAchievements.EstimateTime = EstimateTimeXbox;
                    }

                    Update(gameAchievements);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
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
                    var db = Database.Items.Where(x => x.Value.HaveAchivements && !x.Value.IsDeleted).ToList();
                    foreach (var item in db)
                    {
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
            var db = Database.Items.Where(x => x.Value.IsManual);

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
                    if (PluginSettings.Settings.EnablePsn)
                    {
                        tempSourcesLabels.Add("Playstation");
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
                    if (PluginSettings.Settings.EnableManual)
                    {
                        if (db != null && db.Count() > 0)
                        {
                            var ListSources = db.Select(x => x.Value.SourceId).Distinct();
                            foreach (var Source in ListSources)
                            {
                                var gameSource = PlayniteApi.Database.Sources.Get(Source);
                                if (gameSource != null)
                                {
                                    tempSourcesLabels.Add(gameSource.Name);
                                }
                            }
                        }
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
                if (PluginSettings.Settings.EnablePsn)
                {
                    tempSourcesLabels.Add("Playstation");
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
                if (PluginSettings.Settings.EnableManual)
                {
                    if (db != null && db.Count() > 0)
                    {
                        var ListSources = db.Select(x => x.Value.SourceId).Distinct();
                        foreach (var Source in ListSources)
                        {
                            var gameSource = PlayniteApi.Database.Sources.Get(Source);
                            if (gameSource != null)
                            {
                                tempSourcesLabels.Add(gameSource.Name);
                            }
                        }
                    }
                }
            }

            tempSourcesLabels = tempSourcesLabels.Distinct().ToList();
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


            db = Database.Items.Where(x => x.Value.HaveAchivements && !x.Value.IsDeleted).ToList();
            foreach (var item in db)
            {
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
                    var db = Database.Items.Where(x => x.Value.HaveAchivements && !x.Value.IsDeleted).ToList();
                    foreach (var item in db)
                    {
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
        public static bool VerifToAddOrShow(SuccessStory plugin, IPlayniteAPI PlayniteApi, SuccessStorySettings settings, string PluginUserDataPath, string GameSourceName, string GameName)
        {
            if (settings.EnablePsn && GameSourceName.ToLower() == "playstation")
            {
                if (PlayniteTools.IsDisabledPlaynitePlugins("PSNLibrary"))
                {
                    logger.Warn("PSN is enable then disabled");
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "SuccessStory-Psn-disabled",
                        $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsPsnDisabled")}",
                        NotificationType.Error,
                        () => plugin.OpenSettingsView()
                    ));
                    return false;
                }
                else
                {
                    PSNAchievements pSNAchievements = new PSNAchievements();

                    if (VerifToAddOrShowPsn == null)
                    {
                        VerifToAddOrShowPsn = pSNAchievements.IsConnected();
                    }

                    if (!(bool)VerifToAddOrShowPsn)
                    {
                        logger.Warn("PSN user is not authenticate");
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "SuccessStory-Psn-NoAuthenticate",
                            $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsPsnNoAuthenticate")}",
                            NotificationType.Error,
                            () => plugin.OpenSettingsView()
                        ));
                        return false;
                    }
                }
                return true;
            }
            
            if (settings.EnableSteam && GameSourceName.ToLower() == "steam")
            {
                if (PlayniteTools.IsDisabledPlaynitePlugins("SteamLibrary"))
                {
                    logger.Warn("Steam is enable then disabled");
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
                    SteamAchievements steamAchievements = new SteamAchievements();
                    if (!steamAchievements.IsConfigured())
                    {
                        logger.Warn("Bad Steam configuration");
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "SuccessStory-Steam-NoConfig",
                            $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsSteamBadConfig")}",
                            NotificationType.Error,
                            () => plugin.OpenSettingsView()
                        ));
                        return false;
                    }
                    if (!settings.SteamIsPrivate && !steamAchievements.CheckIsPublic())
                    {
                        logger.Warn("Bad Steam configuration");
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "SuccessStory-Steam-NoConfig",
                            $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsSteamPrivate")}",
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
                if (PlayniteTools.IsDisabledPlaynitePlugins("GogLibrary"))
                {
                    logger.Warn("GOG is enable then disabled");
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
                    GogAchievements gogAchievements = new GogAchievements();

                    if (VerifToAddOrShowGog == null)
                    {
                        VerifToAddOrShowGog = gogAchievements.IsConnected();
                    }

                    if (!(bool)VerifToAddOrShowGog)
                    {
                        logger.Warn("Gog user is not authenticate");
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
                if (PlayniteTools.IsDisabledPlaynitePlugins("OriginLibrary"))
                {
                    logger.Warn("Origin is enable then disabled");
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
                    OriginAchievements originAchievements = new OriginAchievements();

                    if (VerifToAddOrShowOrigin == null)
                    {
                        VerifToAddOrShowOrigin = originAchievements.IsConnected();
                    }

                    if (!(bool)VerifToAddOrShowOrigin)
                    {
                        logger.Warn("Origin user is not authenticated");
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
                if (PlayniteTools.IsDisabledPlaynitePlugins("XboxLibrary"))
                {
                    logger.Warn("Xbox is enable then disabled");
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
                    XboxAchievements xboxAchievements = new XboxAchievements();

                    Common.LogDebug(true, $"VerifToAddOrShowXbox: {VerifToAddOrShowXbox}");

                    if (VerifToAddOrShowXbox == null)
                    {
                        VerifToAddOrShowXbox = xboxAchievements.IsConnected();
                    }

                    Common.LogDebug(true, $"VerifToAddOrShowXbox: {VerifToAddOrShowXbox}");

                    if (!(bool)VerifToAddOrShowXbox)
                    {
                        logger.Warn("Xbox user is not authenticated");
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
                RetroAchievements retroAchievements = new RetroAchievements();
                if (!retroAchievements.IsConfigured())
                {
                    logger.Warn("Bad RetroAchievements configuration");
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
                Rpcs3Achievements rpcs3Achievements = new Rpcs3Achievements();
                if (!rpcs3Achievements.IsConfigured())
                {
                    logger.Warn("Bad RPCS3 configuration");
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

            if (settings.EnableOverwatchAchievements && GameSourceName.ToLower() == "battle.net" && GameName.ToLower() == "overwatch")
            {
                if (PlayniteTools.IsDisabledPlaynitePlugins("BattleNetLibrary"))
                {
                    logger.Warn("Battle.net is enable then disabled");
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "SuccessStory-BattleNet-disabled",
                        $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsBattleNetDisabled")}",
                        NotificationType.Error,
                        () => plugin.OpenSettingsView()
                    ));
                    return false;
                }
                else
                {
                    BattleNetAchievements battleNetAchievements = new BattleNetAchievements();

                    Common.LogDebug(true, $"VerifToAddOrShowOverwatch: {VerifToAddOrShowOverwatch}");

                    if (VerifToAddOrShowOverwatch == null)
                    {
                        VerifToAddOrShowOverwatch = battleNetAchievements.IsConnected();
                    }

                    Common.LogDebug(true, $"VerifToAddOrShowOverwatch: {VerifToAddOrShowOverwatch}");

                    if (!(bool)VerifToAddOrShowOverwatch)
                    {
                        logger.Warn("Battle.net user is not authenticated");
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "SuccessStory-BattleNet-NoAuthenticate",
                            $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsBattleNetNoAuthenticate")}",
                            NotificationType.Error,
                            () => plugin.OpenSettingsView()
                        ));
                        return false;
                    }
                }
                return true;
            }

            if (settings.EnableSc2Achievements && GameSourceName.ToLower() == "battle.net" && (GameName.ToLower() == "starcraft 2" || GameName.ToLower() == "starcraft ii"))
            {
                if (PlayniteTools.IsDisabledPlaynitePlugins("BattleNetLibrary"))
                {
                    logger.Warn("Battle.net is enable then disabled");
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "SuccessStory-BattleNet-disabled",
                        $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsBattleNetDisabled")}",
                        NotificationType.Error,
                        () => plugin.OpenSettingsView()
                    ));
                    return false;
                }
                else
                {
                    BattleNetAchievements battleNetAchievements = new BattleNetAchievements();

                    Common.LogDebug(true, $"VerifToAddOrShowSc2: {VerifToAddOrShowSc2}");

                    if (VerifToAddOrShowSc2 == null)
                    {
                        VerifToAddOrShowSc2 = battleNetAchievements.IsConnected();
                    }

                    Common.LogDebug(true, $"VerifToAddOrShowSc2: {VerifToAddOrShowSc2}");

                    if (!(bool)VerifToAddOrShowSc2)
                    {
                        logger.Warn("Battle.net user is not authenticated");
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "SuccessStory-BattleNet-NoAuthenticate",
                            $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsBattleNetNoAuthenticate")}",
                            NotificationType.Error,
                            () => plugin.OpenSettingsView()
                        ));
                        return false;
                    }
                }
                return true;
            }

            Common.LogDebug(true, $"VerifToAddOrShow() find no action for {GameSourceName}");
            return false;
        }

        public static bool IsAddOrShowManual(Game game, string GameSourceName)
        {
            if (game.PluginId != default(Guid))
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
                    GameSourceName.ToLower().IndexOf("rpcs3") == -1 &&
                    GameSourceName.ToLower().IndexOf("playstation") == -1 &&
                    GameSourceName.ToLower().IndexOf("battle.net") == -1
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
                    if (!PlayniteTools.IsDisabledPlaynitePlugins("GogLibrary") && PluginSettings.Settings.EnableGog && GogAPI == null)
                    {
                        GogAPI = new GogAchievements();
                    }
                    break;

                case "origin":
                    if (!PlayniteTools.IsDisabledPlaynitePlugins("OriginLibrary") && OriginAPI == null)
                    {
                        OriginAPI = new OriginAchievements();
                    }
                    break;

                case "xbox":
                    if (!PlayniteTools.IsDisabledPlaynitePlugins("XboxLibrary") && XboxAPI == null)
                    {
                        XboxAPI = new XboxAchievements();
                    }
                    break;

                case "playstation":
                    if (!PlayniteTools.IsDisabledPlaynitePlugins("PSNLibrary") && PsnAPI == null)
                    {
                        PsnAPI = new PSNAchievements();
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
            PluginSettings.Settings.ListAchievements = gameAchievements.Items;
        }

        public override void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            foreach (var GameUpdated in e.UpdatedItems)
            {
                Database.SetGameInfo<Achievements>(PlayniteApi, GameUpdated.NewData.Id);
            }
        }


        #region Tag system
        protected override void GetPluginTags()
        {
            try
            {
                // Get tags in playnite database
                PluginTags = new List<Tag>();
                foreach (Tag tag in PlayniteApi.Database.Tags)
                {
                    if (tag.Name?.IndexOf("[SS] ") > -1 && tag.Name?.IndexOf("<!LOC") ==-1)
                    {
                        PluginTags.Add(tag);
                    }
                }

                // Add missing tags
                if (PluginTags.Count < 13)
                {
                    if (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon0to5")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SS] {resources.GetString("LOCCommon0to5")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon1to5")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SS] {resources.GetString("LOCCommon1to5")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon5to10")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SS] {resources.GetString("LOCCommon5to10")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon10to20")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SS] {resources.GetString("LOCCommon10to20")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon20to30")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SS] {resources.GetString("LOCCommon20to30")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon30to40")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SS] {resources.GetString("LOCCommon30to40")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon40to50")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SS] {resources.GetString("LOCCommon40to50")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon50to60")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SS] {resources.GetString("LOCCommon50to60")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon60to70")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SS] {resources.GetString("LOCCommon60to70")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon70to80")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SS] {resources.GetString("LOCCommon70to80")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon80to90")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SS] {resources.GetString("LOCCommon80to90")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon90to100")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SS] {resources.GetString("LOCCommon90to100")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon100plus")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SS] {resources.GetString("LOCCommon100plus")}" });
                    }

                    foreach (Tag tag in PlayniteApi.Database.Tags)
                    {
                        if (tag.Name.IndexOf("[SS] ") > -1 && tag.Name.IndexOf("<!LOC") == -1)
                        {
                            PluginTags.Add(tag);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        public override void AddTag(Game game, bool noUpdate = false)
        {
            GetPluginTags();
            GameAchievements gameAchievements = Get(game, true);

            if (gameAchievements.HaveAchivements)
            {
                try
                {
                    if (gameAchievements.EstimateTime == null)
                    {
                        return;
                    }

                    Guid? TagId = FindGoodPluginTags(gameAchievements.EstimateTime.EstimateTimeMax);

                    if (TagId != null)
                    {
                        if (game.TagIds != null)
                        {
                            game.TagIds.Add((Guid)TagId);
                        }
                        else
                        {
                            game.TagIds = new List<Guid> { (Guid)TagId };
                        }

                        if (!noUpdate)
                        {
                            Application.Current.Dispatcher?.Invoke(() =>
                            {
                                PlayniteApi.Database.Games.Update(game);
                                game.OnPropertyChanged();
                            }, DispatcherPriority.Send);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, true);
                    logger.Error($"Tag insert error with {game.Name}");
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        $"{PluginName}-Tag-Errors",
                        $"{PluginName}\r\n" + resources.GetString("LOCCommonNotificationTagError"),
                        NotificationType.Error
                    ));
                }
            }
        }

        private Guid? FindGoodPluginTags(int EstimateTimeMax)
        {
            // Add tag
            if (EstimateTimeMax != 0)
            {
                if (EstimateTimeMax <= 1)
                {
                    return (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCPLaytimeLessThenAnHour")}")).Id;
                }
                if (EstimateTimeMax <= 6)
                {
                    return (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon1to5")}")).Id;
                }
                if (EstimateTimeMax <= 10)
                {
                    return (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon5to10")}")).Id;
                }
                if (EstimateTimeMax <= 20)
                {
                    return (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon10to20")}")).Id;
                }
                if (EstimateTimeMax <= 30)
                {
                    return (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon20to30")}")).Id;
                }
                if (EstimateTimeMax <= 40)
                {
                    return (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon30to40")}")).Id;
                }
                if (EstimateTimeMax <= 50)
                {
                    return (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon40to50")}")).Id;
                }
                if (EstimateTimeMax <= 60)
                {
                    return (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon50to60")}")).Id;
                }
                if (EstimateTimeMax <= 70)
                {
                    return (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon60to70")}")).Id;
                }
                if (EstimateTimeMax <= 80)
                {
                    return (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon70to80")}")).Id;
                }
                if (EstimateTimeMax <= 90)
                {
                    return (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon80to90")}")).Id;
                }
                if (EstimateTimeMax <= 100)
                {
                    return (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon90to100")}")).Id;
                }
                if (EstimateTimeMax > 100)
                {
                    return (PluginTags.Find(x => x.Name == $"[SS] {resources.GetString("LOCCommon100plus")}")).Id;
                }
            }

            return null;
        }
        #endregion


        public void SetIgnored(GameAchievements gameAchievements)
        {
            if (!gameAchievements.IsIgnored)
            {
                Remove(gameAchievements.Id);
                var pluginData = Get(gameAchievements.Id, true);
                pluginData.IsIgnored = true;
                AddOrUpdate(pluginData);
            }
            else
            {
                gameAchievements.IsIgnored = false;
                AddOrUpdate(gameAchievements);
                Refresh(gameAchievements.Id);
            }
        }


        public override void GetSelectData()
        {
            var View = new OptionsDownloadData(PlayniteApi);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, PluginName + " - " + resources.GetString("LOCCommonSelectData"), View);
            windowExtension.ShowDialog();

            var PlayniteDb = View.GetFilteredGames();
            bool OnlyMissing = View.GetOnlyMissing();

            if (PlayniteDb == null)
            {
                return;
            }


            if (OnlyMissing)
            {
                PlayniteDb = PlayniteDb.FindAll(x => !Get(x.Id, true).HasData);
            }
            // Without manual
            else
            {
                PlayniteDb = PlayniteDb.FindAll(x => !Get(x.Id, true).IsManual);
            }

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonGettingData")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    activateGlobalProgress.ProgressMaxValue = (double)PlayniteDb.Count();

                    string CancelText = string.Empty;

                    foreach (Game game in PlayniteDb)
                    {
                        if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                        {
                            CancelText = " canceled";
                            break;
                        }

                        Thread.Sleep(10);

                        try
                        {
                            Get(game, false, true);
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false);
                        }

                        activateGlobalProgress.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    logger.Info($"Task GetSelectData(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }, globalProgressOptions);
        }


        public ProgressionAchievements Progession()
        {
            ProgressionAchievements Result = new ProgressionAchievements();
            int Total = 0;
            int Locked = 0;
            int Unlocked = 0;

            try
            {
                var db = Database.Items.Where(x => x.Value.HaveAchivements).ToList();
                foreach (var item in db)
                {
                    var GameAchievements = item.Value;

                    if (PlayniteApi.Database.Games.Get(item.Key) != null)
                    {
                        Total += GameAchievements.Total;
                        Locked += GameAchievements.Locked;
                        Unlocked += GameAchievements.Unlocked;
                    }
                    else
                    {
                        logger.Warn($"Achievements data without game for {GameAchievements.Name} & {GameAchievements.Id.ToString()}");
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
            int Total = 1;
            int Locked = 0;
            int Unlocked = 0;

            try
            {
                var db = Database.Items.Where(x => x.Value.Playtime > 0 && x.Value.HaveAchivements).ToList();
                foreach (var item in db)
                {
                    var GameAchievements = item.Value;

                    if (PlayniteApi.Database.Games.Get(item.Key) != null)
                    {
                        Total += GameAchievements.Total;
                        Locked += GameAchievements.Locked;
                        Unlocked += GameAchievements.Unlocked;
                    }
                    else
                    {
                        logger.Warn($"Achievements data without game for {GameAchievements.Name} & {GameAchievements.Id.ToString()}");
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

        public ProgressionAchievements ProgessionSource(Guid GameSourceId)
        {
            ProgressionAchievements Result = new ProgressionAchievements();
            int Total = 0;
            int Locked = 0;
            int Unlocked = 0;

            try
            {
                var db = Database.Items.Where(x => x.Value.SourceId == GameSourceId).ToList();
                foreach (var item in db)
                {
                    Guid Id = item.Key;
                    Game Game = PlayniteApi.Database.Games.Get(Id);
                    var GameAchievements = item.Value;

                    if (PlayniteApi.Database.Games.Get(item.Key) != null)
                    {
                        Total += GameAchievements.Total;
                        Locked += GameAchievements.Locked;
                        Unlocked += GameAchievements.Unlocked;
                    }
                    else
                    {
                        logger.Warn($"Achievements data without game for {GameAchievements.Name} & {GameAchievements.Id.ToString()}");
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
