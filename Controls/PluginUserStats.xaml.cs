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

        private PluginUserStatsDataContext ControlDataContext;
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
            double Height = PluginDatabase.PluginSettings.Settings.IntegrationUserStatsHeight;
            if (IgnoreSettings)
            {
                Height = double.NaN;
            }


            ControlDataContext = new PluginUserStatsDataContext
            {
                IsActivated = PluginDatabase.PluginSettings.Settings.IntegrationShowUserStats,
                Height = Height,

                ItemsSource = null
            };
        }


        public override Task<bool> SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            bool MustDisplay = this.MustDisplay;

            return Task.Run(() =>
            {
                GameAchievements gameAchievements = (GameAchievements)PluginGameData;
                List<GameStats> ListGameStats = gameAchievements.ItemsStats;

                if (ListGameStats == null || ListGameStats.Count == 0)
                {
                    MustDisplay = false;
                }
                else
                {
                    ListGameStats.Sort((x, y) => x.Name.CompareTo(y.Name));
                    ControlDataContext.ItemsSource = ListGameStats;
                }

                this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    this.MustDisplay = MustDisplay;
                    this.DataContext = null;
                    this.DataContext = ControlDataContext;
                }));

                return true;
            });
        }
    }


    public class PluginUserStatsDataContext : IDataContext
    {
        public bool IsActivated { get; set; }
        public double Height { get; set; }

        public List<GameStats> ItemsSource { get; set; }
    }
}
