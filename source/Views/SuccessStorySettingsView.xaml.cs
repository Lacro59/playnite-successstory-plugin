using Playnite.SDK;
using CommonPluginsShared;
using SuccessStory.Clients;
using SuccessStory.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Diagnostics;
using SuccessStory.Services;
using CommonPluginsShared.Models;
using Playnite.SDK.Data;
using System.Windows.Media;
using Playnite.SDK.Models;
using System.IO;
using System.Windows.Documents;
using System.Drawing.Imaging;

namespace SuccessStory.Views
{
    public partial class SuccessStorySettingsView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();


        public static SolidColorBrush RarityUncommonColor;
        public static SolidColorBrush RarityRareColor;
        public static SolidColorBrush RarityUltraRareColor;

        public static CompletionStatus completionStatus;

        private TextBlock tbControl;


        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        private ExophaseAchievements exophaseAchievements = new ExophaseAchievements();

        public static List<Folder> LocalPath = new List<Folder>();
        public static List<Folder> Rpcs3Path = new List<Folder>();

        private List<GameAchievements> IgnoredGames;

        public static bool WithoutMessage = false;


        public SuccessStorySettingsView(SuccessStory plugin)
        {
            InitializeComponent();

            PART_WowRegion.Text = PluginDatabase.PluginSettings.Settings.WowRegions.Find(x => x.IsSelected)?.Name;
            PART_WowRealm.Text = PluginDatabase.PluginSettings.Settings.WowRealms.Find(x => x.IsSelected)?.Name;

            PART_SelectorColorPicker.OnlySimpleColor = true;

            RarityUncommonColor = PluginDatabase.PluginSettings.Settings.RarityUncommonColor;
            RarityRareColor = PluginDatabase.PluginSettings.Settings.RarityRareColor;
            RarityUltraRareColor = PluginDatabase.PluginSettings.Settings.RarityUltraRareColor;


            switch (PluginDatabase.PluginSettings.Settings.NameSorting)
            {
                case "Name":
                    cbDefaultSorting.Text = resources.GetString("LOCGameNameTitle");
                    break;
                case "LastActivity":
                    cbDefaultSorting.Text = resources.GetString("LOCLastPlayed");
                    break;
                case "SourceName":
                    cbDefaultSorting.Text = resources.GetString("LOCSourceLabel");
                    break;
                case "ProgressionValue":
                    cbDefaultSorting.Text = resources.GetString("LOCSuccessStorylvGamesProgression");
                    break;
            }


            var task = Task.Run(() => CheckLogged())
                .ContinueWith(antecedent =>
                {
                    this.Dispatcher.Invoke(new Action(() => 
                    {
                        if (antecedent.Result)
                        {
                            lIsAuth.Content = resources.GetString("LOCLoggedIn");
                        }
                        else
                        {
                            lIsAuth.Content = resources.GetString("LOCNotLoggedIn");
                        }
                    }));
                });


            LocalPath = Serialization.GetClone(PluginDatabase.PluginSettings.Settings.LocalPath);
            PART_ItemsControl.ItemsSource = LocalPath;

            Rpcs3Path = Serialization.GetClone(PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolders);
            PART_ItemsRpcs3Folder.ItemsSource = Rpcs3Path;
            if (Rpcs3Path.Count > 0)
            {
                PART_ItemsRpcs3Folder.Visibility = Visibility.Visible;
            }


            // Set ignored game
            IgnoredGames = Serialization.GetClone(PluginDatabase.Database.Where(x => x.IsIgnored).ToList());
            IgnoredGames.Sort((x, y) => x.Name.CompareTo(y.Name));
            PART_IgnoredGames.ItemsSource = IgnoredGames;


            // List features
            PART_FeatureAchievement.ItemsSource = PluginDatabase.PlayniteApi.Database.Features.OrderBy(x => x.Name);


            // List completation
            PART_CbCompletation.ItemsSource = PluginDatabase.PlayniteApi.Database.CompletionStatuses.ToList();
            PART_CbCompletation.SelectedIndex = PluginDatabase.PlayniteApi.Database.CompletionStatuses.ToList()
                .FindIndex(x => x.Id == PluginDatabase.PluginSettings.Settings.CompletionStatus100Percent?.Id);


            PART_Time.Source = BitmapExtensions.BitmapFromFile(Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "time.png"));
            PART_Percent.Source = BitmapExtensions.BitmapFromFile(Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "percent.png"));
        }


        private void cbDefaultSorting_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PluginDatabase.PluginSettings.Settings.NameSorting = ((ComboBoxItem)cbDefaultSorting.SelectedItem).Tag.ToString();
        }


        private void ButtonSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            string SelectedFolder = PluginDatabase.PlayniteApi.Dialogs.SelectFolder();
            if (!SelectedFolder.IsNullOrEmpty())
            {
                PART_Rpcs3Folder.Text = SelectedFolder;
                PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolder = SelectedFolder;
            }
        }


        private void Button_Click_Wiki(object sender, RoutedEventArgs e)
        {
            Process.Start((string)((FrameworkElement)sender).Tag);
        }


        #region Tag
        private void ButtonAddTag_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.AddTagAllGame();
        }

        private void ButtonRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.RemoveTagAllGame();
        }
        #endregion


        #region Exophase
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            lIsAuth.Content = resources.GetString("LOCLoginChecking");

            try
            {
                exophaseAchievements.Login();

                var task = Task.Run(() => CheckLogged())
                    .ContinueWith(antecedent =>
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            if (antecedent.Result)
                            {
                                lIsAuth.Content = resources.GetString("LOCLoggedIn");
                            }
                            else
                            {
                                lIsAuth.Content = resources.GetString("LOCNotLoggedIn");
                            }
                        }));
                    });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to authenticate user.");
            }
        }

        private bool CheckLogged()
        {
            return exophaseAchievements.IsConnected();
        }
        #endregion


        #region Local
        private void ButtonAddLocalFolder_Click(object sender, RoutedEventArgs e)
        {
            PART_ItemsControl.ItemsSource = null;
            LocalPath.Add(new Folder { FolderPath = "" });
            PART_ItemsControl.ItemsSource = LocalPath;
        }

        private void ButtonSelectLocalFolder_Click(object sender, RoutedEventArgs e)
        {
            int indexFolder = int.Parse(((Button)sender).Tag.ToString());

            string SelectedFolder = PluginDatabase.PlayniteApi.Dialogs.SelectFolder();
            if (!SelectedFolder.IsNullOrEmpty())
            {
                PART_ItemsControl.ItemsSource = null;
                LocalPath[indexFolder].FolderPath = SelectedFolder;
                PART_ItemsControl.ItemsSource = LocalPath;
            }
        }

        private void ButtonRemoveLocalFolder_Click(object sender, RoutedEventArgs e)
        {
            int indexFolder = int.Parse(((Button)sender).Tag.ToString());

            PART_ItemsControl.ItemsSource = null;
            LocalPath.RemoveAt(indexFolder);
            PART_ItemsControl.ItemsSource = LocalPath;
        }
        #endregion


        #region Rarity configuration
        private void BtPickColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                tbControl = ((StackPanel)((FrameworkElement)sender).Parent).Children.OfType<TextBlock>().FirstOrDefault();
                
                if (tbControl.Background is SolidColorBrush)
                {
                    Color color = ((SolidColorBrush)tbControl.Background).Color;
                    PART_SelectorColorPicker.SetColors(color);
                }
                
                PART_SelectorColor.Visibility = Visibility.Visible;
                PART_MiscTab.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void BtRestore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBlock tbControl = ((StackPanel)((FrameworkElement)sender).Parent).Children.OfType<TextBlock>().FirstOrDefault();

                switch ((string)((Button)sender).Tag)
                {
                    case "1":
                        tbControl.Background = Brushes.DarkGray;
                        RarityUncommonColor = Brushes.DarkGray;
                        break;

                    case "2":
                        tbControl.Background = Brushes.Gold;
                        RarityRareColor = Brushes.Gold;
                        break;

                    case "3":
                        tbControl.Background = Brushes.MediumPurple;
                        RarityUltraRareColor = Brushes.MediumPurple;
                        break;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void PART_TM_ColorOK_Click(object sender, RoutedEventArgs e)
        {
            Color color = default(Color);

            if (tbControl != null)
            {
                if (PART_SelectorColorPicker.IsSimpleColor)
                {
                    color = PART_SelectorColorPicker.SimpleColor;
                    tbControl.Background = new SolidColorBrush(color);

                    switch ((string)tbControl.Tag)
                    {
                        case "1":
                            RarityUncommonColor = new SolidColorBrush(color);
                            break;

                        case "2":
                            RarityRareColor = new SolidColorBrush(color);
                            break;

                        case "3":
                            RarityUltraRareColor = new SolidColorBrush(color);
                            break;
                    }
                }
            }
            else
            {
                logger.Warn("One control is undefined");
            }

            PART_SelectorColor.Visibility = Visibility.Collapsed;
            PART_MiscTab.Visibility = Visibility.Visible;
        }

        private void PART_TM_ColorCancel_Click(object sender, RoutedEventArgs e)
        {
            PART_SelectorColor.Visibility = Visibility.Collapsed;
            PART_MiscTab.Visibility = Visibility.Visible;
        }


        private void PART_SlidderRare_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            PART_SlidderUncommun.Minimum = PART_SlidderRare.Value;
        }

        private void PART_SlidderUltraRare_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            PART_SlidderRare.Minimum = PART_SlidderUltraRare.Value;
        }
        #endregion


        private void Button_Click_Remove(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(((Button)sender).Tag.ToString());
            GameAchievements gameAchievements = IgnoredGames[index];
            PluginDatabase.SetIgnored(gameAchievements);
            IgnoredGames.RemoveAt(index);

            PART_IgnoredGames.ItemsSource = null;
            PART_IgnoredGames.ItemsSource = IgnoredGames;
        }


        private void Button_Click_Cache(object sender, RoutedEventArgs e)
        {
            SuccessStory.TaskIsPaused = true;
            PluginDatabase.ClearCache();
        }

        private void PART_CbCompletation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            completionStatus = (CompletionStatus)PART_CbCompletation.SelectedItem;
        }


        private void Button_RefreshWowRealm(object sender, RoutedEventArgs e)
        {
            PluginDatabase.PluginSettings.Settings.WowRealms = WowAchievements.GetRealm(PART_WowRegion.Text);
            PART_WowRealm.Text = string.Empty;
        }


        #region Unlocked icon configuration
        private void PART_RemoveCustomIcon_Click(object sender, RoutedEventArgs e)
        {
            PART_IconUnlocked.Source = null;
            ((SuccessStorySettingsViewModel)this.DataContext).Settings.IconCustomLocked = string.Empty;
        }

        private void PART_AddCustomIcon_Click(object sender, RoutedEventArgs e)
        {
            var result = PluginDatabase.PlayniteApi.Dialogs.SelectImagefile();
            if (!result.IsNullOrEmpty())
            {
                try
                {
                    File.Copy(result, Path.Combine(PluginDatabase.Paths.PluginUserDataPath, Path.GetFileName(result)), true);
                    PART_IconUnlocked.Source = ImageSourceManagerPlugin.GetImage(Path.Combine(PluginDatabase.Paths.PluginUserDataPath, Path.GetFileName(result)), false);
                    ((SuccessStorySettingsViewModel)this.DataContext).Settings.IconCustomLocked = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, Path.GetFileName(result));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = (Hyperlink)sender;
            Process.Start((string)link.Tag);
        }
        #endregion


        #region RPCS3 folders
        private void ButtonAddRpcs3Folder_Click(object sender, RoutedEventArgs e)
        {
            PART_ItemsRpcs3Folder.Visibility = Visibility.Visible;
            PART_ItemsRpcs3Folder.ItemsSource = null;
            Rpcs3Path.Add(new Folder { FolderPath = "" });
            PART_ItemsRpcs3Folder.ItemsSource = Rpcs3Path;
        }

        private void ButtonSelectRpcs3Folder_Click(object sender, RoutedEventArgs e)
        {
            int indexFolder = int.Parse(((Button)sender).Tag.ToString());

            string SelectedFolder = PluginDatabase.PlayniteApi.Dialogs.SelectFolder();
            if (!SelectedFolder.IsNullOrEmpty())
            {
                PART_ItemsRpcs3Folder.ItemsSource = null;
                Rpcs3Path[indexFolder].FolderPath = SelectedFolder;
                PART_ItemsRpcs3Folder.ItemsSource = Rpcs3Path;
            }
        }

        private void ButtonRemoveRpcs3Folder_Click(object sender, RoutedEventArgs e)
        {
            int indexFolder = int.Parse(((Button)sender).Tag.ToString());

            PART_ItemsRpcs3Folder.ItemsSource = null;
            Rpcs3Path.RemoveAt(indexFolder);
            PART_ItemsRpcs3Folder.ItemsSource = Rpcs3Path;

            if (Rpcs3Path.Count == 0)
            {
                PART_ItemsRpcs3Folder.Visibility = Visibility.Collapsed;
            }
        }
        #endregion
    }


    public class BooleanAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            foreach (object value in values)
            {
                if ((value is bool) && (bool)value == false)
                {
                    return false;
                }
            }
            return true;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException("BooleanAndConverter is a OneWay converter.");
        }
    }
}