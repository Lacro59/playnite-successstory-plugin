using Newtonsoft.Json;
using Playnite.SDK;
using CommonPluginsShared;
using SuccessStory.Services;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryToggleButtonDetails.xaml
    /// </summary>
    public partial class SuccessStoryToggleButtonDetails : ToggleButton
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public SuccessStoryToggleButtonDetails()
        {
            InitializeComponent();

            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == "GameIsLoaded")
                {
                    return;
                }
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

                        if (!PluginDatabase.GameSelectedData.Is100Percent)
                        {
                            Sc_Icon100Percent.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            Sc_Icon100Percent.Visibility = Visibility.Visible;
                        }

                        sc_labelButton.Content = PluginDatabase.GameSelectedData.Unlocked + "/" + PluginDatabase.GameSelectedData.Total;

                        sc_pbButton.Value = PluginDatabase.GameSelectedData.Unlocked;
                        sc_pbButton.Maximum = PluginDatabase.GameSelectedData.Total;
                    }));
                }
                else
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        if (!PluginDatabase.IsViewOpen)
                        {
                            this.Visibility = Visibility.Collapsed;
                        }
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
