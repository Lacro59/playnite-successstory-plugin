using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LiveCharts;
using LiveCharts.Wpf;
using Playnite.SDK;
using PluginCommon;
using SuccessStory.Database;
using SuccessStory.Models;

namespace SuccessStory
{
    /// <summary>
    /// Logique d'interaction pour SuccessView.xaml
    /// </summary>
    public partial class SuccessView : Window
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        // Variables api.
        public readonly IPlayniteAPI PlayniteApi;
        public readonly IGameDatabaseAPI PlayniteApiDatabase;
        public readonly IPlaynitePathsAPI PlayniteApiPaths;

        public readonly string PluginUserDataPath;
        SuccessStorySettings settings { get; set; }

        AchievementsDatabase AchievementsDatabase;


        public SuccessView(SuccessStorySettings settings, IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            this.PlayniteApi = PlayniteApi;
            PlayniteApiDatabase = PlayniteApi.Database;
            PlayniteApiPaths = PlayniteApi.Paths;
            this.settings = settings;
            this.PluginUserDataPath = PluginUserDataPath;


            AchievementsDatabase = new AchievementsDatabase(PlayniteApi, PluginUserDataPath);
            AchievementsDatabase.Initialize();

            InitializeComponent();

            // Block hidden column.
            lvProgressionValue.IsEnabled = false;
            lvSourceName.IsEnabled = false;


            pbProgressionGlobalCount.Value = AchievementsDatabase.Progession().Unlocked;
            pbProgressionGlobalCount.Maximum = AchievementsDatabase.Progession().Total;
            labelProgressionGlobalCount.Content = AchievementsDatabase.Progession().Progression + "%";


            // Sorting default.
            _lastDirection = ListSortDirection.Descending;
            _lastHeaderClicked = lvLastActivity;
            _lastHeaderClicked.Content += " ▼";

            GetListGame();


            // Set Binding data
            DataContext = this;
        }

