using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
using SuccessStory.Services;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryToggleButton.xaml
    /// </summary>
    public partial class SuccessStoryButton : Button
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        bool? _JustIcon = null;


        public SuccessStoryButton(bool? JustIcon = null)
        {
            _JustIcon = JustIcon;

            InitializeComponent();


            bool EnableIntegrationButtonJustIcon;
            if (_JustIcon == null)
            {
                EnableIntegrationButtonJustIcon = PluginDatabase.PluginSettings.EnableIntegrationInDescriptionOnlyIcon;
            }
            else
            {
                EnableIntegrationButtonJustIcon = (bool)_JustIcon;
            }

            this.DataContext = new
            {
                EnableIntegrationButtonJustIcon = EnableIntegrationButtonJustIcon
            };


            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
#if DEBUG
                logger.Debug($"SuccessStoryButton.OnPropertyChanged({e.PropertyName}): {JsonConvert.SerializeObject(PluginDatabase.GameSelectedData)}");
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


                        bool EnableIntegrationButtonJustIcon;
                        if (_JustIcon == null)
                        {
                            EnableIntegrationButtonJustIcon = PluginDatabase.PluginSettings.EnableIntegrationInDescriptionOnlyIcon;
                        }
                        else
                        {
                            EnableIntegrationButtonJustIcon = (bool)_JustIcon;
                        }

                        this.DataContext = new
                        {
                            EnableIntegrationButtonJustIcon = EnableIntegrationButtonJustIcon
                        };
#if DEBUG
                        logger.Debug($"SuccessStory - DataContext: {JsonConvert.SerializeObject(DataContext)}");
#endif
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
