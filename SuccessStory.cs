using AchievementsLocal;
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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;


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

        //public void tb_Click(object sender, RoutedEventArgs e)
        //{
        //    if ((bool)((ToggleButton)sender).IsChecked)
        //    {
        //        PART_ElemDescription.Visibility = Visibility.Hidden;
        //
        //        StackPanel sp = (StackPanel)LogicalTreeHelper.FindLogicalNode(PART_ElemDescription.Parent, "PART_Achievements");
        //        sp.Visibility = Visibility.Visible;
        //    }
        //    else
        //    {
        //        PART_ElemDescription.Visibility = Visibility.Visible;
        //
        //        StackPanel sp = (StackPanel)LogicalTreeHelper.FindLogicalNode(PART_ElemDescription.Parent, "PART_Achievements");
        //        sp.Visibility = Visibility.Hidden;
        //    }
        //}

        //private StackPanel spButtons = null;
        private StackPanel PART_ElemDescription = null;

        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            logger.Debug("OnGameSelected Start");

            //// Search parent buttons in game details
            //if (spButtons == null)
            //{
            //    foreach (Button bt in Tools.FindVisualChildren<Button>(Application.Current.MainWindow))
            //    {
            //        if (bt.Name == "PART_ButtonPlayAction")
            //        {
            //            spButtons = (StackPanel)bt.Parent;
            //            break;
            //        }
            //    }
            //}

            // Search parent game description
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

            //if (spButtons.ActualWidth == 356)
            //{
            //    ToggleButton tb = new ToggleButton();
            //    tb.IsChecked = false;
            //    tb.Name = "PART_SuccessStoryButton";
            //    tb.Content = resources.GetString("LOCSucessStoryAchievements");
            //    tb.Width = 150;
            //    tb.Height = 40;
            //    tb.HorizontalAlignment = HorizontalAlignment.Right;
            //    tb.VerticalAlignment = VerticalAlignment.Stretch;
            //    tb.Margin = new Thickness(40, 0, 0, 0);
            //    tb.Click += tb_Click;
            //    
            //    spButtons.Children.Add(tb);
            //    spButtons.UpdateLayout();
            //
            //
            //    DockPanel dp = (DockPanel)(PART_ElemDescription).Parent;
            //    
            //    // StackPanel
            //    StackPanel spA = new StackPanel();
            //    spA.Name = "PART_Achievements";
            //    DockPanel.SetDock(spA, Dock.Right);
            //    spA.Margin = new Thickness(10, 0, 2, 0);
            //    //spA.Visibility = Visibility.Hidden;
            //    
            //    TextBlock tbA = new TextBlock();
            //    tbA.Text = resources.GetString("LOCSucessStoryAchievements");
            //    tbA.Style = (Style)resources.GetResource("BaseTextBlockStyle");
            //    
            //    Separator sep = new Separator();
            //    sep.Background = (Brush)resources.GetResource("PanelSeparatorBrush");
            //    
            //    //< StackPanel x: Name = "PART_Achievements_Graphics" Height = "120" MaxHeight = "120" Margin = "0,5,0,5" ></ StackPanel >
            //    StackPanel spAG = new StackPanel();
            //    spAG.Name = "PART_Achievements_Graphics";
            //    spAG.Height = 120;
            //    spAG.MaxHeight = 120;
            //    spAG.Margin = new Thickness(0, 5, 0, 5);
            //    
            //    //< StackPanel x: Name = "PART_Achievements_List" MaxHeight = "300" Margin = "0,5,0,5" ></ StackPanel >
            //    StackPanel spAL = new StackPanel();
            //    spAL.Name = "PART_Achievements_List";
            //    spAL.MaxHeight = 300;
            //    spAL.Margin = new Thickness(0, 5, 0, 5);
            //    
            //    spA.Children.Add(tbA);
            //    spA.Children.Add(sep);
            //    spA.Children.Add(spAG);
            //    spA.Children.Add(spAL);
            //    spA.UpdateLayout();
            //
            //    dp.Children.Add(spA);
            //    dp.UpdateLayout();
            //
            //    //PART_ElemDescription.Children.Add(spA);
            //    //PART_ElemDescription.UpdateLayout();
            //    
            //}

            if (args.NewValue != null)
            {
                if (args.NewValue.Count == 1)
                {
                    logger.Debug("OnGameSelected load game achievement");

                    AchievementsDatabase AchievementsDatabase = new AchievementsDatabase(PlayniteApi, this.GetPluginUserDataPath());
                    AchievementsDatabase.Initialize();

                    Game SelectedGame = args.NewValue[0];
                    GameAchievements SelectedGameAchievements = AchievementsDatabase.Get(SelectedGame.Id);

                    StackPanel sp = (StackPanel)LogicalTreeHelper.FindLogicalNode(PART_ElemDescription.Parent, "PART_Achievements_List");

                    // List achievements
                    logger.Debug("OnGameSelected add list game achievement");
                    if (sp != null)
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
                            string FileName = new StackTrace(ex, true).GetFrame(0).GetFileName();
                            logger.Error(ex, $"SuccesStory [{FileName} {LineNumber}] - {SelectedGame.Name}: ");
                        }

                        sp.UpdateLayout();
                    }

                    // Graphic
                    logger.Debug("OnGameSelected add graphic game achievement");
                    sp = (StackPanel)LogicalTreeHelper.FindLogicalNode(PART_ElemDescription.Parent, "PART_Achievements_Graphics");

                    if (sp != null)
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
                            string FileName = new StackTrace(ex, true).GetFrame(0).GetFileName();
                            logger.Error(ex, $"SuccesStory [{FileName} {LineNumber}] - {SelectedGame.Name}: ");
                        }

                        sp.UpdateLayout();
                    }

                    AchievementsDatabase = null;
                }
            }

            logger.Debug("OnGameSelected End");
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
