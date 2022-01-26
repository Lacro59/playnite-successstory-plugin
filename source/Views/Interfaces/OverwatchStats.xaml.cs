using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace SuccessStory.Views.Interfaces
{
    /// <summary>
    /// Logique d'interaction pour OverwatchStats.xaml
    /// </summary>
    public partial class OverwatchStats : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        private GameAchievements gameAchievements;

        private List<GameStats> gameStats = new List<GameStats>();
        private List<GameStats> gameStatsTopHero = new List<GameStats>();
        private List<GameStats> gameStatsCareer = new List<GameStats>();

        public enum OverWatchMode
        {
            QuickPlay, CompetitivePlay
        }


        #region Properties
        public OverWatchMode DataMode
        {
            get { return (OverWatchMode)GetValue(DataModePropertyProperty); }
            set { SetValue(DataModePropertyProperty, value); }
        }

        public static readonly DependencyProperty DataModePropertyProperty = DependencyProperty.Register(
            nameof(DataMode),
            typeof(OverWatchMode),
            typeof(OverwatchStats),
            new FrameworkPropertyMetadata(OverWatchMode.QuickPlay, PropertyPropertyChangedCallback));


        public Game GameContext
        {
            get { return (Game)GetValue(GameContextProperty); }
            set { SetValue(GameContextProperty, value); }
        }

        public static readonly DependencyProperty GameContextProperty = DependencyProperty.Register(
            nameof(GameContext),
            typeof(Game),
            typeof(OverwatchStats),
            new FrameworkPropertyMetadata(null, PropertyPropertyChangedCallback));


        private static void PropertyPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is OverwatchStats obj && e.NewValue != e.OldValue)
            {
                obj.SetMode();
            }
        }
        #endregion


        public OverwatchStats()
        {
            InitializeComponent();
        }


        public void SetMode()
        {
            if (GameContext == null)
            {
                return;
            }

            try
            {
                gameStats = new List<GameStats>();
                gameAchievements = PluginDatabase.Get(GameContext, true);

                switch (DataMode)
                {
                    case OverWatchMode.QuickPlay:
                        gameStats = Serialization.GetClone(gameAchievements.ItemsStats.Where(x => x.Mode?.IsEqual("quickplay") ?? false).ToList());
                        break;

                    case OverWatchMode.CompetitivePlay:
                        gameStats = Serialization.GetClone(gameAchievements.ItemsStats.Where(x => x.Mode?.IsEqual("competitive") ?? false).ToList());
                        break;
                }


                // Player info
                int Endorsement = (int)gameAchievements.ItemsStats.Find(x => x.Name == "PlayerEndorsement").Value;
                string EndorsementFrame = string.Empty;
                switch (Endorsement)
                {
                    case 1:
                        EndorsementFrame = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "Overwatch", "Endorsement_1.png");
                        break;
                    case 2:
                        EndorsementFrame = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "Overwatch", "Endorsement_2.png");
                        break;
                    case 3:
                        EndorsementFrame = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "Overwatch", "Endorsement_3.png");
                        break;
                    case 4:
                        EndorsementFrame = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "Overwatch", "Endorsement_4.png");
                        break;
                    case 5:
                        EndorsementFrame = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "Overwatch", "Endorsement_5.png");
                        break;
                }

                Player player = new Player
                {
                    Name = gameAchievements.ItemsStats.Find(x => x.Name == "PlayerName").DisplayName,
                    Portrait = gameAchievements.ItemsStats.Find(x => x.Name == "PlayerPortrait").ImageUrl,

                    LevelFrame = gameAchievements.ItemsStats.Find(x => x.Name == "PlayerLevelFrame").ImageUrl,
                    Level = (int)gameAchievements.ItemsStats.Find(x => x.Name == "PlayerLevel").Value,
                    LevelRank = gameAchievements.ItemsStats.Find(x => x.Name == "PlayerLevelRank").ImageUrl,

                    EndorsementFrame = EndorsementFrame,
                    Endorsement = Endorsement,

                    GamesWon = string.Format(resources.GetString("LOCSsOverwatchGamesWon"), (int)gameAchievements.ItemsStats.Find(x => x.Name == "MatchWin").Value)
                };


                // Top hero
                gameStatsTopHero = Serialization.GetClone(gameStats.Where(x => x.Category == "TopHero")).ToList();
                List<string> ComboBoxTopHero = gameStatsTopHero.Select(x => x.CareerType).Distinct().ToList();


                // Career stats
                gameStatsCareer = Serialization.GetClone(gameStats.Where(x => x.Category == "CarrerStats")).ToList();
                List<string> ComboBoxCareer = gameStatsCareer.Select(x => x.CareerType).Distinct().ToList();


                // Achievements
                List<string> ComboBoxAchievements = gameAchievements.Items.Select(x => x.Category).Distinct().ToList();


                this.DataContext = null;
                this.DataContext = new
                {
                    Player = player,
                    ComboBoxTopHero,
                    ComboBoxCareer,
                    ComboBoxAchievements
                };


                PART_TopHeroCategory.SelectedIndex = 0;
                PART_CareerCategory.SelectedIndex = 0;
                PART_ComboBoxAchievements.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }


        private void PART_TopHeroCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<GameStats> StatsTopHero = Serialization.GetClone(gameStatsTopHero.Where(x => x.CareerType == (string)PART_TopHeroCategory.SelectedItem)).ToList();
            List<TopHero> TopHeroCategoryData = new List<TopHero>();

            double HeroMaxValue = 0;

            foreach (var element in StatsTopHero)
            {
                TopHeroCategoryData.Add(new TopHero
                {
                    HeroImage = element.ImageUrl,
                    HeroValue = (element.Time == default(TimeSpan)) ? element.Value : element.Time.TotalSeconds,
                    HeroValueString = (element.Time == default(TimeSpan)) ? (!element.DisplayName.IsNullOrEmpty()) ? element.DisplayName : element.Value.ToString() : element.Time.ToString(),
                    HeroName = element.Name,
                    HeroColor = (SolidColorBrush)new BrushConverter().ConvertFrom(element.Color)
                });

                if (TopHeroCategoryData.Last().HeroValue > HeroMaxValue)
                {
                    HeroMaxValue = TopHeroCategoryData.Last().HeroValue;
                }
            }

            TopHeroCategoryData.ForEach(x => x.HeroMaxValue = HeroMaxValue);
            TopHeroCategoryData.Sort((x, y) => y.HeroValue.CompareTo(x.HeroValue));

            PART_ListBoxTopHero.ItemsSource = null;
            PART_ListBoxTopHero.ItemsSource = TopHeroCategoryData;
        }

        private void PART_CareerCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<GameStats> StatsCareer = Serialization.GetClone(gameStatsCareer.Where(x => x.CareerType == (string)PART_CareerCategory.SelectedItem)).ToList();
            List<string> ComboBoxCareer = StatsCareer.Select(x => x.SubCategory).Distinct().ToList();
            List<List<Career>> Careerdata = new List<List<Career>>();

            foreach (var SubCategory in ComboBoxCareer)
            {
                List<GameStats> CareerCategoryData = Serialization.GetClone(StatsCareer.Where(x => x.SubCategory == SubCategory)).ToList();
                List<Career> Careers = new List<Career>();
                foreach (var element in CareerCategoryData)
                {
                    Careers.Add(new Career
                    {
                        Title = SubCategory,
                        CareerValue = (element.Time == default(TimeSpan)) ? element.Value : element.Time.TotalSeconds,
                        CareerValueString = (element.Time == default(TimeSpan)) ? (!element.DisplayName.IsNullOrEmpty()) ? element.DisplayName : element.Value.ToString() : element.Time.ToString(),
                        CareerName = element.Name,
                    });
                }

                Careerdata.Add(Careers);
            }
            
            PART_CareerStatsData.ItemsSource = null;
            PART_CareerStatsData.Items.Clear();
            PART_CareerStatsData.ItemsSource = Careerdata;
        }

        private void PART_ComboBoxAchievements_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<Achievements> achievements = gameAchievements.Items.Where(x => x.Category == (string)PART_ComboBoxAchievements.SelectedItem).ToList();

            PART_AchievementsData.ItemsSource = null;
            PART_AchievementsData.Items.Clear();
            PART_AchievementsData.ItemsSource = achievements;
        }


        private void TextBlock_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            string Text = ((TextBlock)sender).Text;
            TextBlock textBlock = (TextBlock)sender;

            Typeface typeface = new Typeface(
                textBlock.FontFamily,
                textBlock.FontStyle,
                textBlock.FontWeight,
                textBlock.FontStretch);

            FormattedText formattedText = new FormattedText(
                textBlock.Text,
                System.Threading.Thread.CurrentThread.CurrentCulture,
                textBlock.FlowDirection,
                typeface,
                textBlock.FontSize,
                textBlock.Foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            if (formattedText.Width > textBlock.DesiredSize.Width)
            {
                ((ToolTip)((TextBlock)sender).ToolTip).Visibility = Visibility.Visible;
            }
            else
            {
                ((ToolTip)((TextBlock)sender).ToolTip).Visibility = Visibility.Hidden;
            }
        }

    }


    public class Player
    {
        public string Name { get; set; }
        public string Portrait { get; set; }

        public string LevelFrame { get; set; }
        public int Level { get; set; }
        public string LevelRank { get; set; }

        public string EndorsementFrame { get; set; }
        public int Endorsement { get; set; }

        public string GamesWon { get; set; }
    }

    public class TopHero
    {
        public string HeroImage { get; set; }
        public double HeroValue { get; set; }
        public string HeroValueString { get; set; }
        public double HeroMaxValue { get; set; }
        public string HeroName { get; set; }
        public SolidColorBrush HeroColor { get; set; }
    }

    public class Career
    {
        public string Title { get; set; }
        public double CareerValue { get; set; }
        public string CareerValueString { get; set; }
        public string CareerName { get; set; }
    }
}
