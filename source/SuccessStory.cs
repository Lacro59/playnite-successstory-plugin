using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using CommonPluginsShared;
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
using CommonPluginsPlaynite;
using System.Diagnostics;
using SuccessStory.Services;
using System.Windows.Automation;
using System.Windows.Threading;
using CommonPluginsShared.PlayniteExtended;
using System.Windows.Media;
using CommonPluginsShared.Controls;
using SuccessStory.Controls;

namespace SuccessStory
{
    public class SuccessStory : PluginExtended<SuccessStorySettingsViewModel, SuccessStoryDatabase>
    {
        public override Guid Id { get; } = Guid.Parse("cebe6d32-8c46-4459-b993-5a5189d60788");

        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public static bool IsFromMenu { get; set; } = false;

        private OldToNew oldToNew;


        public SuccessStory(IPlayniteAPI api) : base(api)
        {
            PluginDatabase.InitializeClient(this);

            // Old database
            oldToNew = new OldToNew(this.GetPluginUserDataPath());

            // Custom theme button
            EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(OnCustomThemeButtonClick));

            // Add Event for WindowBase for get the "WindowSettings".
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(WindowBase_LoadedEvent));

            // Custom elements integration
            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                ElementList = new List<string> {
                    "PluginButton", "PluginViewItem", "PluginProgressBar", "PluginCompactList",
                    "PluginCompactLocked", "PluginCompactUnlocked", "PluginChart",
                    "PluginUserStats", "PluginList"
                },
                SourceName = "SuccessStory"
            });

            // Settings integration
            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "SuccessStory",
                SettingsRoot = $"{nameof(PluginSettings)}.{nameof(PluginSettings.Settings)}"
            });
        }


        #region Custom event
        public void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            string ButtonName = string.Empty;
            try
            {
                ButtonName = ((Button)sender).Name;
                if (ButtonName == "PART_CustomScButton")
                {
                    Common.LogDebug(true, $"OnCustomThemeButtonClick()");

                    PluginDatabase.IsViewOpen = true;
                    dynamic ViewExtension = null;

                    var windowOptions = new WindowOptions
                    {
                        ShowMinimizeButton = false,
                        ShowMaximizeButton = true,
                        ShowCloseButton = true
                    };

                    if (PluginDatabase.PluginSettings.Settings.EnableOneGameView)
                    {
                        if (PluginDatabase.GameContext.Source?.Name?.ToLower() == "battle.net" && PluginDatabase.GameContext.Name.ToLower() == "overwatch")
                        {
                            ViewExtension = new SuccessStoryOverwatchView(PluginDatabase.GameContext);
                        }
                        else
                        {
                            ViewExtension = new SuccessStoryOneGameView(PluginDatabase.GameContext);
                        }
                    }
                    else
                    {
                        windowOptions.Width = 1280;
                        windowOptions.Height = 740;

                        if (PluginDatabase.PluginSettings.Settings.EnableRetroAchievementsView && PlayniteTools.IsGameEmulated(PlayniteApi, PluginDatabase.GameContext))
                        {
                            ViewExtension = new SuccessView(this, PlayniteApi, this.GetPluginUserDataPath(), true, PluginDatabase.GameContext);
                        }
                        else
                        {
                            ViewExtension = new SuccessView(this, PlayniteApi, this.GetPluginUserDataPath(), false, PluginDatabase.GameContext);
                        }
                    }

 
                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension, windowOptions);
                    windowExtension.ResizeMode = ResizeMode.CanResize;
                    windowExtension.ShowDialog();
                    PluginDatabase.IsViewOpen = false;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        private void WindowBase_LoadedEvent(object sender, System.EventArgs e)
        {
            string WinIdProperty = string.Empty;
            try
            {
                WinIdProperty = ((Window)sender).GetValue(AutomationProperties.AutomationIdProperty).ToString();

                if (WinIdProperty == "WindowSettings" || WinIdProperty == "WindowLibraryIntegrations")
                {
                    Common.LogDebug(true, $"Reset VerifToAdd");

                    SuccessStoryDatabase.VerifToAddOrShowPsn = null;
                    SuccessStoryDatabase.VerifToAddOrShowGog = null;
                    SuccessStoryDatabase.VerifToAddOrShowOrigin = null;
                    SuccessStoryDatabase.VerifToAddOrShowRetroAchievements = null;
                    SuccessStoryDatabase.VerifToAddOrShowSteam = null;
                    SuccessStoryDatabase.VerifToAddOrShowXbox = null;
                    SuccessStoryDatabase.VerifToAddOrShowOverwatch = null;
                    SuccessStoryDatabase.VerifToAddOrShowSc2 = null;
                    SuccessStoryDatabase.VerifToAddOrShowRpcs3 = null;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on WindowBase_LoadedEvent for {WinIdProperty}");
            }
        }
        #endregion


        #region Theme integration
        // Button on top panel
        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            if (PluginSettings.Settings.EnableIntegrationButtonHeader)
            {
                yield return new TopPanelItem()
                {
                    Icon = new TextBlock
                    {
                        Text = "\ue820",
                        FontSize = 22,
                        FontFamily = resources.GetResource("FontIcoFont") as FontFamily
                    },
                    Title = resources.GetString("LOCSuccessStoryViewGames"),
                    Activated = () =>
                    {
                        var ViewExtension = new SuccessView(this, PlayniteApi, this.GetPluginUserDataPath());

                        var windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = true,
                            ShowCloseButton = true,
                            Width = 1280,
                            Height = 740
                        };

                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension, windowOptions);
                        windowExtension.ResizeMode = ResizeMode.CanResize;
                        windowExtension.ShowDialog();
                        PluginDatabase.IsViewOpen = false;
                    }
                };
            }

            yield break;
        }

        // List custom controls
        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (args.Name == "PluginButton")
            {
                return new PluginButton();
            }

            if (args.Name == "PluginViewItem")
            {
                return new PluginViewItem();
            }

            if (args.Name == "PluginProgressBar")
            {
                return new PluginProgressBar();
            }

            if (args.Name == "PluginCompactList")
            {
                return new PluginCompactList();
            }

            if (args.Name == "PluginCompactLocked")
            {
                return new PluginCompact { IsUnlocked = false };
            }

            if (args.Name == "PluginCompactUnlocked")
            {
                return new PluginCompact { IsUnlocked = true };
            }

            if (args.Name == "PluginChart")
            {
                return new PluginChart();
            }

            if (args.Name == "PluginUserStats")
            {
                return new PluginUserStats();
            }

            if (args.Name == "PluginList")
            {
                return new PluginList();
            }

            return null;
        }

        // SidebarItem
        public class SuccessStoryViewSidebar : SidebarItem
        {
            public SuccessStoryViewSidebar()
            {
                Type = SiderbarItemType.View;
                Title = resources.GetString("LOCSuccessStoryAchievements");
                Icon = new TextBlock
                {
                    Text = "\ue820",
                    FontFamily = resources.GetResource("FontIcoFont") as FontFamily
                };
                Opened = () =>
                {
                    SidebarItemControl sidebarItemControl = new SidebarItemControl(PluginDatabase.PlayniteApi);
                    sidebarItemControl.SetTitle(resources.GetString("LOCSuccessStoryAchievements"));
                    sidebarItemControl.AddContent(new SuccessView(PluginDatabase.Plugin, PluginDatabase.PlayniteApi, PluginDatabase.Paths.PluginUserDataPath));

                    return sidebarItemControl;
                };
            }
        }

        public class SuccessStoryViewRaSidebar : SidebarItem
        {
            public SuccessStoryViewRaSidebar()
            {
                Type = SiderbarItemType.View;
                Title = resources.GetString("LOCSuccessStoryRetroAchievements");
                Icon = new TextBlock
                {
                    Text = "\ue910",
                    FontFamily = resources.GetResource("CommonFont") as FontFamily
                };
                Opened = () =>
                {
                    SidebarItemControl sidebarItemControl = new SidebarItemControl(PluginDatabase.PlayniteApi);
                    sidebarItemControl.SetTitle(resources.GetString("LOCSuccessStoryRetroAchievements"));
                    sidebarItemControl.AddContent(new SuccessView(PluginDatabase.Plugin, PluginDatabase.PlayniteApi, PluginDatabase.Paths.PluginUserDataPath, true));

                    return sidebarItemControl;
                };
            }
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            var items = new List<SidebarItem>
            {
                new SuccessStoryViewSidebar()
            };

            if (PluginSettings.Settings.EnableRetroAchievementsView)
            {
                items.Add(new SuccessStoryViewRaSidebar());
            }

            return items;
        }
        #endregion


        #region Menus
        // To add new game menu items override GetGameMenuItems
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            Game GameMenu = args.Games.First();
            List<Guid> Ids = args.Games.Select(x => x.Id).ToList();
            string SourceName = PlayniteTools.GetSourceName(PlayniteApi, GameMenu);
            string GameName = GameMenu.Name;
            bool IsAddOrShowManual = SuccessStoryDatabase.IsAddOrShowManual(GameMenu, SourceName);
            bool GameCouldHaveAchievements = SuccessStoryDatabase.GameCouldHaveAchievements(PluginSettings.Settings, SourceName, GameName);
            GameAchievements gameAchievements = PluginDatabase.Get(GameMenu, true);

            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>();

            if (!gameAchievements.IsIgnored)
            {
                if (GameCouldHaveAchievements || IsAddOrShowManual)
                {
                    if (!PluginSettings.Settings.EnableOneGameView || (PluginSettings.Settings.EnableOneGameView && gameAchievements.HasData))
                    {
                        // Show list achievements for the selected game
                        gameMenuItems.Add(new GameMenuItem
                        {
                            MenuSection = resources.GetString("LOCSuccessStory"),
                            Description = resources.GetString("LOCSuccessStoryViewGame"),
                            Action = (gameMenuItem) =>
                            {
                                dynamic ViewExtension = null;
                                PluginDatabase.IsViewOpen = true;

                                var windowOptions = new WindowOptions
                                {
                                    ShowMinimizeButton = false,
                                    ShowMaximizeButton = true,
                                    ShowCloseButton = true
                                };

                                if (PluginDatabase.PluginSettings.Settings.EnableOneGameView)
                                {
                                    if (PluginDatabase.GameContext.Source?.Name?.ToLower() == "battle.net" && PluginDatabase.GameContext.Name.ToLower() == "overwatch")
                                    {
                                        ViewExtension = new SuccessStoryOverwatchView(GameMenu);
                                    }
                                    else
                                    {
                                        ViewExtension = new SuccessStoryOneGameView(GameMenu);
                                    }
                                }
                                else
                                {
                                    windowOptions.Width = 1280;
                                    windowOptions.Height = 740;

                                    ViewExtension = new SuccessView(this, PlayniteApi, this.GetPluginUserDataPath(), false, GameMenu);
                                }

                                var windowCreationOptions = new WindowCreationOptions
                                {
                                    ShowMinimizeButton = false,
                                    ShowMaximizeButton = true,
                                    ShowCloseButton = true
                                };
                                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension, windowOptions);
                                windowExtension.ResizeMode = ResizeMode.CanResize;
                                windowExtension.ShowDialog();
                                PluginDatabase.IsViewOpen = false;
                            }
                        });

                        gameMenuItems.Add(new GameMenuItem
                        {
                            MenuSection = resources.GetString("LOCSuccessStory"),
                            Description = "-"
                        });
                    }

                    if (!IsAddOrShowManual && !gameAchievements.IsManual)
                    {
                        gameMenuItems.Add(new GameMenuItem
                        {
                            MenuSection = resources.GetString("LOCSuccessStory"),
                            Description = resources.GetString("LOCCommonRefreshGameData"),
                            Action = (gameMenuItem) =>
                            {
                                IsFromMenu = true;

                                if (Ids.Count == 1)
                                {
                                    PluginDatabase.Refresh(GameMenu.Id);
                                }
                                else
                                {
                                    PluginDatabase.Refresh(Ids);
                                }
                            }
                        });

                        gameMenuItems.Add(new GameMenuItem
                        {
                            MenuSection = resources.GetString("LOCSuccessStory"),
                            Description = resources.GetString("LOCSuccessStoryIgnored"),
                            Action = (mainMenuItem) =>
                            {
                                PluginDatabase.SetIgnored(gameAchievements);
                            }
                        });
                    }
                }

                //if (PluginSettings.Settings.EnableManual && IsAddOrShowManual)
                if (PluginSettings.Settings.EnableManual)
                {
                    if (!gameAchievements.HasData)
                    {
                        gameMenuItems.Add(new GameMenuItem
                        {
                            MenuSection = resources.GetString("LOCSuccessStory"),
                            Description = resources.GetString("LOCAddTitle"),
                            Action = (mainMenuItem) =>
                            {
                                PluginDatabase.Remove(GameMenu);
                                PluginDatabase.GetManual(GameMenu);
                            }
                        });
                    }
                    else if (gameAchievements.IsManual)
                    {
                        gameMenuItems.Add(new GameMenuItem
                        {
                            MenuSection = resources.GetString("LOCSuccessStory"),
                            Description = resources.GetString("LOCEditGame"),
                            Action = (mainMenuItem) =>
                            {
                                var ViewExtension = new SuccessStoryEditManual(GameMenu);
                                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension);
                                windowExtension.ShowDialog();
                            }
                        });

                        gameMenuItems.Add(new GameMenuItem
                        {
                            MenuSection = resources.GetString("LOCSuccessStory"),
                            Description = resources.GetString("LOCRemoveTitle"),
                            Action = (gameMenuItem) =>
                            {
                                var TaskIntegrationUI = Task.Run(() =>
                                {
                                    PluginDatabase.Remove(GameMenu);
                                });
                            }
                        });
                    }
                }

                if ((SourceName == "Playnite" || SourceName == "Hacked") && gameAchievements.HasData && !gameAchievements.IsManual)
                {
                    gameMenuItems.Add(new GameMenuItem
                    {
                        MenuSection = resources.GetString("LOCSuccessStory"),
                        Description = resources.GetString("LOCCommonDeleteGameData"),
                        Action = (gameMenuItem) =>
                        {
                            var TaskIntegrationUI = Task.Run(() =>
                            {
                                PluginDatabase.Remove(GameMenu.Id);
                            });
                        }
                    });
                }
            }
            else
            {
                if (GameCouldHaveAchievements || IsAddOrShowManual)
                {
                    gameMenuItems.Add(new GameMenuItem
                    {
                        MenuSection = resources.GetString("LOCSuccessStory"),
                        Description = resources.GetString("LOCSuccessStoryNotIgnored"),
                        Action = (mainMenuItem) =>
                        {
                            PluginDatabase.SetIgnored(gameAchievements);
                        }
                    });
                }
            }

