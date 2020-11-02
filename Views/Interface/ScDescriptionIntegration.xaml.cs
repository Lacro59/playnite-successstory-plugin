using Playnite.SDK;
using SuccessStory.Database;
using SuccessStory.Models;
using System.Windows.Controls;
using Newtonsoft.Json;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;

namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour ScDescriptionIntegration.xaml
    /// </summary>
    public partial class ScDescriptionIntegration : StackPanel
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private bool _IsCustom;
        private bool _ShowAchievementsGraphic;
        private bool _ShowAchievementsList;
        private bool _ShowAchievementsCompactLocked;
        private bool _ShowAchievementsCompactUnlocked;
        private bool _ShowProgressBar;

        private SuccessStoryAchievementsProgressBar successStoryAchievementsProgressBar;
        private SuccessStoryAchievementsGraphics successStoryAchievementsGraphics;
        private SuccessStoryAchievementsList successStoryAchievementsList;
        private SuccessStoryAchievementsCompact successStoryAchievementsCompact_Locked;
        private SuccessStoryAchievementsCompact successStoryAchievementsCompact_Unlocked;


        public ScDescriptionIntegration(SuccessStorySettings settings, AchievementsDatabase achievementsDatabase, GameAchievements SelectedGameAchievements, 
            bool IsCustom = false, bool ShowAchievementsGraphic = false, bool ShowAchievementsList = false, bool ShowAchievementsCompactLocked = false, 
            bool ShowAchievementsCompactUnlocked = false, bool ShowProgressBar = false)
        {
            InitializeComponent();

            _IsCustom = IsCustom;
            _ShowAchievementsGraphic = ShowAchievementsGraphic;
            _ShowAchievementsList = ShowAchievementsList;
            _ShowAchievementsCompactLocked = ShowAchievementsCompactLocked;
            _ShowAchievementsCompactUnlocked = ShowAchievementsCompactUnlocked;
            _ShowProgressBar = ShowProgressBar;

#if DEBUG
            logger.Debug($"SuccessStory - ScDescriptionIntegration() - _IsCustom: {_IsCustom}");
#endif
            SetScData(settings, achievementsDatabase, SelectedGameAchievements);
        }

        public void SetScData(SuccessStorySettings settings, AchievementsDatabase achievementsDatabase, GameAchievements SelectedGameAchievements)
        {
            if (!settings.IntegrationShowTitle || _IsCustom)
            {
                PART_Title.Visibility = Visibility.Collapsed;
                PART_Separator.Visibility = Visibility.Collapsed;
            }

#if DEBUG
            logger.Debug($"SuccessStory - _IsCustom: {_IsCustom} - _ShowAchievementsGraphic: {_ShowAchievementsGraphic} - _ShowAchievementsList: {_ShowAchievementsList} - _ShowAchievementsCompactLocked: {_ShowAchievementsCompactLocked} - _ShowAchievementsCompactUnlocked: {_ShowAchievementsCompactUnlocked} - _ShowProgressBar: {_ShowProgressBar}");
#endif

            bool Show = true;

            if (SelectedGameAchievements == null || !SelectedGameAchievements.HaveAchivements)
            {
                return;
            }

            PART_SuccessStory_ProgressBar.Visibility = Visibility.Collapsed;
            if (settings.IntegrationShowProgressBar)
            {
                Show = true;
                if (_IsCustom && !_ShowProgressBar)
                {
                    Show = false;
                }

#if DEBUG
                logger.Debug($"SuccessStory - PART_SuccessStory_ProgressBar - Show: {Show} - SelectedGameAchievements: {JsonConvert.SerializeObject(SelectedGameAchievements)}");
#endif
                if (Show)
                {
                    PART_SuccessStory_ProgressBar.Visibility = Visibility.Visible;

                    if (successStoryAchievementsProgressBar == null)
                    {
                        successStoryAchievementsProgressBar = new SuccessStoryAchievementsProgressBar(SelectedGameAchievements.Unlocked, SelectedGameAchievements.Total, settings.IntegrationShowProgressBarPercent, settings.IntegrationShowProgressBarIndicator, _IsCustom);

                        if (!_IsCustom)
                        {
                            PART_SuccessStory_ProgressBar.Height = 40;
                            PART_SuccessStory_ProgressBar.Margin = new Thickness(0, 5, 0, 5);
                        }

                        PART_SuccessStory_ProgressBar.Children.Add(successStoryAchievementsProgressBar);
                    }

                    successStoryAchievementsProgressBar.SetScData(SelectedGameAchievements.Unlocked, SelectedGameAchievements.Total, settings.IntegrationShowProgressBarPercent, settings.IntegrationShowProgressBarIndicator, _IsCustom);
                }
            }

            PART_SuccessStory_Graphic.Visibility = Visibility.Collapsed;
            if (settings.IntegrationShowGraphic)
            {
                Show = true;
                if (_IsCustom && !_ShowAchievementsGraphic)
                {
                    Show = false;
                }

#if DEBUG
                logger.Debug($"SuccessStory - PART_SuccessStory_Graphic - Show: {Show} - SelectedGameAchievements: {JsonConvert.SerializeObject(SelectedGameAchievements)}");
#endif
                if (Show)
                {
                    PART_SuccessStory_Graphic.Visibility = Visibility.Visible;

                    AchievementsGraphicsDataCount GraphicsData = null;
                    if (!settings.GraphicAllUnlockedByDay)
                    {
                        GraphicsData = achievementsDatabase.GetCountByMonth(SuccessStory.GameSelected.Id, (settings.IntegrationGraphicOptionsCountAbscissa - 1));
                    }
                    else
                    {
                        GraphicsData = achievementsDatabase.GetCountByDay(SuccessStory.GameSelected.Id, (settings.IntegrationGraphicOptionsCountAbscissa - 1));
                    }
                    string[] StatsGraphicsAchievementsLabels = GraphicsData.Labels;
                    SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection();
                    StatsGraphicAchievementsSeries.Add(new LineSeries
                    {
                        Title = string.Empty,
                        Values = GraphicsData.Series
                    });

                    if (successStoryAchievementsGraphics == null)
                    {
                        successStoryAchievementsGraphics = new SuccessStoryAchievementsGraphics(StatsGraphicAchievementsSeries, StatsGraphicsAchievementsLabels, settings);

                        if (!_IsCustom)
                        {
                            PART_SuccessStory_Graphic.Height = settings.IntegrationShowGraphicHeight;
                            PART_SuccessStory_Graphic.Margin = new Thickness(0, 5, 0, 5);
                        }

                        PART_SuccessStory_Graphic.Children.Add(successStoryAchievementsGraphics);
                    }

                    successStoryAchievementsGraphics.SetScData(StatsGraphicAchievementsSeries, StatsGraphicsAchievementsLabels, settings);
                }
            }

            PART_SuccessStory_List.Visibility = Visibility.Collapsed;
            if (settings.IntegrationShowAchievements)
            {
                Show = true;
                if (_IsCustom && !_ShowAchievementsList)
                {
                    Show = false;
                }

#if DEBUG
                logger.Debug($"SuccessStory - PART_SuccessStory_List - Show: {Show} - SelectedGameAchievements: {JsonConvert.SerializeObject(SelectedGameAchievements)}");
#endif
                if (Show)
                {
                    PART_SuccessStory_List.Visibility = Visibility.Visible;

                    if (successStoryAchievementsList == null)
                    {
                        successStoryAchievementsList = new SuccessStoryAchievementsList(SelectedGameAchievements.Achievements, _IsCustom, settings.EnableRaretyIndicator);

                        if (!_IsCustom)
                        {
                            PART_SuccessStory_List.Height = settings.IntegrationShowAchievementsHeight;
                            PART_SuccessStory_List.Margin = new Thickness(0, 5, 0, 5);
                        }

                        PART_SuccessStory_List.Children.Add(successStoryAchievementsList);
                    }

                    successStoryAchievementsList.SetScData(SelectedGameAchievements.Achievements, _IsCustom, settings.EnableRaretyIndicator);
                }
            }

            PART_SuccessStory_Compact_Locked.Visibility = Visibility.Collapsed;
            if (settings.IntegrationShowAchievementsCompactLocked)
            {
                Show = true;
                if (_IsCustom && !_ShowAchievementsCompactLocked)
                {
                    Show = false;
                }

#if DEBUG
                logger.Debug($"SuccessStory - PART_SuccessStory_Compact_Locked - Show: {Show} - SelectedGameAchievements: {JsonConvert.SerializeObject(SelectedGameAchievements)}");
#endif
                if (Show)
                {
                    PART_SuccessStory_Compact_Locked.Visibility = Visibility.Visible;

                    if (successStoryAchievementsCompact_Locked == null)
                    {
                        successStoryAchievementsCompact_Locked = new SuccessStoryAchievementsCompact(SelectedGameAchievements.Achievements, false, settings.EnableRaretyIndicator);

                        if (!_IsCustom)
                        {
                            PART_SuccessStory_Compact_Locked.Margin = new Thickness(0, 5, 0, 5);
                            PART_SuccessStory_Compact_Locked.Height = successStoryAchievementsCompact_Locked.Height;
                        }

                        PART_SuccessStory_Compact_Locked.Children.Add(successStoryAchievementsCompact_Locked);
                    }

                    successStoryAchievementsCompact_Locked.SetScData(SelectedGameAchievements.Achievements, false, settings.EnableRaretyIndicator);
                }
            }

            PART_SuccessStory_Compact_Unlocked.Visibility = Visibility.Collapsed;
            if (settings.IntegrationShowAchievementsCompactUnlocked)
            {
                Show = true;
                if (_IsCustom && !_ShowAchievementsCompactUnlocked)
                {
                    Show = false;
                }

#if DEBUG
                logger.Debug($"SuccessStory - PART_SuccessStory_Compact_Unlocked - Show: {Show} - SelectedGameAchievements: {JsonConvert.SerializeObject(SelectedGameAchievements)}");
#endif
                if (Show)
                {
                    PART_SuccessStory_Compact_Unlocked.Visibility = Visibility.Visible;

                    if (successStoryAchievementsCompact_Unlocked == null)
                    {
                        successStoryAchievementsCompact_Unlocked = new SuccessStoryAchievementsCompact(SelectedGameAchievements.Achievements, true, settings.EnableRaretyIndicator);

                        if (!_IsCustom)
                        {
                            PART_SuccessStory_Compact_Unlocked.Margin = new Thickness(0, 5, 0, 5);
                            PART_SuccessStory_Compact_Unlocked.Height = successStoryAchievementsCompact_Unlocked.Height;
                        }

                        PART_SuccessStory_Compact_Unlocked.Children.Add(successStoryAchievementsCompact_Unlocked);
                    }

                    successStoryAchievementsCompact_Unlocked.SetScData(SelectedGameAchievements.Achievements, true, settings.EnableRaretyIndicator);
                }
            }
        }
    }
}
