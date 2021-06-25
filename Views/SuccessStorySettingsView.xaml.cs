using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPluginsShared;
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
using SuccessStory.Services;

namespace SuccessStory.Views
{
    public partial class SuccessStorySettingsView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private IPlayniteAPI _PlayniteApi;

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        private ExophaseAchievements exophaseAchievements = new ExophaseAchievements();

        private string _PluginUserDataPath;

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
        int Rpcs3Total;
        int Rpcs3TotalAchievements;

        int LocalTotal;
        int LocalTotalAchievements;


        public SuccessStorySettingsView(SuccessStory plugin, IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            _PlayniteApi = PlayniteApi;
            _PluginUserDataPath = PluginUserDataPath;

            InitializeComponent();

            DataLoad.Visibility = Visibility.Collapsed;

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
            Rpcs3Total = 0;
            Rpcs3TotalAchievements = 0;

            LocalTotal = 0;
            LocalTotalAchievements = 0;

            try
            {
                foreach (var game in _PlayniteApi.Database.Games)
                {
                    string GameSourceName = PlayniteTools.GetSourceName(_PlayniteApi, game);

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
                Common.LogError(ex, false);
            }

            SteamLoad.Content = SteamTotalAchievements + "/" + SteamTotal;
            GogLoad.Content = GogTotalAchievements + "/" + GogTotal;
            OriginLoad.Content = OriginTotalAchievements + "/" + OriginTotal;
            XboxLoad.Content = XboxTotalAchievements + "/" + XboxTotal;
            RetroAchievementsLoad.Content = RetroAchievementsTotalAchievements + "/" + RetroAchievementsTotal;
            LocalLoad.Content = LocalTotalAchievements + "/" + LocalTotal;
            Rpcs3Load.Content = Rpcs3TotalAchievements + "/" + Rpcs3Total;
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
            PluginDatabase.InitializeMultipleAdd();

            SteamLoad.Content = 0 + "/" + SteamTotal;
            GogLoad.Content = 0 + "/" + GogTotal;
            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("AllInstalled", true);
            SetTotal();
        }
        private void Button_Click_All_Installed(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd();

            SteamLoad.Content = 0 + "/" + SteamTotal;
            GogLoad.Content = 0 + "/" + GogTotal;
            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("AllInstalled");
            SetTotal();
        }

        private void Button_Click_Get_Recent(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd();

            SteamLoad.Content = 0 + "/" + SteamTotal;
            GogLoad.Content = 0 + "/" + GogTotal;
            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("AllRecent", true);
            SetTotal();
        }
        private void Button_Click_All_Recent(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd();

            SteamLoad.Content = 0 + "/" + SteamTotal;
            GogLoad.Content = 0 + "/" + GogTotal;
            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("AllRecent");
            SetTotal();
        }

        private void Button_Click_Get_Steam(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd("Steam");

            SteamLoad.Content = 0 + "/" + SteamTotal;
            RefreshData("Steam", true);
            SetTotal();
        }
        private void Button_Click_Steam(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd("Steam");

            SteamLoad.Content = 0 + "/" + SteamTotal;
            RefreshData("Steam");
            SetTotal();
        }

        private void Button_Click_Get_Gog(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd("GOG");

            GogLoad.Content = 0 + "/" + GogTotal;
            RefreshData("GOG", true);
            SetTotal();
        }
        private void Button_Click_Gog(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd("GOG");

            GogLoad.Content = 0 + "/" + GogTotal;
            RefreshData("GOG");
            SetTotal();
        }

        private void Button_Click_Get_RetroAchievements(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd("RetroAchievements");

            RetroAchievementsLoad.Content = 0 + "/" + RetroAchievementsTotal;
            RefreshData("RetroAchievements", true);
            SetTotal();
        }
        private void Button_Click_RetroAchievements(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd("RetroAchievements");

            RetroAchievementsLoad.Content = 0 + "/" + RetroAchievementsTotal;
            RefreshData("RetroAchievements");
            SetTotal();
        }

        private void Button_Click_Get_Origin(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd("Origin");

            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("Origin", true);
            SetTotal();
        }
        private void Button_Click_Origin(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd("Origin");

            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("Origin");
            SetTotal();
        }

        private void Button_Click_Get_Xbox(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd( "Xbox");

            XboxLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("Xbox", true);
            SetTotal();
        }
        private void Button_Click_Xbox(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd("Xbox");

            XboxLoad.Content = 0 + "/" + XboxTotal;
            RefreshData("Xbox");
            SetTotal();
        }

        private void Button_Click_Get_Rpcs3(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd("Rpcs3");

            Rpcs3Load.Content = 0 + "/" + Rpcs3Total;
            RefreshData("Rpcs3", true);
            SetTotal();
        }
        private void Button_Click_Rpcs3(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd("Rpcs3");

            Rpcs3Load.Content = 0 + "/" + Rpcs3Total;
            RefreshData("Rpcs3");
            SetTotal();
        }

        private void Button_Click_Get_Local(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd("Playnite");

            LocalLoad.Content = 0 + "/" + LocalTotal;
            RefreshData("Playnite", true);
            SetTotal();
        }
        private void Button_Click_Local(object sender, RoutedEventArgs e)
        {
            PluginDatabase.InitializeMultipleAdd("Playnite");

            LocalLoad.Content = 0 + "/" + LocalTotal;
            RefreshData("Playnite");
            SetTotal();
        }

        private void ButtonCancelTask_Click(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
        }

        private void RefreshData(string SourceName, bool IsGet = false)
        {
            SuccessStoryDatabase.ListErrors = new CumulErrors();

#if DEBUG
            Common.LogDebug(true, $"RefreshData() - Start");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
#endif

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
                            FilterDatabaseGame = _PlayniteApi.Database.Games;
                            break;

                        case "allrecent":
                            FilterDatabaseGame = _PlayniteApi.Database.Games.Where(
                                x => x.LastActivity > DateTime.Now.AddMonths(-2) || (x.Added != null && x.Added > DateTime.Now.AddMonths(-2))
                            );
                            break;

                        case "allinstalled":
                            FilterDatabaseGame = _PlayniteApi.Database.Games.Where(x => x.IsInstalled);
                            break;

                        default:
                            FilterDatabaseGame = _PlayniteApi.Database.Games.Where(
                                x => PlayniteTools.GetSourceName(_PlayniteApi, x).ToLower() == SourceName.ToLower()
                            );
                            break;
                    }

                    Application.Current.Dispatcher.BeginInvoke((Action)delegate { pbDataLoad.Maximum = FilterDatabaseGame.Count(); });

                    Common.LogDebug(true, $"FilterDatabaseGame: {FilterDatabaseGame.Count()}");

                    foreach (var game in FilterDatabaseGame)
                    {
                        try
                        {
                            if (SourceName.ToLower() == "steam" && IsFirstLoop)
                            {
                                Common.LogDebug(true, $"Check Steam profil with {game.GameId}");

                                SteamAchievements steamAPI = new SteamAchievements();
                                int AppId = 0;
                                int.TryParse(game.GameId, out AppId);
                                if (!steamAPI.CheckIsPublic(AppId))
                                {
                                    SuccessStoryDatabase.ListErrors.Add(resources.GetString("LOCSuccessStoryNotificationsSteamPrivate"));
                                    break;
                                }
                                IsFirstLoop = false;
                            }

                            // Respect API limitation
                            Thread.Sleep(1000);

                            if (IsGet)
                            {
                                // Add only it's not loaded
                                if (!PluginDatabase.VerifAchievementsLoad(game.Id))
                                {
                                    PluginDatabase.Get(game);
                                }
                            }
                            else
                            {
                                PluginDatabase.Remove(game);
                                PluginDatabase.Get(game);
                            }

                            Application.Current.Dispatcher.BeginInvoke((Action)delegate { pbDataLoad.Value += 1; });
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, $"Error on RefreshData({SourceName}, {IsGet}) for {game.Name}");
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
                    Common.LogError(ex, false, $"Error on RefreshData({SourceName}, {IsGet})");
                }
            }, tokenSource.Token)
            .ContinueWith(antecedent =>
            {
                Application.Current.Dispatcher.BeginInvoke((Action)delegate
                { 
                    DataLoad.Visibility = Visibility.Collapsed;
                    tcSettings.Visibility = Visibility.Visible;

                    if (!WithoutMessage)
                    {
                        if (SuccessStoryDatabase.ListErrors.Get() != string.Empty)
                        {
                            _PlayniteApi.Dialogs.ShowErrorMessage(SuccessStoryDatabase.ListErrors.Get(), "SuccessStory errors");
                        }
                        else
                        {
                            _PlayniteApi.Dialogs.ShowMessage((string)ResourceProvider.GetResource("LOCSuccessStoryRefreshDataMessage"), "Success Story");
                        }
                    }

                    SetTotal();
                    
                    SuccessStorySettings.IsEnabled = true;
#if DEBUG
                    stopwatch.Stop();
                    Common.LogDebug(true, $"RefreshData() - End - {stopwatch.Elapsed}");
#endif
                });
            });
        }


        private void cbDefaultSorting_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PluginDatabase.PluginSettings.Settings.NameSorting = ((ComboBoxItem)cbDefaultSorting.SelectedItem).Tag.ToString();
        }


        private void ButtonSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            string SelectedFolder = _PlayniteApi.Dialogs.SelectFolder();
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
            return exophaseAchievements.GetIsUserLoggedIn();
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