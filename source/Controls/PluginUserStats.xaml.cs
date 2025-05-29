using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SuccessStory.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginUserStats.xaml
    /// </summary>
    public partial class PluginUserStats : PluginUserControlExtend
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginUserStatsDataContext ControlDataContext = new PluginUserStatsDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginUserStatsDataContext)controlDataContext;
        }


        public PluginUserStats()
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
            bool IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationUserStats;
            double Height = PluginDatabase.PluginSettings.Settings.IntegrationUserStatsHeight;
            if (IgnoreSettings)
            {
                IsActivated = true;
                Height = double.NaN;
            }

            ControlDataContext.IsActivated = IsActivated;
            ControlDataContext.Height = Height;
            ControlDataContext.ItemsSource = null;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameAchievements gameAchievements = (GameAchievements)PluginGameData;
            ObservableCollection<GameStats> ListGameStats = gameAchievements.ItemsStats.ToObservable();

            if (ListGameStats == null || ListGameStats.Count == 0)
            {
                MustDisplay = false;
            }
            else
            {
                _ = ListGameStats.OrderBy(x => x.Name);
                ControlDataContext.ItemsSource = ListGameStats;
            }
        }
    }


    public class PluginUserStatsDataContext : ObservableObject, IDataContext
    {
        private bool isActivated;
        public bool IsActivated { get => isActivated; set => SetValue(ref isActivated, value); }

        private double height;
        public double Height { get => height; set => SetValue(ref height, value); }

        private ObservableCollection<GameStats> itemsSource;
        public ObservableCollection<GameStats> ItemsSource { get => itemsSource; set => SetValue(ref itemsSource, value); }
    }
}
