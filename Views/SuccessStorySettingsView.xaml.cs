using Playnite.SDK;
using SuccessStory.Models;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SuccessStory
{
    public partial class SuccessStorySettingsView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        IPlayniteAPI PlayniteApi;
        SuccessStorySettings settings;

        string PluginUserDataPath;
        AchievementsDatabase AchievementsDatabase;

        int SteamTotal;
        int SteamTotalAchievements;
        int GogTotal;
        int GogTotalAchievements;
        int OriginTotal;
        int OriginTotalAchievements;


        public SuccessStorySettingsView(IPlayniteAPI PlayniteApi, string PluginUserDataPath, SuccessStorySettings settings)
        {
            this.PlayniteApi = PlayniteApi;
            this.PluginUserDataPath = PluginUserDataPath;
            this.settings = settings;

            AchievementsDatabase = new AchievementsDatabase(PlayniteApi, PluginUserDataPath);

            InitializeComponent();

            SetTotal();

            SuccessStoryLoad.Visibility = Visibility.Hidden;
        }

        internal void SetTotal()
        {
            SteamTotal = 0;
            SteamTotalAchievements = 0;
            GogTotal = 0;
            GogTotalAchievements = 0;
            OriginTotal = 0;
            OriginTotalAchievements = 0;
            foreach (var game in PlayniteApi.Database.Games)
            {
                if (game.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                {
                    switch (game.Source.Name.ToLower())
                    {
                        case "steam":
                            SteamTotal += 1;
                            if (AchievementsDatabase.VerifAchievementsLoad(game.Id))
                            {
                                SteamTotalAchievements += 1;
                            }
                            break;
                        case "gog":
                            GogTotal += 1;
                            if (AchievementsDatabase.VerifAchievementsLoad(game.Id))
                            {
                                GogTotalAchievements += 1;
                            }
                            break;
                        case "origin":
                            OriginTotal += 1;
                            if (AchievementsDatabase.VerifAchievementsLoad(game.Id))
                            {
                                OriginTotalAchievements += 1;
                            }
                            break;
                    }
                }
            }
            SteamLoad.Content = SteamTotalAchievements + "/" + SteamTotal;
            GogLoad.Content = GogTotalAchievements + "/" + GogTotal;
            OriginLoad.Content = OriginTotalAchievements + "/" + OriginTotal;
        }

        private void Button_Click_All(object sender, RoutedEventArgs e)
        {
            SteamLoad.Content = 0 + "/" + SteamTotal;
            GogLoad.Content = 0 + "/" + GogTotal;
            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("All");
            SetTotal();
        }


        private void Button_Click_All_Recent(object sender, RoutedEventArgs e)
        {
            AchievementsDatabase.InitializeMultipleAdd(settings);

            SteamLoad.Content = 0 + "/" + SteamTotal;
            GogLoad.Content = 0 + "/" + GogTotal;
            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("AllRecent");
            SetTotal();
        }

        private void Button_Click_Steam(object sender, RoutedEventArgs e)
        {
            AchievementsDatabase.InitializeMultipleAdd(settings, "Steam");

            SteamLoad.Content = 0 + "/" + SteamTotal;
            RefreshData("Steam");
            SetTotal();
        }

        private void Button_Click_Gog(object sender, RoutedEventArgs e)
        {
            AchievementsDatabase.InitializeMultipleAdd(settings, "GOG");

            GogLoad.Content = 0 + "/" + GogTotal;
            RefreshData("GOG");
            SetTotal();
        }

        private void Button_Click_Origin(object sender, RoutedEventArgs e)
        {
            AchievementsDatabase.InitializeMultipleAdd(settings, "Origin");

            OriginLoad.Content = 0 + "/" + OriginTotal;
            RefreshData("Origin");
            SetTotal();
        }

        internal void RefreshData(string SourceName)
        {
            // ProgressBar
            SuccessStoryLoad.Visibility = Visibility.Visible;
            SuccessStoryLoad.Value = 0;
            SuccessStoryLoad.Maximum = PlayniteApi.Database.Games.Count;

            SuccessStorySettings.IsEnabled = false;


            foreach (var game in PlayniteApi.Database.Games)
            {
                if (game.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000") 
                    && (game.Source.Name.ToLower() == SourceName.ToLower() || SourceName.ToLower() == "all" || SourceName.ToLower() == "allrecent"))
                {
                    bool isOK = true;
                    if (SourceName.ToLower() == "allrecent")
                    {
                        if (
                            (game.LastActivity != null && game.LastActivity > DateTime.Now.AddMonths(-1)) ||
                            (game.Added != null && game.Added > DateTime.Now.AddMonths(-1))
                            )
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
                        Dispatcher.Invoke(new Action(() =>
                        {
                            // Prevent HTTP 429 with limit request per minutes.
                            Thread.Sleep(1000);

                            AchievementsDatabase.Remove(game);
                            AchievementsDatabase.Add(game, settings);
                        }), DispatcherPriority.ContextIdle, null);
                    }
                }
                SuccessStoryLoad.Value += 1;
            }


            if (AchievementsDatabase.ListErrors.Get() != "")
            {
                PlayniteApi.Dialogs.ShowErrorMessage(AchievementsDatabase.ListErrors.Get(), "SuccesStory errors on " + SourceName);
            }
            else
            {
                PlayniteApi.Dialogs.ShowMessage((string)ResourceProvider.GetResource("LOCSucessStoryRefreshDataMessage"), "Success Story");
            }


            SuccessStoryLoad.Visibility = Visibility.Hidden;
            SuccessStorySettings.IsEnabled = true;
        }
    }
}