using CommonPluginsShared.Converters;
using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace SuccessStory.Views
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryOneGameView.xaml
    /// </summary>
    public partial class SuccessStoryOneGameView : UserControl
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;
        private ControlDataContext ControlDataContext { get; set; } = new ControlDataContext();

        public string UiAdd => "\uec3e";
        public string UiRemove => "\uec7e";


        public SuccessStoryOneGameView(Game GameContext)
        {
            InitializeComponent();
            DataContext = ControlDataContext;


            // Cover
            if (!GameContext.CoverImage.IsNullOrEmpty())
            {
                ControlDataContext.CoverImage = API.Instance.Database.GetFullFilePath(GameContext.CoverImage);
            }

            GameAchievements gameAchievements = PluginDatabase.Get(GameContext, true);
            if (gameAchievements.SourcesLink != null)
            {
                ControlDataContext.SourceLabel = gameAchievements.SourcesLink.GameName + " (" + gameAchievements.SourcesLink.Name + ")";
                ControlDataContext.SourceLink = gameAchievements.SourcesLink.Url;
            }
            else
            {
                ControlDataContext.SourceLabel = string.Empty;
                ControlDataContext.SourceLink = string.Empty;
            }


            if (gameAchievements.HasData)
            {
                ControlDataContext.AchCommon = gameAchievements.Common;
                ControlDataContext.AchNoCommon = gameAchievements.NoCommon;
                ControlDataContext.AchRare = gameAchievements.Rare;
                ControlDataContext.AchUltraRare = gameAchievements.UltraRare;

                ControlDataContext.TotalGamerScore = gameAchievements.TotalGamerScore;

                ControlDataContext.EstimateTime = gameAchievements.EstimateTime.EstimateTime;

                LocalDateTimeConverter converter = new LocalDateTimeConverter();
                ControlDataContext.FirstUnlock = (string)converter.Convert(gameAchievements.Items.Select(x => x.DateWhenUnlocked).Min(), null, null, CultureInfo.CurrentCulture);
                ControlDataContext.LastUnlock = (string)converter.Convert(gameAchievements.Items.Select(x => x.DateWhenUnlocked).Max(), null, null, CultureInfo.CurrentCulture);
            }


            ControlDataContext.GameContext = GameContext;
            ControlDataContext.Settings = PluginDatabase.PluginSettings.Settings;

            ControlDataContext.HasDataStats = gameAchievements.HasDataStats;
        }
    }


    public class ControlDataContext : ObservableObject
    {
        private SuccessStorySettings settings;
        public SuccessStorySettings Settings { get => settings; set => SetValue(ref settings, value); }


        private string uiStatsBt = "\uec3e";
        public string UiStatsBt { get => uiStatsBt; set => SetValue(ref uiStatsBt, value); }

        private bool hasDataStats = true;
        public bool HasDataStats { get => hasDataStats; set => SetValue(ref hasDataStats, value); }


        private Game gameContext;
        public Game GameContext { get => gameContext; set => SetValue(ref gameContext, value); }

        private string coverImage;
        public string CoverImage { get => coverImage; set => SetValue(ref coverImage, value); }

        private string sourceLabel = "Source Label";
        public string SourceLabel { get => sourceLabel; set => SetValue(ref sourceLabel, value); }

        private string sourceLink;
        public string SourceLink { get => sourceLink; set => SetValue(ref sourceLink, value); }

        private AchRaretyStats achCommon = new AchRaretyStats();
        public AchRaretyStats AchCommon { get => achCommon; set => SetValue(ref achCommon, value); }

        private AchRaretyStats achNoCommon = new AchRaretyStats();
        public AchRaretyStats AchNoCommon { get => achNoCommon; set => SetValue(ref achNoCommon, value); }

        private AchRaretyStats achRare = new AchRaretyStats();
        public AchRaretyStats AchRare { get => achRare; set => SetValue(ref achRare, value); }

        private AchRaretyStats achUltraRare = new AchRaretyStats();
        public AchRaretyStats AchUltraRare { get => achUltraRare; set => SetValue(ref achUltraRare, value); }

        private float totalGamerScore = 0;
        public float TotalGamerScore { get => totalGamerScore; set => SetValue(ref totalGamerScore, value); }

        private string firstUnlock = "xxxx-xx-xx xx:xx:xx";
        public string FirstUnlock { get => firstUnlock; set => SetValue(ref firstUnlock, value); }

        private string lastUnlock = "xxxx-xx-xx xx:xx:xx";
        public string LastUnlock { get => lastUnlock; set => SetValue(ref lastUnlock, value); }

        private string estimateTime = "20-30h";
        public string EstimateTime { get => estimateTime; set => SetValue(ref estimateTime, value); }
    }
}
