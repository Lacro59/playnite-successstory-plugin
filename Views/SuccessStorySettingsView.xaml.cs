using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using SuccessStory.Clients;
using SuccessStory.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using System.Diagnostics;

namespace SuccessStory.Views
{
    public partial class SuccessStorySettingsView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        IPlayniteAPI PlayniteApi;
        private static IResourceProvider resources = new ResourceProvider();
        SuccessStorySettings settings;

        string PluginUserDataPath;
        AchievementsDatabase achievementsDatabase;

        public static bool WithoutMessage = false;
        public static CancellationTokenSource tokenSource;
        private CancellationToken ct;

        int SteamTotal;
        int SteamTotalAchievements;
        int GogTotal;
        int GogTotalAchievements;
        int OriginTotal;
        int OriginTotalAchievements;
        int XboxTotal;
        int XboxTotalAchievements;
        int RetroAchievementsTotal;
        int RetroAchievementsTotalAchievements;

        int LocalTotal;
        int LocalTotalAchievements;


        public SuccessStorySettingsView(SuccessStory plugin, IPlayniteAPI PlayniteApi, string PluginUserDataPath, SuccessStorySettings settings)
        {
            this.PlayniteApi = PlayniteApi;
            this.PluginUserDataPath = PluginUserDataPath;
            this.settings = settings;

            achievementsDatabase = new AchievementsDatabase(plugin, PlayniteApi, settings, PluginUserDataPath);

            InitializeComponent();

            DataLoad.Visibility = Visibility.Collapsed;

            SetTotal();

            switch (settings.NameSorting)
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
                    cbDefaultSorting.Text = resources.GetString("LOCSucessStorylvGamesProgression");
                    break;
            }
        }

        private void SetTotal()
        {
            SteamTotal = 0;
            SteamTotalAchievements = 0;
            GogTotal = 0;
            GogTotalAchievements = 0;
            OriginTotal = 0;
            OriginTotalAchievements = 0;
            XboxTotal = 0;
            XboxTotalAchievements = 0;
            RetroAchievementsTotal = 0;
            RetroAchievementsTotalAchievements = 0;

            LocalTotal = 0;
            LocalTotalAchievements = 0;

            foreach (var game in PlayniteApi.Database.Games)
            {
                string GameSourceName = PlayniteTools.GetSourceName(game, PlayniteApi);

                switch (GameSourceName.ToLower())
                {
                    case "steam":
                        SteamTotal += 1;
                        if (achievementsDatabase.VerifAchievementsLoad(game.Id))
                        {
                            SteamTotalAchievements += 1;
                        }
                        break;
                    case "gog":
                        GogTotal += 1;
                        if (achievementsDatabase.VerifAchievementsLoad(game.Id))
                        {
                            GogTotalAchievements += 1;
                        }
                        break;
                    case "origin":
                        OriginTotal += 1;
                        if (achievementsDatabase.VerifAchievementsLoad(game.Id))
                        {
                            OriginTotalAchievements += 1;
                        }
                        break;
                    case "xbox":
                        XboxTotal += 1;
                        if (achievementsDatabase.VerifAchievementsLoad(game.Id))
                        {
                            XboxTotalAchievements += 1;
                        }
                        break;
                    case "retroachievements":
                        RetroAchievementsTotal += 1;
                        if (achievementsDatabase.VerifAchievementsLoad(game.Id))
                        {
                            RetroAchievementsTotalAchievements += 1;
                        }
                        break;
                    case "playnite":
                        LocalTotal += 1;
                        if (achievementsDatabase.VerifAchievementsLoad(game.Id))
                        {
                            LocalTotalAchievements += 1;
                        }
                        break;
                }
            }

            SteamLoad.Content = SteamTotalAchievements + "/" + SteamTotal;
            GogLoad.Content = GogTotalAchievements + "/" + GogTotal;
            OriginLoad.Content = OriginTotalAchievements + "/" + OriginTotal;
            XboxLoad.Content = XboxTotalAchievements + "/" + XboxTotal;
            RetroAchievementsLoad.Content = RetroAchievementsTotalAchievements + "/" + RetroAchievementsTotal;
            LocalLoad.Content = LocalTotalAchievements + "/" + LocalTotal;
        }

        private void Button_Click_Get_All(object sender, RoutedEventArgs e)
        {
            SteamLoad.Content = 0 + "/" + SteamTotal;
            GogLoad.Content = 0 + "/" + GogTotal;
            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("All", true);
            SetTotal();
        }
        private void Button_Click_All(object sender, RoutedEventArgs e)
        {
            SteamLoad.Content = 0 + "/" + SteamTotal;
            GogLoad.Content = 0 + "/" + GogTotal;
            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("All");
            SetTotal();
        }

        private void Button_Click_Get_Installed(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings);

            SteamLoad.Content = 0 + "/" + SteamTotal;
            GogLoad.Content = 0 + "/" + GogTotal;
            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("AllInstalled", true);
            SetTotal();
        }
        private void Button_Click_All_Installed(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings);

            SteamLoad.Content = 0 + "/" + SteamTotal;
            GogLoad.Content = 0 + "/" + GogTotal;
            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("AllInstalled");
            SetTotal();
        }

        private void Button_Click_Get_Recent(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings);

            SteamLoad.Content = 0 + "/" + SteamTotal;
            GogLoad.Content = 0 + "/" + GogTotal;
            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("AllRecent", true);
            SetTotal();
        }
        private void Button_Click_All_Recent(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings);

            SteamLoad.Content = 0 + "/" + SteamTotal;
            GogLoad.Content = 0 + "/" + GogTotal;
            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("AllRecent");
            SetTotal();
        }

        private void Button_Click_Get_Steam(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings, "Steam");

            SteamLoad.Content = 0 + "/" + SteamTotal;
            RefreshData("Steam", true);
            SetTotal();
        }
        private void Button_Click_Steam(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings, "Steam");

            SteamLoad.Content = 0 + "/" + SteamTotal;
            RefreshData("Steam");
            SetTotal();
        }

        private void Button_Click_Get_Gog(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings, "GOG");

            GogLoad.Content = 0 + "/" + GogTotal;
            RefreshData("GOG", true);
            SetTotal();
        }
        private void Button_Click_Gog(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings, "GOG");

            GogLoad.Content = 0 + "/" + GogTotal;
            RefreshData("GOG");
            SetTotal();
        }

        private void Button_Click_Get_RetroAchievements(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings, "RetroAchievements");

            RetroAchievementsLoad.Content = 0 + "/" + RetroAchievementsTotal;
            RefreshData("RetroAchievements", true);
            SetTotal();
        }
        private void Button_Click_RetroAchievements(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings, "RetroAchievements");

            RetroAchievementsLoad.Content = 0 + "/" + RetroAchievementsTotal;
            RefreshData("RetroAchievements");
            SetTotal();
        }

        private void Button_Click_Get_Origin(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings, "Origin");

            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("Origin", true);
            SetTotal();
        }
        private void Button_Click_Origin(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings, "Origin");

            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("Origin");
            SetTotal();
        }

        private void Button_Click_Get_Xbox(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings, "Xbox");

            XboxLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("Xbox", true);
            SetTotal();
        }
        private void Button_Click_Xbox(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings, "Xbox");

            XboxLoad.Content = 0 + "/" + XboxTotal;
            RefreshData("Xbox");
            SetTotal();
        }

        private void Button_Click_Get_Local(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings, "Playnite");

            LocalLoad.Content = 0 + "/" + LocalTotal;
            RefreshData("Playnite", true);
            SetTotal();
        }
        private void Button_Click_Local(object sender, RoutedEventArgs e)
        {
            achievementsDatabase.InitializeMultipleAdd(settings, "Playnite");

            LocalLoad.Content = 0 + "/" + LocalTotal;
            RefreshData("Playnite");
            SetTotal();
        }

        private void ButtonCancelTask_Click(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
        }

        internal void RefreshData(string SourceName, bool IsGet = false)
        {
#if DEBUG
            logger.Info($"SuccessStory - RefreshData() - Start");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            SuccessStory.isFirstLoad = false;

            SuccessStorySettings.IsEnabled = false;

            pbDataLoad.IsIndeterminate = false;
            pbDataLoad.Minimum = 0;
            pbDataLoad.Value = 0;

            DataLoad.Visibility = Visibility.Visible;
            tcSettings.Visibility = Visibility.Hidden;

            tokenSource = new CancellationTokenSource();
            ct = tokenSource.Token;

            bool IsFirstLoop = true;

            var taskSystem = Task.Run(() =>
            {
                try
                {
                    // filter games
                    IEnumerable<Game> FilterDatabaseGame = null;
                    switch (SourceName.ToLower())
                    {
                        case "all":
                            FilterDatabaseGame = PlayniteApi.Database.Games;
                            break;

                        case "allrecent":
                            FilterDatabaseGame = PlayniteApi.Database.Games.Where(
                                x => x.LastActivity > DateTime.Now.AddMonths(-2) || (x.Added != null && x.Added > DateTime.Now.AddMonths(-2))
                            );
                            break;

                        case "allinstalled":
                            FilterDatabaseGame = PlayniteApi.Database.Games.Where(x => x.IsInstalled);
                            break;

                        default:
                            FilterDatabaseGame = PlayniteApi.Database.Games.Where(
                                x => PlayniteTools.GetSourceName(x, PlayniteApi).ToLower() == SourceName.ToLower()
                            );
                            break;
                    }

                    Application.Current.Dispatcher.Invoke(() => { pbDataLoad.Maximum = FilterDatabaseGame.Count(); });
#if DEBUG
                    logger.Debug($"SuccessStory - FilterDatabaseGame: {FilterDatabaseGame.Count()}");
#endif
                    foreach (var game in FilterDatabaseGame)
                    {
                        try
                        {
                            if (SourceName.ToLower() == "steam" && IsFirstLoop)
                            {
#if DEBUG
                                logger.Debug($"SuccessStory - Check Steam profil with {game.GameId}");
#endif

                                SteamAchievements steamAPI = new SteamAchievements(PlayniteApi, settings, PluginUserDataPath);
                                if (!steamAPI.CheckIsPublic(int.Parse(game.GameId)))
                                {
                                    AchievementsDatabase.ListErrors.Add(resources.GetString("LOCSucessStoryNotificationsSteamPrivate"));
                                    break;
                                }
                                IsFirstLoop = false;
                            }

                            // Respect API limitation
                            Thread.Sleep(1000);

                            if (IsGet)
                            {
                                // Add only it's not loaded
                                if (!achievementsDatabase.VerifAchievementsLoad(game.Id))
                                {
                                    achievementsDatabase.Add(game, settings);
                                }
                            }
                            else
                            {
                                achievementsDatabase.Remove(game);
                                achievementsDatabase.Add(game, settings);
                            }

                            Application.Current.Dispatcher.Invoke(new Action(() => { pbDataLoad.Value += 1; }));
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, "SuccessStory", $"Error on RefreshData({SourceName}, {IsGet}) for {game.Name}");
                        }

                        if (ct.IsCancellationRequested)
                        {
                            logger.Info($"IsCancellationRequested for RefreshData({ SourceName}, { IsGet})");
                            break;
                        }
                    }
                }
                catch(Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", $"Error on RefreshData({SourceName}, {IsGet})");
                }
            }, tokenSource.Token)
            .ContinueWith(antecedent =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    DataLoad.Visibility = Visibility.Collapsed;
                    tcSettings.Visibility = Visibility.Visible;

                    if (!WithoutMessage)
                    {
                        if (AchievementsDatabase.ListErrors.Get() != string.Empty)
                        {
                            PlayniteApi.Dialogs.ShowErrorMessage(AchievementsDatabase.ListErrors.Get(), "SuccessStory errors");
                        }
                        else
                        {
                            PlayniteApi.Dialogs.ShowMessage((string)ResourceProvider.GetResource("LOCSuccessStoryRefreshDataMessage"), "Success Story");
                        }
                    }

                    SetTotal();
                    
                    SuccessStorySettings.IsEnabled = true;
#if DEBUG
                    stopwatch.Stop();
                    logger.Debug($"SuccessStory - RefreshData() - End - {stopwatch.Elapsed}");
#endif
                }));
            });
        }


        private void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;

            if ((cb.Name == "Sc_IntegrationInButtonDetails") && (bool)cb.IsChecked)
            {
                Sc_IntegrationInDescriptionOnlyIcon.IsChecked = false;
            }
            if ((cb.Name == "Sc_IntegrationInDescriptionOnlyIcon") && (bool)cb.IsChecked)
            {
                Sc_IntegrationInButtonDetails.IsChecked = false;
            }
            if ((cb.Name == "Sc_IntegrationInDescriptionWithToggle") && (bool)cb.IsChecked)
            {

            }

            if ((cb.Name == "Sc_IntegrationInDescription") && (bool)cb.IsChecked)
            {
                Sc_IntegrationInCustomTheme.IsChecked = false;
            }

            if ((cb.Name == "Sc_IntegrationInCustomTheme") && (bool)cb.IsChecked)
            {
                Sc_IntegrationInDescription.IsChecked = false;
            }
        }

        private void CheckboxGraphicType_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;

            if ((cb.Name == "Sc_AllMonth") && (bool)cb.IsChecked)
            {
                Sc_AllDay.IsChecked = false;
            }
            else if ((cb.Name == "Sc_AllMonth") && !(bool)cb.IsChecked)
            {
                Sc_AllDay.IsChecked = true;
            }

            if ((cb.Name == "Sc_AllDay") && (bool)cb.IsChecked)
            {
                Sc_AllMonth.IsChecked = false;
            }
            else if ((cb.Name == "Sc_AllDay") && !(bool)cb.IsChecked)
            {
                Sc_AllMonth.IsChecked = true;
            }
        }

        private void cbDefaultSorting_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            settings.NameSorting = ((ComboBoxItem)cbDefaultSorting.SelectedItem).Tag.ToString();
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
}