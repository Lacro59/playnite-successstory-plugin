using CommonPluginsShared.Converters;
using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
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
        private ControlDataContext ControlDataContext = new ControlDataContext();


        public SuccessStoryOneGameView(Game GameContext)
        {
            InitializeComponent();
            this.DataContext = ControlDataContext;


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
                var AchUltraRare = gameAchievements.UltraRare;

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
                PART_AchUltraRare.Content = AchUltraRare.UnLocked;            

                PART_AchCommonTotal.Content = AchCommon.Total;
                PART_AchNoCommonTotal.Content = AchNoCommon.Total;
                PART_AchRareTotal.Content = AchRare.Total;
                PART_AchUltraRareTotal.Content = AchUltraRare.Total;

                var converter = new LocalDateTimeConverter();
                PART_FirstUnlock.Text = (string)converter.Convert(gameAchievements.Items.Select(x => x.DateWhenUnlocked).Min(), null, null, null);
                PART_LastUnlock.Text = (string)converter.Convert(gameAchievements.Items.Select(x => x.DateWhenUnlocked).Max(), null, null, null);
            }


            ControlDataContext.GameContext = GameContext;
            ControlDataContext.Settings = PluginDatabase.PluginSettings.Settings;
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


    public class ControlDataContext : ObservableObject
    {
        private Game _GameContext { get; set; }
        public Game GameContext
        {
            get => _GameContext;
            set
            {
                if (value?.Equals(_GameContext) == true)
                {
                    return;
                }

                _GameContext = value;
                OnPropertyChanged();
            }
        }

        private SuccessStorySettings _Settings { get; set; }
        public SuccessStorySettings Settings
        {
            get => _Settings;
            set
            {
                if (value?.Equals(_Settings) == true)
                {
                    return;
                }

                _Settings = value;
                OnPropertyChanged();
            }
        }
    }
}
