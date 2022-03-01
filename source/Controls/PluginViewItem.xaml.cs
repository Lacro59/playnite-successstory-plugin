using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
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
        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;
        internal override IPluginDatabase _PluginDatabase
        {
            get => PluginDatabase;
            set => PluginDatabase = (SuccessStoryDatabase)_PluginDatabase;
        }

        private PluginViewItemDataContext ControlDataContext = new PluginViewItemDataContext();
        internal override IDataContext _ControlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginViewItemDataContext)_ControlDataContext;
        }


        public PluginViewItem()
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
        private bool _IsActivated;
        public bool IsActivated { get => _IsActivated; set => SetValue(ref _IsActivated, value); }

        private bool _IntegrationViewItemWithProgressBar;
        public bool IntegrationViewItemWithProgressBar { get => _IntegrationViewItemWithProgressBar; set => SetValue(ref _IntegrationViewItemWithProgressBar, value); }

        private string _LabelContent;
        public string LabelContent { get => _LabelContent; set => SetValue(ref _LabelContent, value); }

        private double _Unlocked;
        public double Unlocked { get => _Unlocked; set => SetValue(ref _Unlocked, value); }

        private double _Total;
        public double Total { get => _Total; set => SetValue(ref _Total, value); }
    }
}
