using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using SuccessStory.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SuccessStory.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginButton.xaml
    /// </summary>
    public partial class PluginButton : PluginUserControlExtend
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

        private PluginButtonDataContext ControlDataContext = new PluginButtonDataContext();
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginButtonDataContext)_ControlDataContext;
            }
        }


        public PluginButton()
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
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationButton;
            ControlDataContext.DisplayDetails = PluginDatabase.PluginSettings.Settings.EnableIntegrationButtonDetails;

            ControlDataContext.Is100Percent = false;
            ControlDataContext.LabelContent = string.Empty;
            ControlDataContext.Value = 0;
            ControlDataContext.Maximum = 0;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameAchievements gameAchievements = (GameAchievements)PluginGameData;

            ControlDataContext.Is100Percent = gameAchievements.Is100Percent;
            ControlDataContext.LabelContent = gameAchievements.Unlocked + "/" + gameAchievements.Total;
            ControlDataContext.Value = gameAchievements.Unlocked;
            ControlDataContext.Maximum = gameAchievements.Total;
        }
        

        #region Events
        private void PART_PluginButton_Click(object sender, RoutedEventArgs e)
        {
            dynamic ViewExtension = null;
            if (PluginDatabase.GameContext.Name.IsEqual("overwatch") && (PluginDatabase.GameContext.Source?.Name?.IsEqual("battle.net") ?? false))
            {
                ViewExtension = new SuccessStoryOverwatchView(PluginDatabase.GameContext);
            }
            else
            {
                ViewExtension = new SuccessStoryOneGameView(PluginDatabase.GameContext);
            }

            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PlayniteApi, resources.GetString("LOCSuccessStoryAchievements"), ViewExtension);
            windowExtension.ShowDialog();
        }
        #endregion
    }


    public class PluginButtonDataContext : ObservableObject, IDataContext
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

        private bool _DisplayDetails { get; set; }
        public bool DisplayDetails
        {
            get => _DisplayDetails;
            set
            {
                if (value.Equals(_DisplayDetails) == true)
                {
                    return;
                }

                _DisplayDetails = value;
                OnPropertyChanged();
            }
        }

        private bool _Is100Percent { get; set; }
        public bool Is100Percent
        {
            get => _Is100Percent;
            set
            {
                if (value.Equals(_Is100Percent) == true)
                {
                    return;
                }

                _Is100Percent = value;
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

        private int _Value { get; set; }
        public int Value
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

        private int _Maximum { get; set; }
        public int Maximum
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
    }
}
