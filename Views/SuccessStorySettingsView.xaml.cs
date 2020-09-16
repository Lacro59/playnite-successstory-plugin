using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;


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

        private CancellationTokenSource tokenSource;
        private CancellationToken ct;

        int SteamTotal;
        int SteamTotalAchievements;
        int GogTotal;
        int GogTotalAchievements;
        int OriginTotal;
        int OriginTotalAchievements;
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
                    cbDefaultSorting.Text = resources.GetString("LOCSucessStorylvGamesName");
                    break;
                case "LastActivity":
                    cbDefaultSorting.Text = resources.GetString("LOCSucessStorylvGamesLastActivity");
                    break;
                case "SourceName":
                    cbDefaultSorting.Text = resources.GetString("LOCSucessStorylvGamesSourceName");
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
            RetroAchievementsTotal = 0;
            RetroAchievementsTotalAchievements = 0;

            LocalTotal = 0;
            LocalTotalAchievements = 0;

            List<Guid> ListEmulators = new List<Guid>();
            foreach (var item in PlayniteApi.Database.Emulators)
            {
                ListEmulators.Add(item.Id);
            }

            foreach (var game in PlayniteApi.Database.Games)
            {
                string GameSourceName = string.Empty;
                if (game.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                {
                    GameSourceName = game.Source.Name;

                    if (game.PlayAction != null && game.PlayAction.EmulatorId != null && ListEmulators.Contains(game.PlayAction.EmulatorId))
                    {
                        GameSourceName = "RetroAchievements";
                    }
                }
                else
                {
                    if (game.PlayAction != null && game.PlayAction.EmulatorId != null && ListEmulators.Contains(game.PlayAction.EmulatorId))
                    {
                        GameSourceName = "RetroAchievements";
                    }
                    else
                    {
                        GameSourceName = "Playnite";
                    }
                }
                
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
            SuccessStory.isFirstLoad = false;

            SuccessStorySettings.IsEnabled = false;

            List<Guid> ListEmulators = new List<Guid>();
            foreach (var item in PlayniteApi.Database.Emulators)
            {
                ListEmulators.Add(item.Id);
            }


            pbDataLoad.IsIndeterminate = false;
            pbDataLoad.Minimum = 0;
            pbDataLoad.Value = 0;
            pbDataLoad.Maximum = PlayniteApi.Database.Games.Count;

            DataLoad.Visibility = Visibility.Visible;
            tcSettings.Visibility = Visibility.Hidden;

            tokenSource = new CancellationTokenSource();
            ct = tokenSource.Token;

            var taskSystem = Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                foreach (var game in PlayniteApi.Database.Games)
                {
                    logger.Debug($"{game.Name}");

                    try
                    {
                        string GameSourceName = string.Empty;
                        if (game.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                        {
                            GameSourceName = game.Source.Name;

                            if (game.PlayAction != null && game.PlayAction.EmulatorId != null && ListEmulators.Contains(game.PlayAction.EmulatorId))
                            {
                                GameSourceName = "RetroAchievements";
                            }
                        }
                        else
                        {
                            if (game.PlayAction != null && game.PlayAction.EmulatorId != null && ListEmulators.Contains(game.PlayAction.EmulatorId))
                            {
                                GameSourceName = "RetroAchievements";
                            }
                            else
                            {
                                GameSourceName = "Playnite";
                            }
                        }

                        if (GameSourceName.ToLower() == SourceName.ToLower() || SourceName.ToLower() == "all" || SourceName.ToLower() == "allrecent" || SourceName.ToLower() == "allinstalled")
                        {
                            bool isOK = true;
                            if (SourceName.ToLower() == "allrecent")
                            {
                                if ((game.LastActivity != null && game.LastActivity > DateTime.Now.AddMonths(-2)) || (game.Added != null && game.Added > DateTime.Now.AddMonths(-2)))
                                {
                                    isOK = true;
                                }
                                else
                                {
                                    isOK = false;
                                }
                            }
                            if (SourceName.ToLower() == "allinstalled")
                            {
                                if (game.IsInstalled)
                                {
                                    isOK = true;
                                }
                                else
                                {
                                    isOK = false;
                                }
                            }

                            if (isOK)
                            {
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
                            }
                        }
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            pbDataLoad.Value += 1;
                        }));
                    }
                    catch(Exception ex)
                    {
                        Common.LogError(ex, "SuccessStory", $"Error on RefreshData({SourceName}, {IsGet}) for {game.Name}");
                    }
                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }
                }
            }, tokenSource.Token)
            .ContinueWith(antecedent =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    DataLoad.Visibility = Visibility.Collapsed;
                    tcSettings.Visibility = Visibility.Visible;

                    if (AchievementsDatabase.ListErrors.Get() != string.Empty)
                    {
                        PlayniteApi.Dialogs.ShowErrorMessage(AchievementsDatabase.ListErrors.Get(), "SuccessStory errors");
                    }
                    else
                    {
                        PlayniteApi.Dialogs.ShowMessage((string)ResourceProvider.GetResource("LOCSucessStoryRefreshDataMessage"), "Success Story");
                    }

                    SetTotal();
                    
                    SuccessStorySettings.IsEnabled = true;
                }));
            });
        }


        private void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;

            if ((cb.Name == "Sc_IntegrationInDescription") && (bool)cb.IsChecked)
            {
                Sc_IntegrationInCustomTheme.IsChecked = false;
                Sc_IntegrationInDescriptionWithToggle.IsChecked = false;
                //Sc_IntegrationInButton.IsChecked = false;
                //Sc_IntegrationInButtonDetails.IsChecked = false;
            }
            if ((cb.Name == "Sc_IntegrationInDescriptionWithToggle") && (bool)cb.IsChecked)
            {
                Sc_IntegrationInCustomTheme.IsChecked = false;
                Sc_IntegrationInDescription.IsChecked = false;
                Sc_IntegrationInButton.IsChecked = false;
                Sc_IntegrationInButtonDetails.IsChecked = false;
            }


            if ((cb.Name == "Sc_IntegrationInButton") && (bool)cb.IsChecked)
            {
                Sc_IntegrationInCustomTheme.IsChecked = false;
                //Sc_IntegrationInDescription.IsChecked = false;
                Sc_IntegrationInDescriptionWithToggle.IsChecked = false;
                Sc_IntegrationInButtonDetails.IsChecked = false;
            }

            if ((cb.Name == "Sc_IntegrationInButtonDetails") && (bool)cb.IsChecked)
            {
                Sc_IntegrationInCustomTheme.IsChecked = false;
                //Sc_IntegrationInDescription.IsChecked = false;
                Sc_IntegrationInDescriptionWithToggle.IsChecked = false;
                Sc_IntegrationInButton.IsChecked = false;
            }

            if ((cb.Name == "Sc_IntegrationInCustomTheme") && (bool)cb.IsChecked)
            {
                Sc_IntegrationInDescription.IsChecked = false;
                Sc_IntegrationInDescriptionWithToggle.IsChecked = false;
                Sc_IntegrationInButton.IsChecked = false;
                Sc_IntegrationInButtonDetails.IsChecked = false;
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