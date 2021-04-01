using CommonPluginsPlaynite.Controls;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                IsActivated = PluginDatabase.PluginSettings.Settings.IntegrationShowAchievementsCompact,
                Height = PluginDatabase.PluginSettings.Settings.IntegrationAchievementsCompactListHeight + 12,

                ItemsSource = null
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

                List<Achievements> ListAchievements = gameAchievements.Items;
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
                        Common.LogError(ex, false, "Error on convert bitmap");
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
                        EnableRaretyIndicator = PluginDatabase.PluginSettings.Settings.EnableRaretyIndicator,
                        Icon = urlImg,
                        IconImage = urlImg,
                        IsGray = IsGray,
                        Description = ListAchievements[i].Description,
                        Percent = ListAchievements[i].Percent,

                        PictureSize = (ControlDataContext.Height - 12)
                    });
                }

                // Sorting
                //ListBoxAchievements = ListBoxAchievements.OrderByDescending(x => x.DateUnlock).ThenBy(x => x.Name).ToList();
                //var tt = new ListItems();
                //tt._ListBoxAchievements = ListBoxAchievements;
                //ControlDataContext.ItemsSource = new AsyncVirtualizingCollection<ListBoxAchievements>(tt);

                ControlDataContext.ItemsSource = ListBoxAchievements.OrderByDescending(x => x.DateUnlock).ThenBy(x => x.Name).ToObservable();

                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
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
    }


    public class PluginCompactListDataContext : IDataContext
    {
        public bool IsActivated { get; set; }
        public double Height { get; set; }

        //public AsyncVirtualizingCollection<ListBoxAchievements> ItemsSource { get; set; }
        public ObservableCollection<ListBoxAchievements> ItemsSource { get; set; }
    }

    //public class ListItems : IItemsProvider<ListBoxAchievements>
    //{
    //    public List<ListBoxAchievements> _ListBoxAchievements { get; set; } = new List<ListBoxAchievements>();
    //
    //
    //    public int FetchCount()
    //    {
    //        return _ListBoxAchievements.Count;
    //    }
    //
    //    public IList<ListBoxAchievements> FetchRange(int startIndex, int count)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
