using CommonPluginsShared.Converters;
using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

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


        public SuccessStoryOneGameView(Game GameContext)
        {
            InitializeComponent();

            // Cover
            if (!GameContext.CoverImage.IsNullOrEmpty())
            {
                string CoverImage = PluginDatabase.PlayniteApi.Database.GetFullFilePath(GameContext.CoverImage);
                PART_ImageCover.Source = BitmapExtensions.BitmapFromFile(CoverImage);
            }

            GameAchievements gameAchievements = PluginDatabase.Get(GameContext, true);
            if (gameAchievements.SourcesLink != null)
            {
                PART_SourceLabel.Text = gameAchievements.SourcesLink.GameName + " (" + gameAchievements.SourcesLink.Name + ")";
                PART_SourceLink.Tag = gameAchievements.SourcesLink.Url;
            }
            else
            {
                PART_SourceLabel.Text = string.Empty;
                PART_SourceLabel.Tag = string.Empty;
            }


            if (gameAchievements.HasData)
            {
                var AchCommon = gameAchievements.Common;
                var AchNoCommon = gameAchievements.NoCommon;
                var AchRare = gameAchievements.Rare;

                if (gameAchievements.EstimateTime == null || gameAchievements.EstimateTime.EstimateTimeMin == 0)
                {
                    PART_TimeToUnlockContener.Visibility = Visibility.Collapsed;
                }
                else
                { 
                    PART_TimeToUnlock.Text = gameAchievements.EstimateTime.EstimateTime;
                }

                PART_AchCommon.Content = AchCommon.UnLocked;
                PART_AchNoCommon.Content = AchNoCommon.UnLocked;
                PART_AchRare.Content = AchRare.UnLocked;

                PART_AchCommonTotal.Content = AchCommon.Total;
                PART_AchNoCommonTotal.Content = AchNoCommon.Total;
                PART_AchRareTotal.Content = AchRare.Total;


                var converter = new LocalDateTimeConverter();
                PART_FirstUnlock.Text = (string)converter.Convert(gameAchievements.Items.Select(x => x.DateWhenUnlocked).Min(), null, null, null);
                PART_LastUnlock.Text = (string)converter.Convert(gameAchievements.Items.Select(x => x.DateWhenUnlocked).Max(), null, null, null);
            }


            this.DataContext = new
            {
                GameContext
            };
        }


        private void PART_SourceLink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (((Hyperlink)sender).Tag is string)
            {
                if (!((string)((Hyperlink)sender).Tag).IsNullOrEmpty())
                {
                    Process.Start((string)((Hyperlink)sender).Tag);
                }
            }
        }
    }
}
