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

            // REfresh integration interface
            Integration();
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

        #region Interface integration
        private void ScButton_Click(object sender, RoutedEventArgs e)
        {
            // Show SuccessView
            new SuccessView(settings, PlayniteApi, this.GetPluginUserDataPath(), GameSelected).ShowDialog();
        }

        private void ScToggleButton_Click(object sender, RoutedEventArgs e)
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
                            ((FrameworkElement)PART_ElemDescription.Children[i]).Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            else
            {
                logger.Error("SuccessStory - PART_ElemDescription not found in ScToggleButton_Click()");
            }
        }

        private Game GameSelected { get; set; }
        private StackPanel PART_ActionButtons = null;
        private StackPanel PART_ElemDescription = null;

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
                            if (((FrameworkElement)PART_ElemDescription.Children[i]).Name != "PART_Achievements")
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
                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                string FileName = new StackTrace(ex, true).GetFrame(0).GetFileName();
                logger.Error(ex, $"SuccessStory [{FileName} {LineNumber}] - OnGameSelected() ");
            }
        }

        private void Integration()
        {
            bool noAchievements = false;

            try
            {
                AchievementsDatabase achievementsDatabase = new AchievementsDatabase(PlayniteApi, this.GetPluginUserDataPath());
                achievementsDatabase.Initialize();

                GameAchievements SelectedGameAchievements = achievementsDatabase.Get(GameSelected.Id);
                
                // Download Achievements if not exist in database.
                if (SelectedGameAchievements == null)
                {
                    logger.Info("SuccesStory - Download achievements for " + GameSelected.Name);
                    achievementsDatabase.Add(GameSelected, settings);
                    achievementsDatabase.Initialize();
                    SelectedGameAchievements = achievementsDatabase.Get(GameSelected.Id);
                }

                if (SelectedGameAchievements == null || !SelectedGameAchievements.HaveAchivements)
                {
                    logger.Info("SuccessStory - No achievement for " + GameSelected.Name);

                    if (settings.EnableIntegrationInDescription || settings.EnableIntegrationInDescriptionWithToggle)
                    {
                        Button PART_ScButton = (Button)LogicalTreeHelper.FindLogicalNode(PART_ActionButtons, "PART_ScButton");
                        // Delete old ButtonDetails
                        if (settings.EnableIntegrationButtonDetails)
                        {
                            PART_ActionButtons.Children.Remove(PART_ScButton);
                            PART_ScButton = null;
                        }

                        ToggleButton PART_ScToggleButton = (ToggleButton)LogicalTreeHelper.FindLogicalNode(PART_ActionButtons, "PART_ScToggleButton");
                        // Delete old ToggleDetails
                        if (settings.IntegrationToggleDetails)
                        {
                            PART_ActionButtons.Children.Remove(PART_ScToggleButton);
                            PART_ScToggleButton = null;
                        }

                        // Delete old
                        string NameControl = "PART_Achievements";
                        StackPanel PART_Achievements = (StackPanel)LogicalTreeHelper.FindLogicalNode(PART_ElemDescription, NameControl);
                        if (PART_Achievements != null)
                        {
                            PART_ElemDescription.Children.Remove(PART_Achievements);
                        }
                    }

                    noAchievements = true;
                }

                // Auto integration
                if (settings.EnableIntegrationInDescription || settings.EnableIntegrationInDescriptionWithToggle)
                {
                    // Search parent action buttons
                    if (PART_ActionButtons == null)
                    {
                        foreach (Button bt in Tools.FindVisualChildren<Button>(Application.Current.MainWindow))
                        {
                            if (bt.Name == "PART_ButtonEditGame")
                            {
                                PART_ActionButtons = (StackPanel)bt.Parent;
                                break;
                            }
                        }
                    }

                    //Adding togglebutton
                    if (settings.EnableIntegrationInDescriptionWithToggle && PART_ActionButtons != null)
                    {
                        ToggleButton PART_ScToggleButton = (ToggleButton)LogicalTreeHelper.FindLogicalNode(PART_ActionButtons, "PART_ScToggleButton");

                        // Delete old ToggleDetails
                        if (settings.IntegrationToggleDetails)
                        {
                            PART_ActionButtons.Children.Remove(PART_ScToggleButton);
                            PART_ScToggleButton = null;
                        }

                        if (PART_ScToggleButton == null && !noAchievements)
                        {
                            ToggleButton tb = new ToggleButton();
                            if (settings.IntegrationToggleDetails)
                            {
                                tb = new SuccessStoryToggleButtonDetails(SelectedGameAchievements.Unlocked, SelectedGameAchievements.Total);
                            }
                            else
                            {
                                tb.Content = resources.GetString("LOCSucessStoryAchievements");
                            }

                            tb.IsChecked = false;
                            tb.Name = "PART_ScToggleButton";                
                            tb.Width = 150;
                            tb.Height = 40;
                            tb.HorizontalAlignment = HorizontalAlignment.Right;
                            tb.VerticalAlignment = VerticalAlignment.Stretch;
                            tb.Margin = new Thickness(10, 0, 0, 0);
                            tb.Click += ScToggleButton_Click;

                            PART_ActionButtons.Children.Add(tb);
                            PART_ActionButtons.UpdateLayout();
                        }
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

                    // Adding control
                    if (PART_ElemDescription != null)
                    {
                        // Delete old
                        string NameControl = "PART_Achievements";
                        StackPanel PART_Achievements = (StackPanel)LogicalTreeHelper.FindLogicalNode(PART_ElemDescription, NameControl);
                        if (PART_Achievements != null)
                        {
                            if (settings.EnableIntegrationInDescription)
                            {
                                PART_ElemDescription.Children.Remove(PART_Achievements);
                            }
                            if (settings.EnableIntegrationInDescriptionWithToggle)
                            {
                                PART_ElemDescription.Children.Remove(PART_Achievements);
                            }
                        }
                        else
                        {
                            logger.Error($"SuccessStory - {NameControl} not found in Integration()");
                        }

                        if (SelectedGameAchievements != null && SelectedGameAchievements.HaveAchivements)
                        {
                            StackPanel ScA = CreateSc(achievementsDatabase, SelectedGameAchievements, settings.IntegrationShowTitle, settings.IntegrationShowGraphic, settings.IntegrationShowAchievements, false);

                            if (settings.EnableIntegrationInDescription)
                            {
                                // Add
                                if (settings.IntegrationTopGameDetails)
                                {
                                    PART_ElemDescription.Children.Insert(0, ScA);
                                }
                                else
                                {
                                    PART_ElemDescription.Children.Add(ScA);
                                }

                                PART_ElemDescription.UpdateLayout();
                            }

                            if (settings.EnableIntegrationInDescriptionWithToggle)
                            {
                                ScA.Visibility = Visibility.Collapsed;
                                PART_ElemDescription.Children.Add(ScA);
                                PART_ElemDescription.UpdateLayout();
                            }
                        }
                    }
                    else
                    {
                        logger.Error($"SuccessStory - PART_ElemDescription not found in Integration()");
                    }
                }

                // Auto adding button
                if (settings.EnableIntegrationButton || settings.EnableIntegrationButtonDetails)
                {
                    // Search parent action buttons
                    if (PART_ActionButtons == null)
                    {
                        foreach (Button bt in Tools.FindVisualChildren<Button>(Application.Current.MainWindow))
                        {
                            if (bt.Name == "PART_ButtonEditGame")
                            {
                                PART_ActionButtons = (StackPanel)bt.Parent;
                                break;
                            }
                        }
                    }

                    // Adding button
                    if (PART_ActionButtons != null)
                    {
                        Button PART_ScButton = (Button)LogicalTreeHelper.FindLogicalNode(PART_ActionButtons, "PART_ScButton");

                        // Delete old ButtonDetails
                        if (settings.EnableIntegrationButtonDetails)
                        {
                            PART_ActionButtons.Children.Remove(PART_ScButton);
                            PART_ScButton = null;
                        }

                        if (PART_ScButton == null)
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
                            bt.Height = 40;
                            bt.HorizontalAlignment = HorizontalAlignment.Right;
                            bt.VerticalAlignment = VerticalAlignment.Stretch;
                            bt.Margin = new Thickness(10, 0, 0, 0);
                            bt.Click += ScButton_Click;

                            PART_ActionButtons.Children.Add(bt);
                            PART_ActionButtons.UpdateLayout();
                        }
                    }
                }


                // Custom theme
                if (settings.EnableIntegrationInCustomTheme)
                {
                    // Search custom element
                    foreach (StackPanel sp in Tools.FindVisualChildren<StackPanel>(Application.Current.MainWindow))
                    {
                        if (sp.Name == "PART_Achievements_Graphics")
                        {
                            if (SelectedGameAchievements != null && SelectedGameAchievements.HaveAchivements)
                            {
                                // Create 
                                StackPanel scAG = CreateSc(achievementsDatabase, SelectedGameAchievements, false, true, false, true);

                                // Clear & add
                                sp.Children.Clear();
                                sp.Children.Add(scAG);
                                sp.UpdateLayout();
                            }
                            else
                            {
                                sp.Children.Clear();
                                sp.UpdateLayout();
                            }
                        }

                        if (sp.Name == "PART_Achievements_List")
                        {
                            if (SelectedGameAchievements != null && SelectedGameAchievements.HaveAchivements)
                            {
                                // Create 
                                StackPanel scAL = CreateSc(achievementsDatabase, SelectedGameAchievements, false, false, true, true);

                                // Clear & add
                                sp.Children.Clear();
                                sp.Children.Add(scAL);
                                sp.UpdateLayout();
                            }
                            else
                            {
                                sp.Children.Clear();
                                sp.UpdateLayout();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                string FileName = new StackTrace(ex, true).GetFrame(0).GetFileName();
                logger.Error(ex, $"SuccessStory [{FileName} {LineNumber}] - Impossible integration ");
            }
        }

        public StackPanel CreateSc(AchievementsDatabase achievementsDatabase, GameAchievements SelectedGameAchievements, bool ShowTitle, bool ShowGraphic, bool ShowAchievements, bool IsCustom = false)
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

                AchievementsGraphicsDataCount GraphicsData = achievementsDatabase.GetCountByMonth(GameSelected.Id);
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
