using Playnite.SDK;
using CommonPluginsShared;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Logique d'interaction pour ScAchievementsListCompact.xaml
    /// </summary>
    public partial class ScAchievementsListCompact : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public ScAchievementsListCompact()
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
                    SetScData(PluginDatabase.GameSelectedData);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory");
            }
        }


        public void SetScData(GameAchievements gameAchievements, bool noControl = false)
        {
            List<Achievements> ListAchievements = gameAchievements.Items;

            this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
            {
                lbAchievements.ItemsSource = null;
                lbAchievements.Items.Clear();
            }));

            Task.Run(() =>
            {
                List<ListBoxAchievements> ListBoxAchievements = new List<ListBoxAchievements>();

                for (int i = 0; i < ListAchievements.Count; i++)
                {
                    DateTime? dateUnlock = null;

                    bool IsGray = false;

                    string urlImg = string.Empty;
                    try
                    {
                        if (ListAchievements[i].DateUnlocked == default(DateTime) || ListAchievements[i].DateUnlocked == null)
                        {
                            if (ListAchievements[i].UrlLocked == string.Empty || ListAchievements[i].UrlLocked == ListAchievements[i].UrlUnlocked)
                            {
                                urlImg = ListAchievements[i].ImageUnlocked;
                                IsGray = true;
                            }
                            else
                            {
                                urlImg = ListAchievements[i].ImageLocked;
                            }
                        }
                        else
                        {
                            urlImg = ListAchievements[i].ImageUnlocked;
                            dateUnlock = ListAchievements[i].DateUnlocked;
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "SuccessStory", "Error on convert bitmap");
                    }

                    string NameAchievement = ListAchievements[i].Name;

                    // Achievement without unlocktime but achieved = 1
                    if (dateUnlock == new DateTime(1982, 12, 15, 0, 0, 0, 0))
                    {
                        dateUnlock = null;
                    }

                    ListBoxAchievements.Add(new ListBoxAchievements()
                    {
                        Name = NameAchievement,
                        DateUnlock = dateUnlock,
                        EnableRaretyIndicator = PluginDatabase.PluginSettings.EnableRaretyIndicator,
                        Icon = urlImg,
                        IconImage = urlImg,
                        IsGray = IsGray,
                        Description = ListAchievements[i].Description,
                        Percent = ListAchievements[i].Percent
                    });
                }


                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    if (!noControl)
                    {
                        if (gameAchievements.Id != SuccessStoryDatabase.GameSelected.Id)
                        {
                            return;
                        }
                    }

                    // Sorting
                    ListBoxAchievements = ListBoxAchievements.OrderByDescending(x => x.DateUnlock).ThenBy(x => x.Name).ToList();
                    lbAchievements.ItemsSource = ListBoxAchievements;
                }));
            });
        }


        private void LbAchievements_Loaded(object sender, RoutedEventArgs e)
        {
            //IntegrationUI.SetControlSize((FrameworkElement)sender);

            int RowDefinied = 1;
            int ColDefinied = 1;
            if (lbAchievements.ActualWidth > 0)
            {
                ColDefinied = (int)lbAchievements.ActualWidth / 60;
            }

            this.DataContext = new
            {
                ColDefinied = ColDefinied,
                RowDefinied = RowDefinied
            };
        }

        private void VirtualizingStackPanel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                ((VirtualizingStackPanel)sender).LineLeft();
            else
                ((VirtualizingStackPanel)sender).LineRight();
            e.Handled = true;
        }
    }
}
