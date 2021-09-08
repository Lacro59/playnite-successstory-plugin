using CommonPluginsPlaynite.Controls;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

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

        private PluginProgressBarDataContext ControlDataContext;
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

            Task.Run(() =>
            {
                // Wait extension database are loaded
                System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                this.Dispatcher.BeginInvoke((Action)delegate
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


        public override void SetDefaultDataContext()
        {
            bool IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationProgressBar;
            if (IgnoreSettings)
            {
                IsActivated = true;
            }

            ControlDataContext = new PluginProgressBarDataContext
            {
                IsActivated = IsActivated,
                IntegrationShowProgressBarIndicator = PluginDatabase.PluginSettings.Settings.EnableIntegrationProgressBarIndicator,
                IntegrationShowProgressBarPercent = PluginDatabase.PluginSettings.Settings.EnableIntegrationProgressBarPercent,

                Percent = 0,
                Value = 0,
                Maximum = 0,
                LabelContent = string.Empty
            };
        }


        public override Task<bool> SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            return Task.Run(() =>
            {
                GameAchievements gameAchievements = (GameAchievements)PluginGameData;

                ControlDataContext.Percent = gameAchievements.Progression;
                ControlDataContext.Value = gameAchievements.Unlocked;
                ControlDataContext.Maximum = gameAchievements.Total;

                ControlDataContext.LabelContent = gameAchievements.Unlocked + "/" + gameAchievements.Total;

                this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    this.DataContext = ControlDataContext;
                }));

                return true;
            });
        }
    }


    public class PluginProgressBarDataContext : IDataContext
    {
        public bool IsActivated { get; set; }
        public bool IntegrationShowProgressBarIndicator { get; set; }
        public bool IntegrationShowProgressBarPercent { get; set; }

        public double Percent { get; set; }
        public double Value { get; set; }
        public double Maximum { get; set; }
        public string LabelContent { get; set; }
    }
}
