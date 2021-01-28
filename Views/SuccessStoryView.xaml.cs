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
using CommonPluginsShared;
using SuccessStory.Models;
using SuccessStory.Views.Interface;
using System.Threading.Tasks;
using SuccessStory.Services;
using Newtonsoft.Json;
using CommonPluginsControls.LiveChartsCommon;

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


        public SuccessView(SuccessStory plugin, IPlayniteAPI PlayniteApi, string PluginUserDataPath, bool isRetroAchievements = false, Game GameSelected = null)
        {
            _plugin = plugin;
            _PlayniteApi = PlayniteApi;
            _PlayniteApiDatabase = PlayniteApi.Database;
            _PlayniteApiPaths = PlayniteApi.Paths;
            _PluginUserDataPath = PluginUserDataPath;


            InitializeComponent();


            PART_DataLoad.Visibility = Visibility.Visible;
            PART_Data.Visibility = Visibility.Hidden;

            var TaskView = Task.Run(() =>
            {
                GetListGame();
                SetGraphicsAchievementsSources();

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    // Block hidden column.
                    lvProgressionValue.IsEnabled = false;
                    lvSourceName.IsEnabled = false;


                    pbProgressionGlobalCount.Value = PluginDatabase.Progession().Unlocked;
                    pbProgressionGlobalCount.Maximum = PluginDatabase.Progession().Total;
                    labelProgressionGlobalCount.Content = PluginDatabase.Progession().Progression + "%";

                    pbProgressionLaunchedCount.Value = PluginDatabase.ProgessionLaunched().Unlocked;
                    pbProgressionLaunchedCount.Maximum = PluginDatabase.ProgessionLaunched().Total;
                    labelProgressionLaunchedCount.Content = PluginDatabase.ProgessionLaunched().Progression + "%";


                    GraphicTitle.Content = string.Empty;


                    // lvGames options
                    if (!PluginDatabase.PluginSettings.lvGamesIcon100Percent)
                    {
                        lvGameIcon100Percent.Width = 0;
                    }
                    if (!PluginDatabase.PluginSettings.lvGamesIcon)
                    {
                        lvGameIcon.Width = 0;
                    }
                    if (!PluginDatabase.PluginSettings.lvGamesName)
                    {
                        lvGameName.Width = 0;
                    }
                    if (!PluginDatabase.PluginSettings.lvGamesLastSession)
                    {
                        lvGameLastActivity.Width = 0;
                    }
                    if (!PluginDatabase.PluginSettings.lvGamesSource)
                    {
                        lvGamesSource.Width = 0;
                    }
                    if (!PluginDatabase.PluginSettings.lvGamesProgression)
                    {
                        lvGameProgression.Width = 0;
                    }

                    int limit = 5;
                    GraphicTitleALL.Content = resources.GetString("LOCSuccessStoryGraphicTitleALL");

                    SuccessStory_Achievements_Graphics.Children.Clear();
                    PluginDatabase.PluginSettings.IgnoreSettings = true;
                    SuccessStoryAchievementsGraphics successStoryAchievementsGraphics = new SuccessStoryAchievementsGraphics();
                    successStoryAchievementsGraphics.SetScData(null, limit, true);
                    SuccessStory_Achievements_Graphics.Children.Add(successStoryAchievementsGraphics);
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

                    if (PluginDatabase.PluginSettings.EnableRetroAchievementsView && PluginDatabase.PluginSettings.EnableRetroAchievements)
                    {
                        if (isRetroAchievements)
                        {
                            PART_GraphicBySource.Visibility = Visibility.Collapsed;
                            Grid.SetColumn(PART_GraphicAllUnlocked, 0);
                            Grid.SetColumnSpan(PART_GraphicAllUnlocked, 3);

                            if (PluginDatabase.PluginSettings.EnableRetroAchievements)
                            {
                                icon = TransformIcon.Get("RetroAchievements") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "RetroAchievements", SourceNameShort = "RetroAchievements", IsCheck = false });
                            }
                        }
                        else
                        {
                            if (PluginDatabase.PluginSettings.EnableLocal)
                            {
                                icon = TransformIcon.Get("Playnite") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Playnite", SourceNameShort = "Playnite", IsCheck = false });

                                icon = TransformIcon.Get("Hacked") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Hacked", SourceNameShort = "Hacked", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.EnableSteam)
                            {
                                icon = TransformIcon.Get("Steam") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Steam", SourceNameShort = "Steam", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.EnableGog)
                            {
                                icon = TransformIcon.Get("GOG") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "GOG", SourceNameShort = "GOG", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.EnableOrigin)
                            {
                                icon = TransformIcon.Get("Origin") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Origin", SourceNameShort = "Origin", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.EnableXbox)
                            {
                                icon = TransformIcon.Get("Xbox") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Xbox", SourceNameShort = "Xbox", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.EnableRpcs3Achievements)
                            {
                                icon = TransformIcon.Get("Rpcs3") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Rpcs3", SourceNameShort = "Rpcs3", IsCheck = false });
                            }
                            if (PluginDatabase.PluginSettings.EnableManual)
                            {
                                icon = TransformIcon.Get("Manual") + " ";
                                FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Manual", SourceNameShort = "Manual", IsCheck = false });
                            }
                        }
                    }
                    else
                    {
                        if (PluginDatabase.PluginSettings.EnableLocal)
                        {
                            icon = TransformIcon.Get("Playnite") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Playnite", SourceNameShort = "Playnite", IsCheck = false });

                            icon = TransformIcon.Get("Hacked") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Hacked", SourceNameShort = "Hacked", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.EnableSteam)
                        {
                            icon = TransformIcon.Get("Steam") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Steam", SourceNameShort = "Steam", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.EnableGog)
                        {
                            icon = TransformIcon.Get("GOG") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "GOG", SourceNameShort = "GOG", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.EnableOrigin)
                        {
                            icon = TransformIcon.Get("Origin") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Origin", SourceNameShort = "Origin", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.EnableXbox)
                        {
                            icon = TransformIcon.Get("Xbox") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Xbox", SourceNameShort = "Xbox", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.EnableRetroAchievements)
                        {
                            icon = TransformIcon.Get("RetroAchievements") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "RetroAchievements", SourceNameShort = "RetroAchievements", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.EnableRpcs3Achievements)
                        {
                            icon = TransformIcon.Get("RPCS3") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "RPCS3", SourceNameShort = "Rpcs3", IsCheck = false });
                        }
                        if (PluginDatabase.PluginSettings.EnableManual)
                        {
                            icon = TransformIcon.Get("Manual") + " ";
                            FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + "Manual", SourceNameShort = "Manual", IsCheck = false });
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
                            IsManual = successStories.IsManual
                        });
                    }

#if DEBUG
                    logger.Debug($"SuccessStory [Ignored] - ListGames: {JsonConvert.SerializeObject(ListGames)}");
#endif
                }

                Application.Current.Dispatcher.BeginInvoke((Action)delegate
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

                GameAchievements successStories = PluginDatabase.Get(GameId);
                List<Achievements> ListAchievements = successStories.Items;

                SuccessStory_Achievements_List.Children.Clear();
                SuccessStoryAchievementsList successStoryAchievementsList = new SuccessStoryAchievementsList(true);
                successStoryAchievementsList.SetScData(successStories, true);
                SuccessStory_Achievements_List.Children.Add(successStoryAchievementsList);


                int limit = 0;
                if (!PluginDatabase.PluginSettings.GraphicAllUnlockedByDay)
                {
                    GraphicTitle.Content = resources.GetString("LOCSuccessStoryGraphicTitle");
                }
                else
                {
                    GraphicTitle.Content = resources.GetString("LOCSuccessStoryGraphicTitleDay");
                    limit = 8;
                }

                SuccessStory_Achievements_Graphics_Game.Children.Clear();
                PluginDatabase.PluginSettings.IgnoreSettings = true;
                SuccessStoryAchievementsGraphics successStoryAchievementsGraphics = new SuccessStoryAchievementsGraphics();
                successStoryAchievementsGraphics.SetScData(GameId, limit);
                SuccessStory_Achievements_Graphics_Game.Children.Add(successStoryAchievementsGraphics);
            }
        }


        #region Functions sorting ListviewGames.
        private void Sorting()
        {

            PART_TotalFoundCount.Text = ((List<ListViewGames>)ListviewGames.ItemsSource).Count.ToString();

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

                switch (PluginDatabase.PluginSettings.NameSorting)
                {
                    case ("Name"):
                        _lastHeaderClicked = lvName;
                        if (PluginDatabase.PluginSettings.IsAsc)
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
                        if (PluginDatabase.PluginSettings.IsAsc)
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
                        if (PluginDatabase.PluginSettings.IsAsc)
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
                        if (PluginDatabase.PluginSettings.IsAsc)
                        {
                            lvProgression.Content += " ▲";
                        }
                        else
                        {
                            lvProgression.Content += " ▼";
                        }
                        break;
                }


                if (PluginDatabase.PluginSettings.IsAsc)
                {
                    _lastDirection = ListSortDirection.Ascending;
                }
                else
                {
                    _lastDirection = ListSortDirection.Descending;
                }


                view.SortDescriptions.Add(new SortDescription(PluginDatabase.PluginSettings.NameSorting, _lastDirection));
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
                if (headerClicked.Tag is string && (string)headerClicked.Tag == "noSort")
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

            List<string> lists = new List<string>
            {
                "ProgressionValue", "SourceName", "GameIcon100Percent", "LastActivity"
            };

            if (lists.IndexOf(sortBy) > -1)
            {
                SortDescription sd2 = new SortDescription("Name", ListSortDirection.Ascending);
                dataView.SortDescriptions.Add(sd2);
            }

            dataView.Refresh();
        }
        #endregion


        #region Filter
        private void Filter()
        {
            List<ListViewGames> SourcesManual = new List<ListViewGames>();

            // Filter
            if (!TextboxSearch.Text.IsNullOrEmpty() && SearchSources.Count != 0)
            {
                if (SearchSources.IndexOf("Manual") > -1)
                {
                    SourcesManual = ListGames.FindAll(x => x.IsManual);
                }

                ListviewGames.ItemsSource = ListGames.FindAll(
                    x => x.Name.ToLower().IndexOf(TextboxSearch.Text) > -1 && SearchSources.Contains(x.SourceName)
                );

                ListviewGames.ItemsSource = ((List<ListViewGames>)ListviewGames.ItemsSource).Union(SourcesManual).ToList();

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
                if (SearchSources.IndexOf("Manual") > -1)
                {
                    SourcesManual = ListGames.FindAll(x => x.IsManual);
                }

                ListviewGames.ItemsSource = ListGames.FindAll(
                    x => SearchSources.Contains(x.SourceName)
                );

                ListviewGames.ItemsSource = ((List<ListViewGames>)ListviewGames.ItemsSource).Union(SourcesManual).ToList();

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
