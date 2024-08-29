using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsShared;
using CommonPluginsShared.Converters;
using LiveCharts;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Models.Stats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SuccessStory.Services
{
    public enum StatsType { Day, Month }

    public class SuccessStoryStats
    {
        private static ILogger Logger => LogManager.GetLogger();
        private static SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;


        #region Charts
        private static AchievementsGraphicsDataCount GetCount(StatsType statsType, Guid? id, int limit, bool cutPeriod, bool onlyRA, bool excludeRA)
        {
            string[] chartLabels = new string[limit + 1];
            ChartValues<CustomerForSingle> chartSeries = new ChartValues<CustomerForSingle>();

            LocalDateYMConverter localDateYMConverter = new LocalDateYMConverter(); 
            LocalDateConverter localDateConverter = new LocalDateConverter();
            bool includeHiddenGames = PluginDatabase.PluginSettings.Settings.IncludeHiddenGames;

            try
            {
                List<Achievements> Achievements = id == null || id == default
                    ? PluginDatabase.Database.Items
                        .Where(x => x.Value.HasAchievements && !x.Value.IsDeleted && (includeHiddenGames || x.Value.Hidden == false) && (!onlyRA || x.Value.IsRa) && (!excludeRA || !x.Value.IsRa))
                        .SelectMany(x => x.Value.Items)
                        .Where(x => x.IsUnlock)
                        .OrderByDescending(x => x.DateWhenUnlocked)
                        .ToList()
                    : PluginDatabase.GetOnlyCache((Guid)id).Items.Where(x => x.IsUnlock).OrderByDescending(x => x.DateWhenUnlocked).ToList();

                #region Set chart labels
                DateTime? maxDatetime = Achievements.Max(x => x.DateWhenUnlocked);
                DateTime startDateTime = maxDatetime != null ? (DateTime)maxDatetime : DateTime.Now;

                if (cutPeriod)
                {
                    List<string> chartLabelsList = new List<string>();
                    DateTime dtPrevious = default;

                    Achievements.ForEach(x =>
                    {
                        DateTime dt = (DateTime)x.DateWhenUnlocked;
                        string dtCompare = (statsType == StatsType.Day)
                            ? (string)localDateConverter.Convert(dt.AddDays(1), null, null, null)
                            : (string)localDateYMConverter.Convert(dt.AddMonths(1), null, null, null);
                        string previousCompare = (statsType == StatsType.Day)
                            ? (string)localDateConverter.Convert(dtPrevious, null, null, null)
                            : (string)localDateYMConverter.Convert(dtPrevious, null, null, null);

                        if (!chartLabelsList.Contains((string)localDateConverter.Convert(x.DateWhenUnlocked, null, null, null)))
                        {
                            if (dtCompare != previousCompare && !previousCompare.IsNullOrEmpty())
                            {
                                chartLabelsList.Add(string.Empty);
                            }
                            chartLabelsList.Add((string)localDateConverter.Convert(x.DateWhenUnlocked, null, null, null));
                            dtPrevious = dt;
                        }

                    });

                    chartLabelsList.Reverse();
                    chartLabels = chartLabelsList.ToArray();
                }
                else
                {
                    for (int i = limit; i >= 0; i--)
                    {
                        chartLabels[limit - i] = (statsType == StatsType.Day)
                            ? (string)localDateConverter.Convert(startDateTime.AddDays(-i), null, null, null)
                            : (string)localDateYMConverter.Convert(startDateTime.AddMonths(-i), null, null, null);
                    }
                }
                #endregion

                #region Set chart data
                chartLabels.ForEach(x =>
                {
                    chartSeries.Add(new CustomerForSingle
                    {
                        Name = x,
                        Values = cutPeriod ? double.NaN : 0
                    });
                });

                IEnumerable<Tuple<string, int>> data = Achievements
                    .GroupBy(x => ((statsType == StatsType.Day) ? (string)localDateConverter.Convert(x.DateWhenUnlocked, null, null, null) : (string)localDateYMConverter.Convert(x.DateWhenUnlocked, null, null, null)))
                    .OrderByDescending(group => group.Key)
                    .Select(group => Tuple.Create(group.Key, group.Count()));

                string[] sourceArray = chartSeries.Select(y => y.Name).ToArray();
                data.ForEach(x =>
                {
                    int index = Array.IndexOf(sourceArray, x.Item1);
                    if (index >= 0 && index < (limit + 1))
                    {
                        chartSeries[index].Values = x.Item2;
                    }
                });
                #endregion
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error in GetCount({statsType}, {id}, {limit}, {cutPeriod}, {onlyRA}, {excludeRA})", true, PluginDatabase.PluginName);
            }

            return new AchievementsGraphicsDataCount { Labels = chartLabels, Series = chartSeries };
        }


        /// <summary>
        /// Get number achievements unlock by month for a game or not.
        /// </summary>
        /// <param name="id">GameId</param>
        /// <param name="limit"></param>
        /// <param name="onlyRA"></param>
        /// <returns></returns>
        public static AchievementsGraphicsDataCount GetCountByMonth(Guid? id = null, int limit = 11, bool onlyRA = false, bool excludeRA = false)
        {
            return GetCount(StatsType.Month, id, limit, false, onlyRA, excludeRA);
        }

        /// <summary>
        /// Get number achievements unlock/unlocked/total by source.
        /// </summary>
        /// <param name="onlyRA"></param>
        /// <returns></returns>
        public static AchievementsGraphicsDataCountSources GetCountBySources(bool onlyRA = false, bool excludeRA = false)
        {
            List<string> chartLabelsList = new List<string>();
            string[] chartLabels = new string[0];
            ChartValues<CustomerForSingle> chartSeriesUnlocked = new ChartValues<CustomerForSingle>();
            ChartValues<CustomerForSingle> chartSeriesLocked = new ChartValues<CustomerForSingle>();
            ChartValues<CustomerForSingle> chartSeriesTotal = new ChartValues<CustomerForSingle>();

            bool includeHiddenGames = PluginDatabase.PluginSettings.Settings.IncludeHiddenGames;

            try
            {
                #region Set chart labels
                if (onlyRA)
                {
                    //chartLabelsList.Add("RetroAchievements");
                    PluginDatabase.Database.Items.Where(x => (includeHiddenGames || x.Value.Hidden == false) && (!onlyRA || x.Value.IsRa) && (!excludeRA || !x.Value.IsRa))
                        .Select(x => x.Value.Platforms.FirstOrDefault().Id).Distinct()
                        .ForEach(x =>
                        {
                            Platform platform = API.Instance.Database.Platforms.Get(x);
                            if (platform != null)
                            {
                                chartLabelsList.Add(platform.Name);
                            }
                        });
                }
                else
                {
                    if (PluginDatabase.PluginSettings.Settings.EnableRetroAchievements && !excludeRA)
                    {
                        chartLabelsList.Add("RetroAchievements");
                    }
                    if (PluginDatabase.PluginSettings.Settings.EnableGog)
                    {
                        chartLabelsList.Add("GOG");
                    }
                    if (PluginDatabase.PluginSettings.Settings.EnableEpic)
                    {
                        chartLabelsList.Add("Epic");
                    }
                    if (PluginDatabase.PluginSettings.Settings.EnableSteam)
                    {
                        chartLabelsList.Add("Steam");
                    }
                    if (PluginDatabase.PluginSettings.Settings.EnableOrigin)
                    {
                        chartLabelsList.Add("EA app");
                    }
                    if (PluginDatabase.PluginSettings.Settings.EnableXbox)
                    {
                        chartLabelsList.Add("Xbox");
                    }
                    if (PluginDatabase.PluginSettings.Settings.EnablePsn)
                    {
                        chartLabelsList.Add("Playstation");
                    }
                    if (PluginDatabase.PluginSettings.Settings.EnableLocal)
                    {
                        chartLabelsList.Add("Playnite");
                        chartLabelsList.Add("Hacked");
                    }
                    if (PluginDatabase.PluginSettings.Settings.EnableRpcs3Achievements)
                    {
                        chartLabelsList.Add("RPCS3");
                    }
                    if (PluginDatabase.PluginSettings.Settings.EnableSc2Achievements || PluginDatabase.PluginSettings.Settings.EnableOverwatchAchievements || PluginDatabase.PluginSettings.Settings.EnableWowAchievements)
                    {
                        chartLabelsList.Add("Battle.net");
                    }
                    if (PluginDatabase.PluginSettings.Settings.EnableManual)
                    {
                        PluginDatabase.Database.Items.Where(x => x.Value.IsManual && (includeHiddenGames || x.Value.Hidden == false))
                            .Select(x => x.Value.SourceId).Distinct()
                            .ForEach(x =>
                            {
                                GameSource gameSource = API.Instance.Database.Sources.Get(x);
                                if (gameSource != null)
                                {
                                    chartLabelsList.Add(gameSource.Name);
                                }
                            });
                    }
                }

                chartLabels = new string[chartLabelsList.Count];
                List<AchievementsGraphicsDataSources> tempDataUnlocked = new List<AchievementsGraphicsDataSources>();
                List<AchievementsGraphicsDataSources> tempDataLocked = new List<AchievementsGraphicsDataSources>();
                List<AchievementsGraphicsDataSources> tempDataTotal = new List<AchievementsGraphicsDataSources>();

                chartLabelsList = chartLabelsList.Distinct().OrderBy(x => x).ToList();
                for (int i = 0; i < chartLabelsList.Count; i++)
                {
                    chartLabels[i] = TransformIcon.Get(chartLabelsList[i]);
                    tempDataLocked.Add(new AchievementsGraphicsDataSources { Source = chartLabelsList[i], Value = 0 });
                    tempDataUnlocked.Add(new AchievementsGraphicsDataSources { Source = chartLabelsList[i], Value = 0 });
                    tempDataTotal.Add(new AchievementsGraphicsDataSources { Source = chartLabelsList[i], Value = 0 });
                }
                #endregion

                #region Set chart data
                PluginDatabase.Database.Items
                    .Where(x => x.Value.HasAchievements && !x.Value.IsDeleted && (includeHiddenGames || x.Value.Hidden == false) && (!onlyRA || x.Value.IsRa) && (!excludeRA || !x.Value.IsRa))
                    .ForEach(x =>
                    {
                        string sourceName = PlayniteTools.GetSourceName(x.Key);
                        if (onlyRA)
                        {
                            string platormName = x.Value.Platforms.FirstOrDefault().Name;
                            sourceName = platormName.IsNullOrEmpty() ? sourceName : platormName;
                        }

                        x.Value.Items.ForEach(y =>
                        {
                            for (int i = 0; i < tempDataUnlocked.Count; i++)
                            {
                                if (tempDataUnlocked[i].Source.Contains(sourceName, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    tempDataTotal[i].Value += 1;
                                    if (y.DateWhenUnlocked != null)
                                    {
                                        tempDataUnlocked[i].Value += 1;
                                    }
                                    if (y.DateWhenUnlocked == null)
                                    {
                                        tempDataLocked[i].Value += 1;
                                    }
                                }
                            }
                        });
                    });

                
                for (int i = 0; i < tempDataUnlocked.Count; i++)
                {
                    chartSeriesUnlocked.Add(new CustomerForSingle
                    {
                        Name = TransformIcon.Get(tempDataUnlocked[i].Source),
                        Values = tempDataUnlocked[i].Value
                    });
                    chartSeriesLocked.Add(new CustomerForSingle
                    {
                        Name = TransformIcon.Get(tempDataLocked[i].Source),
                        Values = tempDataLocked[i].Value
                    });
                    chartSeriesTotal.Add(new CustomerForSingle
                    {
                        Name = TransformIcon.Get(tempDataTotal[i].Source),
                        Values = tempDataTotal[i].Value
                    });
                }
                #endregion
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error in load GetCountBySources()", true, PluginDatabase.PluginName);
            }

            return new AchievementsGraphicsDataCountSources
            {
                Labels = chartLabels,
                SeriesLocked = chartSeriesLocked,
                SeriesUnlocked = chartSeriesUnlocked,
                SeriesTotal = chartSeriesTotal
            };
        }

        /// <summary>
        /// Get number achievements unlock by month for a game or not.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="limit"></param>
        /// <param name="cutPeriod"></param>
        /// <returns></returns>
        public static AchievementsGraphicsDataCount GetCountByDay(Guid? id = null, int limit = 11, bool cutPeriod = false)
        {
            return GetCount(StatsType.Day, id, limit, cutPeriod, false, false);
        }
        #endregion


        #region Progression
        private static ProgressionAchievements GetProgession(bool withPlaytime, Guid sourceId)
        {
            bool includeHiddenGames = PluginDatabase.PluginSettings.Settings.IncludeHiddenGames;
            ProgressionAchievements Result = new ProgressionAchievements();

            try
            {
                var data = PluginDatabase.Database.Items
                    .Where(x => x.Value.HasAchievements && !x.Value.IsDeleted && (includeHiddenGames || x.Value.Hidden == false) && (!withPlaytime || x.Value.Playtime > 0) && (sourceId == default || x.Value.SourceId == sourceId));
                Result = new ProgressionAchievements
                {
                    Total = data.Sum(x => x.Value.Total),
                    Locked = data.Sum(x => x.Value.Locked),
                    Unlocked = data.Sum(x => x.Value.Unlocked),
                    GamerScore = (int)data.Sum(x => x.Value.TotalGamerScore)
                };
                Result.Progression = (Result.Total != 0) ? (int)Math.Round((double)(Result.Unlocked * 100 / Result.Total)) : 0;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error in GetProgession({withPlaytime}, {sourceId})", true, PluginDatabase.PluginName);
            }

            return Result;
        }


        /// <summary>
        /// Progression for all games with achievements data.
        /// </summary>
        /// <returns></returns>
        public static ProgressionAchievements Progession()
        {
            return GetProgession(false, default);
        }

        /// <summary>
        /// Progression for all games with achievements data and a playtime.
        /// </summary>
        /// <returns></returns>
        public static ProgressionAchievements ProgessionLaunched()
        {
            return GetProgession(true, default);
        }

        /// <summary>
        /// Progression for all games on a source with achievements data.
        /// </summary>
        /// <returns></returns>
        public static ProgressionAchievements ProgessionSource(Guid sourceId)
        {
            return GetProgession(false, sourceId);
        }
        #endregion
    }
}
