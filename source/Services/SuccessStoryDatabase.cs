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
using CommonPluginsShared.Extensions;
using System.Threading.Tasks;
using System.Reflection;

namespace SuccessStory.Services
{
    public class SuccessStoryDatabase : PluginDatabaseObject<SuccessStorySettingsViewModel, SuccessStoryCollection, GameAchievements, Achievements>
    {
        public SuccessStory Plugin { get; set; }

        private bool IsRetroachievements { get; set; }

        private static object AchievementProvidersLock => new object();
        private static Dictionary<AchievementSource, GenericAchievements> achievementProviders;
        internal static Dictionary<AchievementSource, GenericAchievements> AchievementProviders
        {
            get
            {
                lock (AchievementProvidersLock)
                {
                    if (achievementProviders == null)
                    {
                        achievementProviders = new Dictionary<AchievementSource, GenericAchievements> {
                            { AchievementSource.GOG, new GogAchievements() },
                            { AchievementSource.Epic, new EpicAchievements() },
                            { AchievementSource.Origin, new OriginAchievements() },
                            { AchievementSource.Overwatch, new OverwatchAchievements() },
                            { AchievementSource.Wow, new WowAchievements() },
                            { AchievementSource.Playstation, new PSNAchievements() },
                            { AchievementSource.RetroAchievements, new RetroAchievements() },
                            { AchievementSource.RPCS3, new Rpcs3Achievements() },
                            { AchievementSource.Starcraft2, new Starcraft2Achievements() },
                            { AchievementSource.Steam, new SteamAchievements() },
                            { AchievementSource.Xbox, new XboxAchievements() },
                            { AchievementSource.GenshinImpact, new GenshinImpactAchievements() },
                            { AchievementSource.GuildWars2, new GuildWars2Achievements() },
                            { AchievementSource.Local, SteamAchievements.GetLocalSteamAchievementsProvider() }
                        };
                    }
                }
                return achievementProviders;
            }
        }

        public SuccessStoryDatabase(SuccessStorySettingsViewModel PluginSettings, string PluginUserDataPath) : base(PluginSettings, "SuccessStory", PluginUserDataPath)
        {
            TagBefore = "[SS]";
        }


        public void InitializeClient(SuccessStory plugin)
        {
            Plugin = plugin;
        }


        protected override bool LoadDatabase()
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                Database = new SuccessStoryCollection(Paths.PluginDatabasePath);
                Database.SetGameInfo<Achievements>();

                DeleteDataWithDeletedGame();

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"LoadDatabase with {Database.Count} items - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return false;
            }

