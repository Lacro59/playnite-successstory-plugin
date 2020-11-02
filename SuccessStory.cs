using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PluginCommon;
using SuccessStory.Clients;
using SuccessStory.Database;
using SuccessStory.Models;
using SuccessStory.Views;
using SuccessStory.Views.Interface;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Separator = System.Windows.Controls.Separator;
using PluginCommon.PlayniteResources;
using System.Diagnostics;
using SuccessStory.Services;

namespace SuccessStory
{
    public class SuccessStory : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private SuccessStorySettings settings { get; set; }
        public override Guid Id { get; } = Guid.Parse("cebe6d32-8c46-4459-b993-5a5189d60788");

        private readonly IntegrationUI ui = new IntegrationUI();
        private readonly TaskHelper taskHelper = new TaskHelper();

        public static string pluginFolder;
        public static Game GameSelected { get; set; }
        public static SuccessStoryUI successStoryUI { get; set; }
        public static AchievementsDatabase achievementsDatabase;
        public static GameAchievements SelectedGameAchievements;

        CancellationTokenSource tokenSource = new CancellationTokenSource();
        

        public SuccessStory(IPlayniteAPI api) : base(api)
        {
            settings = new SuccessStorySettings(this);

            // Get plugin's location 
            pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.Localization.SetPluginLanguage(pluginFolder, api.ApplicationSettings.Language);
            // Add common in application ressource.
            PluginCommon.Common.Load(pluginFolder);

            // Check version
            if (settings.EnableCheckVersion)
            {
                CheckVersion cv = new CheckVersion();

                if (cv.Check("SuccessStory", pluginFolder))
                {
                    cv.ShowNotification(api, "SuccessStory - " + resources.GetString("LOCUpdaterWindowTitle"));
                }
            }

            // Init ui interagration
            successStoryUI = new SuccessStoryUI(api, settings, this.GetPluginUserDataPath(), this);

            // Custom theme button
            if (settings.EnableIntegrationInCustomTheme)
            {
                EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(successStoryUI.OnCustomThemeButtonClick));
            }

            // Load database
            var TaskLoadDatabase = Task.Run(() =>
            {
                achievementsDatabase = new AchievementsDatabase(this, PlayniteApi, settings, this.GetPluginUserDataPath());
                achievementsDatabase.Initialize();
            });
        }

        // To add new game menu items override GetGameMenuItems
        public override List<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var GameMenu = args.Games.First();

            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>
            {
                new GameMenuItem {
                    MenuSection = resources.GetString("LOCSuccessStory"),
                    Description = resources.GetString("LOCSuccessStoryViewGame"),
                    Action = (gameMenuItem) =>
                    {
                        var ViewExtension = new SuccessView(this, settings, PlayniteApi, this.GetPluginUserDataPath(), false, GameMenu);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension);
                        windowExtension.ShowDialog();
                    }
                },
                new GameMenuItem {
                    MenuSection = resources.GetString("LOCSuccessStory"),
                    Description = resources.GetString("LOCSuccessStoryRefreshData"),
                    Action = (gameMenuItem) =>
                    {
                        var TaskIntegrationUI = Task.Run(() =>
                        {
                            achievementsDatabase.Remove(GameMenu);
                            successStoryUI.AddElements();
                            successStoryUI.RefreshElements(GameSelected);
                        });
                    }
                }
            };

#if DEBUG
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = resources.GetString("LOCSuccessStory"),
                Description = "Test",
                Action = (mainMenuItem) => { }
            });
#endif

            return gameMenuItems;
        }

        // To add new main menu items override GetMainMenuItems
        public override List<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            string MenuInExtensions = string.Empty;
            if (settings.MenuInExtensions)
            {
                MenuInExtensions = "@";
            }

            List<MainMenuItem> mainMenuItems = new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSuccessStory"),
                    Description = resources.GetString("LOCSuccessStoryViewGames"),
                    Action = (mainMenuItem) =>
                    {
                        var ViewExtension = new SuccessView(this, settings, PlayniteApi, this.GetPluginUserDataPath());
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension);
                        windowExtension.ShowDialog();
                    }
                }
            };

            if (settings.EnableRetroAchievementsView && settings.EnableRetroAchievements)
            {
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSuccessStory"),
                    Description = resources.GetString("LOCSuccessStoryViewGames") + " - RetroAchievements",
                    Action = (mainMenuItem) =>
                    {
                        SuccessView ViewExtension = null;
                        if (settings.EnableRetroAchievementsView && PlayniteTools.IsGameEmulated(PlayniteApi, GameSelected))
                        {
                            ViewExtension = new SuccessView(this, settings, PlayniteApi, this.GetPluginUserDataPath(), true, GameSelected);
                        }
                        else
                        {
                            ViewExtension = new SuccessView(this, settings, PlayniteApi, this.GetPluginUserDataPath(), false, GameSelected);
                        }
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension);
                        windowExtension.ShowDialog();
                    }
                });
            }

