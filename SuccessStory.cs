using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PluginCommon;
using SuccessStory.Clients;
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
using PluginCommon.PlayniteResources;
using System.Diagnostics;
using SuccessStory.Services;
using System.Windows.Automation;
using System.Windows.Threading;

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
        public static SuccessStoryDatabase PluginDatabase;
        public static Game GameSelected { get; set; }
        public static SuccessStoryUI successStoryUI { get; set; }

        CancellationTokenSource tokenSource = new CancellationTokenSource();

        private OldToNew oldToNew;


        public SuccessStory(IPlayniteAPI api) : base(api)
        {
            settings = new SuccessStorySettings(this);

            // Old database
            oldToNew = new OldToNew(this.GetPluginUserDataPath());

            // Loading plugin database 
            PluginDatabase = new SuccessStoryDatabase(this, PlayniteApi, settings, this.GetPluginUserDataPath());
            PluginDatabase.InitializeDatabase();

            // Get plugin's location 
            pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.PluginLocalization.SetPluginLanguage(pluginFolder, api.ApplicationSettings.Language);
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
            successStoryUI = new SuccessStoryUI(this, api, this.GetPluginUserDataPath());

            // Custom theme button
            if (settings.EnableIntegrationInCustomTheme)
            {
                EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(successStoryUI.OnCustomThemeButtonClick));
            }

            // Add Event for WindowBase for get the "WindowSettings".
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(WindowBase_LoadedEvent));

            // Add event fullScreen
            if (api.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
            {
                EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(BtFullScreen_ClickEvent));
            }
        }


        #region Custom event
        private void WindowBase_LoadedEvent(object sender, System.EventArgs e)
        {
            string WinIdProperty = String.Empty;
            try
            {
                WinIdProperty = ((Window)sender).GetValue(AutomationProperties.AutomationIdProperty).ToString();

                if (WinIdProperty == "WindowSettings")
                {
#if DEBUG
                    logger.Debug($"SuccessStory - Reset VerifToAdd");
#endif
                    SuccessStoryDatabase.VerifToAddOrShowGog = null;
                    SuccessStoryDatabase.VerifToAddOrShowOrigin = null;
                    SuccessStoryDatabase.VerifToAddOrShowRetroAchievements = null;
                    SuccessStoryDatabase.VerifToAddOrShowSteam = null;
                    SuccessStoryDatabase.VerifToAddOrShowXbox = null;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", $"Error on WindowBase_LoadedEvent for {WinIdProperty}");
            }
        }


        private void BtFullScreen_ClickEvent(object sender, System.EventArgs e)
        {
            try
            {
                if (((Button)sender).Name == "PART_ButtonDetails")
                {
                    var TaskIntegrationUI = Task.Run(() =>
                    {
                        successStoryUI.Initial();
                        successStoryUI.taskHelper.Check();
                        var dispatcherOp = successStoryUI.AddElementsFS();
                        dispatcherOp.Completed += (s, ev) => { successStoryUI.RefreshElements(GameSelected); };
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory");
            }
        }
        #endregion


        // To add new game menu items override GetGameMenuItems
        public override List<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var GameMenu = args.Games.First();

            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>
            {
                // Show list achievements for the selected game
                new GameMenuItem {
                    MenuSection = resources.GetString("LOCSuccessStory"),
                    Description = resources.GetString("LOCSuccessStoryViewGame"),
                    Action = (gameMenuItem) =>
                    {
                        dynamic ViewExtension = null;
                        PluginDatabase.IsViewOpen = true;
                        if (PluginDatabase.PluginSettings.EnableOneGameView)
                        {
                            ViewExtension = new SuccessStoryOneGameView(GameMenu);
                        }
                        else
                        {
                            ViewExtension = new SuccessView(this, PlayniteApi, this.GetPluginUserDataPath(), false, GameMenu);
                        }
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension);
                        windowExtension.ShowDialog();
                        PluginDatabase.IsViewOpen = false;
                    }
                },

                // Delete & download localizations data for the selected game
                new GameMenuItem {
                    MenuSection = resources.GetString("LOCSuccessStory"),
                    Description = resources.GetString("LOCCommonRefreshGameData"),
                    Action = (gameMenuItem) =>
                    {
                        if (settings.EnableIntegrationInCustomTheme || settings.EnableIntegrationInDescription)
                        {
                            PlayniteUiHelper.ResetToggle();
                        }

                        var TaskIntegrationUI = Task.Run(() =>
                        {
                            PluginDatabase.Remove(GameMenu);
                            var dispatcherOp = successStoryUI.AddElements();
                            dispatcherOp.Completed += (s, e) => { successStoryUI.RefreshElements(GameMenu); };
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
                // Show list achievements for all games
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSuccessStory"),
                    Description = resources.GetString("LOCSuccessStoryViewGames"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.IsViewOpen = true;
                        var ViewExtension = new SuccessView(this, PlayniteApi, this.GetPluginUserDataPath());
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension);
                        windowExtension.ShowDialog();
                        PluginDatabase.IsViewOpen = false;
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
                        PluginDatabase.IsViewOpen = true;
                        SuccessView ViewExtension = null;
                        if (settings.EnableRetroAchievementsView && PlayniteTools.IsGameEmulated(PlayniteApi, GameSelected))
                        {
                            ViewExtension = new SuccessView(this, PlayniteApi, this.GetPluginUserDataPath(), true, GameSelected);
                        }
                        else
                        {
                            ViewExtension = new SuccessView(this, PlayniteApi, this.GetPluginUserDataPath(), false, GameSelected);
                        }
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension);
                        windowExtension.ShowDialog();
                        PluginDatabase.IsViewOpen = false;
                    }
                });
            }

            // Download missing data for all game in database
            mainMenuItems.Add(
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSuccessStory"),
                    Description = resources.GetString("LOCCommonGetAllDatas"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.GetAllDatas();
                    }
                }
            );

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


        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            // Old database
            if (oldToNew.IsOld)
            {
                oldToNew.ConvertDB(PlayniteApi);
            }

            try
            {
                if (args.NewValue != null && args.NewValue.Count == 1)
                {
                    GameSelected = args.NewValue[0];
#if DEBUG
                    logger.Debug($"SuccessStory - OnGameSelected() - {GameSelected.Name} - {GameSelected.Id.ToString()}");
#endif
                    if (settings.EnableIntegrationInCustomTheme || settings.EnableIntegrationInDescription)
                    {
                        PlayniteUiHelper.ResetToggle();
                        var TaskIntegrationUI = Task.Run(() =>
                        {
                            successStoryUI.Initial();
                            successStoryUI.taskHelper.Check();
                            var dispatcherOp = successStoryUI.AddElements();
                            if (dispatcherOp != null)
                            {
                                dispatcherOp.Completed += (s, e) => { successStoryUI.RefreshElements(args.NewValue[0]); };
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", $"Error on OnGameSelected()");
            }
        }

        // Add code to be executed when game is finished installing.
        public override void OnGameInstalled(Game game)
        {

        }

        // Add code to be executed when game is started running.
        public override void OnGameStarted(Game game)
        {

        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStarting(Game game)
        {

        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStopped(Game game, long elapsedSeconds)
        {
            // Refresh Achievements database for game played.
            var TaskGameStopped = Task.Run(() =>
            {
                PluginDatabase.Remove(game);
                var dispatcherOp = successStoryUI.AddElements();
                dispatcherOp.Completed += (s, e) => { successStoryUI.RefreshElements(GameSelected); };
            });
        }

        // Add code to be executed when game is uninstalled.
        public override void OnGameUninstalled(Game game)
        {

        }


        // Add code to be executed when Playnite is shutting down.
        public override void OnApplicationStopped()
        {
#if DEBUG
            logger.Debug($"SuccessStory - Cancel TaskCacheImage");
#endif
            tokenSource.Cancel();
        }

        // Add code to be executed when Playnite is initialized.
        public override void OnApplicationStarted()
        {
            successStoryUI.AddBtHeader();

            // Cache images
            if (settings.EnableImageCache)
            {
                CancellationToken ct = tokenSource.Token;
                var TaskCacheImage = Task.Run(() =>
                {
                    // Wait Playnite & extension database are loaded
                    Thread.Sleep(50000);
#if DEBUG
                    logger.Debug($"SuccessStory - TaskCacheImage - {PlayniteApi.Database.Games.Count} - Start");
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
#endif
                    foreach (Game game in PlayniteApi.Database.Games)
                    {
                        try
                        {
                            Models.GameAchievements successStories = PluginDatabase.GetOnlyCache(game.Id);
                            if (successStories != null && successStories.HaveAchivements)
                            {
#if DEBUG
                                logger.Debug($"SuccessStory - TaskCacheImage - {game.Name} - {successStories.Items.Count}");
#endif
                                foreach (var achievement in successStories.Items)
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


        // Add code to be executed when library is updated.
        public override void OnLibraryUpdated()
        {

        }


        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SuccessStorySettingsView(this, PlayniteApi, this.GetPluginUserDataPath());
        }
    }
}
