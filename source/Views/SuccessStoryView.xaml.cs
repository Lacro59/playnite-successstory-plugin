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

namespace SuccessStory
{
    /// <summary>
    /// Logique d'interaction pour SuccessView.xaml
    /// </summary>
    public partial class SuccessView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;
        private SuccessViewData successViewData = new SuccessViewData();

        private ObservableCollection<ListSource> FilterSourceItems = new ObservableCollection<ListSource>();
        private ObservableCollection<ListViewGames> ListGames = new ObservableCollection<ListViewGames>();
        private List<string> SearchSources = new List<string>();
        

        public SuccessView(bool isRetroAchievements = false, Game GameSelected = null)
        {
            InitializeComponent();

            successViewData.Settings = PluginDatabase.PluginSettings.Settings;
            DataContext = successViewData;

            if (PluginDatabase.PluginSettings.Settings.UseUltraRare)
            {
                lvGameRaretyCount.Width = 350;
            }


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


            ProgressionAchievements ProgressionGlobal = null;
            ProgressionAchievements ProgressionLaunched = null;

            AchievementsGraphicsDataCount GraphicsData = null;
            string[] StatsGraphicsAchievementsLabels = null;
            SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection(); 


            Task.Run(() =>
            {
                GetListGame();
                SetGraphicsAchievementsSources();

                ProgressionGlobal = PluginDatabase.Progession();
                ProgressionLaunched = PluginDatabase.ProgessionLaunched();

                GraphicsData = PluginDatabase.GetCountByMonth(null, 12);
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
                            icon = TransformIcon.Get("Origin") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Origin", SourceNameShort = "Origin", IsCheck = false });
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
                        if (PluginDatabase.PluginSettings.Settings.EnablePsn)
                        {
                            icon = TransformIcon.Get("Playstation") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Playstation", SourceNameShort = "Playstation", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.Settings.EnableManual)
                        {
                            icon = TransformIcon.Get("Manual Achievements") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + resources.GetString("LOCSuccessStoryManualAchievements"), SourceNameShort = resources.GetString("LOCSuccessStoryManualAchievements"), IsCheck = false });

                            PluginDatabase.Database.Items.Where(x => x.Value.IsManual && !x.Value.IsEmulators).Select(x => PlayniteTools.GetSourceName(x.Value.Game)).Distinct()
                                    .ForEach(x => 
                                    {
                                        icon = TransformIcon.Get(x) + " ";

                                        var finded = FilterSourceItems.Where(y => y.SourceNameShort.IsEqual(x)).FirstOrDefault();
                                        if (finded == null)
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
                        icon = TransformIcon.Get("Origin") + " ";
                        FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Origin", SourceNameShort = "Origin", IsCheck = false });
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
                    if (PluginDatabase.PluginSettings.Settings.EnablePsn)
                    {
                        icon = TransformIcon.Get("Playstation") + " ";
                        FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Playstation", SourceNameShort = "Playstation", IsCheck = false });
                    }
                    if (PluginDatabase.PluginSettings.Settings.EnableManual)
                    {
                        icon = TransformIcon.Get("Manual Achievements") + " ";
                        FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + resources.GetString("LOCSuccessStoryManualAchievements"), SourceNameShort = resources.GetString("LOCSuccessStoryManualAchievements"), IsCheck = false });

                        PluginDatabase.Database.Items.Where(x => x.Value.IsManual).Select(x => PlayniteTools.GetSourceName(x.Value.Game)).Distinct()
                                .ForEach(x =>
                                {
                                    icon = TransformIcon.Get(x) + " ";

                                    var finded = FilterSourceItems.Where(y => y.SourceNameShort.IsEqual(x)).FirstOrDefault();
                                    if (finded == null)
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
            })
            .ContinueWith(antecedent =>
            {
                this.Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    GraphicTitle.Content = string.Empty;
                    GraphicTitleALL.Content = resources.GetString("LOCSuccessStoryGraphicTitleALL");

                    FilterSourceItems = FilterSourceItems.OrderBy(x => x.SourceNameShort).ToObservable();
                    successViewData.FilterSourceItems = FilterSourceItems;

                    successViewData.ListGames = ListGames;
                    successViewData.TotalFoundCount = ListGames.Count;
                    ListviewGames.Sorting();

                    PART_TotalCommun.Content = successViewData.ListGames.Select(x => x.Common.UnLocked).Sum();
                    PART_TotalNoCommun.Content = successViewData.ListGames.Select(x => x.NoCommon.UnLocked).Sum();
                    PART_TotalRare.Content = successViewData.ListGames.Select(x => x.Rare.UnLocked).Sum();
                    PART_TotalUltraRare.Content = successViewData.ListGames.Select(x => x.UltraRare.UnLocked).Sum();


                    if (PluginDatabase.PluginSettings.Settings.EnableRetroAchievementsView && PluginDatabase.PluginSettings.Settings.EnableRetroAchievements && isRetroAchievements)
                    {
                        PART_GraphicBySource.Visibility = Visibility.Collapsed;
                        Grid.SetColumn(PART_GraphicAllUnlocked, 0);
                        Grid.SetColumnSpan(PART_GraphicAllUnlocked, 3);
                    }


                    successViewData.ProgressionGlobalCountValue = ProgressionGlobal.Unlocked;
                    successViewData.ProgressionGlobalCountMax = ProgressionGlobal.Total;
                    successViewData.ProgressionGlobal = ProgressionGlobal.Progression + "%";

                    successViewData.ProgressionLaunchedCountValue = ProgressionLaunched.Unlocked;
                    successViewData.ProgressionLaunchedCountMax = ProgressionLaunched.Total;
                    successViewData.ProgressionLaunched = ProgressionLaunched.Progression + "%";


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
                }));
            });


            if (!PluginDatabase.PluginSettings.Settings.DisplayChart)
            {
                PART_Chart1.Visibility = Visibility.Collapsed;
                Grid.SetRow(PART_PluginListContener, 2);

                PART_GraphicBySource.Visibility = Visibility.Collapsed;
                PART_GraphicAllUnlocked.Visibility = Visibility.Collapsed;
                Grid.SetRowSpan(PART_PluginListContener, 5);
                Grid.SetRowSpan(PART_GridContenerLv, 5);
            }
        }

