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

        private PluginButtonDataContext ControlDataContext;
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
            ControlDataContext = new PluginButtonDataContext
            {
                IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationButton,
                DisplayDetails = PluginDatabase.PluginSettings.Settings.EnableIntegrationButtonDetails,

                Is100Percent = false,
                LabelContent = string.Empty,
                Value = 0,
                Maximum = 0
            };
        }


        public override Task<bool> SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            return Task.Run(() =>
            {
                GameAchievements gameAchievements = (GameAchievements)PluginGameData;

                ControlDataContext.Is100Percent = gameAchievements.Is100Percent;
                ControlDataContext.LabelContent = gameAchievements.Unlocked + "/" + gameAchievements.Total;
                ControlDataContext.Value = gameAchievements.Unlocked;
                ControlDataContext.Maximum = gameAchievements.Total;

                this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    this.DataContext = ControlDataContext;
                }));

                return true;
            });
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


    public class PluginButtonDataContext : IDataContext
    {
        public bool IsActivated { get; set; }
        public bool DisplayDetails { get; set; }

        public bool Is100Percent { get; set; }
        public string LabelContent { get; set; }
        public int Value { get; set; }
        public int Maximum { get; set; }
    }
}
