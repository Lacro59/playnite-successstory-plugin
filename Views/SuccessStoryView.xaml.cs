using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json;
using Playnite.Controls;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using SuccessStory.Database;
using SuccessStory.Models;
using SuccessStory.Views.Interface;


namespace SuccessStory
{
    /// <summary>
    /// Logique d'interaction pour SuccessView.xaml
    /// </summary>
    public partial class SuccessView : WindowBase
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        // Variables api.
        public IPlayniteAPI PlayniteApi;
        public IGameDatabaseAPI PlayniteApiDatabase;
        public IPlaynitePathsAPI PlayniteApiPaths;

        public readonly string PluginUserDataPath;
        SuccessStorySettings settings { get; set; }

        AchievementsDatabase AchievementsDatabase;
        List<ListSource> FilterSourceItems = new List<ListSource>();
        List<string> SearchSources = new List<string>();


        public SuccessView(SuccessStorySettings settings, IPlayniteAPI PlayniteApi, string PluginUserDataPath, Game GameSelected = null)
        {
            this.PlayniteApi = PlayniteApi;
            PlayniteApiDatabase = PlayniteApi.Database;
            PlayniteApiPaths = PlayniteApi.Paths;
            this.settings = settings;
            this.PluginUserDataPath = PluginUserDataPath;


            AchievementsDatabase = new AchievementsDatabase(PlayniteApi, settings, PluginUserDataPath);
            AchievementsDatabase.Initialize();

            InitializeComponent();

            // Block hidden column.
            lvProgressionValue.IsEnabled = false;
            lvSourceName.IsEnabled = false;


            pbProgressionGlobalCount.Value = AchievementsDatabase.Progession().Unlocked;
            pbProgressionGlobalCount.Maximum = AchievementsDatabase.Progession().Total;
            labelProgressionGlobalCount.Content = AchievementsDatabase.Progession().Progression + "%";


            GetListGame();


            AchievementsGraphicsDataCount GraphicsData = null;
            if (settings.GraphicAllUnlockedByMonth)
            {
                GraphicTitleALL.Content = resources.GetString("LOCSucessStoryGraphicTitleALL");
                GraphicsData = AchievementsDatabase.GetCountByMonth();
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
                Title = "",
                Values = GraphicsData.Series
            });

            SuccessStory_Achievements_Graphics.Children.Clear();
            SuccessStory_Achievements_Graphics.Children.Add(new SuccessStoryAchievementsGraphics(StatsGraphicAchievementsSeries, StatsGraphicsAchievementsLabels, this));
            SuccessStory_Achievements_Graphics.UpdateLayout();

            // Set game selected
            if (GameSelected != null)
            {
                for (int i = 0; i < ListviewGames.Items.Count; i++)
                {
                    if (((ListGames)ListviewGames.Items[i]).Name == GameSelected.Name)
                    {
                        ListviewGames.SelectedIndex = i;
                    }
                }
            }
            ListviewGames.ScrollIntoView(ListviewGames.SelectedItem);


            if (settings.EnableLocal)
            {
                //FilterSource.Items.Add(new { SourceName = "Playnite", IsCheck = false });
                FilterSourceItems.Add(new ListSource { SourceName = "Playnite", IsCheck = false });
            }
            if (settings.EnableSteam)
            {
                //FilterSource.Items.Add(new { SourceName = "Steam", IsCheck = false });
                FilterSourceItems.Add(new ListSource { SourceName = "Steam", IsCheck = false });
            }
            if (settings.EnableGog)
            {
                //FilterSource.Items.Add(new { SourceName = "GOG", IsCheck = false });
                FilterSourceItems.Add(new ListSource { SourceName = "GOG", IsCheck = false });
            }
            if (settings.EnableOrigin)
            {
                //FilterSource.Items.Add(new { SourceName = "Origin", IsCheck = false });
                FilterSourceItems.Add(new ListSource { SourceName = "Origin", IsCheck = false });
            }
            //FilterSource.UpdateLayout();
            FilterSource.ItemsSource = FilterSourceItems;


