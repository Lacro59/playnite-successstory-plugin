using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PluginCommon;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Controls;

namespace SuccessStory
{
    public class SuccessStory : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SuccessStorySettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("cebe6d32-8c46-4459-b993-5a5189d60788");

        public static CumulErrors ListErrors = new CumulErrors();

        public SuccessStory(IPlayniteAPI api) : base(api)
        {
            settings = new SuccessStorySettings(this);

            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            Localization.SetPluginLanguage(pluginFolder, api.Paths.ConfigurationPath);
        }

        public override IEnumerable<ExtensionFunction> GetFunctions()
        {
            return new List<ExtensionFunction>
            {
                new ExtensionFunction(
                    "Success Story",
                    () =>
                    {
                        // Add code to be execute when user invokes this menu entry.

                        logger.Info("SuccessStory - SuccessStoryView");


                        //AchievementsDatabase test = new AchievementsDatabase(PlayniteApi, this.GetPluginUserDataPath());
                        //test.Add(PlayniteApi.Database.Games.Get(Guid.Parse("5af07455-c0d1-4acb-9246-9afbb31539be")));


                        // Show SuccessView
                        new SuccessView(settings, PlayniteApi, this.GetPluginUserDataPath()).ShowDialog();
                    })
            };
        }

        public override void OnGameInstalled(Game game)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(Game game)
        {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(Game game)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(Game game, long elapsedSeconds)
        {
            // Add code to be executed when game is preparing to be started.

            // Refresh Achievements database.
            AchievementsDatabase AchievementsDatabase = new AchievementsDatabase(PlayniteApi, this.GetPluginUserDataPath());
            AchievementsDatabase.Remove(game);
            AchievementsDatabase.Add(game);
        }

        public override void OnGameUninstalled(Game game)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted()
        {
            // Add code to be executed when Playnite is initialized.

            // Get achivements for game listed in database.
            AchievementsDatabase AchievementsDatabase = new AchievementsDatabase(PlayniteApi, this.GetPluginUserDataPath());

            // Create database if not exist (so long...).
            foreach (var game in PlayniteApi.Database.Games)
            {
                AchievementsDatabase.Add(game);
            }

            if (ListErrors.Get() != "")
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ListErrors.Get(), "SuccesStory errors");
            }
            ListErrors = null;
        }

        public override void OnApplicationStopped()
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated()
        {
            // Add code to be executed when library is updated.
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SuccessStorySettingsView(PlayniteApi, this.GetPluginUserDataPath());
        }
    }
}