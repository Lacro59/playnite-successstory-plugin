using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
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

namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryUserStats.xaml
    /// </summary>
    public partial class SuccessStoryUserStats : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public SuccessStoryUserStats()
        {
            InitializeComponent();

            PART_LbUserStats.PreviewMouseWheel += Tools.HandlePreviewMouseWheel;

            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == "GameSelectedData" || e.PropertyName == "PluginSettings")
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        SetScData(PluginDatabase.GameSelectedData.ItemsStats);
                    }));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory");
            }
        }


        public void SetScData(List<GameStats> ListGameStats)
        {
            if (ListGameStats == null || ListGameStats.Count == 0)
            {
                this.Visibility = Visibility.Collapsed;
                return;
            }
            else
            {
                this.Visibility = Visibility.Visible;
            }

            ListGameStats.Sort((x, y) => x.Name.CompareTo(y.Name));
            PART_LbUserStats.ItemsSource = ListGameStats;

            PART_ScStatsView_IsLoaded(null, null);
        }


        private void PART_ScStatsView_IsLoaded(object sender, RoutedEventArgs e)
        {
            IntegrationUI.SetControlSize((FrameworkElement)sender);
        }
    }
}