            // Set Binding data
            DataContext = this;
        }

        /// <summary>
        /// Show list game with achievement.
        /// </summary>
        /// <param name="SearchGameName"></param>
        public void GetListGame(string SearchGameName = "", List<string> SearchSourceName = null)
        {
            if (SearchSourceName != null && SearchSourceName.Count == 0)
            {
                SearchSourceName = null;
            }

            List <ListGames> ListGames = new List<ListGames>();
            foreach (var item in PlayniteApiDatabase.Games)
            {
                string GameSourceName = "";
                if (item.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                {
                    GameSourceName = item.Source.Name;
                }
                else
                {
                    GameSourceName = "Playnite";
                }


                bool isFind = false;

                if (SearchSourceName != null)
                {
                    for (int i = 0; i < SearchSourceName.Count; i++)
                    {
                        if (GameSourceName.ToLower().Contains(SearchSourceName[i].ToLower()))
                        {
                            isFind = true;
                        }
                    }
                }

                if (SearchGameName != "")
                {
                    if (item.Name.ToLower().Contains(SearchGameName.ToLower()))
                    {
                        isFind = true;
                    }
                }

                if (SearchGameName != "" && SearchSourceName != null)
                {
                    isFind = false;

                    for (int i = 0; i < SearchSourceName.Count; i++)
                    {
                        if ((GameSourceName.ToLower().Contains(SearchSourceName[i].ToLower())) && (item.Name.ToLower().Contains(SearchGameName.ToLower())))
                        {
                            isFind = true;
                        }
                    }
                }

                if (SearchGameName == "" && SearchSourceName == null)
                {
                    isFind = true;
                }
            

                if (isFind && AchievementsDatabase.HaveAchievements(item.Id))
                {
                    if (AchievementsDatabase.VerifToAddOrShow(GameSourceName, settings))
                    {
                        string GameId = item.Id.ToString();
                        string GameName = item.Name;
                        string GameIcon;
                        DateTime? GameLastActivity = null;

                        string SourceName = "";
                        if (item.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                        {
                            SourceName = item.Source.Name;
                        }
                        else
                        {
                            SourceName = "Playnite";
                        }

                        GameAchievements GameAchievements = AchievementsDatabase.Get(item.Id);

                        if (item.LastActivity != null)
                        {
                            GameLastActivity = ((DateTime)item.LastActivity).ToLocalTime();
                        }

                        BitmapImage iconImage = new BitmapImage();
                        if (String.IsNullOrEmpty(item.Icon) == false)
                        {
                            iconImage.BeginInit();
                            GameIcon = PlayniteApiDatabase.GetFullFilePath(item.Icon);
                            iconImage.UriSource = new Uri(GameIcon, UriKind.RelativeOrAbsolute);
                            iconImage.EndInit();
                        }

                        ListGames.Add(new ListGames()
                        {
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

            ListviewGames.ItemsSource = ListGames;

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

        /// <summary>
        /// Show Achievements for the selected game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListviewGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListGames GameSelected = (ListGames)((ListBox)sender).SelectedItem;

            if (GameSelected != null)
            {
                listAchievementBorder.BorderThickness = new Thickness(0);


                Guid GameId = Guid.Parse(GameSelected.Id);

                GameAchievements GameAchievements = AchievementsDatabase.Get(GameId);
                List<Achievements> ListAchievements = GameAchievements.Achievements;

                SuccessStory_Achievements_List.Children.Clear();
                SuccessStory_Achievements_List.Children.Add(new SuccessStoryAchievementsList(ListAchievements, this));
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
                    GraphicsData = AchievementsDatabase.GetCountByDay(GameId, 7);
                }
                string[] StatsGraphicsAchievementsLabels = GraphicsData.Labels;
                SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection();
                StatsGraphicAchievementsSeries.Add(new LineSeries
                {
                    Title = "",
                    Values = GraphicsData.Series
                });

                SuccessStory_Achievements_Graphics_Game.Children.Clear();
                SuccessStory_Achievements_Graphics_Game.Children.Add(new SuccessStoryAchievementsGraphics(StatsGraphicAchievementsSeries, StatsGraphicsAchievementsLabels, this));
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
            lvProgressionValue.IsEnabled = true;
            lvSourceName.IsEnabled = true;

            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            // No sort
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
                        _lastHeaderClicked.Content = ((string)_lastHeaderClicked.Content).Replace(" ▲", "");
                        _lastHeaderClicked.Content = ((string)_lastHeaderClicked.Content).Replace(" ▼", "");
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

        private void Label_Loaded(object sender, RoutedEventArgs e)
        {
            Tools.DesactivePlayniteWindowControl(this);
        }


        #region Filter
        /// <summary>
        /// Function search game by name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextboxSearch_KeyUp(object sender, RoutedEventArgs e)
        {
            string SearchGameName = ((TextBox)sender).Text;
            GetListGame(SearchGameName, SearchSources);
        }

        private void ChkSource_Checked(object sender, RoutedEventArgs e)
        {
            FilterSource.Text = "";
            SearchSources = new List<string>();

            for (int i = 0; i < FilterSourceItems.Count; i++)
            {
                if ((string)((CheckBox)sender).Content == FilterSourceItems[i].SourceName)
                {
                    FilterSourceItems[i].IsCheck = (bool)((CheckBox)sender).IsChecked;

                    if (FilterSourceItems[i].IsCheck)
                    {
                        SearchSources.Add(FilterSourceItems[i].SourceName);

                        if (FilterSource.Text == "")
                        {
                            FilterSource.Text = FilterSourceItems[i].SourceName;
                        }
                        else
                        {
                            FilterSource.Text += ", " + FilterSourceItems[i].SourceName;
                        }
                    }
                }
                else
                {
                    if (FilterSourceItems[i].IsCheck)
                    {
                        SearchSources.Add(FilterSourceItems[i].SourceName);

                        if (FilterSource.Text == "")
                        {
                            FilterSource.Text = FilterSourceItems[i].SourceName;
                        }
                        else
                        {
                            FilterSource.Text += ", " + FilterSourceItems[i].SourceName;
                        }
                    }
                }
            }

            if (FilterSource.Text == "")
            {
                SearchSources = null;
            }

            GetListGame(TextboxSearch.Text, SearchSources);
        }

        private void ChkSource_Unchecked(object sender, RoutedEventArgs e)
        {
            FilterSource.Text = "";
            SearchSources = new List<string>();

            for (int i = 0; i < FilterSourceItems.Count; i++)
            {
                if ((string)((CheckBox)sender).Content == FilterSourceItems[i].SourceName)
                {
                    FilterSourceItems[i].IsCheck = (bool)((CheckBox)sender).IsChecked;

                    if (FilterSourceItems[i].IsCheck)
                    {
                        SearchSources.Add(FilterSourceItems[i].SourceName);

                        if (FilterSource.Text == "")
                        {
                            FilterSource.Text = FilterSourceItems[i].SourceName;
                        }
                        else
                        {
                            FilterSource.Text += ", " + FilterSourceItems[i].SourceName;
                        }
                    }
                }
                else
                {
                    if (FilterSourceItems[i].IsCheck)
                    {
                        SearchSources.Add(FilterSourceItems[i].SourceName);

                        if (FilterSource.Text == "")
                        {
                            FilterSource.Text = FilterSourceItems[i].SourceName;
                        }
                        else
                        {
                            FilterSource.Text += ", " + FilterSourceItems[i].SourceName;
                        }
                    }
                }
            }

            if (FilterSource.Text == "")
            {
                SearchSources = null;
            }

            GetListGame(TextboxSearch.Text, SearchSources);
        }
        #endregion
    }

    public class ListSource
    {
        public string SourceName { get; set; }
        public bool IsCheck { get; set; }
    }
}
