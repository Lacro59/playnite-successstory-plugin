using Playnite.SDK;
using SuccessStory.Models;
using System.Windows;
using System.Windows.Controls;


namespace SuccessStory
{
    public partial class SuccessStorySettingsView : UserControl
    {
        IPlayniteAPI PlayniteApi;
        string PluginUserDataPath;

        public SuccessStorySettingsView(IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            this.PlayniteApi = PlayniteApi;
            this.PluginUserDataPath = PluginUserDataPath;

            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AchievementsDatabase AchievementsDatabase = new AchievementsDatabase(PlayniteApi, PluginUserDataPath);

            AchievementsDatabase.ResetData();

            foreach (var game in PlayniteApi.Database.Games)
            {
                AchievementsDatabase.Add(game);
            }

            PlayniteApi.Dialogs.ShowMessage("Database has been reset.", "Success Story");
        }
    }
}