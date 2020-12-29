using Playnite.SDK;
using CommonShared;
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

namespace SuccessStory.Views.InterfaceFS
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryProgressionFS.xaml
    /// </summary>
    public partial class SuccessStoryProgressionFS : StackPanel
    {

        private static readonly ILogger logger = LogManager.GetLogger();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public SuccessStoryProgressionFS()
        {
            InitializeComponent();

            //PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        public void SetData(GameAchievements GameSelectedData)
        {
            if (GameSelectedData.HasData)
            {
                this.Visibility = Visibility.Visible;
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
                return;
            }

            PART_ScCountIndicator.Text = GameSelectedData.Unlocked + "/" + GameSelectedData.Total;

            PART_ScProgressBar.Value = GameSelectedData.Unlocked;
            PART_ScProgressBar.Maximum = GameSelectedData.Total;
        }


        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
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

                        PART_ScCountIndicator.Text = PluginDatabase.GameSelectedData.Unlocked + "/" + PluginDatabase.GameSelectedData.Total;

                        PART_ScProgressBar.Value = PluginDatabase.GameSelectedData.Unlocked;
                        PART_ScProgressBar.Maximum = PluginDatabase.GameSelectedData.Total;
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
