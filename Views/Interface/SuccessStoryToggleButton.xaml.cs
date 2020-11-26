using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
using SuccessStory.Services;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryToggleButton.xaml
    /// </summary>
    public partial class SuccessStoryToggleButton : ToggleButton
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public SuccessStoryToggleButton()
        {
            InitializeComponent();

            this.DataContext = new
            {
                EnableIntegrationButtonJustIcon = PluginDatabase.PluginSettings.EnableIntegrationInDescriptionOnlyIcon
            };

            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
#if DEBUG
                logger.Debug($"SuccessStoryToggleButton.OnPropertyChanged({e.PropertyName}): {JsonConvert.SerializeObject(PluginDatabase.GameSelectedData)}");
#endif
                if (e.PropertyName == "GameSelectedData" || e.PropertyName == "PluginSettings")
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        if (PluginDatabase.GameSelectedData.HasData)
                        {
                            this.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            this.Visibility = Visibility.Collapsed;
                            return;
                        }

                        this.DataContext = new
                        {
                            EnableIntegrationButtonJustIcon = PluginDatabase.PluginSettings.EnableIntegrationInDescriptionOnlyIcon
                        };
                    }));
                }
                else
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        this.Visibility = Visibility.Collapsed;
                    }));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory");
            }
        }
    }
}
