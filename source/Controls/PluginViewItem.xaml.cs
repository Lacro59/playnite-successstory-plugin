using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginViewItem.xaml
    /// </summary>
    public partial class PluginViewItem : PluginUserControlExtend
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

        private PluginViewItemDataContext ControlDataContext = new PluginViewItemDataContext();
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginViewItemDataContext)_ControlDataContext;
            }
        }


        public PluginViewItem()
        {
            InitializeComponent();
            this.DataContext = ControlDataContext;

            Task.Run(() =>
            {
                // Wait extension database are loaded
                System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                this.Dispatcher?.BeginInvoke((Action)delegate
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
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationViewItem;
            ControlDataContext.IntegrationViewItemWithProgressBar = PluginDatabase.PluginSettings.Settings.IntegrationViewItemWithProgressBar;

            ControlDataContext.LabelContent = string.Empty;
            ControlDataContext.Unlocked = 0;
            ControlDataContext.Total = 0;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameAchievements gameAchievements = (GameAchievements)PluginGameData;

            ControlDataContext.Unlocked = gameAchievements.Unlocked;
            ControlDataContext.Total = gameAchievements.Total;
            ControlDataContext.LabelContent = gameAchievements.Unlocked + "/" + gameAchievements.Total;
        }
    }


    public class PluginViewItemDataContext : ObservableObject, IDataContext
    {
        private bool _IsActivated { get; set; }
        public bool IsActivated
        {
            get => _IsActivated;
            set
            {
                if (value.Equals(_IsActivated) == true)
                {
                    return;
                }

                _IsActivated = value;
                OnPropertyChanged();
            }
        }

        private bool _IntegrationViewItemWithProgressBar { get; set; }
        public bool IntegrationViewItemWithProgressBar
        {
            get => _IntegrationViewItemWithProgressBar;
            set
            {
                if (value.Equals(_IntegrationViewItemWithProgressBar) == true)
                {
                    return;
                }

                _IntegrationViewItemWithProgressBar = value;
                OnPropertyChanged();
            }
        }

        private string _LabelContent { get; set; }
        public string LabelContent
        {
            get => _LabelContent;
            set
            {
                if (value?.Equals(_LabelContent) == true)
                {
                    return;
                }

                _LabelContent = value;
                OnPropertyChanged();
            }
        }

        private double _Unlocked { get; set; }
        public double Unlocked
        {
            get => _Unlocked;
            set
            {
                if (value.Equals(_Unlocked) == true)
                {
                    return;
                }

                _Unlocked = value;
                OnPropertyChanged();
            }
        }

        private double _Total { get; set; }
        public double Total
        {
            get => _Total;
            set
            {
                if (value.Equals(_Total) == true)
                {
                    return;
                }

                _Total = value;
                OnPropertyChanged();
            }
        }
    }
}
