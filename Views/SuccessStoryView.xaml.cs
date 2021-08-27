using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPluginsShared;
using SuccessStory.Models;
using SuccessStory.Views.Interface;
using System.Threading.Tasks;
using SuccessStory.Services;
using CommonPluginsControls.LiveChartsCommon;
using SuccessStory.Controls;
using System.Windows.Threading;
using System.Threading;

namespace SuccessStory
{
    /// <summary>
    /// Logique d'interaction pour SuccessView.xaml
    /// </summary>
    public partial class SuccessView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        // Variables api.
        private IPlayniteAPI _PlayniteApi;
        private IGameDatabaseAPI _PlayniteApiDatabase;
        private IPlaynitePathsAPI _PlayniteApiPaths;

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        private readonly string _PluginUserDataPath;
        private SuccessStory _plugin { get; set; }
        
        private List<ListSource> FilterSourceItems = new List<ListSource>();
        private List<ListViewGames> ListGames = new List<ListViewGames>();
        private List<string> SearchSources = new List<string>();

        private PluginChart pluginChart;
        private PluginList pluginList;

        private string NameSorting { get; set; }


        public SuccessView(SuccessStory plugin, IPlayniteAPI PlayniteApi, string PluginUserDataPath, bool isRetroAchievements = false, Game GameSelected = null)
        {
            _plugin = plugin;
            _PlayniteApi = PlayniteApi;
            _PlayniteApiDatabase = PlayniteApi.Database;
            _PlayniteApiPaths = PlayniteApi.Paths;
            _PluginUserDataPath = PluginUserDataPath;


            InitializeComponent();


            ListviewGames.SortingDefaultDataName = PluginDatabase.PluginSettings.Settings.NameSorting;
            ListviewGames.SortingSortDirection = (PluginDatabase.PluginSettings.Settings.IsAsc) ? ListSortDirection.Ascending : ListSortDirection.Descending;
            ListviewGames.Sorting();


            PART_DataLoad.Visibility = Visibility.Visible;
            PART_Data.Visibility = Visibility.Hidden;

            var TaskView = Task.Run(() =>
            {
                GetListGame();
                SetGraphicsAchievementsSources();

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    var ProgressionGlobal = PluginDatabase.Progession();
                    pbProgressionGlobalCount.Value = ProgressionGlobal.Unlocked;
                    pbProgressionGlobalCount.Maximum = ProgressionGlobal.Total;
                    labelProgressionGlobalCount.Content = ProgressionGlobal.Progression + "%";

                    var ProgressionLaunched = PluginDatabase.ProgessionLaunched();
                    pbProgressionLaunchedCount.Value = ProgressionLaunched.Unlocked;
                    pbProgressionLaunchedCount.Maximum = ProgressionLaunched.Total;
                    labelProgressionLaunchedCount.Content = ProgressionLaunched.Progression + "%";


                    GraphicTitle.Content = string.Empty;


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

                    GraphicTitleALL.Content = resources.GetString("LOCSuccessStoryGraphicTitleALL");
                    AchievementsGraphicsDataCount GraphicsData = PluginDatabase.GetCountByMonth(null, 4);

                    string[] StatsGraphicsAchievementsLabels = GraphicsData.Labels;
                    SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection();
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
                        for (int i = 0; i < ListviewGames.Items.Count; i++)
                        {
                            if (((ListViewGames)ListviewGames.Items[i]).Name == GameSelected.Name)
                            {
                                ListviewGames.SelectedIndex = i;
                            }
                        }
                    }
                    ListviewGames.ScrollIntoView(ListviewGames.SelectedItem);

                    string icon = string.Empty;

                    if (PluginDatabase.PluginSettings.Settings.EnableRetroAchievementsView && PluginDatabase.PluginSettings.Settings.EnableRetroAchievements)
                    {
                        if (isRetroAchievements)
                        {
                            PART_GraphicBySource.Visibility = Visibility.Collapsed;
                            Grid.SetColumn(PART_GraphicAllUnlocked, 0);
                            Grid.SetColumnSpan(PART_GraphicAllUnlocked, 3);

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
                    }

                    FilterSource.ItemsSource = FilterSourceItems;


                    // Set Binding data
                    DataContext = this;

                    PART_DataLoad.Visibility = Visibility.Hidden;
                    PART_Data.Visibility = Visibility.Visible;
                });
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
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            try
            {
                if (ListGames.Count == 0)
                {
                    var dataGameAchievements = PluginDatabase.Database.Where(x => x.HaveAchivements && x.IsDeleted == false);
                    foreach (GameAchievements item in dataGameAchievements)
                    {
                        string SourceName = PlayniteTools.GetSourceName(_PlayniteApi, item.Id);

                        string GameId = item.Id.ToString();
                        string GameName = item.Name;
                        string GameIcon = string.Empty;
                        string Icon100 = string.Empty;
                        DateTime? GameLastActivity = null;

                        GameAchievements successStories = PluginDatabase.Get(item.Id);

                        if (item.LastActivity != null)
                        {
                            GameLastActivity = ((DateTime)item.LastActivity).ToLocalTime();
                        }

                        if (!item.Icon.IsNullOrEmpty())
                        {
                            GameIcon = _PlayniteApiDatabase.GetFullFilePath(item.Icon);
                        }
                            
                        if (successStories.Is100Percent)
                        {
                            Icon100 = Path.Combine(pluginFolder, "Resources\\badge.png");
                        }

                        ListGames.Add(new ListViewGames()
                        {
                            Icon100Percent = Icon100,
                            Id = GameId,
                            Name = GameName,
                            Icon = GameIcon,
                            LastActivity = GameLastActivity,
                            SourceName = SourceName,
                            SourceIcon = TransformIcon.Get(SourceName),
                            ProgressionValue = successStories.Progression,
                            Total = successStories.Total,
                            TotalPercent = successStories.Progression + "%",
                            Unlocked = successStories.Unlocked,
                            IsManual = successStories.IsManual,

                            Common = successStories.Common,
                            NoCommon = successStories.NoCommon,
                            Rare = successStories.Rare
                        });
                    }

                    Common.LogDebug(true, $"ListGames: {Serialization.ToJson(ListGames)}");
                }

                Application.Current.Dispatcher.BeginInvoke((Action)delegate
                {
                    ListviewGames.ItemsSource = ListGames;
                    PART_TotalFoundCount.Text = ((List<ListViewGames>)ListviewGames.ItemsSource).Count.ToString();
                    ListviewGames.Sorting();
                });
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
                listAchievementBorder.BorderThickness = new Thickness(0);

                Guid GameId = Guid.Parse(GameSelected.Id);


                if (pluginList == null)
                {
                    pluginList = new PluginList
                    {
                        IgnoreSettings = true,
                        ForceOneCol = true,
                        Height = SuccessStory_Achievements_List.ActualHeight,
                    };
                    SuccessStory_Achievements_List.Children.Add(pluginList);
                }

                pluginList.GameContext = PluginDatabase.PlayniteApi.Database.Games.Get(GameId);


                GraphicTitle.Content = resources.GetString("LOCSuccessStoryGraphicTitleDay");

                if (pluginChart == null)
                {
                    pluginChart = new PluginChart
                    {
                        IgnoreSettings = true,
                        Height = SuccessStory_Achievements_Graphics_Game.Height,
                        AxisLimit = 8
                    };
                    SuccessStory_Achievements_Graphics_Game.Children.Add(pluginChart);
                }

                pluginChart.GameContext = PluginDatabase.PlayniteApi.Database.Games.Get(GameId);
            }
        }


