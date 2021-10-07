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

        private string NameSorting { get; set; }
        

        public SuccessView(bool isRetroAchievements = false, Game GameSelected = null)
        {
            InitializeComponent();
            DataContext = successViewData;


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


            PART_DataLoad.Visibility = Visibility.Visible;
            PART_Data.Visibility = Visibility.Hidden;


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

                GraphicsData = PluginDatabase.GetCountByMonth(null, 4);
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
                        }
                        if (PluginDatabase.PluginSettings.Settings.EnableManual)
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
                    }
                    if (PluginDatabase.PluginSettings.Settings.EnableManual)
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

                    FilterSourceItems = FilterSourceItems.OrderBy(x => x.SourceName).ToObservable();
                    successViewData.FilterSourceItems = FilterSourceItems;

                    successViewData.ListGames = ListGames;
                    successViewData.TotalFoundCount = ListGames.Count;
                    ListviewGames.Sorting();

                    PART_TotalCommun.Content = successViewData.ListGames.Select(x => x.Common.UnLocked).Sum();
                    PART_TotalNoCommun.Content = successViewData.ListGames.Select(x => x.NoCommon.UnLocked).Sum();
                    PART_TotalRare.Content = successViewData.ListGames.Select(x => x.Rare.UnLocked).Sum();


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


                    PART_DataLoad.Visibility = Visibility.Hidden;
                    PART_Data.Visibility = Visibility.Visible;
                }));
            });
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

                ListGames = PluginDatabase.Database.Where(x => x.HasAchivements && !x.IsDeleted)
                                .Select(x => new ListViewGames
                                {
                                    Icon100Percent = x.Is100Percent ? Path.Combine(pluginFolder, "Resources\\badge.png") : string.Empty,
                                    Id = x.Id.ToString(),
                                    Name = x.Name,
                                    Icon = !x.Icon.IsNullOrEmpty() ? PluginDatabase.PlayniteApi.Database.GetFullFilePath(x.Icon) : string.Empty,
                                    LastActivity = x.LastActivity?.ToLocalTime(),
                                    SourceName = PlayniteTools.GetSourceName(PluginDatabase.PlayniteApi, x.Id),
                                    SourceIcon = TransformIcon.Get(PlayniteTools.GetSourceName(PluginDatabase.PlayniteApi, x.Id)),
                                    ProgressionValue = x.Progression,
                                    Total = x.Total,
                                    TotalPercent = x.Progression + "%",
                                    Unlocked = x.Unlocked,
                                    IsManual = x.IsManual,

                                    Common = x.Common,
                                    NoCommon = x.NoCommon,
                                    Rare = x.Rare
                                }).ToObservable();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
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
        }


        #region Filter
        private void Filter()
        {
            ObservableCollection<ListViewGames> SourcesManual = new ObservableCollection<ListViewGames>();
            if (SearchSources.Contains(resources.GetString("LOCSuccessStoryManualAchievements")))
            {
                SourcesManual = ListGames.Where(x => x.IsManual).ToObservable();
            }


            // Filter
            if (!TextboxSearch.Text.IsNullOrEmpty() && SearchSources.Count != 0)
            {
                successViewData.ListGames = ListGames.Where(x => x.Name.Contains(TextboxSearch.Text, StringComparison.InvariantCultureIgnoreCase) && SearchSources.Contains(x.SourceName, StringComparer.InvariantCultureIgnoreCase))
                                                    .Union(SourcesManual).Distinct().ToObservable();
                successViewData.TotalFoundCount = successViewData.ListGames.Count;
                ListviewGames.Sorting();

                PART_TotalCommun.Content = successViewData.ListGames.Select(x => x.Common.UnLocked).Sum();
                PART_TotalNoCommun.Content = successViewData.ListGames.Select(x => x.NoCommon.UnLocked).Sum();
                PART_TotalRare.Content = successViewData.ListGames.Select(x => x.Rare.UnLocked).Sum();

                return;
            }

            if (!TextboxSearch.Text.IsNullOrEmpty())
            {
                successViewData.ListGames = ListGames.Where(x => x.Name.Contains(TextboxSearch.Text, StringComparison.InvariantCultureIgnoreCase)).ToObservable();
                successViewData.TotalFoundCount = successViewData.ListGames.Count;
                ListviewGames.Sorting();

                PART_TotalCommun.Content = successViewData.ListGames.Select(x => x.Common.UnLocked).Sum();
                PART_TotalNoCommun.Content = successViewData.ListGames.Select(x => x.NoCommon.UnLocked).Sum();
                PART_TotalRare.Content = successViewData.ListGames.Select(x => x.Rare.UnLocked).Sum();

                return;
            }

            if (SearchSources.Count != 0)
            {
                successViewData.ListGames = ListGames.Where(x => SearchSources.Contains(x.SourceName, StringComparer.InvariantCultureIgnoreCase)).Union(SourcesManual).Distinct().ToObservable();
                successViewData.TotalFoundCount = successViewData.ListGames.Count;
                ListviewGames.Sorting();

                PART_TotalCommun.Content = successViewData.ListGames.Select(x => x.Common.UnLocked).Sum();
                PART_TotalNoCommun.Content = successViewData.ListGames.Select(x => x.NoCommon.UnLocked).Sum();
                PART_TotalRare.Content = successViewData.ListGames.Select(x => x.Rare.UnLocked).Sum();

                return;
            }

            successViewData.ListGames = ListGames;
            successViewData.TotalFoundCount = successViewData.ListGames.Count;
            ListviewGames.Sorting();

            PART_TotalCommun.Content = successViewData.ListGames.Select(x => x.Common.UnLocked).Sum();
            PART_TotalNoCommun.Content = successViewData.ListGames.Select(x => x.NoCommon.UnLocked).Sum();
            PART_TotalRare.Content = successViewData.ListGames.Select(x => x.Rare.UnLocked).Sum();
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
    }


    public class ListSource
    {
        public string SourceName { get; set; }
        public string SourceNameShort { get; set; }
        public bool IsCheck { get; set; }
    }
}
