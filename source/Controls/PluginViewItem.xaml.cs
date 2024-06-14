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

namespace SuccessStory.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginViewItem.xaml
    /// </summary>
    public partial class PluginViewItem : PluginUserControlExtend
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;
        internal override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginViewItemDataContext ControlDataContext = new PluginViewItemDataContext();
        internal override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginViewItemDataContext)controlDataContext;
        }


        public PluginViewItem()
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


        public override void SetDefaultDataContext()
        {
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationViewItem;
            ControlDataContext.IntegrationViewItemWithProgressBar = PluginDatabase.PluginSettings.Settings.IntegrationViewItemWithProgressBar;

            ControlDataContext.LabelContent = string.Empty;
            ControlDataContext.Unlocked = 0;
            ControlDataContext.Total = 0;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameAchievements gameAchievements = (GameAchievements)PluginGameData;

            ControlDataContext.Unlocked = gameAchievements.Unlocked;
            ControlDataContext.Total = gameAchievements.Total;
            ControlDataContext.LabelContent = gameAchievements.Unlocked + "/" + gameAchievements.Total;
        }
    }


    public class PluginViewItemDataContext : ObservableObject, IDataContext
    {
        private bool isActivated;
        public bool IsActivated { get => isActivated; set => SetValue(ref isActivated, value); }

        private bool integrationViewItemWithProgressBar;
        public bool IntegrationViewItemWithProgressBar { get => integrationViewItemWithProgressBar; set => SetValue(ref integrationViewItemWithProgressBar, value); }

        private string labelContent;
        public string LabelContent { get => labelContent; set => SetValue(ref labelContent, value); }

        private double unlocked;
        public double Unlocked { get => unlocked; set => SetValue(ref unlocked, value); }

        private double total;
        public double Total { get => total; set => SetValue(ref total, value); }
    }
}
