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
    /// Logique d'interaction pour PluginViewItem.xaml
    /// </summary>
    public partial class PluginViewItem : PluginUserControlExtend
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

        private PluginViewItemDataContext ControlDataContext;
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginViewItemDataContext)_ControlDataContext;
            }
        }


        public PluginViewItem()
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
            ControlDataContext = new PluginViewItemDataContext
            {
                IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationViewItem,
                IntegrationViewItemWithProgressBar = PluginDatabase.PluginSettings.Settings.IntegrationViewItemWithProgressBar,

                LabelContent = string.Empty,
                Unlocked = 0,
                Total = 0
            };
        }


        public override Task<bool> SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            return Task.Run(() =>
            {
                GameAchievements gameAchievements = (GameAchievements)PluginGameData;

                ControlDataContext.Unlocked = gameAchievements.Unlocked;
                ControlDataContext.Total = gameAchievements.Total;
                ControlDataContext.LabelContent = gameAchievements.Unlocked + "/" + gameAchievements.Total;

                this.Dispatcher.BeginInvoke(DispatcherPriority.Render, new ThreadStart(delegate
                {
                    MustDisplay = true;
                    this.DataContext = ControlDataContext;
                }));

                return true;
            });
        }
    }


    public class PluginViewItemDataContext : IDataContext
    {
        public bool IsActivated { get; set; }
        public bool IntegrationViewItemWithProgressBar { get; set; }

        public string LabelContent { get; set; }
        public double Unlocked { get; set; }
        public double Total { get; set; }
    }
}
