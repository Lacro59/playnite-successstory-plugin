using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
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
        public readonly IGameDatabaseAPI dbPlaynite;
        public readonly IPlaynitePathsAPI pathsPlaynite;
        public readonly string pathExtentionData;
        SuccessStorySettings settingsPlaynite { get; set; }

        // Variables list games.
        public string lvGamesID { get; set; }
        public string lvGamesIcon { get; set; }
        public string lvGamesTitle { get; set; }
        public string lvGamesLastActivity { get; set; }

        // Variables "Informations"
        string totalCountLabel;
        string totalCountUnlockLabel;

        public SuccessView(SuccessStorySettings settings, IPlayniteAPI PlayniteApi, string pathExtData)
        {
            this.PlayniteApi = PlayniteApi;
            dbPlaynite = PlayniteApi.Database;
            pathsPlaynite = PlayniteApi.Paths;
            settingsPlaynite = settings;
            pathExtentionData = pathExtData;

            #region text localization
            //Informations
            string infoLabel = "Informations";
            totalCountLabel = "Total achievements";
            totalCountUnlockLabel = "Total achievements unlocked";

            // listViewGames
            lvGamesID = "gameID";
            lvGamesIcon = "GameIcon";
            lvGamesTitle = "Title";
            lvGamesLastActivity = "Last session";
            #endregion

            InitializeComponent();


            lInfo.Content = infoLabel;
            lTotalCount.Content = "";
            ltotalCountUnlock.Content = "";
            totalCount.Content = "";
            totalCountUnlock.Content = "";


            getListGame();

            // Set Binding data
            DataContext = this;
        }



        public void getListGame()
        {
            logger.Info("getListGame()");

            List<listGame> ListGames = new List<listGame>();

            //IEnumerable<Game> gogGames = dbPlaynite.Games.Where(a => a.Source.Name.ToLower()?.Contains("gog") == true);
            //IEnumerable<Game> steamGames = dbPlaynite.Games.Where(a => a.Source.Name.ToLower()?.Contains("steam") == true);
            //logger.Info("gogGames: " + JsonConvert.SerializeObject(gogGames));
            //logger.Info("steamGames: " + JsonConvert.SerializeObject(steamGames));

            foreach (var item in dbPlaynite.Games)
            {
                if (item.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000") && (item.Source.Name.ToLower() == "gog" || item.Source.Name.ToLower() == "steam"))
                {
                    string gameID = item.Id.ToString();
                    string gameTitle = item.Name;
                    string gameIcon;
                    string dateLastActivity = "";

                    if (item.LastActivity != null)
                    {
                        dateLastActivity = ((DateTime)item.LastActivity).ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
                    }

                    BitmapImage iconImage = new BitmapImage();
                    if (String.IsNullOrEmpty(item.Icon) == false)
                    {
                        iconImage.BeginInit();
                        gameIcon = dbPlaynite.GetFullFilePath(item.Icon);
                        iconImage.UriSource = new Uri(gameIcon, UriKind.RelativeOrAbsolute);
                        iconImage.EndInit();
                    }

                    ListGames.Add(new listGame()
                    {
                        listGameID = gameID,
                        listGameTitle = gameTitle,
                        listGameIcon = iconImage,
                        listGameLastActivity = dateLastActivity
                    });

                    iconImage = null;
                }
            }

            // Sorting default.
            lvGames.ItemsSource = ListGames;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvGames.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("listGameTitle", ListSortDirection.Ascending));
        }


        private void lvGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (ListBox)sender;
            listGame gameItem = (listGame)item.SelectedItem;
            logger.Info("itemSelected: " + JsonConvert.SerializeObject(gameItem));
            Guid gameId = Guid.Parse(gameItem.listGameID);

            int countAchievements = 0;
            int countAchievementsUnlocked = 0;


            AchievementsCollection data = new AchievementsCollection();
            List<Achievements> ListAchievements;
            ListAchievements = data.GetAchievementsListWEB(gameId, PlayniteApi, pathExtentionData);

            logger.Info("ListAchievements: " + JsonConvert.SerializeObject(ListAchievements));

            List<listAchievements> ListBoxAchievements = new List<listAchievements>();

            for (int i = 0; i < ListAchievements.Count; i++)
            {
                string dateUnlock;
                BitmapImage iconImage = new BitmapImage();
                iconImage.BeginInit();

                if (ListAchievements[i].DateUnlocked == default(DateTime))
                {
                    iconImage.UriSource = new Uri(ListAchievements[i].UrlLocked, UriKind.RelativeOrAbsolute);
                    dateUnlock = "";
                    countAchievements += 1;
                }
                else
                {
                    iconImage.UriSource = new Uri(ListAchievements[i].UrlUnlocked, UriKind.RelativeOrAbsolute);
                    dateUnlock = ListAchievements[i].DateUnlocked.ToString("dd/MM/yyyy HH:mm:ss");
                    countAchievementsUnlocked += 1;
                }

                iconImage.EndInit();

                ListBoxAchievements.Add(new listAchievements()
                {
                    lbAchievementsName = ListAchievements[i].Name,
                    lbAchievementsDateUnlock = dateUnlock,
                    lbAchievementsIcon = iconImage,
                    lbAchievementsDescription = ListAchievements[i].Description
                });

                iconImage = null;
            }

            lTotalCount.Content = totalCountLabel;
            ltotalCountUnlock.Content = totalCountUnlockLabel;
            totalCount.Content = countAchievements;
            totalCountUnlock.Content = countAchievementsUnlocked;

            // Sorting default.
            lbAchievements.ItemsSource = ListBoxAchievements;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lbAchievements.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("lbAchievementsDateUnlock", ListSortDirection.Descending));
        }


        #region Functions sorting lvGames.
        //https://stackoverflow.com/questions/30787068/wpf-listview-sorting-on-column-click
        private GridViewColumnHeader lastHeaderClicked = null;
        private ListSortDirection lastDirection = ListSortDirection.Ascending;

        private void onHeaderClick(object sender, RoutedEventArgs e)
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
            var dv = CollectionViewSource.GetDefaultView(lvGames.ItemsSource);
            dv.SortDescriptions.Clear();
            var sd = new SortDescription(bn, dir);
            dv.SortDescriptions.Add(sd);
            dv.Refresh();
        }
        #endregion

    }

    // Listview games
    public class listGame
    {
        public string listGameTitle { get; set; }
        public string listGameID { get; set; }
        public BitmapImage listGameIcon { get; set; }
        public string listGameLastActivity { get; set; }
    }


    // listbox Achievements
    public class listAchievements
    {
        public BitmapImage lbAchievementsIcon { get; set; }
        public string lbAchievementsName { get; set; }
        // TODO set DateTime for sort
        public string lbAchievementsDateUnlock { get; set; }
        public string lbAchievementsDescription { get; set; }
    }
}
