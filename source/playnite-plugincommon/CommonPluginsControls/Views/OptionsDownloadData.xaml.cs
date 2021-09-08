using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsControls.Controls
{
    /// <summary>
    /// Logique d'interaction pour OptionsDownloadData.xaml
    /// </summary>
    public partial class OptionsDownloadData : UserControl
    {
        private IPlayniteAPI _PlayniteApi { get; set; }
        private List<Game> _FilteredGames { get; set; }


        public OptionsDownloadData(IPlayniteAPI PlayniteApi, bool WithoutMissing = false)
        {
            _PlayniteApi = PlayniteApi;

            InitializeComponent();

            if (WithoutMissing)
            {
                PART_OnlyMissing.Visibility = Visibility.Collapsed;
            }
        }


        private void PART_BtClose_Click(object sender, RoutedEventArgs e)
        {
            ((Window)this.Parent).Close();
        }

        private void PART_BtDownload_Click(object sender, RoutedEventArgs e)
        {
            _FilteredGames = _PlayniteApi.Database.Games.Where(x => x.Hidden == false).ToList();

            if ((bool)PART_AllGames.IsChecked)
            {

            }

            if ((bool)PART_GamesRecentlyPlayed.IsChecked)
            {
                _FilteredGames = _FilteredGames.Where(x => x.LastActivity != null && (DateTime)x.LastActivity >= DateTime.Now.AddMonths(-1)).ToList();
            }

            if ((bool)PART_GamesRecentlyAdded.IsChecked)
            {
                _FilteredGames = _FilteredGames.Where(x => x.Added != null && (DateTime)x.Added >= DateTime.Now.AddMonths(-1)).ToList();
            }

            if ((bool)PART_GamesInstalled.IsChecked)
            {
                _FilteredGames = _FilteredGames.Where(x => x.IsInstalled).ToList();
            }

            if ((bool)PART_GamesNotInstalled.IsChecked)
            {
                _FilteredGames = _FilteredGames.Where(x => !x.IsInstalled).ToList();
            }

            if ((bool)PART_GamesFavorite.IsChecked)
            {
                _FilteredGames = _FilteredGames.Where(x => x.Favorite).ToList();
            }

            ((Window)this.Parent).Close();
        }


        public List<Game> GetFilteredGames()
        {
            return _FilteredGames;
        }

        public bool GetOnlyMissing()
        {
            return (bool)PART_OnlyMissing.IsChecked;
        }
    }
}
