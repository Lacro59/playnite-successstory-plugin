using LiveCharts;
using LiveCharts.Wpf;
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private AchievementsDatabase achievementsDatabase;
        

        public SuccessStory(IPlayniteAPI api) : base(api)
        {
            settings = new SuccessStorySettings(this);

            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.Localization.SetPluginLanguage(pluginFolder, api.Paths.ConfigurationPath);
            // Add common in application ressource.
            PluginCommon.Common.Load(pluginFolder);
        }

        public override IEnumerable<ExtensionFunction> GetFunctions()
        {
            return new List<ExtensionFunction>
            {
                new ExtensionFunction(
                    resources.GetString("LOCSucessStory"),
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
            new SuccessView(settings, PlayniteApi, this.GetPluginUserDataPath()).ShowDialog();
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

        private void OnBtGameSelectedActionBarClick(object sender, RoutedEventArgs e)
        {
            // Show SuccessView
            new SuccessView(settings, PlayniteApi, this.GetPluginUserDataPath(), GameSelected).ShowDialog();
        }

        private void OnGameSelectedToggleButtonClick(object sender, RoutedEventArgs e)
        {
            if (PART_ElemDescription != null)
            {
                if ((bool)((ToggleButton)sender).IsChecked)
                {
                    for (int i = 0; i < PART_ElemDescription.Children.Count; i++)
                    {
                        logger.Debug(((FrameworkElement)PART_ElemDescription.Children[i]).Name);

                        if (((FrameworkElement)PART_ElemDescription.Children[i]).Name == "PART_Achievements")
                        {
                            ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Visible;

                            // Uncheck other integratio ToggleButton
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
                logger.Error("SuccessStory - PART_ElemDescription not found in OnGameSelectedToggleButtonClick()");
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

        private void Integration()
        {
            try
            {
                // Refresh database
                if (SuccessStory.isFirstLoad)
                {
                    achievementsDatabase = new AchievementsDatabase(PlayniteApi, settings, this.GetPluginUserDataPath());
                    achievementsDatabase.Initialize();
                    SuccessStory.isFirstLoad = false;
                }


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


                GameAchievements SelectedGameAchievements = achievementsDatabase.Get(GameSelected.Id);
                
                // Download Achievements if not exist in database.
                if (SelectedGameAchievements == null)
                {
                    logger.Info("SuccessStory - Download achievements for " + GameSelected.Name);
                    achievementsDatabase.Add(GameSelected, settings);
                    achievementsDatabase.Initialize();
                    SelectedGameAchievements = achievementsDatabase.Get(GameSelected.Id);
                }


                // Delete
                logger.Info("SuccessStory - Delete");
                ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButton("PART_ScButton");
                ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButton("PART_ScToggleButton");
                ui.RemoveElementInGameSelectedDescription("PART_Achievements");
                ui.ClearElementInCustomTheme("PART_Achievements_Graphics");
                ui.ClearElementInCustomTheme("PART_Achievements_List");
                ui.ClearElementInCustomTheme("PART_Achievements_ProgressBar");


                // Reset resources
                List<ResourcesList> resourcesLists = new List<ResourcesList>();
                resourcesLists.Add(new ResourcesList { Key = "Sc_Total", Value = "0" });
                resourcesLists.Add(new ResourcesList { Key = "Sc_Unlocked", Value = "0" });
                resourcesLists.Add(new ResourcesList { Key = "Sc_Locked", Value = "0" });
                ui.AddResources(resourcesLists);


                // No achievements
                if (SelectedGameAchievements == null || !SelectedGameAchievements.HaveAchivements)
                {
                    logger.Info("SuccessStory - No achievement for " + GameSelected.Name);
                    return;
                }


                // Add resources
                resourcesLists.Add(new ResourcesList { Key = "Sc_Total", Value = SelectedGameAchievements.Total.ToString() });
                resourcesLists.Add(new ResourcesList { Key = "Sc_Unlocked", Value = SelectedGameAchievements.Unlocked.ToString() });
                resourcesLists.Add(new ResourcesList { Key = "Sc_Locked", Value = SelectedGameAchievements.Locked.ToString() });
                ui.AddResources(resourcesLists);


                // Auto integration
                if (settings.EnableIntegrationInDescription || settings.EnableIntegrationInDescriptionWithToggle)
                {
                    if (settings.EnableIntegrationInDescriptionWithToggle)
                    {
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
                    StackPanel ScA = CreateSc(achievementsDatabase, SelectedGameAchievements, settings.IntegrationShowTitle, settings.IntegrationShowGraphic, settings.IntegrationShowAchievements, settings.IntegrationShowProgressBar, false);
                    
                    if (settings.EnableIntegrationInDescriptionWithToggle)
                    {
                        ScA.Visibility = Visibility.Collapsed;
                    }

                    ui.AddElementInGameSelectedDescription(ScA, settings.IntegrationTopGameDetails);
                }


                // Auto adding button
                if (settings.EnableIntegrationButton || settings.EnableIntegrationButtonDetails)
                {
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

                    ui.AddButtonInGameSelectedActionBarButtonOrToggleButton(bt);
                }


                // Custom theme
                if (settings.EnableIntegrationInCustomTheme)
                {
                    if (settings.IntegrationShowGraphic)
                    {
                        StackPanel scAG = CreateSc(achievementsDatabase, SelectedGameAchievements, false, true, false, false, true);
                        ui.AddElementInCustomTheme(scAG, "PART_Achievements_Graphics");
                    }

                    if (settings.IntegrationShowAchievements)
                    {
                        StackPanel scAL = CreateSc(achievementsDatabase, SelectedGameAchievements, false, false, true, false, true);
                        ui.AddElementInCustomTheme(scAL, "PART_Achievements_List");
                    }

                    if (settings.IntegrationShowProgressBar)
                    {
                        StackPanel scPB = CreateSc(achievementsDatabase, SelectedGameAchievements, false, false, false, true, true);
                        ui.AddElementInCustomTheme(scPB, "PART_Achievements_ProgressBar");
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SucessStory", $"Impossible integration");
            }
        }

        // Create FrameworkElement with achievements datas
        public StackPanel CreateSc(AchievementsDatabase achievementsDatabase, GameAchievements SelectedGameAchievements, bool ShowTitle, bool ShowGraphic, bool ShowAchievements, bool ShowProgressBar, bool IsCustom = false)
        {
            StackPanel spA = new StackPanel();
            spA.Name = "PART_Achievements";

            if (ShowTitle)
            {
                TextBlock tbA = new TextBlock();
                tbA.Name = "PART_Achievements_TextBlock";
                tbA.Text = resources.GetString("LOCSucessStoryAchievements");
                tbA.Style = (Style)resources.GetResource("BaseTextBlockStyle");
                tbA.Margin = new Thickness(0, 15, 0, 10);

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
                    spAG.Height = 120;
                    spAG.MaxHeight = 120;
                    spAG.Margin = new Thickness(0, 5, 0, 5);
                }

                AchievementsGraphicsDataCount GraphicsData = null;
                if (settings.GraphicAllUnlockedByDay)
                {
                    GraphicsData = achievementsDatabase.GetCountByMonth(GameSelected.Id);
                }
                else
                {
                    GraphicsData = achievementsDatabase.GetCountByDay(GameSelected.Id);
                }
                string[] StatsGraphicsAchievementsLabels = GraphicsData.Labels;
                SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection();
                StatsGraphicAchievementsSeries.Add(new LineSeries
                {
                    Title = "",
                    Values = GraphicsData.Series
                });

                spAG.Children.Add(new SuccessStoryAchievementsGraphics(StatsGraphicAchievementsSeries, StatsGraphicsAchievementsLabels));

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

                spPB.Children.Add(new SuccessStoryAchievementsProgressBar(SelectedGameAchievements.Unlocked, SelectedGameAchievements.Total, settings.IntegrationShowProgressBarPercent, settings.IntegrationShowProgressBarIndicator));

                spA.Children.Add(spPB);
                spA.UpdateLayout();
            }

            if (ShowAchievements)
            {
                StackPanel spAL = new StackPanel();
                if (!IsCustom)
                {
                    spAL.Name = "PART_Achievements_List";
                    spAL.MaxHeight = 300;
                    spAL.Margin = new Thickness(0, 5, 0, 5);
                }

                spAL.Children.Add(new SuccessStoryAchievementsList(SelectedGameAchievements.Achievements));

                spA.Children.Add(spAL);
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
            return new SuccessStorySettingsView(PlayniteApi, this.GetPluginUserDataPath(), settings);
        }
    }
}
