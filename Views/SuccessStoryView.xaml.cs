using System;
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

namespace SuccessStory
{
    /// <summary>
    /// Logique d'interaction pour SuccessView.xaml
    /// </summary>
    public partial class SuccessView : Window
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        // Variables api.
        public IPlayniteAPI PlayniteApi;
        public IGameDatabaseAPI PlayniteApiDatabase;
        public IPlaynitePathsAPI PlayniteApiPaths;

        public readonly string PluginUserDataPath;
        SuccessStory plugin { get; set; }
        SuccessStorySettings settings { get; set; }

        AchievementsDatabase AchievementsDatabase;
        List<ListSource> FilterSourceItems = new List<ListSource>();
        List<ListViewGames> ListGames = new List<ListViewGames>();
        List<string> SearchSources = new List<string>();


        public SuccessView(SuccessStory plugin, SuccessStorySettings settings, IPlayniteAPI PlayniteApi, string PluginUserDataPath, bool isRetroAchievements = false, Game GameSelected = null)
        {
            this.plugin = plugin;
            this.PlayniteApi = PlayniteApi;
            PlayniteApiDatabase = PlayniteApi.Database;
            PlayniteApiPaths = PlayniteApi.Paths;
            this.settings = settings;
            this.PluginUserDataPath = PluginUserDataPath;

            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            AchievementsDatabase = new AchievementsDatabase(plugin, PlayniteApi, settings, PluginUserDataPath, isRetroAchievements);
            AchievementsDatabase.Initialize(false);

            InitializeComponent();

            // Block hidden column.
            lvProgressionValue.IsEnabled = false;
            lvSourceName.IsEnabled = false;


            pbProgressionGlobalCount.Value = AchievementsDatabase.Progession().Unlocked;
            pbProgressionGlobalCount.Maximum = AchievementsDatabase.Progession().Total;
            labelProgressionGlobalCount.Content = AchievementsDatabase.Progession().Progression + "%";

            pbProgressionLaunchedCount.Value = AchievementsDatabase.ProgessionLaunched().Unlocked;
            pbProgressionLaunchedCount.Maximum = AchievementsDatabase.ProgessionLaunched().Total;
            labelProgressionLaunchedCount.Content = AchievementsDatabase.ProgessionLaunched().Progression + "%";


            GetListGame();


            AchievementsGraphicsDataCount GraphicsData = null;
            if (settings.GraphicAllUnlockedByMonth)
            {
                GraphicTitleALL.Content = resources.GetString("LOCSucessStoryGraphicTitleALL");
                GraphicsData = AchievementsDatabase.GetCountByMonth(null, 8);
            }
            else
            {
                GraphicTitleALL.Content = resources.GetString("LOCSucessStoryGraphicTitleALLDay");
                GraphicsData = AchievementsDatabase.GetCountByDay();
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


            SetGraphicsAchievementsSources();


            // Set Binding data
            DataContext = this;
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void SetGraphicsAchievementsSources()
        {
            var data = AchievementsDatabase.GetCountBySources();
            
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
                    foreach (var item in PlayniteApiDatabase.Games)
                    {
                        string GameSourceName = string.Empty;
                        if (item.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                        {
                            try
                            {
                                GameSourceName = item.Source.Name;
                            }
                            catch
                            {
                                GameSourceName = "Undefined";
                                logger.Warn($"SuccessStory - No source name for {item.Name}");
                            }

                            if (PlayniteTools.IsGameEmulated(PlayniteApi, item))
                            {
                                GameSourceName = "RetroAchievements";
                            }
                        }
                        else
                        {
                            if (PlayniteTools.IsGameEmulated(PlayniteApi, item))
                            {
                                GameSourceName = "RetroAchievements";
                            }
                            else
                            {
                                GameSourceName = "Playnite";
                            }
                        }

                        if (AchievementsDatabase.HaveAchievements(item.Id))
                        {
                            if (AchievementsDatabase.VerifToAddOrShow(plugin, PlayniteApi, GameSourceName, settings, PluginUserDataPath))
                            {
                                string GameId = item.Id.ToString();
                                string GameName = item.Name;
                                string GameIcon;
                                DateTime? GameLastActivity = null;

                                string SourceName = string.Empty;
                                if (item.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                                {
                                    SourceName = item.Source.Name;

                                    if (PlayniteTools.IsGameEmulated(PlayniteApi, item))
                                    {
                                        SourceName = "RetroAchievements";
                                    }
                                }
                                else
                                {
                                    if (PlayniteTools.IsGameEmulated(PlayniteApi, item))
                                    {
                                        SourceName = "RetroAchievements";
                                    }
                                    else
                                    {
                                        SourceName = "Playnite";
                                    }
                                }

                                GameAchievements GameAchievements = AchievementsDatabase.Get(item.Id);

                                if (item.LastActivity != null)
                                {
                                    GameLastActivity = ((DateTime)item.LastActivity).ToLocalTime();
                                }

                                BitmapImage iconImage = new BitmapImage();
                                if (!item.Icon.IsNullOrEmpty())
                                {
                                    iconImage.BeginInit();
                                    GameIcon = PlayniteApiDatabase.GetFullFilePath(item.Icon);
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
                }


                ListviewGames.ItemsSource = ListGames;
                // Filter
                if (!TextboxSearch.Text.IsNullOrEmpty() && SearchSources.Count != 0)
                {
                    ListviewGames.ItemsSource = ListGames.FindAll(
                        x => x.Name.ToLower().IndexOf(TextboxSearch.Text) > -1 && SearchSources.Contains(x.SourceName)
                    );
                    return;
                }

                if (!TextboxSearch.Text.IsNullOrEmpty())
                {
                    ListviewGames.ItemsSource = ListGames.FindAll(
                        x => x.Name.ToLower().IndexOf(TextboxSearch.Text) > -1
                    );
                    return;
                }

                if (SearchSources.Count != 0)
                {
                    ListviewGames.ItemsSource = ListGames.FindAll(
                        x => SearchSources.Contains(x.SourceName)
                    );
                    return;
                }


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

                GameAchievements GameAchievements = AchievementsDatabase.Get(GameId);
                List<Achievements> ListAchievements = GameAchievements.Achievements;

                SuccessStory_Achievements_List.Children.Clear();
                SuccessStory_Achievements_List.Children.Add(new SuccessStoryAchievementsList(ListAchievements));
                SuccessStory_Achievements_List.UpdateLayout();


                AchievementsGraphicsDataCount GraphicsData = null;
                if (settings.GraphicAllUnlockedByDay)
                {
                    GraphicTitle.Content = resources.GetString("LOCSucessStoryGraphicTitle");
                    GraphicsData = AchievementsDatabase.GetCountByMonth(GameId);
                }
                else
                {
                    GraphicTitle.Content = resources.GetString("LOCSucessStoryGraphicTitleDay");
                    GraphicsData = AchievementsDatabase.GetCountByDay(GameId, 8);
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

        //private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        //{
        //    // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));
        //
        //    using (MemoryStream outStream = new MemoryStream())
        //    {
        //        BitmapEncoder enc = new BmpBitmapEncoder();
        //        enc.Frames.Add(BitmapFrame.Create(bitmapImage));
        //        enc.Save(outStream);
        //        System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);
        //
        //        return new Bitmap(bitmap);
        //    }
        //}
        //
        //private BitmapImage BitmapToImageSource(Bitmap bitmap)
        //{
        //    using (MemoryStream memory = new MemoryStream())
        //    {
        //        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
        //        memory.Position = 0;
        //        BitmapImage bitmapimage = new BitmapImage();
        //        bitmapimage.BeginInit();
        //        bitmapimage.StreamSource = memory;
        //        bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
        //        bitmapimage.EndInit();
        //
        //        return bitmapimage;
        //    }
        //}



        #region Functions sorting ListviewGames.
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


        protected override void OnClosing(CancelEventArgs e)
        {
            AchievementsDatabase = null;
            PlayniteApi = null;
            PlayniteApiDatabase = null;
            PlayniteApiPaths = null;

            ListviewGames.ItemsSource = null;
            ListviewGames.UpdateLayout();

            SuccessStory_Achievements_List.Children.Clear();
            SuccessStory_Achievements_List.UpdateLayout();
            SuccessStory_Achievements_Graphics_Game.Children.Clear();
            SuccessStory_Achievements_Graphics_Game.UpdateLayout();

            GC.Collect();
        }


        #region Filter
        /// <summary>
        /// Function search game by name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextboxSearch_KeyUp(object sender, RoutedEventArgs e)
        {
            GetListGame();
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

            GetListGame();
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
