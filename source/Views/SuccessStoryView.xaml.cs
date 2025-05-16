using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPluginsShared;
using SuccessStory.Models;
using System.Threading.Tasks;
using SuccessStory.Services;
using CommonPluginsControls.LiveChartsCommon;
using System.Windows.Threading;
using System.Threading;
using System.Collections.ObjectModel;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Converters;
using System.Globalization;
using SuccessStory.Models.Stats;
using System.Diagnostics;

namespace SuccessStory
{
    /// <summary>
    /// Logique d'interaction pour SuccessView.xaml
    /// </summary>
    public partial class SuccessView : UserControl
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;
        private SuccessViewData SuccessViewData { get; set; } = new SuccessViewData();

        private ObservableCollection<ListSource> FilterSourceItems { get; set; } = new ObservableCollection<ListSource>();
        private ObservableCollection<ListViewGames> ListGames { get; set; } = new ObservableCollection<ListViewGames>();
        private List<string> SearchSources { get; set; } = new List<string>();
        private List<string> SearchStatus { get; set; } = new List<string>();

        private static Filters Filters { get; set; } = null;

        private bool OnlyRa { get; }
        private bool ExcludeRa { get; }


        public SuccessView(bool isRetroAchievements = false, Game GameSelected = null)
        {
            try
            {
                OnlyRa = PluginDatabase.PluginSettings.Settings.EnableRetroAchievementsView && isRetroAchievements;
                ExcludeRa = PluginDatabase.PluginSettings.Settings.EnableRetroAchievementsView && !isRetroAchievements;

                InitializeComponent();

                SuccessViewData.Settings = PluginDatabase.PluginSettings.Settings;
                DataContext = SuccessViewData;

                lvGameRaretyCount.Width = PluginDatabase.PluginSettings.Settings.UseUltraRare ? 240 : 210;


                // sorting options
                ListviewGames.SortingDefaultDataName = PluginDatabase.PluginSettings.Settings.NameSorting;
                ListviewGames.SortingSortDirection = (PluginDatabase.PluginSettings.Settings.IsAsc) ? ListSortDirection.Ascending : ListSortDirection.Descending;
                ListviewGames.Sorting();

                // lvGames options
                if (!PluginDatabase.PluginSettings.Settings.lvGamesIcon100Percent)
                {
                    lvGameIcon100Percent.Width = 0;
                }
                if (!PluginDatabase.PluginSettings.Settings.lvGamesIcon)
                {
                    lvGameIcon.Width = 0;
                }
                if (!PluginDatabase.PluginSettings.Settings.lvGamesName)
                {
                    lvGameName.Width = 0;
                }
                if (!PluginDatabase.PluginSettings.Settings.lvGamesLastSession)
                {
                    lvGameLastActivity.Width = 0;
                }
                if (!PluginDatabase.PluginSettings.Settings.lvGamesSource)
                {
                    lvGamesSource.Width = 0;
                }
                if (!PluginDatabase.PluginSettings.Settings.lvGamesProgression)
                {
                    lvGameProgression.Width = 0;
                }
                if (!PluginDatabase.PluginSettings.Settings.EnableGamerScore)
                {
                    lvTotalGamerScore.Width = 0;
                    lvGamerScore.Width = 0;
                }


                AchProgressionTotal ProgressionGlobal = null;
                AchProgressionTotal ProgressionLaunched = null;

                AchGraphicsDataCount GraphicsData = null;
                string[] StatsGraphicsAchievementsLabels = null;
                SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection();


                PART_DataLoad.Visibility = Visibility.Visible;
                PART_Data.Visibility = Visibility.Collapsed;

                _ = Task.Run(() =>
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    GetListGame(isRetroAchievements);

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    Common.LogDebug(true, $"Task GetListGame({isRetroAchievements}) - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
                    

                    stopWatch = new Stopwatch();
                    stopWatch.Start();

                    GetListAll(isRetroAchievements);

                    stopWatch.Stop();
                    ts = stopWatch.Elapsed;
                    Common.LogDebug(true, $"Task GetListAll({isRetroAchievements}) - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");

                    
                    stopWatch = new Stopwatch();
                    stopWatch.Start();

                    SetGraphicsAchievementsSources();

                    stopWatch.Stop();
                    ts = stopWatch.Elapsed;
                    Common.LogDebug(true, $"Task SetGraphicsAchievementsSources({isRetroAchievements}) - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");


                    ProgressionGlobal = SuccessStoryStats.Progession();
                    ProgressionLaunched = SuccessStoryStats.ProgessionLaunched();

                    GraphicsData = SuccessStoryStats.GetCountByMonth(null, 12, OnlyRa, ExcludeRa);
                    StatsGraphicsAchievementsLabels = GraphicsData.Labels;


                    string icon = string.Empty;

                    if (PluginDatabase.PluginSettings.Settings.EnableRetroAchievementsView && PluginDatabase.PluginSettings.Settings.EnableRetroAchievements)
                    {
                        if (isRetroAchievements)
                        {
                            if (PluginDatabase.PluginSettings.Settings.EnableRetroAchievements)
                            {
                                icon = TransformIcon.Get("RetroAchievements") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "RetroAchievements", SourceNameShort = "RetroAchievements", IsCheck = false });
                            }
                        }
                        else
                        {
                            if (PluginDatabase.PluginSettings.Settings.EnableLocal)
                            {
                                icon = TransformIcon.Get("Playnite") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Playnite", SourceNameShort = "Playnite", IsCheck = false });

                                icon = TransformIcon.Get("Hacked") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Hacked", SourceNameShort = "Hacked", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.Settings.EnableSteam)
                            {
                                icon = TransformIcon.Get("Steam") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Steam", SourceNameShort = "Steam", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.Settings.EnableGog)
                            {
                                icon = TransformIcon.Get("GOG") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "GOG", SourceNameShort = "GOG", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.Settings.EnableEpic)
                            {
                                icon = TransformIcon.Get("Epic") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Epic", SourceNameShort = "Epic", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.Settings.EnableOrigin)
                            {
                                icon = TransformIcon.Get("EA app") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "EA app", SourceNameShort = "EA app", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.Settings.EnableXbox)
                            {
                                icon = TransformIcon.Get("Xbox") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Xbox", SourceNameShort = "Xbox", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.Settings.EnableRpcs3Achievements)
                            {
                                icon = TransformIcon.Get("Rpcs3") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Rpcs3", SourceNameShort = "Rpcs3", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.Settings.EnableGameJolt)
                            {
                                icon = TransformIcon.Get("Game Jolt") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Game Jolt", SourceNameShort = "Game Jolt", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.Settings.EnablePsn)
                            {
                                icon = TransformIcon.Get("Playstation") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Playstation", SourceNameShort = "Playstation", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.Settings.EnableManual)
                            {
                                icon = TransformIcon.Get("Manual Achievements") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + ResourceProvider.GetString("LOCSuccessStoryManualAchievements"), SourceNameShort = ResourceProvider.GetString("LOCSuccessStoryManualAchievements"), IsCheck = false });

                                PluginDatabase.Database.Items.Where(x => x.Value.IsManual && !x.Value.IsEmulators).Select(x => PlayniteTools.GetSourceName(x.Value.Game)).Distinct()
                                        .ForEach(x =>
                                        {
                                            icon = TransformIcon.Get(x) + " ";

                                            ListSource found = FilterSourceItems.Where(y => y.SourceNameShort.IsEqual(x)).FirstOrDefault();
                                            if (found == null)
                                            {
                                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + x, SourceNameShort = x, IsCheck = false });
                                            }
                                        });
                            }
                            if (PluginDatabase.PluginSettings.Settings.EnableOverwatchAchievements || PluginDatabase.PluginSettings.Settings.EnableSc2Achievements)
                            {
                                icon = TransformIcon.Get("Battle.net") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Battle.net", SourceNameShort = "Battle.net", IsCheck = false });
                            }
                        }
                    }
                    else
                    {
                        if (PluginDatabase.PluginSettings.Settings.EnableLocal)
                        {
                            icon = TransformIcon.Get("Playnite") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Playnite", SourceNameShort = "Playnite", IsCheck = false });

                            icon = TransformIcon.Get("Hacked") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Hacked", SourceNameShort = "Hacked", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.Settings.EnableSteam)
                        {
                            icon = TransformIcon.Get("Steam") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Steam", SourceNameShort = "Steam", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.Settings.EnableGog)
                        {
                            icon = TransformIcon.Get("GOG") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "GOG", SourceNameShort = "GOG", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.Settings.EnableEpic)
                        {
                            icon = TransformIcon.Get("Epic") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Epic", SourceNameShort = "Epic", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.Settings.EnableOrigin)
                        {
                            icon = TransformIcon.Get("EA app") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "EA app", SourceNameShort = "EA app", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.Settings.EnableXbox)
                        {
                            icon = TransformIcon.Get("Xbox") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Xbox", SourceNameShort = "Xbox", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.Settings.EnableRetroAchievements)
                        {
                            icon = TransformIcon.Get("RetroAchievements") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "RetroAchievements", SourceNameShort = "RetroAchievements", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.Settings.EnableRpcs3Achievements)
                        {
                            icon = TransformIcon.Get("RPCS3") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "RPCS3", SourceNameShort = "Rpcs3", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.Settings.EnableGameJolt)
                        {
                            icon = TransformIcon.Get("Game Jolt") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Game Jolt", SourceNameShort = "Game Jolt", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.Settings.EnablePsn)
                        {
                            icon = TransformIcon.Get("Playstation") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Playstation", SourceNameShort = "Playstation", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.Settings.EnableManual)
                        {
                            icon = TransformIcon.Get("Manual Achievements") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + ResourceProvider.GetString("LOCSuccessStoryManualAchievements"), SourceNameShort = ResourceProvider.GetString("LOCSuccessStoryManualAchievements"), IsCheck = false });

                            PluginDatabase.Database.Items.Where(x => x.Value.IsManual).Select(x => PlayniteTools.GetSourceName(x.Value.Game)).Distinct()
                                    .ForEach(x =>
                                    {
                                        icon = TransformIcon.Get(x) + " ";

                                        ListSource found = FilterSourceItems.FirstOrDefault(y => y.SourceNameShort.IsEqual(x));
                                        if (found == null)
                                        {
                                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + x, SourceNameShort = x, IsCheck = false });
                                        }
                                    });
                        }
                        if (PluginDatabase.PluginSettings.Settings.EnableOverwatchAchievements || PluginDatabase.PluginSettings.Settings.EnableSc2Achievements)
                        {
                            icon = TransformIcon.Get("Battle.net") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Battle.net", SourceNameShort = "Battle.net", IsCheck = false });
                        }
                    }


                    SuccessViewData.Data = SuccessStoryStats.GetCountUnlocked(StatsType.Day, null, null, OnlyRa, ExcludeRa);
                })
                 .ContinueWith(antecedent =>
                 {
                     _ = API.Instance.MainView.UIDispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                     {
                         GraphicTitle.Content = string.Empty;
                         GraphicTitleALL.Content = ResourceProvider.GetString("LOCSuccessStoryGraphicTitleALL");

                         FilterSourceItems = FilterSourceItems.OrderBy(x => x.SourceNameShort).ToObservable();
                         SuccessViewData.FilterSourceItems = FilterSourceItems;

                         SuccessViewData.ListGames = ListGames;
                         SuccessViewData.TotalFoundCount = ListGames.Count;
                         ListviewGames.Sorting();

                         PART_TotalCommun.Content = SuccessViewData.ListGames.Select(x => x.Common.UnLocked).Sum();
                         PART_TotalNoCommun.Content = SuccessViewData.ListGames.Select(x => x.UnCommon.UnLocked).Sum();
                         PART_TotalRare.Content = SuccessViewData.ListGames.Select(x => x.Rare.UnLocked).Sum();
                         PART_TotalUltraRare.Content = SuccessViewData.ListGames.Select(x => x.UltraRare.UnLocked).Sum();


                         if (PluginDatabase.PluginSettings.Settings.EnableRetroAchievementsView && PluginDatabase.PluginSettings.Settings.EnableRetroAchievements && isRetroAchievements)
                         {
                             PART_GraphicBySource.Visibility = Visibility.Collapsed;
                             Grid.SetColumn(PART_GraphicAllUnlocked, 0);
                             Grid.SetColumnSpan(PART_GraphicAllUnlocked, 3);
                         }


                         SuccessViewData.ProgressionGlobalCountValue = ProgressionGlobal.Unlocked;
                         SuccessViewData.ProgressionGlobalCountMax = ProgressionGlobal.Total;
                         SuccessViewData.ProgressionGlobal = ProgressionGlobal.Progression + "%";

                         SuccessViewData.ProgressionLaunchedCountValue = ProgressionLaunched.Unlocked;
                         SuccessViewData.ProgressionLaunchedCountMax = ProgressionLaunched.Total;
                         SuccessViewData.ProgressionLaunched = ProgressionLaunched.Progression + "%";


                         StatsGraphicAchievementsSeries.Add(new LineSeries
                         {
                             Title = string.Empty,
                             Values = GraphicsData.Series
                         });
                         AchievementsMonth.Series = StatsGraphicAchievementsSeries;
                         AchievementsMonthX.Labels = StatsGraphicsAchievementsLabels;


                     // Set game selected
                     if (GameSelected != null)
                         {
                             ListviewGames.SelectedIndex = ListGames.IndexOf(ListGames.Where(x => x.Name == GameSelected.Name).FirstOrDefault());
                         }
                         ListviewGames.ScrollIntoView(ListviewGames.SelectedItem);


                         if (Filters != null)
                         {
                             PART_DatePicker.SelectedDate = Filters.FilterDate;
                             PART_FilteredGames.IsChecked = Filters.FilteredGames;
                             PART_NoUnlockedGames.IsChecked = Filters.HideNoUnlocked;
                             PART_FilterRange.UpperValue = Filters.FilterRangeMax;
                             PART_FilterRange.LowerValue = Filters.FilterRangeMin;
                             TextboxSearch.Text = Filters.SearchText;

                             SearchSources = Filters.SearchSources;
                             if (SearchSources.Count != 0)
                             {
                                 FilterSource.Text = string.Join(", ", SearchSources);
                             }

                             SearchStatus = Filters.SearchStatus;
                             if (SearchStatus.Count != 0)
                             {
                                 FilterStatus.Text = string.Join(", ", SearchStatus);
                             }

                             Filters = null;
                             Filter();
                         }


                         PART_DataLoad.Visibility = Visibility.Collapsed;
                         PART_Data.Visibility = Visibility.Visible;
                     }));
                 });


                if (!PluginDatabase.PluginSettings.Settings.DisplayChart)
                {
                    Part_Charts.Visibility = Visibility.Collapsed;
                }


                SuccessViewData.FilterStatusItems = API.Instance.Database.CompletionStatuses.Select(x => new ListStatus { StatusName = x.Name }).ToObservable();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void SetGraphicsAchievementsSources()
        {
            AchGraphicsDataCountSources data = SuccessStoryStats.GetCountBySources(OnlyRa, ExcludeRa);

            _ = API.Instance.MainView.UIDispatcher?.BeginInvoke((Action)delegate
            {
                //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
                CartesianMapper<CustomerForSingle> customerVmMapper = Mappers.Xy<CustomerForSingle>()
                    .X((value, index) => index)
                    .Y(value => value.Values);

                //lets save the mapper globally
                Charting.For<CustomerForSingle>(customerVmMapper);

                SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection();
                StatsGraphicAchievementsSeries.Add(new ColumnSeries
                {
                    Title = string.Empty,
                    Values = data.SeriesUnlocked
                });

                StatsGraphicAchievementsSources.Series = StatsGraphicAchievementsSeries;
                StatsGraphicAchievementsSourcesX.Labels = data.Labels;
            });
        }

        /// <summary>
        /// Show list game with achievement.
        /// </summary>
        public void GetListGame(bool isRetroAchievements)
        {
            try
            {
                string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                bool ShowHidden = PluginDatabase.PluginSettings.Settings.IncludeHiddenGames;

                RelayCommand<Guid> GoToGame = new RelayCommand<Guid>((Id) =>
                {
                    Filters = new Filters
                    {
                        FilterDate = PART_DatePicker.SelectedDate,
                        FilteredGames = (bool)PART_FilteredGames.IsChecked,
                        FilterRangeMax = PART_FilterRange.UpperValue,
                        FilterRangeMin = PART_FilterRange.LowerValue,
                        SearchSources = SearchSources,
                        SearchStatus = SearchStatus,
                        SearchText = TextboxSearch.Text,
                        HideNoUnlocked = (bool)PART_NoUnlockedGames.IsChecked
                    };

                    API.Instance.MainView.SelectGame(Id);
                    API.Instance.MainView.SwitchToLibraryView();
                });


                ListGames = PluginDatabase.Database
                    .Where(x => x.HasAchievements && !x.IsDeleted && (ShowHidden || x.Hidden == false)
                            && (PluginDatabase.PluginSettings.Settings.EnableRetroAchievementsView && isRetroAchievements ? x.IsRa : ((PluginDatabase.PluginSettings.Settings.EnableRetroAchievementsView && !isRetroAchievements) ? !x.IsRa : true)))
                    .Select(x => new ListViewGames
                    {
                        Icon100Percent = x.Is100Percent ? Path.Combine(pluginFolder, "Resources\\badge.png") : string.Empty,
                        Id = x.Id.ToString(),
                        Name = x.Name,
                        CompletionStatus = x.Game?.CompletionStatus?.Name ?? string.Empty,
                        Icon = !x.Icon.IsNullOrEmpty() ? API.Instance.Database.GetFullFilePath(x.Icon) : string.Empty,
                        LastActivity = x.LastActivity?.ToLocalTime(),
                        SourceName = PlayniteTools.GetSourceName(x.Id),
                        SourceIcon = TransformIcon.Get(PlayniteTools.GetSourceName(x.Id)),
                        ProgressionValue = x.Progression,
                        Total = x.Total,
                        TotalPercent = x.Progression + "%",
                        TotalGamerScore = x.TotalGamerScore,
                        Unlocked = x.Unlocked,
                        IsManual = x.IsManual,

                        FirstUnlock = x.FirstUnlock,
                        LastUnlock = x.LastUnlock,
                        DatesUnlock = x.DatesUnlock,

                        Common = x.Common,
                        UnCommon = x.UnCommon,
                        Rare = x.Rare,
                        UltraRare = x.UltraRare
                    }).ToObservable();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        public void GetListAll(bool isRetroAchievements)
        {
            try
            {
                SuccessStorySettings settings = PluginDatabase.PluginSettings.Settings;
                ObservableCollection<ListAll> listAll = new ObservableCollection<ListAll>();

                foreach (GameAchievements item in PluginDatabase.Database)
                {
                    if (!item.HasAchievements || item.IsDeleted)
                    {
                        continue;
                    }

                    bool isGameRa = item.IsRa;
                    bool showGame = (settings.EnableRetroAchievementsView && isRetroAchievements && isGameRa) || !settings.EnableRetroAchievementsView || isRetroAchievements || !isGameRa;

                    if (!showGame)
                    {
                        continue;
                    }

                    string gameId = item.Id.ToString();
                    string gameName = item.Name;
                    string iconPath = !item.Icon.IsNullOrEmpty() ? API.Instance.Database.GetFullFilePath(item.Icon) : string.Empty;
                    string sourceName = PlayniteTools.GetSourceName(item.Id);
                    string sourceIcon = TransformIcon.Get(sourceName);

                    foreach (Achievement achievement in item.Items.Where(y => y.IsUnlock))
                    {
                        listAll.Add(new ListAll
                        {
                            Id = gameId,
                            Name = gameName,
                            Icon = iconPath,
                            LastActivity = item.LastActivity?.ToLocalTime(),
                            SourceName = sourceName,
                            SourceIcon = sourceIcon,
                            IsManual = item.IsManual,

                            FirstUnlock = item.FirstUnlock,
                            LastUnlock = item.LastUnlock,

                            AchEnableRaretyIndicator = settings.EnableRaretyIndicator,
                            AchDisplayRaretyValue = settings.EnableRaretyIndicator,
                            Achievement = achievement
                        });
                    }
                }

                SuccessViewData.ListAll = listAll;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        /// <summary>
        /// Show Achievements for the selected game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListviewGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListViewGames GameSelected = (ListViewGames)((ListBox)sender).SelectedItem;
            if (GameSelected != null)
            {
                GraphicTitle.Content = ResourceProvider.GetString("LOCSuccessStoryGraphicTitleDay");

                Guid GameId = Guid.Parse(GameSelected.Id);
                SuccessViewData.GameContext = API.Instance.Database.Games.Get(GameId);
            }
            else
            {
                SuccessViewData.GameContext = null;
            }
        }


        #region Filter
        private void Filter()
        {
            double Min = PART_FilterRange.LowerValue;
            double Max = PART_FilterRange.UpperValue;

            bool OnlyFilteredGames = (bool)PART_FilteredGames.IsChecked;
            bool HideNoUnlocked = (bool)PART_NoUnlockedGames.IsChecked;

            DateTime dateStart = default;
            DateTime dateEnd = default;
            if (!PART_TextDate.Text.IsNullOrEmpty())
            {
                dateStart = (DateTime)PART_DatePicker.SelectedDate;
                dateEnd = new DateTime(dateStart.Year, dateStart.Month, DateTime.DaysInMonth(dateStart.Year, dateStart.Month));
            }

            bool IsManual = false;
            if (SearchSources.Contains(ResourceProvider.GetString("LOCSuccessStoryManualAchievements")))
            {
                IsManual = true;
                _ = SearchSources.Remove(ResourceProvider.GetString("LOCSuccessStoryManualAchievements"));
            }

            SuccessViewData.ListGames = ListGames.Where(x => CheckData(x, Min, Max, dateStart, dateEnd, IsManual, OnlyFilteredGames, HideNoUnlocked)).Distinct().ToObservable();

            SuccessViewData.TotalFoundCount = SuccessViewData.ListGames.Count;
            ListviewGames.Sorting();
            ListviewGames.SelectedIndex = -1;

            PART_TotalCommun.Content = SuccessViewData.ListGames.Select(x => x.Common.UnLocked).Sum();
            PART_TotalNoCommun.Content = SuccessViewData.ListGames.Select(x => x.UnCommon.UnLocked).Sum();
            PART_TotalRare.Content = SuccessViewData.ListGames.Select(x => x.Rare.UnLocked).Sum();
            PART_TotalUltraRare.Content = SuccessViewData.ListGames.Select(x => x.UltraRare.UnLocked).Sum();
        }

        private bool CheckData(ListViewGames listViewGames, double Min, double Max, DateTime dateStart, DateTime dateEnd, bool IsManual, bool OnlyFilteredGames, bool hideNoUnlocked)
        {
            bool aa = listViewGames.ProgressionValue >= Min;
            bool bb = listViewGames.ProgressionValue <= Max;
            bool cc = !TextboxSearch.Text.IsNullOrEmpty() ? listViewGames.Name.RemoveDiacritics().Contains(TextboxSearch.Text.RemoveDiacritics(), StringComparison.InvariantCultureIgnoreCase) : true;
            bool dd = !PART_TextDate.Text.IsNullOrEmpty() ? listViewGames.DatesUnlock.Any(y => y >= dateStart && y <= dateEnd) : true;
            bool ee = SearchSources.Count != 0 ? SearchSources.Contains(listViewGames.SourceName, StringComparer.InvariantCultureIgnoreCase) : true;
            bool gg = IsManual ? listViewGames.IsManual : true;
            bool hh = OnlyFilteredGames ? API.Instance.MainView.FilteredGames.Find(y => y.Id.ToString().IsEqual(listViewGames.Id)) != null : true;
            bool ii = SearchStatus.Count != 0 ? SearchStatus.Contains(listViewGames.CompletionStatus, StringComparer.InvariantCultureIgnoreCase) : true;
            bool ff = hideNoUnlocked ? listViewGames.Unlocked > 0 : true;

            bool zz = aa && bb && cc && dd && ee && gg && hh && ii && ff;
            return zz;
        }

        private void TextboxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            Filter();
        }

        private void ChkSource_Checked(object sender, RoutedEventArgs e)
        {
            FilterCbSource((CheckBox)sender);
        }

        private void ChkSource_Unchecked(object sender, RoutedEventArgs e)
        {
            FilterCbSource((CheckBox)sender);
        }

        private void FilterCbSource(CheckBox sender)
        {
            FilterSource.Text = string.Empty;

            if ((bool)sender.IsChecked)
            {
                SearchSources.Add((string)sender.Tag);
            }
            else
            {
                _ = SearchSources.Remove((string)sender.Tag);
            }

            if (SearchSources.Count != 0)
            {
                FilterSource.Text = string.Join(", ", SearchSources);
            }

            Filter();
        }

        private void Chkstatus_Checked(object sender, RoutedEventArgs e)
        {
            FilterCbStatus((CheckBox)sender);
        }

        private void Chkstatus_Unchecked(object sender, RoutedEventArgs e)
        {
            FilterCbStatus((CheckBox)sender);
        }

        private void FilterCbStatus(CheckBox sender)
        {
            FilterStatus.Text = string.Empty;

            if ((bool)sender.IsChecked)
            {
                SearchStatus.Add((string)sender.Tag);
            }
            else
            {
                SearchStatus.Remove((string)sender.Tag);
            }

            if (SearchStatus.Count != 0)
            {
                FilterStatus.Text = string.Join(", ", SearchStatus);
            }

            Filter();
        }

        private void RangeSlider_ValueChanged(object sender, RoutedEventArgs e)
        {
            Filter();
        }

        private void PART_FilteredGames_Click(object sender, RoutedEventArgs e)
        {
            Filter();
        }


        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DatePicker control = sender as DatePicker;
            DateTime dateNew = (DateTime)control.SelectedDate;

            LocalDateYMConverter localDateYMConverter = new LocalDateYMConverter();
            PART_TextDate.Text = localDateYMConverter.Convert(dateNew, null, null, CultureInfo.CurrentCulture).ToString();
            Filter();
        }
        private void PART_ClearButton_Click(object sender, RoutedEventArgs e)
        {
            PART_TextDate.Text = string.Empty;
            Filter();
        }
        #endregion

        private void PART_NoUnlockedGames_Click(object sender, RoutedEventArgs e)
        {
            Filter();
        }
    }


    public class SuccessViewData : ObservableObject
    {
        private ObservableCollection<ListViewGames> listGames = new ObservableCollection<ListViewGames>();
        public ObservableCollection<ListViewGames> ListGames { get => listGames; set => SetValue(ref listGames, value); }

        private ObservableCollection<ListAll> listAll = new ObservableCollection<ListAll>();
        public ObservableCollection<ListAll> ListAll { get => listAll; set => SetValue(ref listAll, value); }

        private ObservableCollection<ListSource> filterSourceItems = new ObservableCollection<ListSource>();
        public ObservableCollection<ListSource> FilterSourceItems { get => filterSourceItems; set => SetValue(ref filterSourceItems, value); }

        private ObservableCollection<ListStatus> filterStatusItems = new ObservableCollection<ListStatus>();
        public ObservableCollection<ListStatus> FilterStatusItems { get => filterStatusItems; set => SetValue(ref filterStatusItems, value); }

        private int totalFoundCount = 100;
        public int TotalFoundCount { get => totalFoundCount; set => SetValue(ref totalFoundCount, value); }

        private int progressionGlobalCountValue = 20;
        public int ProgressionGlobalCountValue { get => progressionGlobalCountValue; set => SetValue(ref progressionGlobalCountValue, value); }

        private int progressionGlobalCountMax = 100;
        public int ProgressionGlobalCountMax { get => progressionGlobalCountMax; set => SetValue(ref progressionGlobalCountMax, value); }

        private string progressionGlobal = "20%";
        public string ProgressionGlobal { get => progressionGlobal; set => SetValue(ref progressionGlobal, value); }

        private int progressionLaunchedCountValue = 40;
        public int ProgressionLaunchedCountValue { get => progressionLaunchedCountValue; set => SetValue(ref progressionLaunchedCountValue, value); }

        private int progressionLaunchedCountMax = 100;
        public int ProgressionLaunchedCountMax { get => progressionLaunchedCountMax; set => SetValue(ref progressionLaunchedCountMax, value); }

        private string progressionLaunched = "40%";
        public string ProgressionLaunched { get => progressionLaunched; set => SetValue(ref progressionLaunched, value); }

        private List<KeyValuePair<string, List<StatsData>>> data;
        public List<KeyValuePair<string, List<StatsData>>> Data { get => data; set => SetValue(ref data, value); }

        private Game gameContext;
        public Game GameContext { get => gameContext; set => SetValue(ref gameContext, value); }

        private SuccessStorySettings settings;
        public SuccessStorySettings Settings { get => settings; set => SetValue(ref settings, value); }
    }


    public class ListSource
    {
        public string SourceName { get; set; }
        public string SourceNameShort { get; set; }
        public bool IsCheck { get; set; }
    }

    public class ListStatus
    {
        public string StatusName { get; set; }
        public bool IsCheck { get; set; }
    }


    public class Filters
    {
        public string SearchText { get; set; } = string.Empty;
        public List<string> SearchSources { get; set; } = new List<string>();
        public List<string> SearchStatus { get; set; } = new List<string>();
        public double FilterRangeMin { get; set; } = 0;
        public double FilterRangeMax { get; set; } = 100;
        public DateTime? FilterDate { get; set; } = null;
        public bool FilteredGames { get; set; } = false;
        public bool HideNoUnlocked { get; set; } = false;
    }
}
