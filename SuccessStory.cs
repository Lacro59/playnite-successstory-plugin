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

        private AchievementsDatabase achievementsDatabase;
        

        public SuccessStory(IPlayniteAPI api) : base(api)
        {
            settings = new SuccessStorySettings(this);

            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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

            // Custom theme button
            if (settings.EnableIntegrationInCustomTheme)
            {
                EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(OnCustomThemeButtonClick));
            }
        }

        public override IEnumerable<ExtensionFunction> GetFunctions()
        {
            List<ExtensionFunction> listFunctions = new List<ExtensionFunction>();
            listFunctions.Add(
                new ExtensionFunction(
                    resources.GetString("LOCSucessStory"),
                    () =>
                    {
                        // Add code to be execute when user invokes this menu entry.

                        new SuccessView(this, settings, PlayniteApi, this.GetPluginUserDataPath()).ShowDialog();
                    })
                );

            if (settings.EnableRetroAchievementsView && settings.EnableRetroAchievements)
            {
                listFunctions.Add(
                    new ExtensionFunction(
                        resources.GetString("LOCSucessStory") + " - RetroAchievements",
                        () =>
                        {
                            // Add code to be execute when user invokes this menu entry.

                            new SuccessView(this, settings, PlayniteApi, this.GetPluginUserDataPath(), true).ShowDialog();
                        })
                    );
            }

            return listFunctions;
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
            achievementsDatabase.Remove(game);
            achievementsDatabase.Add(game, settings);

            // Refresh integration interface
            SuccessStory.isFirstLoad = true;
            Integration();
        }

        public override void OnGameUninstalled(Game game)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStopped()
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated()
        {
            // Add code to be executed when library is updated.

            // Get achievements for the new game added in the library.
            foreach (var game in PlayniteApi.Database.Games)
            {
                if (game.Added == null && ((DateTime)game.Added).ToString("yyyy-MM-dd") == DateTime.Now.ToString("yyyy-MM-dd"))
                {
                    achievementsDatabase.Remove(game);
                    achievementsDatabase.Add(game, settings);
                }
            }

            // Refresh integration interface
            SuccessStory.isFirstLoad = true;
            Integration();
        }

        #region Interface integration
        private Game GameSelected { get; set; }
        private StackPanel PART_ElemDescription = null;

        public static bool isFirstLoad = true;

        /// <summary>
        /// Event for the header button for show plugin view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBtHeaderClick(object sender, RoutedEventArgs e)
        {
            new SuccessView(this, settings, PlayniteApi, this.GetPluginUserDataPath()).ShowDialog();
        }

        public override void OnApplicationStarted()
        {
            // Add code to be executed when Playnite is initialized.

            if (settings.EnableIntegrationButtonHeader)
            {
                logger.Info("SuccessStory - Add Header button");
                Button btHeader = new SuccessStoryButtonHeader(TransformIcon.Get("SuccessStory"));
                btHeader.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
                btHeader.Click += OnBtHeaderClick;
                ui.AddButtonInWindowsHeader(btHeader);
            }
        }

        private void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            string ButtonName = string.Empty;
            try
            {
                ButtonName = ((Button)sender).Name;
                if (ButtonName == "PART_ScCustomButton")
                {
                    OnBtGameSelectedActionBarClick(sender, e);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", "OnCustomThemeButtonClick() error");
            }
        }

        private void OnBtGameSelectedActionBarClick(object sender, RoutedEventArgs e)
        {
            // Show SuccessView
            if (settings.EnableRetroAchievementsView && PlayniteTools.IsGameEmulated(PlayniteApi, GameSelected))
            {
                new SuccessView(this, settings, PlayniteApi, this.GetPluginUserDataPath(), true, GameSelected).ShowDialog();
            }
            else
            {
                new SuccessView(this, settings, PlayniteApi, this.GetPluginUserDataPath(), false, GameSelected).ShowDialog();
            }
        }

        private void OnGameSelectedToggleButtonClick(object sender, RoutedEventArgs e)
        {
            if (PART_ElemDescription != null)
            {
                if ((bool)((ToggleButton)sender).IsChecked)
                {
                    for (int i = 0; i < PART_ElemDescription.Children.Count; i++)
                    {
                        if (((FrameworkElement)PART_ElemDescription.Children[i]).Name == "PART_Achievements")
                        {
                            ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Visible;

                            // Uncheck other integration ToggleButton
                            foreach (ToggleButton sp in Tools.FindVisualChildren<ToggleButton>(Application.Current.MainWindow))
                            {
                                if (sp.Name == "PART_GaToggleButton")
                                {
                                    sp.IsChecked = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < PART_ElemDescription.Children.Count; i++)
                    {
                        if (((FrameworkElement)PART_ElemDescription.Children[i]).Name == "PART_Achievements")
                        {
                            ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            if (((FrameworkElement)PART_ElemDescription.Children[i]).Name != "PART_GameActivity")
                            {
                                ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
            }
            else
            {
                logger.Warn("SuccessStory - PART_ElemDescription not found in OnGameSelectedToggleButtonClick()");
            }
        }

        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            try
            {
                if (args.NewValue != null && args.NewValue.Count == 1)
                {
                    GameSelected = args.NewValue[0];

                    // Reset view visibility
                    if (PART_ElemDescription != null)
                    {
                        for (int i = 0; i < PART_ElemDescription.Children.Count; i++)
                        {
                            if ((((FrameworkElement)PART_ElemDescription.Children[i]).Name != "PART_GameActivity") && (((FrameworkElement)PART_ElemDescription.Children[i]).Name != "PART_Achievements"))
                            {
                                ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Visible;
                            }
                        }
                    }

                    Integration();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", $"OnGameSelected() ");
            }
        }

        private async Task<GameAchievements> LoadData(IPlayniteAPI PlayniteApi, string PluginUserDataPath, SuccessStorySettings settings)
        {
            // Refresh database
            if (SuccessStory.isFirstLoad)
            {
                achievementsDatabase = new AchievementsDatabase(this, PlayniteApi, settings, this.GetPluginUserDataPath());
                achievementsDatabase.Initialize();
                SuccessStory.isFirstLoad = false;
            }

            GameAchievements SelectedGameAchievements = achievementsDatabase.Get(GameSelected.Id);

            // TODO Add in PluginCommon
            string GameSourceName = string.Empty;

            if (GameSelected.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
            {
                GameSourceName = GameSelected.Source.Name;

                if (PlayniteTools.IsGameEmulated(PlayniteApi, GameSelected))
                {
                    GameSourceName = "RetroAchievements";
                }
            }
            else
            {
                if (PlayniteTools.IsGameEmulated(PlayniteApi, GameSelected))
                {
                    GameSourceName = "RetroAchievements";
                }
                else
                {
                    GameSourceName = "Playnite";
                }
            }

            // Download Achievements if not exist in database.
            if (SelectedGameAchievements == null)
            {
                logger.Info($"SuccessStory - Download achievements for {GameSelected.Name} - {GameSourceName}");
                achievementsDatabase.Add(GameSelected, settings);
                achievementsDatabase.Initialize();
                SelectedGameAchievements = achievementsDatabase.Get(GameSelected.Id);
            }

            return SelectedGameAchievements;
        }

        private void Integration()
        {
            try
            {
                // Search game description
                if (PART_ElemDescription == null)
                {
                    foreach (StackPanel sp in Tools.FindVisualChildren<StackPanel>(Application.Current.MainWindow))
                    {
                        if (sp.Name == "PART_ElemDescription")
                        {
                            PART_ElemDescription = sp;
                            break;
                        }
                    }
                }

                // Delete
                logger.Info("SuccessStory - Delete");
                ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButton("PART_ScButton");
                ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButton("PART_ScToggleButton");
                ui.RemoveElementInGameSelectedDescription("PART_Achievements");
                ui.ClearElementInCustomTheme("PART_Achievements_Graphics");
                ui.ClearElementInCustomTheme("PART_Achievements_List");
                ui.ClearElementInCustomTheme("PART_Achievements_ListCompactLocked");
                ui.ClearElementInCustomTheme("PART_Achievements_ListCompactUnlocked");
                ui.ClearElementInCustomTheme("PART_Achievements_ProgressBar");


                // Reset resources
#if DEBUG
                logger.Debug($"SuccessStory - Reset ressource for {GameSelected.Name}");
#endif
                List<ResourcesList> resourcesLists = new List<ResourcesList>();
                resourcesLists.Add(new ResourcesList { Key = "Sc_HasData", Value = false });
                resourcesLists.Add(new ResourcesList { Key = "Sc_Is100Percent", Value = false });
                resourcesLists.Add(new ResourcesList { Key = "Sc_Total", Value = 0 });
                resourcesLists.Add(new ResourcesList { Key = "Sc_TotalDouble", Value = (double)0 });
                resourcesLists.Add(new ResourcesList { Key = "Sc_TotalString", Value = "0" });
                resourcesLists.Add(new ResourcesList { Key = "Sc_Unlocked", Value = 0 });
                resourcesLists.Add(new ResourcesList { Key = "Sc_UnlockedDouble", Value = (double)0 });
                resourcesLists.Add(new ResourcesList { Key = "Sc_UnlockedString", Value = "0" });
                resourcesLists.Add(new ResourcesList { Key = "Sc_Locked", Value = 0 });
                resourcesLists.Add(new ResourcesList { Key = "Sc_LockedDouble", Value = (double)0 });
                resourcesLists.Add(new ResourcesList { Key = "Sc_LockedString", Value = "0" });

                resourcesLists.Add(new ResourcesList { Key = "Sc_EnableIntegrationInCustomTheme", Value = settings.EnableIntegrationInCustomTheme });
                resourcesLists.Add(new ResourcesList { Key = "Sc_IntegrationShowGraphic", Value = settings.IntegrationShowGraphic });
                resourcesLists.Add(new ResourcesList { Key = "Sc_IntegrationShowAchievements", Value = settings.IntegrationShowAchievements });
                resourcesLists.Add(new ResourcesList { Key = "Sc_IntegrationShowProgressBar", Value = settings.IntegrationShowProgressBar });
                resourcesLists.Add(new ResourcesList { Key = "Sc_IntegrationShowAchievementsCompactLocked", Value = settings.IntegrationShowAchievementsCompactLocked });
                resourcesLists.Add(new ResourcesList { Key = "Sc_IntegrationShowAchievementsCompactUnlocked", Value = settings.IntegrationShowAchievementsCompactUnlocked });
                ui.AddResources(resourcesLists);


                taskHelper.Check();

                CancellationTokenSource tokenSource = new CancellationTokenSource();
                CancellationToken ct = tokenSource.Token;

                var taskIntegration = Task.Run(() => LoadData(PlayniteApi, this.GetPluginUserDataPath(), settings), tokenSource.Token)
                .ContinueWith(antecedent =>
                {
                    GameAchievements SelectedGameAchievements = antecedent.Result;

                    Application.Current.Dispatcher.Invoke(new Action(() => {
                        // No achievements
                        if (SelectedGameAchievements == null || !SelectedGameAchievements.HaveAchivements)
                        {
                            logger.Warn("SuccessStory - No achievement for " + GameSelected.Name);
                            return;
                        }

                        // Add resources
#if DEBUG
                        logger.Debug($"SuccessStory - Add ressource for {GameSelected.Name}");
#endif
                        resourcesLists.Add(new ResourcesList { Key = "Sc_HasData", Value = true });
                        resourcesLists.Add(new ResourcesList { Key = "Sc_Is100Percent", Value = SelectedGameAchievements.Is100Percent });
                        resourcesLists.Add(new ResourcesList { Key = "Sc_Total", Value = SelectedGameAchievements.Total });
                        resourcesLists.Add(new ResourcesList { Key = "Sc_TotalDouble", Value = double.Parse(SelectedGameAchievements.Total.ToString()) });
                        resourcesLists.Add(new ResourcesList { Key = "Sc_TotalString", Value = SelectedGameAchievements.Total.ToString() });
                        resourcesLists.Add(new ResourcesList { Key = "Sc_Unlocked", Value = SelectedGameAchievements.Unlocked });
                        resourcesLists.Add(new ResourcesList { Key = "Sc_UnlockedDouble", Value = double.Parse(SelectedGameAchievements.Unlocked.ToString()) });
                        resourcesLists.Add(new ResourcesList { Key = "Sc_UnlockedString", Value = SelectedGameAchievements.Unlocked.ToString() });
                        resourcesLists.Add(new ResourcesList { Key = "Sc_Locked", Value = SelectedGameAchievements.Locked });
                        resourcesLists.Add(new ResourcesList { Key = "Sc_LockedDouble", Value = double.Parse(SelectedGameAchievements.Locked.ToString()) });
                        resourcesLists.Add(new ResourcesList { Key = "Sc_LockedString", Value = SelectedGameAchievements.Locked.ToString() });
                        ui.AddResources(resourcesLists);


                        // Auto integration
                        if (settings.EnableIntegrationInDescription || settings.EnableIntegrationInDescriptionWithToggle)
                        {
                            if (settings.EnableIntegrationInDescriptionWithToggle)
                            {
#if DEBUG
                                logger.Debug($"SuccessStory - Add IntegrationInDescriptionWithToggle for {GameSelected.Name}");
#endif
                                ToggleButton tb = new ToggleButton();
                                if (settings.IntegrationToggleDetails)
                                {
                                    tb = new SuccessStoryToggleButtonDetails(SelectedGameAchievements.Unlocked, SelectedGameAchievements.Total);
                                }
                                else
                                {
                                    tb = new SuccessStoryToggleButton();
                                    tb.Content = resources.GetString("LOCSucessStoryAchievements");
                                }

                                tb.IsChecked = false;
                                tb.Name = "PART_ScToggleButton";
                                tb.Width = 150;
                                tb.HorizontalAlignment = HorizontalAlignment.Right;
                                tb.VerticalAlignment = VerticalAlignment.Stretch;
                                tb.Margin = new Thickness(10, 0, 0, 0);
                                tb.Click += OnGameSelectedToggleButtonClick;

                                ui.AddButtonInGameSelectedActionBarButtonOrToggleButton(tb);
                            }


                            // Add Achievements elements
                            StackPanel ScA = CreateSc(achievementsDatabase, SelectedGameAchievements, settings.IntegrationShowTitle, settings.IntegrationShowGraphic, settings.IntegrationShowAchievements, settings.IntegrationShowAchievementsCompactLocked, settings.IntegrationShowAchievementsCompactUnlocked, settings.IntegrationShowProgressBar, false);

                            if (settings.EnableIntegrationInDescriptionWithToggle)
                            {
                                ScA.Visibility = Visibility.Collapsed;
                            }

                            if (!ct.IsCancellationRequested)
                            {
                                ui.AddElementInGameSelectedDescription(ScA, settings.IntegrationTopGameDetails);
                            }
                        }


                        // Auto adding button
                        if (settings.EnableIntegrationButton || settings.EnableIntegrationButtonDetails)
                        {
#if DEBUG
                            logger.Debug($"SuccessStory - Add IntegrationButtonDetails for {GameSelected.Name}");
#endif
                            Button bt = new Button();
                            if (settings.EnableIntegrationButton)
                            {
                                bt.Content = resources.GetString("LOCSucessStoryAchievements");
                            }

                            if (settings.EnableIntegrationButtonDetails)
                            {
                                bt = new SuccessStoryButtonDetails(SelectedGameAchievements.Unlocked, SelectedGameAchievements.Total);
                            }

                            bt.Name = "PART_ScButton";
                            bt.Width = 150;
                            bt.HorizontalAlignment = HorizontalAlignment.Right;
                            bt.VerticalAlignment = VerticalAlignment.Stretch;
                            bt.Margin = new Thickness(10, 0, 0, 0);
                            bt.Click += OnBtGameSelectedActionBarClick;

                            if (!ct.IsCancellationRequested)
                            {
                                ui.AddButtonInGameSelectedActionBarButtonOrToggleButton(bt);
                            }
                        }


                        // Custom theme
                        if (settings.EnableIntegrationInCustomTheme)
                        {
#if DEBUG
                            logger.Debug($"SuccessStory - Add IntegrationInCustomTheme for {GameSelected.Name}");
#endif

                            if (settings.IntegrationShowGraphic)
                            {
                                StackPanel scAG = CreateSc(achievementsDatabase, SelectedGameAchievements, false, true, false, false, false, false, true);
                                if (!ct.IsCancellationRequested)
                                {
                                    ui.AddElementInCustomTheme(scAG, "PART_Achievements_Graphics");
                                }
                            }

                            if (settings.IntegrationShowAchievements)
                            {
                                StackPanel scAL = CreateSc(achievementsDatabase, SelectedGameAchievements, false, false, true, false, false, false, true);
                                if (!ct.IsCancellationRequested)
                                {
                                    ui.AddElementInCustomTheme(scAL, "PART_Achievements_List");
                                }
                            }

                            if (settings.IntegrationShowProgressBar)
                            {
                                StackPanel scPB = CreateSc(achievementsDatabase, SelectedGameAchievements, false, false, false, false, false, true, true);
                                if (!ct.IsCancellationRequested)
                                {
                                    ui.AddElementInCustomTheme(scPB, "PART_Achievements_ProgressBar");
                                }
                            }

                            if (settings.IntegrationShowAchievementsCompactLocked)
                            {
                                StackPanel scPB = CreateSc(achievementsDatabase, SelectedGameAchievements, false, false, false, true, false, false, true);
                                if (!ct.IsCancellationRequested)
                                {
                                    ui.AddElementInCustomTheme(scPB, "PART_Achievements_ListCompactLocked");
                                }
                            }

                            if (settings.IntegrationShowAchievementsCompactUnlocked)
                            {
                                StackPanel scPB = CreateSc(achievementsDatabase, SelectedGameAchievements, false, false, false, false, true, false, true);
                                if (!ct.IsCancellationRequested)
                                {
                                    ui.AddElementInCustomTheme(scPB, "PART_Achievements_ListCompactUnlocked");
                                }
                            }
                        }
                    }));
                });

                taskHelper.Add(taskIntegration, tokenSource);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", $"Impossible integration");
            }
        }

        // Create FrameworkElement with achievements datas
        public StackPanel CreateSc(AchievementsDatabase achievementsDatabase, GameAchievements SelectedGameAchievements, bool ShowTitle, bool ShowGraphic, bool ShowAchievements, bool ShowAchievementsCompactLocked, bool ShowAchievementsCompactUnlocked, bool ShowProgressBar, bool IsCustom = false)
        {
            StackPanel spA = new StackPanel();
            spA.Name = "PART_Achievements";

            if (ShowTitle)
            {
                TextBlock tbA = new TextBlock();
                tbA.Name = "PART_Achievements_TextBlock";
                tbA.Text = resources.GetString("LOCSucessStoryAchievements");
                tbA.Style = (Style)resources.GetResource("BaseTextBlockStyle");
                tbA.Margin = new Thickness(0, 15, 0, 5);

                Separator sep = new Separator();
                sep.Name = "PART_Achievements_Separator";
                sep.Background = (Brush)resources.GetResource("PanelSeparatorBrush");

                spA.Children.Add(tbA);
                spA.Children.Add(sep);
                spA.UpdateLayout();
            }

            if (ShowGraphic)
            {
                StackPanel spAG = new StackPanel();
                if (!IsCustom)
                {
                    spAG.Name = "PART_Achievements_Graphics";
                    spAG.Height = settings.IntegrationShowGraphicHeight;
                    spAG.Margin = new Thickness(0, 5, 0, 5);
                }

                AchievementsGraphicsDataCount GraphicsData = null;
                if (settings.GraphicAllUnlockedByDay)
                {
                    GraphicsData = achievementsDatabase.GetCountByMonth(GameSelected.Id, (settings.IntegrationGraphicOptionsCountAbscissa - 1));
                }
                else
                {
                    GraphicsData = achievementsDatabase.GetCountByDay(GameSelected.Id, (settings.IntegrationGraphicOptionsCountAbscissa - 1));
                }
                string[] StatsGraphicsAchievementsLabels = GraphicsData.Labels;
                SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection();
                StatsGraphicAchievementsSeries.Add(new LineSeries
                {
                    Title = string.Empty,
                    Values = GraphicsData.Series
                });

                settings.IgnoreSettings = false;
                spAG.Children.Add(new SuccessStoryAchievementsGraphics(StatsGraphicAchievementsSeries, StatsGraphicsAchievementsLabels, settings, IsCustom));

                spA.Children.Add(spAG);
                spA.UpdateLayout();
            }

            if (ShowProgressBar)
            {
                StackPanel spPB = new StackPanel();
                if (!IsCustom)
                {
                    spPB.Name = "PART_Achievements_ProgressBar";
                    spPB.Height = 40;
                    spPB.Margin = new Thickness(0, 5, 0, 5);
                }

                spPB.Children.Add(new SuccessStoryAchievementsProgressBar(SelectedGameAchievements.Unlocked, SelectedGameAchievements.Total, settings.IntegrationShowProgressBarPercent, settings.IntegrationShowProgressBarIndicator, IsCustom));

                spA.Children.Add(spPB);
                spA.UpdateLayout();
            }

            if (ShowAchievements)
            {
                StackPanel spAL = new StackPanel();
                if (!IsCustom)
                {
                    spAL.Name = "PART_Achievements_List";
                    spAL.Height = settings.IntegrationShowAchievementsHeight;
                    spAL.Margin = new Thickness(0, 5, 0, 5);
                }

                spAL.Children.Add(new SuccessStoryAchievementsList(SelectedGameAchievements.Achievements, IsCustom, settings.EnableRaretyIndicator));

                spA.Children.Add(spAL);
                spA.UpdateLayout();
            }

            if (ShowAchievementsCompactLocked)
            {
                StackPanel spALCL = new StackPanel();
                if (!IsCustom)
                {
                    spALCL.Name = "PART_Achievements_ListCompactLocked";
                    //spALC.Height = settings.IntegrationShowAchievementsHeight;
                    spALCL.Margin = new Thickness(0, 5, 0, 5);
                }

                spALCL.Children.Add(new SuccessStoryAchievementsCompact(SelectedGameAchievements.Achievements, false, settings.EnableRaretyIndicator));

                spA.Children.Add(spALCL);
                spA.UpdateLayout();
            }

            if (ShowAchievementsCompactUnlocked)
            {
                StackPanel spALCUL = new StackPanel();
                if (!IsCustom)
                {
                    spALCUL.Name = "PART_Achievements_ListCompactUnlocked";
                    //spALC.Height = settings.IntegrationShowAchievementsHeight;
                    spALCUL.Margin = new Thickness(0, 5, 0, 5);
                }

                spALCUL.Children.Add(new SuccessStoryAchievementsCompact(SelectedGameAchievements.Achievements, true, settings.EnableRaretyIndicator));

                spA.Children.Add(spALCUL);
                spA.UpdateLayout();
            }

            return spA;
        }
        #endregion

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