        #region Filter
        private void Filter()
        {
            List<ListViewGames> SourcesManual = new List<ListViewGames>();

            // Filter
            if (!TextboxSearch.Text.IsNullOrEmpty() && SearchSources.Count != 0)
            {
                if (SearchSources.IndexOf(resources.GetString("LOCSuccessStoryManualAchievements")) > -1)
                {
                    SourcesManual = ListGames.FindAll(x => x.IsManual);
                }

                ListviewGames.ItemsSource = ListGames.FindAll(
                    x => x.Name.ToLower().IndexOf(TextboxSearch.Text) > -1 && SearchSources.Contains(x.SourceName)
                );

                ListviewGames.ItemsSource = ((List<ListViewGames>)ListviewGames.ItemsSource).Union(SourcesManual).ToList();

                PART_TotalFoundCount.Text = ((List<ListViewGames>)ListviewGames.ItemsSource).Count.ToString();
                ListviewGames.Sorting();
                return;
            }

            if (!TextboxSearch.Text.IsNullOrEmpty())
            {
                ListviewGames.ItemsSource = ListGames.FindAll(
                    x => x.Name.ToLower().IndexOf(TextboxSearch.Text) > -1
                );
                PART_TotalFoundCount.Text = ((List<ListViewGames>)ListviewGames.ItemsSource).Count.ToString();
                ListviewGames.Sorting();
                return;
            }

            if (SearchSources.Count != 0)
            {
                if (SearchSources.IndexOf(resources.GetString("LOCSuccessStoryManualAchievements")) > -1)
                {
                    SourcesManual = ListGames.FindAll(x => x.IsManual);
                }

                ListviewGames.ItemsSource = ListGames.FindAll(
                    x => SearchSources.Contains(x.SourceName)
                );

                ListviewGames.ItemsSource = ((List<ListViewGames>)ListviewGames.ItemsSource).Union(SourcesManual).ToList();

                PART_TotalFoundCount.Text = ((List<ListViewGames>)ListviewGames.ItemsSource).Count.ToString();
                ListviewGames.Sorting();
                return;
            }

            ListviewGames.ItemsSource = ListGames;
            PART_TotalFoundCount.Text = ((List<ListViewGames>)ListviewGames.ItemsSource).Count.ToString();
            ListviewGames.Sorting();
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


    public class ListSource
    {
        public string SourceName { get; set; }
        public string SourceNameShort { get; set; }
        public bool IsCheck { get; set; }
    }
}
