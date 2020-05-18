using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Dashboard.Commons;
using Playnite.SDK;
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

        AchievementsDatabase AchievementsDatabase = new AchievementsDatabase();

        // Variables list games.
        public string ListviewGamesIcon { get; set; }
        public string ListviewGamesName { get; set; }
        public string ListviewGamesLastActivity { get; set; }
        public string ListviewGamesSourceName { get; set; }
        public string ListviewGamesProgression { get; set; }

        public string labelProgressionGlobal { get; set; }
        public int ProgressionGlobalCount { get; set; }

        // Variables "Informations"
        public string totalCountLabel;
        public string totalCountUnlockLabel;
        public string totalProgressionCount;


        public SuccessView(SuccessStorySettings settings, IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            this.PlayniteApi = PlayniteApi;
            PlayniteApiDatabase = PlayniteApi.Database;
            PlayniteApiPaths = PlayniteApi.Paths;
            this.settings = settings;
            this.PluginUserDataPath = PluginUserDataPath;


            AchievementsDatabase = new AchievementsDatabase();
            AchievementsDatabase.Initialize(PlayniteApi, PluginUserDataPath);


            #region text localization
            // Informations
            string infoLabel = "Informations";
            totalCountLabel = "Number achievements";
            totalCountUnlockLabel = "Unlocked achievements";
            totalProgressionCount = "Progression";

            // ListviewGames
            ListviewGamesIcon = "Icon";
            ListviewGamesName = "Name";
            ListviewGamesLastActivity = "Last session";
            ListviewGamesSourceName = "Source";
            ListviewGamesProgression = "Progression";

            labelProgressionGlobal = "Global progression";
            #endregion


            InitializeComponent();


            //ProgressionGlobalCount = AchievementsDatabase.Progession().Progression;
            pbProgressionGlobalCount.Value = AchievementsDatabase.Progession().Unlocked;
            pbProgressionGlobalCount.Maximum = AchievementsDatabase.Progession().Total;


            // Informations panel
            lInfo.Content = infoLabel;
            lTotalCount.Content = "";
            ltotalCountUnlock.Content = "";
            totalCount.Content = "";
            totalCountUnlock.Content = "";
            labelProgression.Content = "";
            ProgressionCount.Visibility = Visibility.Hidden;


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
            logger.Info("getListGame()");

            List<listGame> ListGames = new List<listGame>();
            foreach (var item in PlayniteApiDatabase.Games)
            {
                if (item.Name.ToLower().Contains(SearchGameName.ToLower()) && AchievementsDatabase.HaveAchievements(item.Id))
                {
                    if (item.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000") && (item.Source.Name.ToLower() == "gog" || item.Source.Name.ToLower() == "steam"))
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
                            SourceName = TransformIcon.Get(SourceName),
                            Progression = GameAchievements.Progression
                        });

                        iconImage = null;
                    }
                }
            }

            // Sorting default.
            ListviewGames.ItemsSource = ListGames;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ListviewGames.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("LastActivity", ListSortDirection.Descending));
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

                    iconImage.BeginInit();
                    if (ListAchievements[i].DateUnlocked == default(DateTime) || ListAchievements[i].DateUnlocked == null)
                    {
                        iconImage.UriSource = new Uri(ListAchievements[i].UrlLocked, UriKind.RelativeOrAbsolute);
                        dateUnlock = null;
                    }
                    else
                    {
                        iconImage.UriSource = new Uri(ListAchievements[i].UrlUnlocked, UriKind.RelativeOrAbsolute);
                        dateUnlock = ListAchievements[i].DateUnlocked;
                    }
                    iconImage.EndInit();

                    ListBoxAchievements.Add(new listAchievements()
                    {
                        Name = ListAchievements[i].Name,
                        DateUnlock = dateUnlock,
                        Icon = iconImage,
                        Description = ListAchievements[i].Description
                    });

                    iconImage = null;
                }

                // Informations panel
                lTotalCount.Content = totalCountLabel;
                ltotalCountUnlock.Content = totalCountUnlockLabel;
                totalCount.Content = GameAchievements.Total;
                totalCountUnlock.Content = GameAchievements.Unlocked;
                labelProgression.Content = totalProgressionCount;
                ProgressionCount.Visibility = Visibility.Visible;
                ProgressionCount.Value = GameAchievements.Unlocked;
                ProgressionCount.Maximum = GameAchievements.Total;

                // Sorting default.
                lbAchievements.ItemsSource = ListBoxAchievements;
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lbAchievements.ItemsSource);
                view.SortDescriptions.Add(new SortDescription("DateUnlock", ListSortDirection.Descending));
            }
            else
            {
                // Informations panel
                lTotalCount.Content = "";
                ltotalCountUnlock.Content = "";
                totalCount.Content = "";
                totalCountUnlock.Content = "";
                labelProgression.Content = "";
                ProgressionCount.Visibility = Visibility.Hidden;

                lbAchievements.ItemsSource = null;
            }
        }

        #region Functions sorting ListviewGames.
        //https://stackoverflow.com/questions/30787068/wpf-listview-sorting-on-column-click
        private GridViewColumnHeader lastHeaderClicked = null;
        private ListSortDirection lastDirection = ListSortDirection.Ascending;

        private void ListviewGames_onHeaderClick(object sender, RoutedEventArgs e)
        {
            if (!(e.OriginalSource is GridViewColumnHeader ch)) return;
            var dir = ListSortDirection.Ascending;
            if (ch == lastHeaderClicked && lastDirection == ListSortDirection.Ascending)
                dir = ListSortDirection.Descending;
            sort(ch, dir);
            lastHeaderClicked = ch; lastDirection = dir;
        }

        private void sort(GridViewColumnHeader ch, ListSortDirection dir)
        {
            var bn = (ch.Column.DisplayMemberBinding as Binding)?.Path.Path;
            bn = bn ?? ch.Column.Header as string;
            var dv = CollectionViewSource.GetDefaultView(ListviewGames.ItemsSource);
            dv.SortDescriptions.Clear();
            var sd = new SortDescription(bn, dir);
            dv.SortDescriptions.Add(sd);
            dv.Refresh();
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
        public int Progression { get; set; }
    }

    /// <summary>
    /// Class for the listbox achievements
    /// </summary>
    public class listAchievements
    {
        public BitmapImage Icon { get; set; }
        public string Name { get; set; }
        public DateTime? DateUnlock { get; set; }
        public string Description { get; set; }
    }
}
