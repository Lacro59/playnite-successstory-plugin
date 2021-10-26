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
using System.Threading.Tasks;
using static SuccessStory.Clients.TrueAchievements;
using System.Windows.Threading;
using System.Windows;
using System.Threading;
using SuccessStory.Views;
using CommonPluginsShared.Converters;
using CommonPluginsControls.Controls;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static CommonPluginsShared.PlayniteTools;

namespace SuccessStory.Services
{
    public class SuccessStoryDatabase : PluginDatabaseObject<SuccessStorySettingsViewModel, SuccessStoryCollection, GameAchievements>
    {
        public SuccessStory Plugin;

        private bool _isRetroachievements { get; set; }

        private static Dictionary<AchievementSource, GenericAchievements> _achievementProviders;
        private static readonly object _achievementProvidersLock = new object();
        internal static Dictionary<AchievementSource, GenericAchievements> AchievementProviders
        {
            get
            {
                lock (_achievementProvidersLock)
                {
                    if (_achievementProviders == null)
                    {
                        _achievementProviders = new Dictionary<AchievementSource, GenericAchievements> {
                            { AchievementSource.GOG, new GogAchievements() },
                            { AchievementSource.Epic, new EpicAchievements() },
                            { AchievementSource.Origin, new OriginAchievements() },
                            { AchievementSource.Overwatch, new OverwatchAchievements() },
                            { AchievementSource.Playstation, new PSNAchievements() },
                            { AchievementSource.RetroAchievements, new RetroAchievements() },
                            { AchievementSource.RPCS3, new Rpcs3Achievements() },
                            { AchievementSource.Starcraft2, new Starcraft2Achievements() },
                            { AchievementSource.Steam, new SteamAchievements() },
                            { AchievementSource.Xbox, new XboxAchievements() },
                            { AchievementSource.Local, SteamAchievements.GetLocalSteamAchievementsProvider() }
                        };
                    }
                }
                return _achievementProviders;
            }
        }

        public SuccessStoryDatabase(IPlayniteAPI PlayniteApi, SuccessStorySettingsViewModel PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, "SuccessStory", PluginUserDataPath)
        {
            TagBefore = "[SS]";
        }


        public void InitializeClient(SuccessStory Plugin)
        {
            this.Plugin = Plugin;

            //Task.Run(() =>
            //{
            //    // Wait extension database are loaded
            //    System.Threading.SpinWait.SpinUntil(() => PlayniteApi.Database.IsOpen, -1);
            //
            //    foreach (var achievementProvider in AchievementProviders.Values)
            //    {
            //        if (achievementProvider.EnabledInSettings())
            //        {
            //            achievementProvider.ValidateConfiguration();
            //        }
            //    }
            //});
        }


        protected override bool LoadDatabase()
        {
            Database = new SuccessStoryCollection(Paths.PluginDatabasePath);
            Database.SetGameInfo<Achievements>(PlayniteApi);

            DeleteDataWithDeletedGame();

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
                if (game != null)
                {
                    gameAchievements = GetDefault(game);
                    Add(gameAchievements);
                }
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

            var achievementSource = GetAchievementSource(PluginSettings.Settings, game);

            // Generate database only this source
            if (VerifToAddOrShow(Plugin, PlayniteApi, PluginSettings.Settings, game))
            {
                var achievementProvider = AchievementProviders[achievementSource];
                var retroAchievementsProvider = achievementProvider as RetroAchievements;
                var psnAchievementsProvider = achievementProvider as PSNAchievements;

                if (retroAchievementsProvider != null && !SuccessStory.IsFromMenu)
                {
                    // use a chached RetroAchievements game ID to skip retrieving that if possible
                    // TODO: store this with the game somehow so we don't need to get this from the achievements object
                    GameAchievements TEMPgameAchievements = Get(game, true);
                    ((RetroAchievements)achievementProvider).GameId = TEMPgameAchievements.RAgameID;
                }
                else if (retroAchievementsProvider != null)
                {
                    ((RetroAchievements)achievementProvider).GameId = 0;
                }


                if (psnAchievementsProvider != null && !SuccessStory.IsFromMenu)
                {
                    GameAchievements TEMPgameAchievements = Get(game, true);
                    ((PSNAchievements)achievementProvider).CommunicationId = TEMPgameAchievements.CommunicationId;
                }
                else if (psnAchievementsProvider != null)
                {
                    ((PSNAchievements)achievementProvider).CommunicationId = null;
                }


                gameAchievements = achievementProvider.GetAchievements(game);

                if (retroAchievementsProvider != null)
                {
                    gameAchievements.RAgameID = retroAchievementsProvider.GameId;
                }

                Common.LogDebug(true, $"Achievements for {game.Name} - {achievementSource} - {Serialization.ToJson(gameAchievements)}");
            }
            else
            {
                Common.LogDebug(true, $"VerifToAddOrShow({game.Name}, {achievementSource}) - KO");
            }


            SetEstimateTimeToUnlock(game, gameAchievements);


            return gameAchievements;
        }


