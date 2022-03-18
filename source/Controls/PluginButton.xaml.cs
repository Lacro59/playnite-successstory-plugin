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
            get => PluginDatabase;
            set => PluginDatabase = (SuccessStoryDatabase)_PluginDatabase;
        }

        private PluginButtonDataContext ControlDataContext = new PluginButtonDataContext();
        internal override IDataContext _ControlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginButtonDataContext)_ControlDataContext;
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
            else if (PluginDatabase.PluginSettings.Settings.EnableGenshinImpact && PluginDatabase.GameContext.Name.IsEqual("Genshin Impact"))
            {
                ViewExtension = new SuccessStoryCategoryView(PluginDatabase.GameContext);
            }
            else if (PluginDatabase.PluginSettings.Settings.EnableGuildWars2 && PluginDatabase.GameContext.Name.IsEqual("Guild Wars 2"))
            {
                ViewExtension = new SuccessStoryCategoryView(PluginDatabase.GameContext);
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
        private bool _IsActivated;
        public bool IsActivated { get => _IsActivated; set => SetValue(ref _IsActivated, value); }

        private bool _DisplayDetails = true;
        public bool DisplayDetails { get => _DisplayDetails; set => SetValue(ref _DisplayDetails, value); }

        private bool _Is100Percent;
        public bool Is100Percent { get => _Is100Percent; set => SetValue(ref _Is100Percent, value); }

        private string _LabelContent = "15/23";
        public string LabelContent { get => _LabelContent; set => SetValue(ref _LabelContent, value); }

        private int _Value;
        public int Value { get => _Value; set => SetValue(ref _Value, value); }

        private int _Maximum;
        public int Maximum { get => _Maximum; set => SetValue(ref _Maximum, value); }
    }
}
