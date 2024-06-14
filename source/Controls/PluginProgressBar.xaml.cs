using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SuccessStory.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginProgressBar.xaml
    /// </summary>
    public partial class PluginProgressBar : PluginUserControlExtend
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;
        internal override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginProgressBarDataContext ControlDataContext = new PluginProgressBarDataContext();
        internal override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginProgressBarDataContext)controlDataContext;
        }


        public PluginProgressBar()
        {
            InitializeComponent();
            DataContext = ControlDataContext;

            _ = Task.Run(() =>
            {
                // Wait extension database are loaded
                _ = System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                _ = Dispatcher?.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;

                    // Apply settings
                    PluginSettings_PropertyChanged(null, null);
                });
            });
        }


        private void PluginUserControlExtend_Loaded(object sender, RoutedEventArgs e)
        {
            ContentControl elParent = UI.FindParent<ContentControl>((FrameworkElement)sender);
            if (elParent != null && (double.IsNaN(elParent.Height) || elParent.Height == 0))
            {
                elParent.Height = 40;
            }
        }


        public override void SetDefaultDataContext()
        {
            bool IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationProgressBar;
            if (IgnoreSettings)
            {
                IsActivated = true;
            }

            ControlDataContext.IsActivated = IsActivated;
            ControlDataContext.IntegrationShowProgressBarIndicator = PluginDatabase.PluginSettings.Settings.EnableIntegrationProgressBarIndicator;
            ControlDataContext.IntegrationShowProgressBarPercent = PluginDatabase.PluginSettings.Settings.EnableIntegrationProgressBarPercent;

            ControlDataContext.Percent = 0;
            ControlDataContext.Value = 0;
            ControlDataContext.Maximum = 0;
            ControlDataContext.LabelContent = string.Empty;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameAchievements gameAchievements = (GameAchievements)PluginGameData;

            ControlDataContext.Percent = gameAchievements.Progression;
            ControlDataContext.Value = gameAchievements.Unlocked;
            ControlDataContext.Maximum = gameAchievements.Total;

            ControlDataContext.LabelContent = gameAchievements.Unlocked + "/" + gameAchievements.Total;
        }
    }


    public class PluginProgressBarDataContext : ObservableObject, IDataContext
    {
        private bool isActivated;
        public bool IsActivated { get => isActivated; set => SetValue(ref isActivated, value); }

        private bool integrationShowProgressBarIndicator;
        public bool IntegrationShowProgressBarIndicator { get => integrationShowProgressBarIndicator; set => SetValue(ref integrationShowProgressBarIndicator, value); }

        private bool integrationShowProgressBarPercent;
        public bool IntegrationShowProgressBarPercent { get => integrationShowProgressBarPercent; set => SetValue(ref integrationShowProgressBarPercent, value); }

        private double percent;
        public double Percent { get => percent; set => SetValue(ref percent, value); }

        private double value;
        public double Value { get => value; set => SetValue(ref this.value, value); }

        private double maximum;
        public double Maximum { get => maximum; set => SetValue(ref maximum, value); }

        private string labelContent;
        public string LabelContent { get => labelContent; set => SetValue(ref labelContent, value); }
    }
}
