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
    /// Logique d'interaction pour SuccessStoryButtonDetails.xaml
    /// </summary>
    public partial class SuccessStoryButtonDetails : Button
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public SuccessStoryButtonDetails()
        {
            InitializeComponent();

            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
#if DEBUG
                logger.Debug($"SuccessStoryButtonDetails.OnPropertyChanged({e.PropertyName}): {JsonConvert.SerializeObject(PluginDatabase.GameSelectedData)}");
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

                        if (PluginDatabase.GameSelectedData.Total != PluginDatabase.GameSelectedData.Unlocked)
                        {
                            Sc_Icon100Percent.Visibility = Visibility.Collapsed;
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