#if DEBUG
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = resources.GetString("LOCSuccessStory"),
                Description = "-"
            });
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = resources.GetString("LOCSuccessStory"),
                Description = "Test",
                Action = (mainMenuItem) => 
                {

                }
            });
#endif
            return gameMenuItems;
        }

        // To add new main menu items override GetMainMenuItems
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            string MenuInExtensions = string.Empty;
            if (PluginSettings.Settings.MenuInExtensions)
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

                        var windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = true,
                            ShowCloseButton = true,
                            Width = 1280,
                            Height = 740
                        };

                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension, windowOptions);
                        windowExtension.ResizeMode = ResizeMode.CanResize;
                        windowExtension.ShowDialog();
                        PluginDatabase.IsViewOpen = false;
                    }
                }
            };

            if (PluginSettings.Settings.EnableRetroAchievementsView && PluginSettings.Settings.EnableRetroAchievements)
            {
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSuccessStory"),
                    Description = resources.GetString("LOCSuccessStoryViewGames") + " - RetroAchievements",
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.IsViewOpen = true;
                        SuccessView ViewExtension = null;
                        if (PluginSettings.Settings.EnableRetroAchievementsView && PlayniteTools.IsGameEmulated(PlayniteApi, PluginDatabase.GameContext))
                        {
                            ViewExtension = new SuccessView(this, PlayniteApi, this.GetPluginUserDataPath(), true, PluginDatabase.GameContext);
                        }
                        else
                        {
                            ViewExtension = new SuccessView(this, PlayniteApi, this.GetPluginUserDataPath(), false, PluginDatabase.GameContext);
                        }
                        ViewExtension.Width = 1280;
                        ViewExtension.Height = 740;

                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSuccessStory"), ViewExtension);
                        windowExtension.ShowDialog();
                        PluginDatabase.IsViewOpen = false;
                    }
                });
            }

            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCSuccessStory"),
                Description = "-"
            });

            // Download missing data for all game in database
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCSuccessStory"),
                Description = resources.GetString("LOCCommonDownloadPluginData"),
                Action = (mainMenuItem) =>
                {
                    IsFromMenu = true;
                    PluginDatabase.GetSelectData();
                    IsFromMenu = false;
                }
            });

            if (PluginDatabase.PluginSettings.Settings.EnableTag)
            {
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSuccessStory"),
                    Description = "-"
                });

                // Tag menus
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSuccessStory"),
                    Description = resources.GetString("LOCCommonAddAllTags"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.AddTagAllGame();
                    }
                });
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSuccessStory"),
                    Description = resources.GetString("LOCCommonRemoveAllTags"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.RemoveTagAllGame();
                    }
                });
            }


