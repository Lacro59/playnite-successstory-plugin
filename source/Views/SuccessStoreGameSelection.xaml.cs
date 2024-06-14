using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Clients;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SuccessStory.Views
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoreGameSelection.xaml
    /// </summary>
    public partial class SuccessStoreGameSelection : UserControl
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;

        public GameAchievements GameAchievements { get; set; } = null;
        private Game GameContext { get; set; }

        private SteamAchievements SteamAchievements { get; set; } = new SteamAchievements();
        private ExophaseAchievements ExophaseAchievements { get; set; } = new ExophaseAchievements();


        public SuccessStoreGameSelection(Game game)
        {
            InitializeComponent();

            GameContext = game;

            PART_DataLoadWishlist.Visibility = Visibility.Collapsed;
            PART_GridData.IsEnabled = true;

            SearchElement.Text = game.Name;
            SearchElements();
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            lbSelectable.ItemsSource = null;
            lbSelectable.UpdateLayout();
        }


        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            ((Window)Parent).Close();
        }

        private void BtOk_Click(object sender, RoutedEventArgs e)
        {
            SearchResult searchResult = (SearchResult)lbSelectable.SelectedItem;

            if ((bool)rbSteam.IsChecked)
            {
                SteamAchievements.SetLocal();
                SteamAchievements.SetManual();
                GameAchievements = SteamAchievements.GetAchievements(GameContext, searchResult.AppId);
            }

            if ((bool)rbExophase.IsChecked)
            {
                GameAchievements = ExophaseAchievements.GetAchievements(GameContext, searchResult);
            }

            ((Window)Parent).Close();
        }


        private void LbSelectable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btOk.IsEnabled = true;
        }

        private void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchElements();
        }

        private void Rb_Check(object sender, RoutedEventArgs e)
        {
            SearchElements();
        }

        private void SearchElement_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ButtonSearch_Click(null, null);
            }
        }


        private void SearchElements()
        {
            bool IsSteam = (rbSteam != null) && (bool)rbSteam.IsChecked;
            bool IsExophase = (rbExophase != null) && (bool)rbExophase.IsChecked;

            if (SearchElement == null || SearchElement.Text.IsNullOrEmpty())
            {
                return;
            }

            PART_DataLoadWishlist.Visibility = Visibility.Visible;
            PART_GridData.IsEnabled = false;

            string gameSearch = RemoveAccents(SearchElement.Text);

            lbSelectable.ItemsSource = null;
            _ = Task.Run(() => LoadData(gameSearch, IsSteam, IsExophase))
                .ContinueWith(antecedent =>
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        if (antecedent.Result != null)
                        {
                            lbSelectable.ItemsSource = antecedent.Result;
                            Common.LogDebug(true, $"SearchElements({gameSearch}) - " + Serialization.ToJson(antecedent.Result));
                        }

                        PART_DataLoadWishlist.Visibility = Visibility.Collapsed;
                        PART_GridData.IsEnabled = true;
                    }));
                });
        }

        private string RemoveAccents(string text)
        {
            StringBuilder sbReturn = new StringBuilder();
            char[] arrayText = text.Normalize(NormalizationForm.FormD).ToCharArray();
            foreach (char letter in arrayText)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(letter) != UnicodeCategory.NonSpacingMark)
                {
                    _ = sbReturn.Append(letter);
                }
            }
            return sbReturn.ToString();
        }

        private List<SearchResult> LoadData(string SearchElement, bool IsSteam, bool IsExophase)
        {
            List<SearchResult> results = new List<SearchResult>();

            try
            {
                if (IsSteam)
                {
                    results = SteamAchievements.SearchGame(SearchElement);
                }

                if (IsExophase)
                {
                    results = ExophaseAchievements.SearchGame(SearchElement);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return results;
        }


        private void Button_ClickWeb(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = Process.Start((string)((FrameworkElement)sender).Tag);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
    }
}
