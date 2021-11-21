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
using System.Windows.Markup;
using Playnite.SDK.Models;

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

        private List<GameAchievements> IgnoredGames;

        public static bool WithoutMessage = false;


        int SteamTotal;
        int SteamTotalAchievements;
        int GogTotal;
        int GogTotalAchievements;
        int EpicTotal;
        int EpicTotalAchievements;
        int OriginTotal;
        int OriginTotalAchievements;
        int XboxTotal;
        int XboxTotalAchievements;
        int RetroAchievementsTotal;
        int RetroAchievementsTotalAchievements;
        int Rpcs3Total;
        int Rpcs3TotalAchievements;
        int PsnTotal;
        int PsnTotalAchievements;

        int LocalTotal;
        int LocalTotalAchievements;


        public SuccessStorySettingsView(SuccessStory plugin)
        {
            InitializeComponent();


            PART_SelectorColorPicker.OnlySimpleColor = true;

            RarityUncommonColor = PluginDatabase.PluginSettings.Settings.RarityUncommonColor;
            RarityRareColor = PluginDatabase.PluginSettings.Settings.RarityRareColor;
            RarityUltraRareColor = PluginDatabase.PluginSettings.Settings.RarityUltraRareColor;

            SetTotal();

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
        }

        private void SetTotal()
        {
            SteamTotal = 0;
            SteamTotalAchievements = 0;
            GogTotal = 0;
            GogTotalAchievements = 0;
            EpicTotal = 0;
            EpicTotalAchievements = 0;
            OriginTotal = 0;
            OriginTotalAchievements = 0;
            XboxTotal = 0;
            XboxTotalAchievements = 0;
            RetroAchievementsTotal = 0;
            RetroAchievementsTotalAchievements = 0;
            Rpcs3Total = 0;
            Rpcs3TotalAchievements = 0;
            PsnTotal = 0;
            PsnTotalAchievements = 0;

            LocalTotal = 0;
            LocalTotalAchievements = 0;

            try
            {
                foreach (var game in PluginDatabase.PlayniteApi.Database.Games)
                {
                    string GameSourceName = PlayniteTools.GetSourceName(game);

                    switch (GameSourceName.ToLower())
                    {
                        case "steam":
                            SteamTotal += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                SteamTotalAchievements += 1;
                            }
                            break;
                        case "gog":
                            GogTotal += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                GogTotalAchievements += 1;
                            }
                            break;
                        case "epic":
                            EpicTotal += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                EpicTotalAchievements += 1;
                            }
                            break;
                        case "origin":
                            OriginTotal += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                OriginTotalAchievements += 1;
                            }
                            break;
                        case "xbox":
                            XboxTotal += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                XboxTotalAchievements += 1;
                            }
                            break;
                        case "retroachievements":
                            RetroAchievementsTotal += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                RetroAchievementsTotalAchievements += 1;
                            }
                            break;
                        case "rpcs3":
                            Rpcs3Total += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                Rpcs3TotalAchievements += 1;
                            }
                            break;
                        case "playstation":
                            PsnTotal += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                PsnTotalAchievements += 1;
                            }
                            break;
                        case "playnite":
                            LocalTotal += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                LocalTotalAchievements += 1;
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "SuccessStory");
            }

            SteamLoad.Content = SteamTotalAchievements + "/" + SteamTotal;
            GogLoad.Content = GogTotalAchievements + "/" + GogTotal;
            EpicLoad.Content = EpicTotalAchievements + "/" + EpicTotal;
            OriginLoad.Content = OriginTotalAchievements + "/" + OriginTotal;
            XboxLoad.Content = XboxTotalAchievements + "/" + XboxTotal;
            RetroAchievementsLoad.Content = RetroAchievementsTotalAchievements + "/" + RetroAchievementsTotal;
            LocalLoad.Content = LocalTotalAchievements + "/" + LocalTotal;
            Rpcs3Load.Content = Rpcs3TotalAchievements + "/" + Rpcs3Total;
            PSNLoad.Content = PsnTotalAchievements + "/" + PsnTotal;
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
                Common.LogError(ex, false, true, "SuccessStory");
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
                Common.LogError(ex, false, true, "SuccessStory");
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

    public class OrderAchievementTypeToStringConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var source = (OrderAchievementType)value;
            switch (source)
            {
                case OrderAchievementType.AchievementName:
                    return ResourceProvider.GetString("LOCGameNameTitle");
                case OrderAchievementType.AchievementDateUnlocked:
                    return ResourceProvider.GetString("LOCSuccessStoryDateUnlocked");
                case OrderAchievementType.AchievementRarety:
                    return ResourceProvider.GetString("LOCSuccessStoryRarety");
                default:
                    return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class OrderTypeToStringConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var source = (OrderType)value;
            switch (source)
            {
                case OrderType.Ascending:
                    return ResourceProvider.GetString("LOCMenuSortAscending");
                case OrderType.Descending:
                    return ResourceProvider.GetString("LOCMenuSortDescending");
                default:
                    return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}