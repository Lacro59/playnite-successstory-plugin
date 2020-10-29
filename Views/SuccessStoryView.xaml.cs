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
using Playnite.SDK.Models;
using PluginCommon;
using PluginCommon.LiveChartsCommon;
using SuccessStory.Database;
using SuccessStory.Models;
using SuccessStory.Views.Interface;
using System.Threading.Tasks;

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

        private readonly string PluginUserDataPath;
        private SuccessStory plugin { get; set; }
        private SuccessStorySettings settings { get; set; }

        private AchievementsDatabase AchievementsDb;
        private List<ListSource> FilterSourceItems = new List<ListSource>();
        private List<ListViewGames> ListGames = new List<ListViewGames>();
        private List<string> SearchSources = new List<string>();


        public SuccessView(SuccessStory plugin, SuccessStorySettings settings, IPlayniteAPI PlayniteApi, string PluginUserDataPath, bool isRetroAchievements = false, Game GameSelected = null)
        {
            this.plugin = plugin;
            this._PlayniteApi = PlayniteApi;
            _PlayniteApiDatabase = PlayniteApi.Database;
            _PlayniteApiPaths = PlayniteApi.Paths;
            this.settings = settings;
            this.PluginUserDataPath = PluginUserDataPath;


            InitializeComponent();


            PART_DataLoad.Visibility = Visibility.Visible;
            PART_Data.Visibility = Visibility.Hidden;

            var TaskView = Task.Run(() =>
            {
                AchievementsDb = new AchievementsDatabase(plugin, PlayniteApi, settings, PluginUserDataPath, isRetroAchievements);
                AchievementsDb.Initialize(false);

                SetGraphicsAchievementsSources();
                GetListGame();

                AchievementsGraphicsDataCount GraphicsData = null;
                if (settings.GraphicAllUnlockedByMonth)
                {
                    GraphicsData = AchievementsDb.GetCountByMonth(null, 8);
                }
                else
                {
                    GraphicsData = AchievementsDb.GetCountByDay();
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Block hidden column.
                    lvProgressionValue.IsEnabled = false;
                    lvSourceName.IsEnabled = false;


                    pbProgressionGlobalCount.Value = AchievementsDb.Progession().Unlocked;
                    pbProgressionGlobalCount.Maximum = AchievementsDb.Progession().Total;
                    labelProgressionGlobalCount.Content = AchievementsDb.Progession().Progression + "%";

                    pbProgressionLaunchedCount.Value = AchievementsDb.ProgessionLaunched().Unlocked;
                    pbProgressionLaunchedCount.Maximum = AchievementsDb.ProgessionLaunched().Total;
                    labelProgressionLaunchedCount.Content = AchievementsDb.ProgessionLaunched().Progression + "%";


                    GraphicTitle.Content = string.Empty;


                    // lvGames options
                    if (!settings.lvGamesIcon100Percent)
                    {
                        lvGameIcon100Percent.Width = 0;
                    }
                    if (!settings.lvGamesIcon)
                    {
                        lvGameIcon.Width = 0;
                    }
                    if (!settings.lvGamesName)
                    {
                        lvGameName.Width = 0;
                    }
                    if (!settings.lvGamesLastSession)
                    {
                        lvGameLastActivity.Width = 0;
                    }
                    if (!settings.lvGamesSource)
                    {
                        lvGamesSource.Width = 0;
                    }
                    if (!settings.lvGamesProgression)
                    {
                        lvGameProgression.Width = 0;
                    }


                    if (settings.GraphicAllUnlockedByMonth)
                    {
                        GraphicTitleALL.Content = resources.GetString("LOCSucessStoryGraphicTitleALL");
                    }
                    else
                    {
                        GraphicTitleALL.Content = resources.GetString("LOCSucessStoryGraphicTitleALLDay");
                    }
                    string[] StatsGraphicsAchievementsLabels = GraphicsData.Labels;
                    SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection();
                    StatsGraphicAchievementsSeries.Add(new LineSeries
                    {
                        Title = string.Empty,
                        Values = GraphicsData.Series
                    });

                    SuccessStory_Achievements_Graphics.Children.Clear();
                    settings.IgnoreSettings = true;
                    SuccessStory_Achievements_Graphics.Children.Add(new SuccessStoryAchievementsGraphics(StatsGraphicAchievementsSeries, StatsGraphicsAchievementsLabels, settings));
                    SuccessStory_Achievements_Graphics.UpdateLayout();

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

                    if (settings.EnableRetroAchievementsView && settings.EnableRetroAchievements)
                    {
                        if (isRetroAchievements)
                        {
                            PART_GraphicBySource.Visibility = Visibility.Collapsed;
                            Grid.SetColumn(PART_GraphicAllUnlocked, 0);
                            Grid.SetColumnSpan(PART_GraphicAllUnlocked, 3);

                            if (settings.EnableRetroAchievements)
                            {
                                icon = TransformIcon.Get("RetroAchievements") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "RetroAchievements", SourceNameShort = "RetroAchievements", IsCheck = false });
                            }
                        }
                        else
                        {
                            if (settings.EnableLocal)
                            {
                                icon = TransformIcon.Get("Playnite") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Playnite", SourceNameShort = "Playnite", IsCheck = false });
                            }
                            if (settings.EnableSteam)
                            {
                                icon = TransformIcon.Get("Steam") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Steam", SourceNameShort = "Steam", IsCheck = false });
                            }
                            if (settings.EnableGog)
                            {
                                icon = TransformIcon.Get("GOG") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "GOG", SourceNameShort = "GOG", IsCheck = false });
                            }
                            if (settings.EnableOrigin)
                            {
                                icon = TransformIcon.Get("Origin") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Origin", SourceNameShort = "Origin", IsCheck = false });
                            }
                            if (settings.EnableXbox)
                            {
                                icon = TransformIcon.Get("Xbox") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Xbox", SourceNameShort = "Xbox", IsCheck = false });
                            }
                        }
                    }
                    else
                    {
                        if (settings.EnableLocal)
                        {
                            icon = TransformIcon.Get("Playnite") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Playnite", SourceNameShort = "Playnite", IsCheck = false });
                        }
                        if (settings.EnableSteam)
                        {
                            icon = TransformIcon.Get("Steam") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Steam", SourceNameShort = "Steam", IsCheck = false });
                        }
                        if (settings.EnableGog)
                        {
                            icon = TransformIcon.Get("GOG") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "GOG", SourceNameShort = "GOG", IsCheck = false });
                        }
                        if (settings.EnableOrigin)
                        {
                            icon = TransformIcon.Get("Origin") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Origin", SourceNameShort = "Origin", IsCheck = false });
                        }
                        if (settings.EnableXbox)
                        {
                            icon = TransformIcon.Get("Xbox") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Xbox", SourceNameShort = "Xbox", IsCheck = false });
                        }
                        if (settings.EnableRetroAchievements)
                        {
                            icon = TransformIcon.Get("RetroAchievements") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "RetroAchievements", SourceNameShort = "RetroAchievements", IsCheck = false });
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
            var data = AchievementsDb.GetCountBySources();

            Application.Current.Dispatcher.Invoke(() =>
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
                    var dataGameAchievements = AchievementsDb.gameAchievements.Where(x => x.Value.HaveAchivements);
                    foreach (var item in dataGameAchievements)
                    {
                        Game game = _PlayniteApiDatabase.Games.Get(item.Key);
                        string SourceName = PlayniteTools.GetSourceName(game, _PlayniteApi);

                        if (AchievementsDatabase.VerifToAddOrShow(plugin, _PlayniteApi, SourceName, settings, PluginUserDataPath))
                        {
                            string GameId = game.Id.ToString();
                            string GameName = game.Name;
                            string GameIcon;
                            DateTime? GameLastActivity = null;

                            GameAchievements GameAchievements = AchievementsDb.Get(game.Id);

                            if (game.LastActivity != null)
                            {
                                GameLastActivity = ((DateTime)game.LastActivity).ToLocalTime();
                            }

                            BitmapImage iconImage = new BitmapImage();
                            if (!game.Icon.IsNullOrEmpty())
                            {
                                iconImage.BeginInit();
                                GameIcon = _PlayniteApiDatabase.GetFullFilePath(game.Icon);
                                iconImage.UriSource = new Uri(GameIcon, UriKind.RelativeOrAbsolute);
                                iconImage.EndInit();
                            }

                            BitmapImage Icon100Percent = new BitmapImage();
                            if (GameAchievements.Is100Percent)
                            {
                                Icon100Percent.BeginInit();
                                string Icon100 = Path.Combine(pluginFolder, "Resources\\badge.png");
                                Icon100Percent.UriSource = new Uri(Icon100, UriKind.RelativeOrAbsolute);
                                Icon100Percent.EndInit();
                            }

                            ListGames.Add(new ListViewGames()
                            {
                                Icon100Percent = Icon100Percent,
                                Id = GameId,
                                Name = GameName,
                                Icon = iconImage,
                                LastActivity = GameLastActivity,
                                SourceName = SourceName,
                                SourceIcon = TransformIcon.Get(SourceName),
                                ProgressionValue = GameAchievements.Progression,
                                Total = GameAchievements.Total,
                                TotalPercent = GameAchievements.Progression + "%",
                                Unlocked = GameAchievements.Unlocked
                            });

                            iconImage = null;
                        }
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ListviewGames.ItemsSource = ListGames;

                    Sorting();
                });
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", "Errorn on GetListGames()");
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

                GameAchievements GameAchievements = AchievementsDb.Get(GameId);
                List<Achievements> ListAchievements = GameAchievements.Achievements;

                SuccessStory_Achievements_List.Children.Clear();
                SuccessStory_Achievements_List.Children.Add(new SuccessStoryAchievementsList(ListAchievements, false, settings.EnableRaretyIndicator));
                SuccessStory_Achievements_List.UpdateLayout();


                AchievementsGraphicsDataCount GraphicsData = null;
                if (settings.GraphicAllUnlockedByDay)
                {
                    GraphicTitle.Content = resources.GetString("LOCSucessStoryGraphicTitle");
                    GraphicsData = AchievementsDb.GetCountByMonth(GameId);
                }
                else
                {
                    GraphicTitle.Content = resources.GetString("LOCSucessStoryGraphicTitleDay");
                    GraphicsData = AchievementsDb.GetCountByDay(GameId, 8);
                }
                string[] StatsGraphicsAchievementsLabels = GraphicsData.Labels;
                SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection();
                StatsGraphicAchievementsSeries.Add(new LineSeries
                {
                    Title = string.Empty,
                    Values = GraphicsData.Series
                });

                SuccessStory_Achievements_Graphics_Game.Children.Clear();
                settings.IgnoreSettings = true;
                SuccessStory_Achievements_Graphics_Game.Children.Add(new SuccessStoryAchievementsGraphics(StatsGraphicAchievementsSeries, StatsGraphicsAchievementsLabels, settings));
                SuccessStory_Achievements_Graphics_Game.UpdateLayout();

                GC.Collect();
            }
        }


        #region Functions sorting ListviewGames.
        private void Sorting()
        {
            // Sorting
            try
            {
                var columnBinding = _lastHeaderClicked.Column.DisplayMemberBinding as Binding;
                var sortBy = columnBinding?.Path.Path ?? _lastHeaderClicked.Column.Header as string;

                // Specific sort with another column
                if (_lastHeaderClicked.Name == "lvSourceIcon")
                {
                    columnBinding = lvSourceName.Column.DisplayMemberBinding as Binding;
                    sortBy = columnBinding?.Path.Path ?? _lastHeaderClicked.Column.Header as string;
                }
                if (_lastHeaderClicked.Name == "lvProgression")
                {
                    columnBinding = lvProgressionValue.Column.DisplayMemberBinding as Binding;
                    sortBy = columnBinding?.Path.Path ?? _lastHeaderClicked.Column.Header as string;
                }
                Sort(sortBy, _lastDirection);
            }
            // If first view
            catch
            {
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ListviewGames.ItemsSource);

                switch (settings.NameSorting)
                {
                    case ("Name"):
                        _lastHeaderClicked = lvName;
                        if (settings.IsAsc)
                        {
                            _lastHeaderClicked.Content += " ▲";
                        }
                        else
                        {
                            _lastHeaderClicked.Content += " ▼";
                        }
                        break;
                    case ("LastActivity"):
                        _lastHeaderClicked = lvLastActivity;
                        if (settings.IsAsc)
                        {
                            _lastHeaderClicked.Content += " ▲";
                        }
                        else
                        {
                            _lastHeaderClicked.Content += " ▼";
                        }
                        break;
                    case ("SourceName"):
                        _lastHeaderClicked = lvSourceIcon;
                        if (settings.IsAsc)
                        {
                            lvSourceIcon.Content += " ▲";
                        }
                        else
                        {
                            lvSourceIcon.Content += " ▼";
                        }
                        break;
                    case ("ProgressionValue"):
                        _lastHeaderClicked = lvProgression;
                        if (settings.IsAsc)
                        {
                            lvProgression.Content += " ▲";
                        }
                        else
                        {
                            lvProgression.Content += " ▼";
                        }
                        break;
                }


                if (settings.IsAsc)
                {
                    _lastDirection = ListSortDirection.Ascending;
                }
                else
                {
                    _lastDirection = ListSortDirection.Descending;
                }


                view.SortDescriptions.Add(new SortDescription(settings.NameSorting, _lastDirection));
            }
        }


        private GridViewColumnHeader _lastHeaderClicked = null;

        private ListSortDirection _lastDirection ;

        private void ListviewGames_onHeaderClick(object sender, RoutedEventArgs e)
        {
            try
            {
                lvProgressionValue.IsEnabled = true;
                lvSourceName.IsEnabled = true;

                var headerClicked = e.OriginalSource as GridViewColumnHeader;
                ListSortDirection direction;

                // No sort
                if (headerClicked.Name == "lvGameIcon100Percent")
                {
                    headerClicked = null;
                }
                if (headerClicked.Name == "lvGameIcon")
                {
                    headerClicked = null;
                }

                if (headerClicked != null)
                {
                    if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                    {
                        if (headerClicked != _lastHeaderClicked)
                        {
                            direction = ListSortDirection.Ascending;
                        }
                        else
                        {
                            if (_lastDirection == ListSortDirection.Ascending)
                            {
                                direction = ListSortDirection.Descending;
                            }
                            else
                            {
                                direction = ListSortDirection.Ascending;
                            }
                        }

                        var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                        var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                        // Specific sort with another column
                        if (headerClicked.Name == "lvSourceIcon")
                        {
                            columnBinding = lvSourceName.Column.DisplayMemberBinding as Binding;
                            sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                        }
                        if (headerClicked.Name == "lvProgression")
                        {
                            columnBinding = lvProgressionValue.Column.DisplayMemberBinding as Binding;
                            sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                        }


                        Sort(sortBy, direction);

                        if (_lastHeaderClicked != null)
                        {
                            _lastHeaderClicked.Content = ((string)_lastHeaderClicked.Content).Replace(" ▲", string.Empty);
                            _lastHeaderClicked.Content = ((string)_lastHeaderClicked.Content).Replace(" ▼", string.Empty);
                        }

                        if (direction == ListSortDirection.Ascending)
                        {
                            headerClicked.Content += " ▲";
                        }
                        else
                        {
                            headerClicked.Content += " ▼";
                        }

                        // Remove arrow from previously sorted header
                        if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                        {
                            _lastHeaderClicked.Column.HeaderTemplate = null;
                        }

                        _lastHeaderClicked = headerClicked;
                        _lastDirection = direction;
                    }
                }

                lvProgressionValue.IsEnabled = false;
                lvSourceName.IsEnabled = false;
            }
            catch
            {

            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(ListviewGames.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }
        #endregion


        //protected override void OnClosing(CancelEventArgs e)
        //{
        //    AchievementsDb = null;
        //    _PlayniteApi = null;
        //    _PlayniteApiDatabase = null;
        //    _PlayniteApiPaths = null;
        //
        //    ListviewGames.ItemsSource = null;
        //    ListviewGames.UpdateLayout();
        //
        //    SuccessStory_Achievements_List.Children.Clear();
        //    SuccessStory_Achievements_List.UpdateLayout();
        //    SuccessStory_Achievements_Graphics_Game.Children.Clear();
        //    SuccessStory_Achievements_Graphics_Game.UpdateLayout();
        //
        //    GC.Collect();
        //}


        #region Filter
        private void Filter()
        {
            // Filter
            if (!TextboxSearch.Text.IsNullOrEmpty() && SearchSources.Count != 0)
            {
                ListviewGames.ItemsSource = ListGames.FindAll(
                    x => x.Name.ToLower().IndexOf(TextboxSearch.Text) > -1 && SearchSources.Contains(x.SourceName)
                );
                Sorting();
                return;
            }

            if (!TextboxSearch.Text.IsNullOrEmpty())
            {
                ListviewGames.ItemsSource = ListGames.FindAll(
                    x => x.Name.ToLower().IndexOf(TextboxSearch.Text) > -1
                );
                Sorting();
                return;
            }

            if (SearchSources.Count != 0)
            {
                ListviewGames.ItemsSource = ListGames.FindAll(
                    x => SearchSources.Contains(x.SourceName)
                );
                Sorting();
                return;
            }

            ListviewGames.ItemsSource = ListGames;
            Sorting();
        }


        /// <summary>
        /// Function search game by name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextboxSearch_KeyUp(object sender, RoutedEventArgs e)
        {
            //GetListGame();
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

            //GetListGame();
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