        private GameAchievements SetEstimateTimeToUnlock(Game game, GameAchievements gameAchievements)
        {
            if (game != null && gameAchievements.HasAchivements)
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
                    var db = Database.Items.Where(x => x.Value.HasAchivements && !x.Value.IsDeleted).ToList();
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
                //TODO: _isRetroachievements this is never set
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
                    if (PluginSettings.Settings.EnableEpic)
                    {
                        tempSourcesLabels.Add("Epic");
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
                    if (PluginSettings.Settings.EnableSc2Achievements || PluginSettings.Settings.EnableOverwatchAchievements)
                    {
                        tempSourcesLabels.Add("Battle.net");
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
                if (PluginSettings.Settings.EnableEpic)
                {
                    tempSourcesLabels.Add("Epic");
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
                if (PluginSettings.Settings.EnableSc2Achievements || PluginSettings.Settings.EnableOverwatchAchievements)
                {
                    tempSourcesLabels.Add("Battle.net");
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
                            if (Source != default(Guid))
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


            db = Database.Items.Where(x => x.Value.HasAchivements && !x.Value.IsDeleted).ToList();
            foreach (var item in db)
            {
                try
                {
                    string SourceName = PlayniteTools.GetSourceName(item.Key);

                    foreach (Achievements achievements in item.Value.Items)
                    {
                        for (int i = 0; i < tempDataUnlocked.Count; i++)
                        {
                            if (tempDataUnlocked[i].source.Contains(SourceName, StringComparison.InvariantCultureIgnoreCase))
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
        public AchievementsGraphicsDataCount GetCountByDay(Guid? GameID = null, int limit = 11, bool CutPeriod = false)
        {
            string[] GraphicsAchievementsLabels = new string[limit + 1];
            ChartValues<CustomerForSingle> SourceAchievementsSeries = new ChartValues<CustomerForSingle>();

            LocalDateConverter localDateConverter = new LocalDateConverter();

            // All achievements
            if (GameID == null)
            {
                for (int i = limit; i >= 0; i--)
                {
                    GraphicsAchievementsLabels[(limit - i)] = (string)localDateConverter.Convert(DateTime.Now.AddDays(-i), null, null, null);
                    SourceAchievementsSeries.Add(new CustomerForSingle
                    {
                        Name = (string)localDateConverter.Convert(DateTime.Now.AddDays(-i), null, null, null),
                        Values = 0
                    });
                }

                try
                {
                    var db = Database.Items.Where(x => x.Value.HasAchivements && !x.Value.IsDeleted).ToList();
                    foreach (var item in db)
                    {
                        List<Achievements> temp = item.Value.Items;
                        foreach (Achievements itemAchievements in temp)
                        {
                            if (itemAchievements.DateWhenUnlocked != null)
                            {
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
                        DateTime TempDateTime = Achievements.Where(x => x.IsUnlock).Select(x => x.DateWhenUnlocked).Max()?.ToLocalTime() ?? DateTime.Now;

                        for (int i = limit; i >= 0; i--)
                        {
                            GraphicsAchievementsLabels[(limit - i)] = (string)localDateConverter.Convert(TempDateTime.AddDays(-i), null, null, null);

                            double DataValue = CutPeriod ? double.NaN : 0;

                            SourceAchievementsSeries.Add(new CustomerForSingle
                            {
                                Name = (string)localDateConverter.Convert(TempDateTime.AddDays(-i), null, null, null),
                                Values = DataValue
                            });
                        }

                        for (int i = 0; i < Achievements.Count; i++)
                        {
                            if (Achievements[i].DateWhenUnlocked != null)
                            {
                                string tempDate = (string)localDateConverter.Convert(((DateTime)Achievements[i].DateUnlocked).ToLocalTime(), null, null, null);
                                int index = Array.IndexOf(GraphicsAchievementsLabels, tempDate);

                                if (index >= 0 && index < (limit + 1))
                                {
                                    if (double.IsNaN(SourceAchievementsSeries[index].Values))
                                    {
                                        SourceAchievementsSeries[index].Values = 0;
                                    }
                                    SourceAchievementsSeries[index].Values += 1;
                                }
                            }
                        }

                        if (CutPeriod)
                        {
                            bool PrevIsNaN = false;

                            for (int i = (SourceAchievementsSeries.Count - 1); i > -1; i--)
                            {
                                if (double.IsNaN(SourceAchievementsSeries[i].Values))
                                {
                                    if (PrevIsNaN)
                                    {
                                        SourceAchievementsSeries.RemoveAt(i);
                                        var temp = GraphicsAchievementsLabels.ToList();
                                        temp.RemoveAt(i);
                                        GraphicsAchievementsLabels = temp.ToArray();
                                    }
                                    else
                                    {
                                        GraphicsAchievementsLabels[i] = string.Empty;
                                        PrevIsNaN = true;
                                    }
                                }
                                else
                                {
                                    PrevIsNaN = false;
                                }
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

        public enum AchievementSource
        {
            None,
            Local,
            Playstation,
            Steam,
            GOG,
            Epic,
            Origin,
            Xbox,
            RetroAchievements,
            RPCS3,
            Overwatch,
            Starcraft2
        }

        private static AchievementSource GetAchievementSourceFromLibraryPlugin(SuccessStorySettings settings, Game game)
        {
            ExternalPlugin pluginType = PlayniteTools.GetPluginType(game.PluginId);
            if (pluginType == ExternalPlugin.None)
            {
                if (game.Source?.Name?.Contains("Xbox Game Pass", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    return AchievementSource.Xbox;
                }
                if (game.Source?.Name?.Contains("Microsoft Store", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    return AchievementSource.Xbox;
                }

                return AchievementSource.None;
            }

            switch (pluginType)
            {
                case ExternalPlugin.BattleNetLibrary:
                    switch (game.Name.ToLowerInvariant())
                    {
                        case "overwatch":
                            if (settings.EnableOverwatchAchievements)
                            {
                                return AchievementSource.Overwatch;
                            }
                            break;
                        case "starcraft 2":
                        case "starcraft ii":
                            if (settings.EnableSc2Achievements)
                            {
                                return AchievementSource.Starcraft2;
                            }
                            break;
                    }
                    break;
                case ExternalPlugin.GogLibrary:
                    if (settings.EnableGog)
                    {
                        return AchievementSource.GOG;
                    }
                    break;
                case ExternalPlugin.EpicLibrary:
                    if (settings.EnableEpic)
                    {
                        return AchievementSource.Epic;
                    }
                    break;
                case ExternalPlugin.OriginLibrary:
                    if (settings.EnableOrigin)
                    {
                        return AchievementSource.Origin;
                    }
                    break;
                case ExternalPlugin.PSNLibrary:
                    if (settings.EnablePsn)
                    {
                        return AchievementSource.Playstation;
                    }
                    break;
                case ExternalPlugin.SteamLibrary:
                    if (settings.EnableSteam)
                    {
                        return AchievementSource.Steam;
                    }
                    break;
                case ExternalPlugin.XboxLibrary:
                    if (settings.EnableXbox)
                    {
                        return AchievementSource.Xbox;
                    }
                    break;
            }
            return AchievementSource.None;
        }

        private static AchievementSource GetAchievementSourceFromEmulator(SuccessStorySettings settings, Game game)
        {
            if (game.GameActions == null)
            {
                return AchievementSource.None;
            }

            foreach (var action in game.GameActions)
            {
                if (!action.IsPlayAction || action.EmulatorId == Guid.Empty)
                {
                    continue;
                }

                var emulator = API.Instance.Database.Emulators.FirstOrDefault(e => e.Id == action.EmulatorId);
                if (emulator == null)
                {
                    continue;
                }

                if (PlayniteTools.GameUseRpcs3(game) && settings.EnableRpcs3Achievements)
                {
                    return AchievementSource.RPCS3;
                }

                // TODO With the emulator migration problem emulator.BuiltInConfigId is null
                // TODO emulator.BuiltInConfigId = "retroarch" is limited; other emulators has RA
                if (settings.EnableRetroAchievements)
                {
                    return AchievementSource.RetroAchievements;
                }
            }

            return AchievementSource.None;
        }

        public static AchievementSource GetAchievementSource(SuccessStorySettings settings, Game game)
        {
            var source = GetAchievementSourceFromLibraryPlugin(settings, game);
            if (source != AchievementSource.None)
            {
                return source;
            }

            source = GetAchievementSourceFromEmulator(settings, game);
            if (source != AchievementSource.None)
            {
                return source;
            }

            //any game can still get local achievements when that's enabled
            return settings.EnableLocal ? AchievementSource.Local : AchievementSource.None;
        }

        /// <summary>
        /// Validate achievement configuration for the service this game is linked to
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="playniteApi"></param>
        /// <param name="settings"></param>
        /// <param name="game"></param>
        /// <returns>true when achievements can be retrieved for the supplied game</returns>
        public static bool VerifToAddOrShow(SuccessStory plugin, IPlayniteAPI playniteApi, SuccessStorySettings settings, Game game)
        {
            var achievementSource = GetAchievementSource(settings, game);
            if (!AchievementProviders.TryGetValue(achievementSource, out var achievementProvider))
            {
                return false;
            }

            if (achievementProvider.EnabledInSettings())
            {
                return achievementProvider.ValidateConfiguration();
            }

            Common.LogDebug(true, $"VerifToAddOrShow() find no action for {achievementSource}");
            return false;
        }
        public bool VerifAchievementsLoad(Guid gameID)
        {
            return GetOnlyCache(gameID) != null;
        }


        public override void SetThemesResources(Game game)
        {
            if (game == null)
            {
                logger.Warn("game null in SetThemesResources()");
                return;
            }

            GameAchievements gameAchievements = Get(game, true);

            if (gameAchievements == null || !gameAchievements.HasData)
            {
                PluginSettings.Settings.HasData = false;

                PluginSettings.Settings.Is100Percent = false;
                PluginSettings.Settings.Unlocked = 0;
                PluginSettings.Settings.Locked = 0;
                PluginSettings.Settings.Total = 0;
                PluginSettings.Settings.Percent = 0;
                PluginSettings.Settings.EstimateTimeToUnlock = string.Empty;
                PluginSettings.Settings.ListAchievements = new List<Achievements>();

                return;
            }

            PluginSettings.Settings.HasData = gameAchievements.HasData;

            PluginSettings.Settings.Is100Percent = gameAchievements.Is100Percent;
            PluginSettings.Settings.Unlocked = gameAchievements.Unlocked;
            PluginSettings.Settings.Locked = gameAchievements.Locked;
            PluginSettings.Settings.Total = gameAchievements.Total;
            PluginSettings.Settings.Percent = gameAchievements.Progression;
            PluginSettings.Settings.EstimateTimeToUnlock = gameAchievements.EstimateTime?.EstimateTime;
            PluginSettings.Settings.ListAchievements = gameAchievements.Items;
        }

        public override void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            foreach (var GameUpdated in e.UpdatedItems)
            {
                Database.SetGameInfo<Achievements>(PlayniteApi, GameUpdated.NewData.Id);
            }
        }


        public override void Refresh(List<Guid> Ids)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonProcessing")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                activateGlobalProgress.ProgressMaxValue = Ids.Count;

                string CancelText = string.Empty;

                foreach (Guid Id in Ids)
                {
                    if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                    {
                        CancelText = " canceled";
                        break;
                    }

                    Game game = PlayniteApi.Database.Games.Get(Id);
                    string SourceName = PlayniteTools.GetSourceName(game);
                    var achievementSource = GetAchievementSource(PluginSettings.Settings, game);
                    string GameName = game.Name;
                    bool VerifToAddOrShow = SuccessStoryDatabase.VerifToAddOrShow(Plugin, PlayniteApi, PluginSettings.Settings, game);
                    GameAchievements gameAchievements = Get(game, true);

                    if (!gameAchievements.IsIgnored && VerifToAddOrShow)
                    {
                        if (VerifToAddOrShow)
                        {
                            if (!gameAchievements.IsManual)
                            {
                                RefreshNoLoader(Id);
                            }
                        }
                    }

                    activateGlobalProgress.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Info($"Task Refresh(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{Ids.Count} items");
            }, globalProgressOptions);
        }


        public void RefreshRarety()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonProcessing")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                var db = Database.Where(x => x.IsManual && x.HasAchivements);
                activateGlobalProgress.ProgressMaxValue = (double)db.Count();
                string CancelText = string.Empty;

                var exophaseAchievements = new ExophaseAchievements();
                var steamAchievements = new SteamAchievements();
                bool SteamConfig = steamAchievements.IsConfigured();

                foreach (GameAchievements gameAchievements in db)
                {
                    logger.Info($"RefreshRarety({gameAchievements.Name})");

                    if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                    {
                        CancelText = " canceled";
                        break;
                    }

                    string SourceName = gameAchievements.SourcesLink?.Name?.ToLower();
                    switch (SourceName)
                    {
                        case "steam":
                            int.TryParse(Regex.Match(gameAchievements.SourcesLink.Url, @"\d+").Value, out int AppId);
                            if (AppId != 0)
                            {
                                if (SteamConfig)
                                {
                                    gameAchievements.Items = steamAchievements.GetGlobalAchievementPercentagesForApp(AppId, gameAchievements.Items);
                                }
                                else
                                {
                                    logger.Warn($"No Steam config");
                                }
                            }
                            break;
                        case "exophase":
                            exophaseAchievements.SetRarety(gameAchievements, AchievementSource.Local);
                            break;
                        default:
                            logger.Warn($"No sourcesLink for {gameAchievements.Name}");
                            break;
                    }

                    AddOrUpdate(gameAchievements);
                    activateGlobalProgress.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Info($"Task RefreshRarety(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)db.Count()} items");
            }, globalProgressOptions);
        }

        public void RefreshEstimateTime()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonProcessing")}",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                var db = Database.Where(x => x.IsManual && x.HasAchivements);
                activateGlobalProgress.ProgressMaxValue = (double)db.Count();
                string CancelText = string.Empty;

                var exophaseAchievements = new ExophaseAchievements();
                var steamAchievements = new SteamAchievements();
                bool SteamConfig = steamAchievements.IsConfigured();

                foreach (GameAchievements gameAchievements in db)
                {
                    logger.Info($"RefreshEstimateTime({gameAchievements.Name})");

                    if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                    {
                        CancelText = " canceled";
                        break;
                    }

                    Game game = PlayniteApi.Database.Games.Get(gameAchievements.Id);
                    SetEstimateTimeToUnlock(game, gameAchievements);
                    AddOrUpdate(gameAchievements);
                    activateGlobalProgress.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Info($"Task RefreshEstimateTime(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)db.Count()} items");
            }, globalProgressOptions);
        }


        #region Tag system
        public override void AddTag(Game game, bool noUpdate = false)
        {
            GetPluginTags();
            GameAchievements gameAchievements = Get(game, true);

            if (gameAchievements.HasAchivements)
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
                    return CheckTagExist($"{resources.GetString("LOCCommon0to1")}");
                }
                if (EstimateTimeMax <= 6)
                {
                    return CheckTagExist($"{resources.GetString("LOCCommon1to5")}");
                }
                if (EstimateTimeMax <= 10)
                {
                    return CheckTagExist($"{resources.GetString("LOCCommon5to10")}");
                }
                if (EstimateTimeMax <= 20)
                {
                    return CheckTagExist($"{resources.GetString("LOCCommon10to20")}");
                }
                if (EstimateTimeMax <= 30)
                {
                    return CheckTagExist($"{resources.GetString("LOCCommon20to30")}");
                }
                if (EstimateTimeMax <= 40)
                {
                    return CheckTagExist($"{resources.GetString("LOCCommon30to40")}");
                }
                if (EstimateTimeMax <= 50)
                {
                    return CheckTagExist($"{resources.GetString("LOCCommon40to50")}");
                }
                if (EstimateTimeMax <= 60)
                {
                    return CheckTagExist($"{resources.GetString("LOCCommon50to60")}");
                }
                if (EstimateTimeMax <= 70)
                {
                    return CheckTagExist($"{resources.GetString("LOCCommon60to70")}");
                }
                if (EstimateTimeMax <= 80)
                {
                    return CheckTagExist($"{resources.GetString("LOCCommon70to80")}");
                }
                if (EstimateTimeMax <= 90)
                {
                    return CheckTagExist($"{resources.GetString("LOCCommon80to90")}");
                }
                if (EstimateTimeMax <= 100)
                {
                    return CheckTagExist($"{resources.GetString("LOCCommon90to100")}");
                }
                if (EstimateTimeMax > 100)
                {
                    return CheckTagExist($"{resources.GetString("LOCCommon100plus")}");
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
                var db = Database.Items.Where(x => x.Value.HasAchivements).ToList();
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
                var db = Database.Items.Where(x => x.Value.Playtime > 0 && x.Value.HasAchivements).ToList();
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
