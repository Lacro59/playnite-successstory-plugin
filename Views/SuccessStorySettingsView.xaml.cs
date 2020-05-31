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

        public SuccessStorySettingsView(IPlayniteAPI PlayniteApi, string PluginUserDataPath, SuccessStorySettings settings)
        {
            this.PlayniteApi = PlayniteApi;
            this.PluginUserDataPath = PluginUserDataPath;
            this.settings = settings;

            AchievementsDatabase = new AchievementsDatabase(PlayniteApi, PluginUserDataPath);

            InitializeComponent();

            SuccessStoryLoad.Visibility = Visibility.Hidden;
        }

        private void Button_Click_All(object sender, RoutedEventArgs e)
        {
            RefreshData("All");
        }

        private void Button_Click_Steam(object sender, RoutedEventArgs e)
        {
            RefreshData("Steam");
        }

        private void Button_Click_Gog(object sender, RoutedEventArgs e)
        {
            RefreshData("GOG");
        }

        private void Button_Click_Origin(object sender, RoutedEventArgs e)
        {
            RefreshData("Origin");
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
                    && (game.Source.Name.ToLower() == SourceName.ToLower() || SourceName.ToLower() == "all"))
                {
                    // Prevent HTTP 429 with 60 request max per minutes.
                    Thread.Sleep(1000);

                    Dispatcher.Invoke(new Action(() => {
                        AchievementsDatabase.Remove(game);
                        AchievementsDatabase.Add(game, settings);
                    }), DispatcherPriority.ContextIdle, null);
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