            return true;
        }


        public void GetManual(Game game)
        {
            try
            {
                GameAchievements gameAchievements = GetDefault(game);

                SuccessStoreGameSelection ViewExtension = new SuccessStoreGameSelection(game);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCSuccessStory"), ViewExtension);
                _ = windowExtension.ShowDialog();

                if (ViewExtension.GameAchievements != null)
                {
                    gameAchievements = ViewExtension.GameAchievements;
                    gameAchievements.IsManual = true;
                }

                gameAchievements = SetEstimateTimeToUnlock(game, gameAchievements);
                AddOrUpdate(gameAchievements);

                Common.LogDebug(true, $"GetManual({game.Id}) - gameAchievements: {Serialization.ToJson(gameAchievements)}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }
        }

        public GameAchievements RefreshManual(Game game)
        {
            Logger.Info($"RefreshManual({game?.Name} - {game?.Id} - {game?.Source?.Name})");
            GameAchievements gameAchievements = null;

            try
            {
                gameAchievements = Get(game, true);
                if (gameAchievements != null && gameAchievements.HasData)
                {
                    if (gameAchievements.SourcesLink?.Name.IsEqual("steam") ?? false)
                    {
                        string str = gameAchievements.SourcesLink?.Url.Replace("https://steamcommunity.com/stats/", string.Empty).Replace("/achievements", string.Empty);
                        if (uint.TryParse(str, out uint AppId))
                        {
                            SteamAchievements steamAchievements = new SteamAchievements();
                            steamAchievements.SetLocal();
                            steamAchievements.SetManual();
                            gameAchievements = steamAchievements.GetAchievements(game, AppId);
                        }
                    }
                    else if (gameAchievements.SourcesLink?.Name.IsEqual("exophase") ?? false)
                    {
                        SearchResult searchResult = new SearchResult
                        {
                            Name = gameAchievements.SourcesLink?.GameName,
                            Url = gameAchievements.SourcesLink?.Url
                        };

                        ExophaseAchievements exophaseAchievements = new ExophaseAchievements();
                        gameAchievements = exophaseAchievements.GetAchievements(game, searchResult);
                    }

                    Common.LogDebug(true, $"RefreshManual({game.Id}) - gameAchievements: {Serialization.ToJson(gameAchievements)}");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return gameAchievements;
        }


        public void GetGenshinImpact(Game game)
        {
            try
            {
                GenshinImpactAchievements genshinImpactAchievements = new GenshinImpactAchievements();
                GameAchievements gameAchievements = genshinImpactAchievements.GetAchievements(game);
                AddOrUpdate(gameAchievements);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }
        }

        public GameAchievements RefreshGenshinImpact(Game game)
        {
            Logger.Info($"RefreshGenshinImpact({game?.Name} - {game?.Id})");
            GameAchievements gameAchievements = null;

            try
            {
                GenshinImpactAchievements genshinImpactAchievements = new GenshinImpactAchievements();
                gameAchievements = genshinImpactAchievements.GetAchievements(game);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return gameAchievements;
        }



        public override GameAchievements Get(Guid Id, bool OnlyCache = false, bool Force = false)
        {
            GameAchievements gameAchievements = base.GetOnlyCache(Id);
            Game game = API.Instance.Database.Games.Get(Id);

            // Get from web
            if ((gameAchievements == null && !OnlyCache) || Force)
            {
                gameAchievements = GetWeb(Id);
                AddOrUpdate(gameAchievements);
            }
            else if (gameAchievements == null && game != null)
            {
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
            Game game = API.Instance.Database.Games.Get(Id);
            GameAchievements gameAchievements = GetDefault(game);
            AchievementSource achievementSource = GetAchievementSource(PluginSettings.Settings, game);

            if (achievementSource == AchievementSource.None)
            {
                Logger.Warn($"No provider find for {game.Name} - {achievementSource} - {game.Source?.Name} - {game?.Platforms?.FirstOrDefault()?.Name}");
            }

            // Generate database only this source
            if (VerifToAddOrShow(PluginSettings.Settings, game))
            {
                GenericAchievements achievementProvider = AchievementProviders[achievementSource];
                RetroAchievements retroAchievementsProvider = achievementProvider as RetroAchievements;
                PSNAchievements psnAchievementsProvider = achievementProvider as PSNAchievements;

                Logger.Info($"Used {achievementProvider} for {game.Name} - {achievementSource} - {game.Source?.Name} - {game?.Platforms?.FirstOrDefault()?.Name}");

                GameAchievements TEMPgameAchievements = Get(game, true);

                if (retroAchievementsProvider != null && (!SuccessStory.IsFromMenu || TEMPgameAchievements.RAgameID != 0))
                {
                    ((RetroAchievements)achievementProvider).GameId = TEMPgameAchievements.RAgameID;
                }
                else if (retroAchievementsProvider != null)
                {
                    ((RetroAchievements)achievementProvider).GameId = 0;
                }


                if (psnAchievementsProvider != null && (!SuccessStory.IsFromMenu || !TEMPgameAchievements.CommunicationId.IsNullOrEmpty()))
                {
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

                if (!(gameAchievements?.HasAchievements ?? false))
                {
                    Logger.Info($"No achievements find for {game.Name} - {achievementSource} - {game.Source?.Name} - {game?.Platforms?.FirstOrDefault()?.Name}");
                }
                else
                {
                    gameAchievements = SetEstimateTimeToUnlock(game, gameAchievements);
                    Logger.Info($"{gameAchievements.Unlocked}/{gameAchievements.Total} achievements find for {game.Name} - {achievementSource} - {game.Source?.Name} - {game?.Platforms?.FirstOrDefault()?.Name}");
                }

                Common.LogDebug(true, $"Achievements for {game.Name} - {achievementSource} - {Serialization.ToJson(gameAchievements)}");
            }

            return gameAchievements;
        }


        private GameAchievements SetEstimateTimeToUnlock(Game game, GameAchievements gameAchievements)
        {
            if (game != null && (gameAchievements?.HasAchievements ?? false))
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
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
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
                    bool ShowHidden = PluginSettings.Settings.IncludeHiddenGames;
                    List<KeyValuePair<Guid, GameAchievements>> db = Database.Items.Where(x => x.Value.HasAchievements && !x.Value.IsDeleted && (ShowHidden ? true : x.Value.Hidden == false)).ToList();
                    foreach (KeyValuePair<Guid, GameAchievements> item in db)
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
                    Common.LogError(ex, false, true, PluginName);
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
                    Common.LogError(ex, false, $"Error in load GetCountByMonth({GameID.ToString()})", true, PluginName);
                }
            }

            return new AchievementsGraphicsDataCount { Labels = GraphicsAchievementsLabels, Series = SourceAchievementsSeries };
        }

        public AchievementsGraphicsDataCountSources GetCountBySources()
        {
            List<string> tempSourcesLabels = new List<string>();
            IEnumerable<KeyValuePair<Guid, GameAchievements>> db = Database.Items.Where(x => x.Value.IsManual);

            if (PluginSettings.Settings.EnableRetroAchievementsView && PluginSettings.Settings.EnableRetroAchievements)
            {
                //TODO: _isRetroachievements this is never set
                if (IsRetroachievements)
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
                        tempSourcesLabels.Add("EA app");
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
                    if (PluginSettings.Settings.EnableSc2Achievements || PluginSettings.Settings.EnableOverwatchAchievements || PluginSettings.Settings.EnableWowAchievements)
                    {
                        tempSourcesLabels.Add("Battle.net");
                    }
                    if (PluginSettings.Settings.EnableManual)
                    {
                        if (db != null && db.Count() > 0)
                        {
                            IEnumerable<Guid> ListSources = db.Select(x => x.Value.SourceId).Distinct();
                            foreach (Guid Source in ListSources)
                            {
                                GameSource gameSource = API.Instance.Database.Sources.Get(Source);
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
                    tempSourcesLabels.Add("EA app");
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
                if (PluginSettings.Settings.EnableSc2Achievements || PluginSettings.Settings.EnableOverwatchAchievements || PluginSettings.Settings.EnableWowAchievements)
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
                        IEnumerable<Guid> ListSources = db.Select(x => x.Value.SourceId).Distinct();
                        foreach (Guid Source in ListSources)
                        {
                            if (Source != default)
                            {
                                GameSource gameSource = API.Instance.Database.Sources.Get(Source);
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
                tempDataLocked.Add(new AchievementsGraphicsDataSources { Source = tempSourcesLabels[i], Value = 0 });
                tempDataUnlocked.Add(new AchievementsGraphicsDataSources { Source = tempSourcesLabels[i], Value = 0 });
                tempDataTotal.Add(new AchievementsGraphicsDataSources { Source = tempSourcesLabels[i], Value = 0 });
            }

            bool ShowHidden = PluginSettings.Settings.IncludeHiddenGames;
            db = Database.Items.Where(x => x.Value.HasAchievements && !x.Value.IsDeleted && (ShowHidden ? true : x.Value.Hidden == false)).ToList();
            foreach (KeyValuePair<Guid, GameAchievements> item in db)
            {
                try
                {
                    string SourceName = PlayniteTools.GetSourceName(item.Key);
                    foreach (Achievements achievements in item.Value.Items)
                    {
                        for (int i = 0; i < tempDataUnlocked.Count; i++)
                        {
                            if (tempDataUnlocked[i].Source.Contains(SourceName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                tempDataTotal[i].Value += 1;
                                if (achievements.DateUnlocked != default(DateTime))
                                {
                                    tempDataUnlocked[i].Value += 1;
                                }
                                if (achievements.DateUnlocked == default(DateTime))
                                {
                                    tempDataLocked[i].Value += 1;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on GetCountBySources() for {item.Key}", true, PluginName);
                }
            }

            ChartValues<CustomerForSingle> SourceAchievementsSeriesUnlocked = new ChartValues<CustomerForSingle>();
            ChartValues<CustomerForSingle> SourceAchievementsSeriesLocked = new ChartValues<CustomerForSingle>();
            ChartValues<CustomerForSingle> SourceAchievementsSeriesTotal = new ChartValues<CustomerForSingle>();
            for (int i = 0; i < tempDataUnlocked.Count; i++)
            {
                SourceAchievementsSeriesUnlocked.Add(new CustomerForSingle
                {
                    Name = TransformIcon.Get(tempDataUnlocked[i].Source),
                    Values = tempDataUnlocked[i].Value
                });
                SourceAchievementsSeriesLocked.Add(new CustomerForSingle
                {
                    Name = TransformIcon.Get(tempDataLocked[i].Source),
                    Values = tempDataLocked[i].Value
                });
                SourceAchievementsSeriesTotal.Add(new CustomerForSingle
                {
                    Name = TransformIcon.Get(tempDataTotal[i].Source),
                    Values = tempDataTotal[i].Value
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
                    bool ShowHidden = PluginSettings.Settings.IncludeHiddenGames;
                    List<KeyValuePair<Guid, GameAchievements>> db = Database.Items.Where(x => x.Value.HasAchievements && !x.Value.IsDeleted && (ShowHidden ? true : x.Value.Hidden == false)).ToList();
                    foreach (KeyValuePair<Guid, GameAchievements> item in db)
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
                    Common.LogError(ex, false, true, PluginName);
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
                        if (CutPeriod)
                        {
                            IOrderedEnumerable<IGrouping<DateTime, Achievements>> groupedAchievements = Achievements
                                .Where(a => a.IsUnlock && a.DateWhenUnlocked.HasValue)
                                .GroupBy(a => a.DateWhenUnlocked.Value.ToLocalTime().Date)
                                .OrderBy(g => g.Key);

                            DateTime? previousDate = null;

                            foreach (var grouping in groupedAchievements)
                            {
                                if (previousDate.HasValue && previousDate < grouping.Key.AddDays(-1))
                                {
                                    SourceAchievementsSeries.Add(new CustomerForSingle
                                    {
                                        Name = string.Empty,
                                        Values = double.NaN
                                    });
                                }
                                SourceAchievementsSeries.Add(new CustomerForSingle
                                {
                                    Name = (string)localDateConverter.Convert(grouping.Key, null, null, null),
                                    Values = grouping.Count()
                                });
                                previousDate = grouping.Key;
                            }
                            GraphicsAchievementsLabels = SourceAchievementsSeries.Select(x => x.Name).ToArray();
                        }
                        else
                        {
                            Achievements.Sort((x, y) => (x.DateUnlocked == null ? default : (DateTime)x.DateUnlocked).CompareTo(x.DateUnlocked == null ? default : (DateTime)x.DateUnlocked));
                            DateTime TempDateTime = Achievements.Where(x => x.IsUnlock).Select(x => x.DateWhenUnlocked).Max()?.ToLocalTime() ?? DateTime.Now;
                            TempDateTime = TempDateTime == default ? DateTime.Now : TempDateTime;

                            for (int i = limit; i >= 0; i--)
                            {
                                GraphicsAchievementsLabels[limit - i] = (string)localDateConverter.Convert(TempDateTime.AddDays(-i), null, null, null);

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
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error in load GetCountByDay({GameID.ToString()})", true, PluginName);
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
            Starcraft2,
            Wow,
            GenshinImpact,
            GuildWars2
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
                        case "overwatch 2":
                            if (settings.EnableOverwatchAchievements)
                            {
                                return AchievementSource.None;
                                //return AchievementSource.Overwatch;
                            }
                            break;

                        case "starcraft 2":
                        case "starcraft ii":
                            if (settings.EnableSc2Achievements)
                            {
                                return AchievementSource.None;
                                //return AchievementSource.Starcraft2;
                            }
                            break;

                        case "wow":
                        case "world of warcraft":
                            if (settings.EnableWowAchievements)
                            {
                                return AchievementSource.Wow;
                            }
                            break;

                        default:
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
                case ExternalPlugin.LegendaryLibrary:
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

                case ExternalPlugin.None:
                    break;
                case ExternalPlugin.IndiegalaLibrary:
                    break;
                case ExternalPlugin.AmazonGamesLibrary:
                    break;
                case ExternalPlugin.BethesdaLibrary:
                    break;
                case ExternalPlugin.HumbleLibrary:
                    break;
                case ExternalPlugin.ItchioLibrary:
                    break;
                case ExternalPlugin.RockstarLibrary:
                    break;
                case ExternalPlugin.TwitchLibrary:
                    break;
                case ExternalPlugin.OculusLibrary:
                    break;
                case ExternalPlugin.RiotLibrary:
                    break;
                case ExternalPlugin.UplayLibrary:
                    break;
                case ExternalPlugin.SuccessStory:
                    break;
                case ExternalPlugin.CheckDlc:
                    break;
                case ExternalPlugin.EmuLibrary:
                    break;

                default:
                    break;
            }

            return AchievementSource.None;
        }

        private static AchievementSource GetAchievementSourceFromEmulator(SuccessStorySettings settings, Game game)
        {
            AchievementSource achievementSource = AchievementSource.None;

            if (game.GameActions == null)
            {
                return achievementSource;
            }

            foreach (GameAction action in game.GameActions)
            {
                if (action.Type != GameActionType.Emulator)
                {
                    continue;
                }
                else
                {
                    achievementSource = AchievementSource.RetroAchievements;
                }

                if (PlayniteTools.GameUseRpcs3(game) && settings.EnableRpcs3Achievements)
                {
                    return AchievementSource.RPCS3;
                }

                // TODO With the emulator migration problem emulator.BuiltInConfigId is null
                // TODO emulator.BuiltInConfigId = "retroarch" is limited; other emulators has RA
                if (game.Platforms?.Count > 0)
                {
                    string PlatformName = game.Platforms.FirstOrDefault().Name;
                    Guid PlatformId = game.Platforms.FirstOrDefault().Id;
                    int consoleID = settings.RaConsoleAssociateds.Find(x => x.Platforms.Find(y => y.Id == PlatformId) != null)?.RaConsoleId ?? 0;
                    if (settings.EnableRetroAchievements && consoleID != 0)
                    {
                        return AchievementSource.RetroAchievements;
                    }
                }
                else
                {
                    Logger.Warn($"No platform for {game.Name}");
                }
            }

            return achievementSource;
        }

        public static AchievementSource GetAchievementSource(SuccessStorySettings settings, Game game, bool ignoreSpecial = false)
        {
            if (game.Name.IsEqual("Genshin Impact") && !ignoreSpecial)
            {
                return AchievementSource.GenshinImpact;
            }

            if (game.Name.IsEqual("Guild Wars 2"))
            {
                return AchievementSource.GuildWars2;
            }

            AchievementSource source = GetAchievementSourceFromLibraryPlugin(settings, game);
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
        /// <param name="settings"></param>
        /// <param name="game"></param>
        /// <returns>true when achievements can be retrieved for the supplied game</returns>
        public static bool VerifToAddOrShow(SuccessStorySettings settings, Game game)
        {
            AchievementSource achievementSource = GetAchievementSource(settings, game);
            if (!AchievementProviders.TryGetValue(achievementSource, out GenericAchievements achievementProvider))
            {
                return false;
            }

            if (achievementProvider.EnabledInSettings())
            {
                return achievementProvider.ValidateConfiguration();
            }

            Logger.Warn($"VerifToAddOrShow() find no action for {game.Name} - {achievementSource} - {game.Source?.Name} - {game?.Platforms?.FirstOrDefault()?.Name}");
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
                Logger.Warn("game null in SetThemesResources()");
                return;
            }

            GameAchievements gameAchievements = Get(game, true);

            if (gameAchievements == null || !gameAchievements.HasData)
            {
                PluginSettings.Settings.HasData = false;

                PluginSettings.Settings.Is100Percent = false;
                PluginSettings.Settings.Common = new AchRaretyStats();
                PluginSettings.Settings.NoCommon = new AchRaretyStats();
                PluginSettings.Settings.Rare = new AchRaretyStats();
                PluginSettings.Settings.UltraRare = new AchRaretyStats();
                PluginSettings.Settings.Unlocked = 0;
                PluginSettings.Settings.Locked = 0;
                PluginSettings.Settings.Total = 0;
                PluginSettings.Settings.TotalGamerScore = 0;
                PluginSettings.Settings.Percent = 0;
                PluginSettings.Settings.EstimateTimeToUnlock = string.Empty;
                PluginSettings.Settings.ListAchievements = new List<Achievements>();

                return;
            }

            PluginSettings.Settings.HasData = gameAchievements.HasData;

            PluginSettings.Settings.Is100Percent = gameAchievements.Is100Percent;
            PluginSettings.Settings.Common = gameAchievements.Common;
            PluginSettings.Settings.NoCommon = gameAchievements.NoCommon;
            PluginSettings.Settings.Rare = gameAchievements.Rare;
            PluginSettings.Settings.UltraRare = gameAchievements.UltraRare;
            PluginSettings.Settings.Unlocked = gameAchievements.Unlocked;
            PluginSettings.Settings.Locked = gameAchievements.Locked;
            PluginSettings.Settings.Total = gameAchievements.Total;
            PluginSettings.Settings.TotalGamerScore = (int)gameAchievements.TotalGamerScore;
            PluginSettings.Settings.Percent = gameAchievements.Progression;
            PluginSettings.Settings.EstimateTimeToUnlock = gameAchievements.EstimateTime?.EstimateTime;
            PluginSettings.Settings.ListAchievements = gameAchievements.Items;
        }


        public async Task RefreshData(Game game)
        {
            await Task.Run(() =>
            {
                string SourceName = GetSourceName(game);
                string GameName = game.Name;
                bool VerifToAddOrShow = SuccessStoryDatabase.VerifToAddOrShow(PluginSettings.Settings, game);
                GameAchievements gameAchievements = Get(game, true);

                if (!gameAchievements.IsIgnored && VerifToAddOrShow)
                {
                    RefreshNoLoader(game.Id);

                    // Set to Beaten
                    if (PluginSettings.Settings.CompletionStatus100Percent != null && PluginSettings.Settings.Auto100PercentCompleted)
                    {
                        gameAchievements = Get(game, true);
                        if (gameAchievements.HasAchievements && gameAchievements.Is100Percent)
                        {
                            game.CompletionStatusId = PluginSettings.Settings.CompletionStatus100Percent.Id;
                            API.Instance.Database.Games.Update(game);
                        }
                    }
                }

                // refresh themes resources
                if (game.Id == GameContext.Id)
                {
                    SetThemesResources(GameContext);
                }
            });
        }


        public override void RefreshNoLoader(Guid id)
        {
            Game game = API.Instance.Database.Games.Get(id);
            GameAchievements loadedItem = Get(id, true);
            GameAchievements webItem = null;

            if (loadedItem?.IsIgnored ?? true)
            {
                return;
            }

            Logger.Info($"RefreshNoLoader({game?.Name} - {game?.Id} - {game.Source?.Name})");

            if (loadedItem.IsManual)
            {
                webItem = game.Name.IsEqual("Genshin Impact") ? RefreshGenshinImpact(game) : RefreshManual(game);

                if (webItem != null)
                {
                    webItem.IsManual = true;
                    for (int i = 0; i < webItem.Items.Count; i++)
                    {
                        Achievements finded = loadedItem.Items.Find(x => (x.ApiName.IsNullOrEmpty() || x.ApiName.IsEqual(webItem.Items[i].ApiName)) && x.Name.IsEqual(webItem.Items[i].Name));
                        if (finded != null)
                        {
                            webItem.Items[i].DateUnlocked = finded.DateUnlocked;
                        }
                    }
                }
            }
            else
            {
                webItem = GetWeb(id);
            }

            bool mustUpdate = true;
            if (webItem != null && !webItem.HasAchievements)
            {
                mustUpdate = !loadedItem.HasAchievements;
            }

            if (webItem != null && !ReferenceEquals(loadedItem, webItem) && mustUpdate)
            {
                if (webItem.HasAchievements)
                {
                    webItem = SetEstimateTimeToUnlock(game, webItem);
                }
                Update(webItem);
            }
            else
            {
                webItem = loadedItem;
            }

            ActionAfterRefresh(webItem);
        }

        public override void Refresh(List<Guid> ids)
        {
            GlobalProgressOptions options = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
            {
                Cancelable = true,
                IsIndeterminate = false
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((a) =>
            {
                Logger.Info($"Refresh() started");
                Database.BeginBufferUpdate();

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                a.ProgressMaxValue = ids.Count;

                string cancelText = string.Empty;
                foreach (Guid id in ids)
                {
                    Game game = API.Instance.Database.Games.Get(id);
                    a.Text = $"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}"
                        + "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}"
                        + "\n" + game.Name + (game.Source == null ? string.Empty : $" ({game.Source.Name})");

                    if (a.CancelToken.IsCancellationRequested)
                    {
                        cancelText = " canceled";
                        break;
                    }

                    string sourceName = PlayniteTools.GetSourceName(game);
                    AchievementSource achievementSource = GetAchievementSource(PluginSettings.Settings, game);
                    string gameName = game.Name;
                    bool verifToAddOrShow = VerifToAddOrShow(PluginSettings.Settings, game);
                    GameAchievements gameAchievements = Get(game, true);

                    if (!gameAchievements.IsIgnored && verifToAddOrShow && !gameAchievements.IsManual)
                    {
                        try
                        {
                            RefreshNoLoader(id);
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, PluginName);
                        }
                    }

                    a.CurrentProgressValue++;
                }
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"Task Refresh(){cancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{ids.Count} items");

                Database.EndBufferUpdate();
            }, options);
        }

        public override void RefreshRecent()
        {
            GlobalProgressOptions options = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonGettingNewDatas")}")
            {
                Cancelable = true,
                IsIndeterminate = false
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((a) =>
            {
                Database.BeginBufferUpdate();

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                string cancelText = string.Empty;

                List<Game> playniteDb = PluginSettings.Settings.AutoImportOnInstalled
                    ? API.Instance.Database.Games
                       .Where(x => (x.Added != null && x.Added > PluginSettings.Settings.LastAutoLibUpdateAssetsDownload) || x.IsInstalled)
                       .ToList()
                    : API.Instance.Database.Games
                        .Where(x => x.Added != null && x.Added > PluginSettings.Settings.LastAutoLibUpdateAssetsDownload)
                        .ToList();

                Logger.Info($"RefreshRecent found {playniteDb.Count} game(s) that need updating");
                a.ProgressMaxValue = playniteDb.Count;

                playniteDb.ForEach(x =>
                {
                    a.Text = $"{PluginName} - {ResourceProvider.GetString("LOCCommonGettingNewDatas")}"
                        + "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}"
                        + "\n" + x.Name + (x.Source == null ? string.Empty : $" ({x.Source.Name})");

                    if (a.CancelToken.IsCancellationRequested)
                    {
                        cancelText = " canceled";
                        return;
                    }

                    try
                    {
                        Thread.Sleep(100);
                        RefreshNoLoader(x.Id);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }

                    a.CurrentProgressValue++;
                });

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"Task RefreshRecent() - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {playniteDb.Count} items");

                Database.EndBufferUpdate();
            }, options);
        }

        public override void ActionAfterRefresh(GameAchievements item)
        {
            Game game = API.Instance.Database.Games.Get(item.Id);
            if ((item?.HasAchievements ?? false) && PluginSettings.Settings.AchievementFeature != null)
            {
                if (game.FeatureIds != null)
                {
                    _ = game.FeatureIds.AddMissing(PluginSettings.Settings.AchievementFeature.Id);
                }
                else
                {
                    game.FeatureIds = new List<Guid> { PluginSettings.Settings.AchievementFeature.Id };
                }
                API.Instance.Database.Games.Update(game);
            }
        }


        public void RefreshRarety()
        {
            GlobalProgressOptions options = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
            {
                Cancelable = true,
                IsIndeterminate = false
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((a) =>
            {
                Logger.Info($"RefreshRarety() started");
                Database.BeginBufferUpdate();

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                IEnumerable<GameAchievements> db = Database.Where(x => x.IsManual && x.HasAchievements);
                a.ProgressMaxValue = (double)db.Count();
                string CancelText = string.Empty;

                ExophaseAchievements exophaseAchievements = new ExophaseAchievements();
                SteamAchievements steamAchievements = new SteamAchievements();
                bool SteamConfig = steamAchievements.IsConfigured();

                foreach (GameAchievements gameAchievements in db)
                {
                    a.Text = $"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}"
                        + "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}"
                        + "\n" + gameAchievements.Name + (gameAchievements.Source == null ? string.Empty : $" ({gameAchievements.Source.Name})");

                    if (a.CancelToken.IsCancellationRequested)
                    {
                        CancelText = " canceled";
                        break;
                    }

                    try
                    {
                        string SourceName = gameAchievements.SourcesLink?.Name?.ToLower();
                        switch (SourceName)
                        {
                            case "steam":
                                if (uint.TryParse(Regex.Match(gameAchievements.SourcesLink.Url, @"\d+").Value, out uint appId))
                                {
                                    steamAchievements.SetRarity(appId, gameAchievements);
                                }
                                else
                                {
                                    Logger.Warn($"No Steam appId");
                                }
                                break;

                            case "exophase":
                                exophaseAchievements.SetRarety(gameAchievements, AchievementSource.Local);
                                break;

                            default:
                                Logger.Warn($"No sourcesLink for {gameAchievements.Name} with {SourceName}");
                                break;
                        }

                        AddOrUpdate(gameAchievements);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }

                    a.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"Task RefreshRarety(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{(double)db.Count()} items");

                Database.EndBufferUpdate();
            }, options);
        }

        public void RefreshEstimateTime()
        {
            GlobalProgressOptions options = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
            {
                Cancelable = true,
                IsIndeterminate = false
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((a) =>
            {
                Logger.Info($"RefreshEstimateTime() started");
                Database.BeginBufferUpdate();

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                IEnumerable<GameAchievements> db = Database.Where(x => x.IsManual && x.HasAchievements);
                a.ProgressMaxValue = db.Count();
                string CancelText = string.Empty;

                ExophaseAchievements exophaseAchievements = new ExophaseAchievements();
                SteamAchievements steamAchievements = new SteamAchievements();
                bool SteamConfig = steamAchievements.IsConfigured();

                foreach (GameAchievements gameAchievements in db)
                {
                    a.Text = $"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}"
                        + "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}"
                        + "\n" + gameAchievements.Name + (gameAchievements.Source == null ? string.Empty : $" ({gameAchievements.Source.Name})");

                    if (a.CancelToken.IsCancellationRequested)
                    {
                        CancelText = " canceled";
                        break;
                    }

                    try
                    {
                        Game game = API.Instance.Database.Games.Get(gameAchievements.Id);
                        GameAchievements gameAchievementsNew = Serialization.GetClone(gameAchievements);
                        gameAchievementsNew = SetEstimateTimeToUnlock(game, gameAchievements);
                        AddOrUpdate(gameAchievementsNew);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }

                    a.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"Task RefreshEstimateTime(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{(double)db.Count()} items");

                Database.EndBufferUpdate();
            }, options);
        }


        #region Tag system
        public override void AddTag(Game game, bool noUpdate = false)
        {
            GetPluginTags();
            GameAchievements gameAchievements = Get(game, true);

            if (gameAchievements.HasAchievements)
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
                                API.Instance.Database.Games.Update(game);
                                game.OnPropertyChanged();
                            }, DispatcherPriority.Send);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Tag insert error with {game.Name}", true, PluginName, string.Format(ResourceProvider.GetString("LOCCommonNotificationTagError"), game.Name));
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
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon0to1")}");
                }
                if (EstimateTimeMax <= 6)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon1to5")}");
                }
                if (EstimateTimeMax <= 10)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon5to10")}");
                }
                if (EstimateTimeMax <= 20)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon10to20")}");
                }
                if (EstimateTimeMax <= 30)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon20to30")}");
                }
                if (EstimateTimeMax <= 40)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon30to40")}");
                }
                if (EstimateTimeMax <= 50)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon40to50")}");
                }
                if (EstimateTimeMax <= 60)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon50to60")}");
                }
                if (EstimateTimeMax <= 70)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon60to70")}");
                }
                if (EstimateTimeMax <= 80)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon70to80")}");
                }
                if (EstimateTimeMax <= 90)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon80to90")}");
                }
                if (EstimateTimeMax <= 100)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon90to100")}");
                }
                if (EstimateTimeMax > 100)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon100plus")}");
                }
            }

            return null;
        }
        #endregion


        public void SetIgnored(GameAchievements gameAchievements)
        {
            if (!gameAchievements.IsIgnored)
            {
                _ = Remove(gameAchievements.Id);
                GameAchievements pluginData = Get(gameAchievements.Id, true);
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
            OptionsDownloadData View = new OptionsDownloadData();
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginName + " - " + ResourceProvider.GetString("LOCCommonSelectData"), View);
            _ = windowExtension.ShowDialog();

            List<Game> PlayniteDb = View.GetFilteredGames();
            bool OnlyMissing = View.GetOnlyMissing();

            if (PlayniteDb == null)
            {
                return;
            }

            PlayniteDb = PlayniteDb.FindAll(x => !Get(x.Id, true).IsIgnored);

            if (OnlyMissing)
            {
                PlayniteDb = PlayniteDb.FindAll(x => !Get(x.Id, true).HasData);
            }
            // Without manual
            else
            {
                PlayniteDb = PlayniteDb.FindAll(x => !Get(x.Id, true).IsManual);
            }

            GlobalProgressOptions options = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonGettingData")}")
            {
                Cancelable = true,
                IsIndeterminate = false
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((a) =>
            {
                try
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    a.ProgressMaxValue = (double)PlayniteDb.Count();

                    string CancelText = string.Empty;
                    foreach (Game game in PlayniteDb)
                    {
                        a.Text = $"{PluginName} - {ResourceProvider.GetString("LOCCommonGettingData")}"
                            + "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}"
                            + "\n" + game.Name + (game.Source == null ? string.Empty : $" ({game.Source.Name})");

                        if (a.CancelToken.IsCancellationRequested)
                        {
                            CancelText = " canceled";
                            break;
                        }

                        Thread.Sleep(100);

                        try
                        {
                            _ = Get(game, false, true);
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, PluginName);
                        }

                        a.CurrentProgressValue++;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    Logger.Info($"Task GetSelectData(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }, options);
        }


        public ProgressionAchievements Progession()
        {
            ProgressionAchievements Result = new ProgressionAchievements();
            int Total = 0;
            int Locked = 0;
            int Unlocked = 0;

            try
            {
                List<KeyValuePair<Guid, GameAchievements>> db = Database.Items.Where(x => x.Value.HasAchievements).ToList();
                foreach (KeyValuePair<Guid, GameAchievements> item in db)
                {
                    GameAchievements GameAchievements = item.Value;
                    if (API.Instance.Database.Games.Get(item.Key) != null)
                    {
                        Total += GameAchievements.Total;
                        Locked += GameAchievements.Locked;
                        Unlocked += GameAchievements.Unlocked;
                    }
                    else
                    {
                        Logger.Warn($"Achievements data without game for {GameAchievements.Name} & {GameAchievements.Id.ToString()}");
                    }
                }

                Result.Total = Total;
                Result.Locked = Locked;
                Result.Unlocked = Unlocked;
                Result.Progression = (Total != 0) ? (int)Math.Round((double)(Unlocked * 100 / Total)) : 0;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
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
                List<KeyValuePair<Guid, GameAchievements>> db = Database.Items.Where(x => x.Value.Playtime > 0 && x.Value.HasAchievements).ToList();
                foreach (KeyValuePair<Guid, GameAchievements> item in db)
                {
                    GameAchievements GameAchievements = item.Value;
                    if (API.Instance.Database.Games.Get(item.Key) != null)
                    {
                        Total += GameAchievements.Total;
                        Locked += GameAchievements.Locked;
                        Unlocked += GameAchievements.Unlocked;
                    }
                    else
                    {
                        Logger.Warn($"Achievements data without game for {GameAchievements.Name} & {GameAchievements.Id.ToString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
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
                List<KeyValuePair<Guid, GameAchievements>> db = Database.Items.Where(x => x.Value.SourceId == GameSourceId).ToList();
                foreach (KeyValuePair<Guid, GameAchievements> item in db)
                {
                    Guid Id = item.Key;
                    Game Game = API.Instance.Database.Games.Get(Id);
                    GameAchievements GameAchievements = item.Value;

                    if (API.Instance.Database.Games.Get(item.Key) != null)
                    {
                        Total += GameAchievements.Total;
                        Locked += GameAchievements.Locked;
                        Unlocked += GameAchievements.Unlocked;
                    }
                    else
                    {
                        Logger.Warn($"Achievements data without game for {GameAchievements.Name} & {GameAchievements.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
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
        public string Source { get; set; }
        public int Value { get; set; }
    }
}
