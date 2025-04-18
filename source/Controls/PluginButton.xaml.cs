using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
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
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;
        internal override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginButtonDataContext ControlDataContext = new PluginButtonDataContext();
        internal override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginButtonDataContext)controlDataContext;
        }


        public PluginButton()
        {
            InitializeComponent();
            DataContext = ControlDataContext;

            _ = Task.Run(() =>
            {
                // Wait extension database are loaded
                _ = System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                _ = Dispatcher?.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;

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
            WindowOptions windowOptions = new WindowOptions
            {
                ShowMinimizeButton = false,
                ShowMaximizeButton = false,
                ShowCloseButton = true,
                CanBeResizable = false,
                Height = 800,
                Width = 1110
            };

            dynamic ViewExtension;
            if (PluginDatabase.GameContext.Name.IsEqual("overwatch") && (PluginDatabase.GameContext.Source?.Name?.IsEqual("battle.net") ?? false))
            {
                ViewExtension = new SuccessStoryOverwatchView(PluginDatabase.GameContext);
            }
            else if (PluginDatabase.PluginSettings.Settings.EnableGenshinImpact && PluginDatabase.GameContext.Name.IsEqual("Genshin Impact"))
            {
                ViewExtension = new SuccessStoryCategoryView(PluginDatabase.GameContext);
            }
            else if (PluginDatabase.PluginSettings.Settings.EnableWutheringWaves && PluginDatabase.GameContext.Name.IsEqual("Wuthering Waves"))
            {
                ViewExtension = new SuccessStoryCategoryView(PluginDatabase.GameContext);
            }
            else if (PluginDatabase.PluginSettings.Settings.EnableWutheringWaves && PluginDatabase.GameContext.Name.IsEqual("Honkai: Star Rail"))
            {
                ViewExtension = new SuccessStoryCategoryView(PluginDatabase.GameContext);
            }
            else if (PluginDatabase.PluginSettings.Settings.EnableZenlessZoneZero && PluginDatabase.GameContext.Name.IsEqual("Zenless Zone Zero"))
            {
                ViewExtension = new SuccessStoryCategoryView(PluginDatabase.GameContext);
            }
            else if (PluginDatabase.PluginSettings.Settings.EnableGuildWars2 && PluginDatabase.GameContext.Name.IsEqual("Guild Wars 2"))
            {
                ViewExtension = new SuccessStoryCategoryView(PluginDatabase.GameContext);
            }
            else
            {
                ViewExtension = PluginDatabase.GameContext.PluginId == PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.SteamLibrary) && PluginDatabase.PluginSettings.Settings.SteamGroupData
                    ? (dynamic)new SuccessStoryCategoryView(PluginDatabase.GameContext)
                    : (dynamic)new SuccessStoryOneGameView(PluginDatabase.GameContext);
            }

            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCSuccessStoryAchievements"), ViewExtension, windowOptions);
            _ = windowExtension.ShowDialog();
        }
        #endregion
    }


    public class PluginButtonDataContext : ObservableObject, IDataContext
    {
        private bool isActivated;
        public bool IsActivated { get => isActivated; set => SetValue(ref isActivated, value); }

        private bool displayDetails = true;
        public bool DisplayDetails { get => displayDetails; set => SetValue(ref displayDetails, value); }

        private bool is100Percent;
        public bool Is100Percent { get => is100Percent; set => SetValue(ref is100Percent, value); }

        private string labelContent = "15/23";
        public string LabelContent { get => labelContent; set => SetValue(ref labelContent, value); }

        private int value;
        public int Value { get => value; set => SetValue(ref this.value, value); }

        private int maximum;
        public int Maximum { get => maximum; set => SetValue(ref maximum, value); }
    }
}
