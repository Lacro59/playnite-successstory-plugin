using Playnite.SDK;
using SuccessStory.Models;
using System.Windows.Controls;
using Newtonsoft.Json;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using SuccessStory.Services;
using System.Windows.Threading;
using System.Threading;
using System;
using PluginCommon;

namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour ScDescriptionIntegration.xaml
    /// </summary>
    public partial class ScDescriptionIntegration : StackPanel
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public ScDescriptionIntegration()
        {
            InitializeComponent();

            if (PluginDatabase.PluginSettings.IntegrationShowProgressBar)
            {
                SuccessStoryAchievementsProgressBar successStoryAchievementsProgressBar = new SuccessStoryAchievementsProgressBar();
                PART_SuccessStory_ProgressBar.Children.Add(successStoryAchievementsProgressBar);
            }

            if (PluginDatabase.PluginSettings.IntegrationShowGraphic)
            {
                SuccessStoryAchievementsGraphics successStoryAchievementsGraphics = new SuccessStoryAchievementsGraphics();
                successStoryAchievementsGraphics.DisableAnimations(true);
                PART_SuccessStory_Graphic.Children.Add(successStoryAchievementsGraphics);
            }

            if (PluginDatabase.PluginSettings.IntegrationShowAchievements)
            {
                SuccessStoryAchievementsList successStoryAchievementsList = new SuccessStoryAchievementsList();
                PART_SuccessStory_List.Children.Add(successStoryAchievementsList);
            }

            if (PluginDatabase.PluginSettings.IntegrationShowAchievementsCompactUnlocked)
            {
                SuccessStoryAchievementsCompact successStoryAchievementsCompact_Unlocked = new SuccessStoryAchievementsCompact(true);
                PART_SuccessStory_Compact_Unlocked.Children.Add(successStoryAchievementsCompact_Unlocked);
            }

            if (PluginDatabase.PluginSettings.IntegrationShowAchievementsCompactLocked)
            {
                SuccessStoryAchievementsCompact successStoryAchievementsCompact_Locked = new SuccessStoryAchievementsCompact();
                PART_SuccessStory_Compact_Locked.Children.Add(successStoryAchievementsCompact_Locked);
            }

            if (PluginDatabase.PluginSettings.IntegrationShowUserStats)
            {
                SuccessStoryUserStats successStoryUserStats = new SuccessStoryUserStats();
                PART_SuccessStoryUserStats.Children.Add(successStoryUserStats);
            }


            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == "GameSelectedData" || e.PropertyName == "PluginSettings")
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        // ToggleButton
                        if (PluginDatabase.PluginSettings.EnableIntegrationInDescriptionWithToggle && PluginDatabase.PluginSettings.EnableIntegrationButton)
                        {
                            this.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            // No data
                            if (!PluginDatabase.GameSelectedData.HasData)
                            {
                                this.Visibility = Visibility.Collapsed;
                                return;
                            }
                            else
                            {
                                this.Visibility = Visibility.Visible;
                            }
                        }

                        // Margin with title
                        if (PluginDatabase.PluginSettings.IntegrationShowTitle)
                        {
                            PART_SuccessStory_ProgressBar.Margin = new Thickness(0, 5, 0, 5);
                            PART_SuccessStory_Graphic.Margin = new Thickness(0, 5, 0, 5);
                            PART_SuccessStory_List.Margin = new Thickness(0, 5, 0, 5);
                            PART_SuccessStory_Compact_Unlocked.Margin = new Thickness(0, 5, 0, 5);
                            PART_SuccessStory_Compact_Locked.Margin = new Thickness(0, 5, 0, 5);
                            PART_SuccessStoryUserStats.Margin = new Thickness(0, 5, 0, 5);
                        }
                        // Without title
                        else
                        {
                            PART_SuccessStory_ProgressBar.Margin = new Thickness(0, 5, 0, 5);
                            PART_SuccessStory_Graphic.Margin = new Thickness(0, 5, 0, 5);
                            PART_SuccessStory_List.Margin = new Thickness(0, 5, 0, 5);
                            PART_SuccessStory_Compact_Unlocked.Margin = new Thickness(0, 5, 0, 5);
                            PART_SuccessStory_Compact_Locked.Margin = new Thickness(0, 5, 0, 5);
                            PART_SuccessStoryUserStats.Margin = new Thickness(0, 5, 0, 5);

                            if (!PluginDatabase.PluginSettings.IntegrationTopGameDetails)
                            {
                                if (PluginDatabase.PluginSettings.IntegrationShowGraphic)
                                {
                                    PART_SuccessStory_ProgressBar.Margin = new Thickness(0, 15, 0, 5);
                                }
                                else if (PluginDatabase.PluginSettings.IntegrationShowGraphic)
                                {
                                    PART_SuccessStory_Graphic.Margin = new Thickness(0, 15, 0, 5);
                                }
                                else if (PluginDatabase.PluginSettings.IntegrationShowAchievements)
                                {
                                    PART_SuccessStory_List.Margin = new Thickness(0, 15, 0, 5);
                                }
                                else if (PluginDatabase.PluginSettings.IntegrationShowAchievementsCompactUnlocked)
                                {
                                    PART_SuccessStory_Compact_Unlocked.Margin = new Thickness(0, 15, 0, 5);
                                }
                                else if (PluginDatabase.PluginSettings.IntegrationShowAchievementsCompactLocked)
                                {
                                    PART_SuccessStory_Compact_Locked.Margin = new Thickness(0, 15, 0, 5);
                                }
                                else if (PluginDatabase.PluginSettings.IntegrationShowUserStats)
                                {
                                    PART_SuccessStoryUserStats.Margin = new Thickness(0, 15, 0, 5);
                                }
                            }
                        }


                        bool IntegrationShowTitle = PluginDatabase.PluginSettings.IntegrationShowTitle;
                        if (PluginDatabase.PluginSettings.EnableIntegrationInDescriptionWithToggle)
                        {
                            IntegrationShowTitle = true;
                        }


                        PART_SuccessStory_Graphic.Height = PluginDatabase.PluginSettings.IntegrationShowGraphicHeight;
                        PART_SuccessStory_List.Height = PluginDatabase.PluginSettings.IntegrationShowAchievementsHeight;
                        PART_SuccessStoryUserStats.Height = 100;

                        if (!PluginDatabase.GameSelectedData.HasDataStats)
                        {
                            PART_SuccessStoryUserStats.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            PART_SuccessStoryUserStats.Visibility = Visibility.Visible;
                        }


                        this.DataContext = new
                        {
                            IntegrationShowTitle = IntegrationShowTitle,
                            IntegrationShowProgressBar = PluginDatabase.PluginSettings.IntegrationShowProgressBar,
                            IntegrationShowGraphic = PluginDatabase.PluginSettings.IntegrationShowGraphic,
                            IntegrationShowAchievements = PluginDatabase.PluginSettings.IntegrationShowAchievements,
                            IntegrationShowAchievementsCompactUnlocked = PluginDatabase.PluginSettings.IntegrationShowAchievementsCompactUnlocked,
                            IntegrationShowAchievementsCompactLocked = PluginDatabase.PluginSettings.IntegrationShowAchievementsCompactLocked,
                            IntegrationShowUserStats = PluginDatabase.PluginSettings.IntegrationShowUserStats
                        };
#if DEBUG
                        logger.Debug($"SuccessStory - DataContext: {JsonConvert.SerializeObject(DataContext)}");
#endif
                    }));
                }
                else
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        if (!PluginDatabase.IsViewOpen)
                        {
                            this.Visibility = Visibility.Collapsed;
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory");
            }
        }
    }
}
