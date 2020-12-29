using Newtonsoft.Json;
using Playnite.SDK;
using CommonShared;
using SuccessStory.Services;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryAchievementsProgressBar.xaml
    /// </summary>
    public partial class SuccessStoryAchievementsProgressBar : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public SuccessStoryAchievementsProgressBar()
        {            
            InitializeComponent();

            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == "GameSelectedData" || e.PropertyName == "PluginSettings")
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        SetScData(PluginDatabase.GameSelectedData.Unlocked, PluginDatabase.GameSelectedData.Total);
                    }));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory");
            }
        }


        public void SetScData(long value, long maxValue)
        {
            if (PluginDatabase.PluginSettings.IntegrationShowProgressBarIndicator)
            {
                AchievementsIndicator.Content = value + "/" + maxValue;
            }
            else
            {
                AchievementsIndicator.Content = string.Empty;
                AchievementsProgressBar.SetValue(Grid.ColumnProperty, 0);
                AchievementsProgressBar.SetValue(Grid.ColumnSpanProperty, 3);
            }

            AchievementsProgressBar.Value = value;
            AchievementsProgressBar.Maximum = maxValue;

            if (PluginDatabase.PluginSettings.IntegrationShowProgressBarPercent)
            {
                AchievementsPercent.Content = (maxValue != 0) ? (int)Math.Round((double)(value * 100 / maxValue)) + "%" : 0 + "%";
            }
            else
            {
                AchievementsPercent.Content = string.Empty;
            }
        }


        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            IntegrationUI.SetControlSize((FrameworkElement)sender);
        }
    }
}