        /// <summary>
        /// Show list game with achievement.
        /// </summary>
        /// <param name="SearchGameName"></param>
        public void GetListGame(string SearchGameName = "")
        {
            List<listGame> ListGames = new List<listGame>();
            foreach (var item in PlayniteApiDatabase.Games)
            {
                if (item.Name.ToLower().Contains(SearchGameName.ToLower()) && AchievementsDatabase.HaveAchievements(item.Id))
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

                    if (AchievementsDatabase.VerifToAddOrShow(GameSourceName, settings))
                    {
                        string GameId = item.Id.ToString();
                        string GameName = item.Name;
                        string GameIcon;
                        DateTime? GameLastActivity = null;
                        string SourceName = item.Source.Name;

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

                        ListGames.Add(new listGame()
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
                view.SortDescriptions.Add(new SortDescription("LastActivity", ListSortDirection.Descending));
            }

            // Graphic
            SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection();
            string[] StatsGraphicsAchievementsLabels = new string[12];
            ChartValues<double> SourceAchievementsSeries = new ChartValues<double>();
            int counter = 0;
            foreach (var item in AchievementsDatabase.GetCountByMonth())
            {
                    SourceAchievementsSeries.Add(item.Value);
                    StatsGraphicsAchievementsLabels[counter] = item.Key;
                    counter += 1;
            }

            StatsGraphicAchievementsSeries.Add(new LineSeries
            {
                Title = "",
                Values = SourceAchievementsSeries
            });

            StatsGraphicAchievements.Series = StatsGraphicAchievementsSeries;
            //StatsGraphicAchievementsX.LabelFormatter = value => value;
            StatsGraphicAchievementsX.Labels = StatsGraphicsAchievementsLabels;
        }

        /// <summary>
        /// Show Achievements for the selected game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListviewGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            listGame GameSelected = (listGame)((ListBox)sender).SelectedItem;

            if (GameSelected != null)
            {
                Guid GameId = Guid.Parse(GameSelected.Id);

                GameAchievements GameAchievements = AchievementsDatabase.Get(GameId);
                List<Achievements> ListAchievements = GameAchievements.Achievements;

                List<listAchievements> ListBoxAchievements = new List<listAchievements>();

                for (int i = 0; i < ListAchievements.Count; i++)
                {
                    DateTime? dateUnlock;
                    BitmapImage iconImage = new BitmapImage();
                    FormatConvertedBitmap ConvertBitmapSource = new FormatConvertedBitmap();

                    bool isGray = false;

                    iconImage.BeginInit();
                    if (ListAchievements[i].DateUnlocked == default(DateTime) || ListAchievements[i].DateUnlocked == null)
                    {
                        dateUnlock = null;
                        if (ListAchievements[i].UrlLocked == "")
                        {
                            iconImage.UriSource = new Uri(ListAchievements[i].UrlUnlocked, UriKind.RelativeOrAbsolute);
                            isGray = true;
                        }
                        else
                        {
                            iconImage.UriSource = new Uri(ListAchievements[i].UrlLocked, UriKind.RelativeOrAbsolute);
                        }
                    }
                    else
                    {
                        iconImage.UriSource = new Uri(ListAchievements[i].UrlUnlocked, UriKind.RelativeOrAbsolute);
                        dateUnlock = ListAchievements[i].DateUnlocked;
                    }
                    iconImage.EndInit();


                    //Bitmap iconBitmap = BitmapImage2Bitmap(iconImage);
                    //iconBitmap.MakeTransparent(iconBitmap.GetPixel(1, 1));
                    //iconImage = BitmapToImageSource(iconBitmap);

                    //FormatConvertedBitmap _sourceGray = new FormatConvertedBitmap(
                    //    new BitmapImage(new Uri(ListAchievements[i].UrlUnlocked, UriKind.RelativeOrAbsolute)), 
                    //    PixelFormats.Gray32Float, null, 100);


                    ConvertBitmapSource.BeginInit();
                    ConvertBitmapSource.Source = iconImage;
                    if (isGray)
                    {
                        ConvertBitmapSource.DestinationFormat = PixelFormats.Gray32Float;
                    }
                    ConvertBitmapSource.EndInit();

                    string NameAchievement = ListAchievements[i].Name;
                    if (NameAchievement.Length > 35)
                    {
                        NameAchievement = NameAchievement.Substring(0, 35).Trim() + "...";
                    }

                    ListBoxAchievements.Add(new listAchievements()
                    {
                        Name = NameAchievement,
                        NameToolTip = ListAchievements[i].Name,
                        IsTrimmed = (NameAchievement != ListAchievements[i].Name),
                        DateUnlock = dateUnlock,
                        //Icon = iconImage,
                        Icon = ConvertBitmapSource,
                        Description = ListAchievements[i].Description
                    });

                    iconImage = null;
                }


                // Sorting default.
                lbAchievements.ItemsSource = ListBoxAchievements;
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lbAchievements.ItemsSource);
                view.SortDescriptions.Add(new SortDescription("DateUnlock", ListSortDirection.Descending));
            }
            else
            {
                lbAchievements.ItemsSource = null;
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

            logger.Info(headerClicked.Name);

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

        /// <summary>
        /// Function search game by name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextboxSearch_KeyUp(object sender, RoutedEventArgs e)
        {
            string SearchGameName = ((TextBox)sender).Text;
            GetListGame(SearchGameName);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            AchievementsDatabase = null;
        }
    }


    /// <summary>
    /// Class for the listview games
    /// </summary>
    public class listGame
    {
        public string Id { get; set; }
        public BitmapImage Icon { get; set; }
        public string Name { get; set; }
        public DateTime? LastActivity { get; set; }
        public string SourceName { get; set; }
        public string SourceIcon { get; set; }
        public int ProgressionValue { get; set; }
        public int Total { get; set; }
        public string TotalPercent { get; set; }
        public int Unlocked { get; set; }
    }

    /// <summary>
    /// Class for the listbox achievements
    /// </summary>
    public class listAchievements
    {
        //public BitmapImage Icon { get; set; }
        public FormatConvertedBitmap Icon { get; set; }
        public string Name { get; set; }
        public string NameToolTip { get; set; }
        public Boolean IsTrimmed { get; set; }
        public DateTime? DateUnlock { get; set; }
        public string Description { get; set; }
    }
}
