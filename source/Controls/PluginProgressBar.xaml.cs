using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;
        internal override IPluginDatabase _PluginDatabase
        {
            get
            {
                return PluginDatabase;
            }
            set
            {
                PluginDatabase = (SuccessStoryDatabase)_PluginDatabase;
            }
        }

        private PluginProgressBarDataContext ControlDataContext = new PluginProgressBarDataContext();
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginProgressBarDataContext)_ControlDataContext;
            }
        }


        public PluginProgressBar()
        {
            InitializeComponent();
            this.DataContext = ControlDataContext;

            Task.Run(() =>
            {
                // Wait extension database are loaded
                System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                this.Dispatcher?.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    PluginDatabase.PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;

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
        private bool _IsActivated;
        public bool IsActivated { get => _IsActivated; set => SetValue(ref _IsActivated, value); }

        private bool _IntegrationShowProgressBarIndicator;
        public bool IntegrationShowProgressBarIndicator { get => _IntegrationShowProgressBarIndicator; set => SetValue(ref _IntegrationShowProgressBarIndicator, value); }

        private bool _IntegrationShowProgressBarPercent;
        public bool IntegrationShowProgressBarPercent { get => _IntegrationShowProgressBarPercent; set => SetValue(ref _IntegrationShowProgressBarPercent, value); }

        private double _Percent;
        public double Percent { get => _Percent; set => SetValue(ref _Percent, value); }

        private double _Value;
        public double Value { get => _Value; set => SetValue(ref _Value, value); }

        private double _Maximum;
        public double Maximum { get => _Maximum; set => SetValue(ref _Maximum, value); }

        private string _LabelContent;
        public string LabelContent { get => _LabelContent; set => SetValue(ref _LabelContent, value); }
    }
}
