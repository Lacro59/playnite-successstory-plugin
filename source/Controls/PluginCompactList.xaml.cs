using CommonPluginsPlaynite.Controls;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Threading;

namespace SuccessStory.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginCompactList.xaml
    /// </summary>
    public partial class PluginCompactList : PluginUserControlExtend
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

        private PluginCompactListDataContext ControlDataContext;
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginCompactListDataContext)_ControlDataContext;
            }
        }


        public PluginCompactList()
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
            ControlDataContext = new PluginCompactListDataContext
            {
                IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationCompact,
                Height = PluginDatabase.PluginSettings.Settings.IntegrationCompactHeight + 12,

                PictureHeight = PluginDatabase.PluginSettings.Settings.IntegrationCompactHeight,
                ItemsSource = new ObservableCollection<Achievements>()
            };
        }


        public override Task<bool> SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            return Task.Run(() =>
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new ThreadStart(delegate
                {
                    this.DataContext = null;
                    this.DataContext = ControlDataContext;
                })).Wait();
                
                GameAchievements gameAchievements = (GameAchievements)PluginGameData;

                List<Achievements> ListAchievements = Serialization.GetClone(gameAchievements.Items);
                ListAchievements = ListAchievements.OrderByDescending(x => x.DateUnlocked).ThenBy(x => x.IsUnlock).ThenBy(x => x.Name).ToList();
                ControlDataContext.ItemsSource = ListAchievements.ToObservable();

                this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    this.DataContext = null;
                    this.DataContext = ControlDataContext;
                }));

                return true;
            });
        }


        private void VirtualizingStackPanel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                ((VirtualizingStackPanel)sender).LineLeft();
            else
                ((VirtualizingStackPanel)sender).LineRight();
            e.Handled = true;
        }


        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ((Image)sender).Source = new BitmapImage(new Uri(Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "default_icon.png")));
        }
    }


    public class PluginCompactListDataContext : IDataContext
    {
        public bool IsActivated { get; set; }
        public double Height { get; set; }

        public double PictureHeight { get; set; }
        public ObservableCollection<Achievements> ItemsSource { get; set; }
    }
}
