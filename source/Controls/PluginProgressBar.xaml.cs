using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SuccessStory.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginProgressBar.xaml
    /// </summary>
    public partial class PluginProgressBar : PluginUserControlExtend
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

        private PluginProgressBarDataContext ControlDataContext = new PluginProgressBarDataContext();
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginProgressBarDataContext)_ControlDataContext;
            }
        }


        public PluginProgressBar()
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


        private void PluginUserControlExtend_Loaded(object sender, RoutedEventArgs e)
        {
            ContentControl elParent = UI.FindParent<ContentControl>((FrameworkElement)sender);
            if (elParent != null && (double.IsNaN(elParent.Height) || elParent.Height == 0))
            {
                elParent.Height = 40;
            }
        }


        public override void SetDefaultDataContext()
        {
            bool IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationProgressBar;
            if (IgnoreSettings)
            {
                IsActivated = true;
            }

            ControlDataContext.IsActivated = IsActivated;
            ControlDataContext.IntegrationShowProgressBarIndicator = PluginDatabase.PluginSettings.Settings.EnableIntegrationProgressBarIndicator;
            ControlDataContext.IntegrationShowProgressBarPercent = PluginDatabase.PluginSettings.Settings.EnableIntegrationProgressBarPercent;

            ControlDataContext.Percent = 0;
            ControlDataContext.Value = 0;
            ControlDataContext.Maximum = 0;
            ControlDataContext.LabelContent = string.Empty;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameAchievements gameAchievements = (GameAchievements)PluginGameData;

            ControlDataContext.Percent = gameAchievements.Progression;
            ControlDataContext.Value = gameAchievements.Unlocked;
            ControlDataContext.Maximum = gameAchievements.Total;

            ControlDataContext.LabelContent = gameAchievements.Unlocked + "/" + gameAchievements.Total;
        }
    }


    public class PluginProgressBarDataContext : ObservableObject, IDataContext
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

        private bool _IntegrationShowProgressBarIndicator { get; set; }
        public bool IntegrationShowProgressBarIndicator
        {
            get => _IntegrationShowProgressBarIndicator;
            set
            {
                if (value.Equals(_IntegrationShowProgressBarIndicator) == true)
                {
                    return;
                }

                _IntegrationShowProgressBarIndicator = value;
                OnPropertyChanged();
            }
        }

        private bool _IntegrationShowProgressBarPercent { get; set; }
        public bool IntegrationShowProgressBarPercent
        {
            get => _IntegrationShowProgressBarPercent;
            set
            {
                if (value.Equals(_IntegrationShowProgressBarPercent) == true)
                {
                    return;
                }

                _IntegrationShowProgressBarPercent = value;
                OnPropertyChanged();
            }
        }

        private double _Percent { get; set; }
        public double Percent
        {
            get => _Percent;
            set
            {
                if (value.Equals(_Percent) == true)
                {
                    return;
                }

                _Percent = value;
                OnPropertyChanged();
            }
        }

        private double _Value { get; set; }
        public double Value
        {
            get => _Value;
            set
            {
                if (value.Equals(_Value) == true)
                {
                    return;
                }

                _Value = value;
                OnPropertyChanged();
            }
        }

        private double _Maximum { get; set; }
        public double Maximum
        {
            get => _Maximum;
            set
            {
                if (value.Equals(_Maximum) == true)
                {
                    return;
                }

                _Maximum = value;
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
    }
}
