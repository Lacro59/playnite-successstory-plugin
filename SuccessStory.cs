using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PluginCommon;
using SuccessStory.Database;
using SuccessStory.Models;
using SuccessStory.Views.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SuccessStory
{
    public class SuccessStory : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SuccessStorySettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("cebe6d32-8c46-4459-b993-5a5189d60788");


        public SuccessStory(IPlayniteAPI api) : base(api)
        {
            settings = new SuccessStorySettings(this);

            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.Localization.SetPluginLanguage(pluginFolder, api.Paths.ConfigurationPath);
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

            // Refresh Achievements database for game played.
            AchievementsDatabase AchievementsDatabase = new AchievementsDatabase(PlayniteApi, this.GetPluginUserDataPath());
            AchievementsDatabase.Remove(game);
            AchievementsDatabase.Add(game, settings);
        }

        public override void OnGameUninstalled(Game game)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted()
        {
            // Add code to be executed when Playnite is initialized.
        }

        public override void OnApplicationStopped()
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated()
        {
            // Add code to be executed when library is updated.

            // Get achievements for the new game added int he library.
            AchievementsDatabase AchievementsDatabase = new AchievementsDatabase(PlayniteApi, this.GetPluginUserDataPath());
            foreach (var game in PlayniteApi.Database.Games)
            {
                if (game.Added == null && ((DateTime)game.Added).ToString("yyyy-MM-dd") == DateTime.Now.ToString("yyyy-MM-dd"))
                {
                    AchievementsDatabase.Remove(game);
                    AchievementsDatabase.Add(game, settings);
                }
            }
        }

        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            if (args.NewValue != null)
            {
                if (args.NewValue.Count == 1)
                {
                    AchievementsDatabase AchievementsDatabase = new AchievementsDatabase(PlayniteApi, this.GetPluginUserDataPath());
                    AchievementsDatabase.Initialize();

                    Game SelectedGame = args.NewValue[0];
                    GameAchievements SelectedGameAchievements = AchievementsDatabase.Get(SelectedGame.Id);

                    foreach (StackPanel sp in Tools.FindVisualChildren<StackPanel>(Application.Current.MainWindow))
                    {
                        // List achievements
                        if (sp.Name == "PART_Achievements_List")
                        {
                            sp.Children.Clear();

                            try
                            {
                                List<listAchievements> ListBoxAchievements = new List<listAchievements>();

                                // Download Achievements if not exist in database.
                                if (SelectedGameAchievements == null)
                                {
                                    logger.Info("SuccesStory - Download achievements for " + SelectedGame.Name);
                                    AchievementsDatabase.Add(SelectedGame, settings);
                                    AchievementsDatabase.Initialize();
                                    SelectedGameAchievements = AchievementsDatabase.Get(SelectedGame.Id);
                                }

                                if (SelectedGameAchievements != null)
                                {
                                    if (SelectedGameAchievements.HaveAchivements)
                                    {
                                        AchievementsDatabase.GetCountByMonth(SelectedGame.Id);
                                        sp.Children.Add(new SuccessStoryAchievementsList(SelectedGameAchievements.Achievements));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                                logger.Error(ex, $"SuccesStory [{LineNumber}] - {SelectedGame.Name}: ");
                            }

                            sp.UpdateLayout();
                        }

                        // Graphic
                        if (sp.Name == "PART_Achievements_Graphics")
                        {
                            sp.Children.Clear();

                            try
                            {
                                // Download Achievements if not exist in database.
                                if (SelectedGameAchievements == null)
                                {
                                    logger.Info("SuccesStory - Download achievements for " + SelectedGame.Name);
                                    AchievementsDatabase.Add(SelectedGame, settings);
                                    AchievementsDatabase.Initialize();
                                    SelectedGameAchievements = AchievementsDatabase.Get(SelectedGame.Id);
                                }

                                if (SelectedGameAchievements != null)
                                {
                                    if (SelectedGameAchievements.HaveAchivements)
                                    {
                                        AchievementsGraphicsDataCount GraphicsData = AchievementsDatabase.GetCountByMonth(SelectedGame.Id);
                                        string[] StatsGraphicsAchievementsLabels = GraphicsData.Labels;
                                        SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection();
                                        StatsGraphicAchievementsSeries.Add(new LineSeries
                                        {
                                            Title = "",
                                            Values = GraphicsData.Series
                                        });

                                        sp.Children.Add(new SuccessStoryAchievementsGraphics(StatsGraphicAchievementsSeries, StatsGraphicsAchievementsLabels));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                                logger.Error(ex, $"SuccesStory [{LineNumber}] - {SelectedGame.Name}: ");
                            }

                            sp.UpdateLayout();
                        }
                    }

                    AchievementsDatabase = null;
                }
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SuccessStorySettingsView(PlayniteApi, this.GetPluginUserDataPath(), settings);
        }
    }
}