        private void SetGraphicsAchievementsSources()
        {
            var data = PluginDatabase.GetCountBySources();

            this.Dispatcher.BeginInvoke((Action)delegate
            {
                //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
                var customerVmMapper = Mappers.Xy<CustomerForSingle>()
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
        public void GetListGame()
        {
            try
            {
                string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                ListGames = PluginDatabase.Database.Where(x => x.HasAchievements && !x.IsDeleted)
                                .Select(x => new ListViewGames
                                {
                                    Icon100Percent = x.Is100Percent ? Path.Combine(pluginFolder, "Resources\\badge.png") : string.Empty,
                                    Id = x.Id.ToString(),
                                    Name = x.Name,
                                    Icon = !x.Icon.IsNullOrEmpty() ? PluginDatabase.PlayniteApi.Database.GetFullFilePath(x.Icon) : string.Empty,
                                    LastActivity = x.LastActivity?.ToLocalTime(),
                                    SourceName = PlayniteTools.GetSourceName(x.Id),
                                    SourceIcon = TransformIcon.Get(PlayniteTools.GetSourceName(x.Id)),
                                    ProgressionValue = x.Progression,
                                    Total = x.Total,
                                    TotalPercent = x.Progression + "%",
                                    Unlocked = x.Unlocked,
                                    IsManual = x.IsManual,

                                    FirstUnlock = x.FirstUnlock,
                                    LastUnlock = x.LastUnlock,
                                    DatesUnlock = x.DatesUnlock,

                                    Common = x.Common,
                                    NoCommon = x.NoCommon,
                                    Rare = x.Rare,
                                    UltraRare = x.UltraRare
                                }).ToObservable();
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
                GraphicTitle.Content = resources.GetString("LOCSuccessStoryGraphicTitleDay");

                Guid GameId = Guid.Parse(GameSelected.Id);
                successViewData.GameContext = PluginDatabase.PlayniteApi.Database.Games.Get(GameId);
            }
            else
            {
                successViewData.GameContext = null;
            }
        }


        #region Filter
        private void Filter()
        {
            double Min = PART_FilterRange.LowerValue;
            double Max = PART_FilterRange.UpperValue;

            DateTime dateStart = default(DateTime);
            DateTime dateEnd = default(DateTime);
            if (!PART_TextDate.Text.IsNullOrEmpty())
            {
                dateStart = (DateTime)PART_DatePicker.SelectedDate;
                dateEnd = new DateTime(dateStart.Year, dateStart.Month, DateTime.DaysInMonth(dateStart.Year, dateStart.Month));
            }

            bool IsManual = false;
            if (SearchSources.Contains(resources.GetString("LOCSuccessStoryManualAchievements")))
            {
                IsManual = true;
                SearchSources.Remove(resources.GetString("LOCSuccessStoryManualAchievements"));
            }

            successViewData.ListGames = ListGames.Where(x => CheckData(x, Min, Max, dateStart, dateEnd, IsManual)).Distinct().ToObservable();

            successViewData.TotalFoundCount = successViewData.ListGames.Count;
            ListviewGames.Sorting();
            ListviewGames.SelectedIndex = -1;

            PART_TotalCommun.Content = successViewData.ListGames.Select(x => x.Common.UnLocked).Sum();
            PART_TotalNoCommun.Content = successViewData.ListGames.Select(x => x.NoCommon.UnLocked).Sum();
            PART_TotalRare.Content = successViewData.ListGames.Select(x => x.Rare.UnLocked).Sum();
            PART_TotalUltraRare.Content = successViewData.ListGames.Select(x => x.UltraRare.UnLocked).Sum();
        }

        private bool CheckData(ListViewGames listViewGames, double Min, double Max, DateTime dateStart, DateTime dateEnd, bool IsManual)
        {
            bool aa = listViewGames.ProgressionValue >= Min;
            bool bb = listViewGames.ProgressionValue <= Max;
            bool cc = !TextboxSearch.Text.IsNullOrEmpty() ? listViewGames.Name.RemoveDiacritics().Contains(TextboxSearch.Text.RemoveDiacritics(), StringComparison.InvariantCultureIgnoreCase) : true;
            bool dd = !PART_TextDate.Text.IsNullOrEmpty() ? listViewGames.DatesUnlock.Any(y => y >= dateStart && y <= dateEnd) : true;
            bool ee = SearchSources.Count != 0 ? SearchSources.Contains(listViewGames.SourceName, StringComparer.InvariantCultureIgnoreCase) : true;
            bool gg = IsManual ? listViewGames.IsManual : true;

            bool ff = aa && bb && cc && dd && ee && gg;
            return ff;
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
                SearchSources.Remove((string)sender.Tag);
            }

            if (SearchSources.Count != 0)
            {
                FilterSource.Text = String.Join(", ", SearchSources);
            }

            Filter();
        }

        private void RangeSlider_ValueChanged(object sender, RoutedEventArgs e)
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
    }


    public class SuccessViewData : ObservableObject
    {
        private ObservableCollection<ListViewGames> _ListGames = new ObservableCollection<ListViewGames>();
        public ObservableCollection<ListViewGames> ListGames
        {
            get => _ListGames;
            set
            {
                _ListGames = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<ListSource> _FilterSourceItems = new ObservableCollection<ListSource>();
        public ObservableCollection<ListSource> FilterSourceItems
        {
            get => _FilterSourceItems;
            set
            {
                _FilterSourceItems = value;
                OnPropertyChanged();
            }
        }

        private int _TotalFoundCount = 100;
        public int TotalFoundCount
        {
            get => _TotalFoundCount;
            set
            {
                _TotalFoundCount = value;
                OnPropertyChanged();
            }
        }

        private int _ProgressionGlobalCountValue = 20;
        public int ProgressionGlobalCountValue
        {
            get => _ProgressionGlobalCountValue;
            set
            {
                _ProgressionGlobalCountValue = value;
                OnPropertyChanged();
            }
        }

        private int _ProgressionGlobalCountMax= 100;
        public int ProgressionGlobalCountMax
        {
            get => _ProgressionGlobalCountMax;
            set
            {
                _ProgressionGlobalCountMax = value;
                OnPropertyChanged();
            }
        }

        private string _ProgressionGlobal = "20%";
        public string ProgressionGlobal
        {
            get => _ProgressionGlobal;
            set
            {
                _ProgressionGlobal = value;
                OnPropertyChanged();
            }
        }

        private int _ProgressionLaunchedCountValue = 40;
        public int ProgressionLaunchedCountValue
        {
            get => _ProgressionLaunchedCountValue;
            set
            {
                _ProgressionLaunchedCountValue = value;
                OnPropertyChanged();
            }
        }

        private int _ProgressionLaunchedCountMax = 100;
        public int ProgressionLaunchedCountMax
        {
            get => _ProgressionLaunchedCountMax;
            set
            {
                _ProgressionLaunchedCountMax = value;
                OnPropertyChanged();
            }
        }

        private string _ProgressionLaunched = "40%";
        public string ProgressionLaunched
        {
            get => _ProgressionLaunched;
            set
            {
                _ProgressionLaunched = value;
                OnPropertyChanged();
            }
        }

        private Game _GameContext;
        public Game GameContext
        {
            get => _GameContext;
            set
            {
                _GameContext = value;
                OnPropertyChanged();
            }
        }

        private SuccessStorySettings _Settings;
        public SuccessStorySettings Settings
        {
            get => _Settings;
            set
            {
                _Settings = value;
                OnPropertyChanged();
            }
        }
    }


    public class ListSource
    {
        public string SourceName { get; set; }
        public string SourceNameShort { get; set; }
        public bool IsCheck { get; set; }
    }
}