#if DEBUG
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCSuccessStory"),
                Description = "Test",
                Action = (mainMenuItem) => { }
            });
#endif

            return mainMenuItems;
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

            var TaskGameStopped = Task.Run(() =>
            {
                // Refresh Achievements database for game played.
                achievementsDatabase.Remove(game);
                achievementsDatabase.Add(game, settings);
                achievementsDatabase.Initialize();
            });
        }

        public override void OnGameUninstalled(Game game)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStopped()
        {
            // Add code to be executed when Playnite is shutting down.

#if DEBUG
            logger.Debug($"SuccessStory - Cancel TaskCacheImage");
#endif
            tokenSource.Cancel();
        }

        public override void OnLibraryUpdated()
        {
            // Add code to be executed when library is updated.

            // Get achievements for the new game added in the library.
            var TaskLibraryUpdated = Task.Run(() =>
            {
                foreach (var game in PlayniteApi.Database.Games)
                {
                    if (game.Added == null && ((DateTime)game.Added).ToString("yyyy-MM-dd") == DateTime.Now.ToString("yyyy-MM-dd"))
                    {
                        achievementsDatabase.Remove(game);
                        achievementsDatabase.Add(game, settings);
                    }
                }
            });
        }

        public override void OnApplicationStarted()
        {
            // Add code to be executed when Playnite is initialized.

            successStoryUI.AddBtHeader();

            // Cache images
            if (settings.EnableImageCache)
            {
                CancellationToken ct = tokenSource.Token;
                var TaskCacheImage = Task.Run(() =>
                {
                    // Wait Playnite & extension database are loaded
                    Thread.Sleep(60000);
#if DEBUG
                    logger.Debug($"SuccessStory - TaskCacheImage - {PlayniteApi.Database.Games.Count} - Start");
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
#endif
                    foreach (Game game in PlayniteApi.Database.Games)
                    {
                        try
                        {
                            GameAchievements gameAchievements = achievementsDatabase.Get(game.Id);
                            if (gameAchievements != null && gameAchievements.HaveAchivements)
                            {
#if DEBUG
                                logger.Debug($"SuccessStory - TaskCacheImage - {game.Name} - {gameAchievements.Achievements.Count}");
#endif
                                foreach (var achievement in gameAchievements.Achievements)
                                {
                                    if (!achievement.UrlLocked.IsNullOrEmpty() && PlayniteTools.GetCacheFile(achievement.CacheLocked, "SuccessStory").IsNullOrEmpty())
                                    {
#if DEBUG
                                        logger.Debug($"SuccessStory - TaskCacheImage.DownloadFileImage - {game.Name} - GetCacheFile({achievement.Name + "_Locked"})");
#endif
                                        Web.DownloadFileImage(achievement.CacheLocked, achievement.UrlLocked, PlaynitePaths.ImagesCachePath, "SuccessStory").GetAwaiter().GetResult();
                                    }

                                    if (ct.IsCancellationRequested)
                                    {
                                        logger.Info($"IsCancellationRequested for TaskCacheImage()");
                                        break;
                                    }

                                    if (PlayniteTools.GetCacheFile(achievement.CacheUnlocked, "SuccessStory").IsNullOrEmpty())
                                    {
#if DEBUG
                                        logger.Debug($"SuccessStory - TaskCacheImage.DownloadFileImage - {game.Name} - GetCacheFile({achievement.Name + "_Unlocked"})");
#endif
                                        Web.DownloadFileImage(achievement.CacheUnlocked, achievement.UrlUnlocked, PlaynitePaths.ImagesCachePath, "SuccessStory").GetAwaiter().GetResult();
                                    }

                                    if (ct.IsCancellationRequested)
                                    {
                                        logger.Info($"IsCancellationRequested for TaskCacheImage()");
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
#if DEBUG
                            Common.LogError(ex, "SuccessStory", $"Error on TaskCacheImage");
#endif
                        }
                    }

#if DEBUG
                    stopwatch.Stop();
                    TimeSpan ts = stopwatch.Elapsed;
                    logger.Debug($"SuccessStory - TaskCacheImage() - End - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
#endif
                }, tokenSource.Token);
            }
        }

        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            try
            {
                if (args.NewValue != null && args.NewValue.Count == 1)
                {
                    GameSelected = args.NewValue[0];
#if DEBUG
                    logger.Debug($"SuccessStory - Game selected: {GameSelected.Name}");
#endif
                    var TaskIntegrationUI = Task.Run(() =>
                    {
                        successStoryUI.taskHelper.Check();
                        successStoryUI.AddElements();
                        successStoryUI.RefreshElements(GameSelected);
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", $"Error on OnGameSelected()");
            }
        }
        
        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SuccessStorySettingsView(this, PlayniteApi, this.GetPluginUserDataPath(), settings);
        }
    }
}