#if DEBUG
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCSuccessStory"),
                Description = "-"
            });
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCSuccessStory"),
                Description = "Test",
                Action = (mainMenuItem) => 
                {
                    
                }
            });
#endif

            return mainMenuItems;
        }
        #endregion


        #region Game event
        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            // Old database
            if (oldToNew.IsOld)
            {
                oldToNew.ConvertDB(PlayniteApi);
            }

            try
            {
                if (args.NewValue?.Count == 1)
                {
                    PluginDatabase.GameContext = args.NewValue[0];
                    PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        // Add code to be executed when game is finished installing.
        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {

        }

        // Add code to be executed when game is uninstalled.
        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {

        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStarting(OnGameStartingEventArgs args)
        {

        }

        // Add code to be executed when game is started running.
        public override void OnGameStarted(OnGameStartedEventArgs args)
        {

        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            // Refresh Achievements database for game played.
            var TaskGameStopped = Task.Run(() =>
            {
                PluginDatabase.Refresh(args.Game.Id);

                // Set to Beaten
                if (PluginSettings.Settings.Auto100PercentCompleted)
                {
                    GameAchievements gameAchievements = PluginDatabase.Get(args.Game, true);

                    if (gameAchievements.Is100Percent)
                    {
                        args.Game.CompletionStatusId = PlayniteApi.Database.CompletionStatuses.Where(x => x.Name == "Beaten").FirstOrDefault().Id;
                        PlayniteApi.Database.Games.Update(args.Game);
                    }
                }

                // refresh themes resources
                if (args.Game.Id == PluginDatabase.GameContext.Id)
                {
                    PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                }
            });
        }
        #endregion  


        #region Application event
        // Add code to be executed when Playnite is initialized.
        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // Cache images
            if (PluginSettings.Settings.EnableImageCache)
            {
                CancellationToken ct = tokenSource.Token;
                var TaskCacheImage = Task.Run(() =>
                {
                    // Wait Playnite & extension database are loaded
                    System.Threading.SpinWait.SpinUntil(() => PlayniteApi.Database.IsOpen, -1);
#if DEBUG
                    Common.LogDebug(true, $"TaskCacheImage - {PlayniteApi.Database.Games.Count} - Start");
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
#endif
                    foreach (Game game in PlayniteApi.Database.Games)
                    {
                        Models.GameAchievements successStories = PluginDatabase.GetOnlyCache(game.Id);
                        if (successStories != null && successStories.HaveAchivements)
                        {
                            Common.LogDebug(true, $"TaskCacheImage - {game.Name} - {successStories.Items.Count}");

                            foreach (var achievement in successStories.Items)
                            {
                                try
                                {
                                    if (!achievement.UrlLocked.IsNullOrEmpty() && PlayniteTools.GetCacheFile(achievement.CacheLocked, "SuccessStory").IsNullOrEmpty())
                                    {
                                        Common.LogDebug(true, $"TaskCacheImage.DownloadFileImage - {game.Name} - GetCacheFile({achievement.Name}" + "_Locked)");
                                        Web.DownloadFileImage(achievement.CacheLocked, achievement.UrlLocked, PlaynitePaths.DataCachePath, "SuccessStory").GetAwaiter().GetResult();
                                    }

                                    if (ct.IsCancellationRequested)
                                    {
                                        logger.Info($"TaskCacheImage - IsCancellationRequested");
                                        break;
                                    }

                                    if (PlayniteTools.GetCacheFile(achievement.CacheUnlocked, "SuccessStory").IsNullOrEmpty())
                                    {
                                        Common.LogDebug(true, $"TaskCacheImage.DownloadFileImage - {game.Name} - GetCacheFile({achievement.Name}" + "_Unlocked)");
                                        Web.DownloadFileImage(achievement.CacheUnlocked, achievement.UrlUnlocked, PlaynitePaths.DataCachePath, "SuccessStory").GetAwaiter().GetResult();
                                    }

                                    if (ct.IsCancellationRequested)
                                    {
                                        logger.Info($"TaskCacheImage - IsCancellationRequested");
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Common.LogError(ex, true, $"Error on TaskCacheImage");
                                }
                            }
                        }
                    }

#if DEBUG
                    stopwatch.Stop();
                    TimeSpan ts = stopwatch.Elapsed;
                    Common.LogDebug(true, $"TaskCacheImage() - End - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
#endif
                }, tokenSource.Token);
            }
        }

        // Add code to be executed when Playnite is shutting down.
        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            tokenSource.Cancel();
        }
        #endregion


        // Add code to be executed when library is updated.
        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {

        }


        #region Settings
        public override ISettings GetSettings(bool firstRunSettings)
        {
            return PluginSettings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SuccessStorySettingsView(this, PlayniteApi, this.GetPluginUserDataPath());
        }
        #endregion
    }
}
