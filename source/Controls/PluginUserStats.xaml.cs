using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginUserStats.xaml
    /// </summary>
    public partial class PluginUserStats : PluginUserControlExtend
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

        private PluginUserStatsDataContext ControlDataContext = new PluginUserStatsDataContext();
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginUserStatsDataContext)_ControlDataContext;
            }
        }


        public PluginUserStats()
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
                ListGameStats.OrderBy(x => x.Name);
                ControlDataContext.ItemsSource = ListGameStats;
            }
        }
    }


    public class PluginUserStatsDataContext : ObservableObject, IDataContext
    {
        private bool _IsActivated;
        public bool IsActivated { get => _IsActivated; set => SetValue(ref _IsActivated, value); }

        private double _Height;
        public double Height { get => _Height; set => SetValue(ref _Height, value); }

        private ObservableCollection<GameStats> _ItemsSource;
        public ObservableCollection<GameStats> ItemsSource { get => _ItemsSource; set => SetValue(ref _ItemsSource, value); }
    }
}
