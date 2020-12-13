using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using SuccessStory.Views.Interface;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace SuccessStory.Views
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryOneGameView.xaml
    /// </summary>
    public partial class SuccessStoryOneGameView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public SuccessStoryOneGameView(Game GameSelected)
        {
            InitializeComponent();

            LoadData(GameSelected);
        }

        private void LoadData(Game GameSelected)
        {
            Guid GameId = GameSelected.Id;
            GameAchievements successStories = PluginDatabase.Get(GameSelected);
            List<Achievements> ListAchievements = successStories.Items;
            List<GameStats> ListGameStats = successStories.ItemsStats;


            // List Achievements
            SuccessStoryAchievementsList successStoryAchievementsList = new SuccessStoryAchievementsList();
            successStoryAchievementsList.SetScData(successStories);
            PART_Achievements_List.Children.Add(successStoryAchievementsList);


            // Chart achievements
            int limit = 0;
            if (!PluginDatabase.PluginSettings.GraphicAllUnlockedByDay)
            {
                PART_ChartTitle.Content = resources.GetString("LOCSuccessStoryGraphicTitle");
                limit = 20;
            }
            else
            {
                PART_ChartTitle.Content = resources.GetString("LOCSuccessStoryGraphicTitleDay");
                limit = 16;
            }

            PluginDatabase.PluginSettings.IgnoreSettings = true;
            SuccessStoryAchievementsGraphics successStoryAchievementsGraphics = new SuccessStoryAchievementsGraphics();
            successStoryAchievementsGraphics.SetScData(GameId, limit);
            PART_Achievements_Graphics.Children.Add(successStoryAchievementsGraphics);


            // User stats
            SuccessStoryUserStats successStoryUserStats = new SuccessStoryUserStats();
            successStoryUserStats.SetScData(ListGameStats);
            PART_ScUserStats.Children.Add(successStoryUserStats);
        }
    }
}
