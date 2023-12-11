using Playnite.SDK;
using System.Collections.Generic;
using SuccessStory.Models;
using Playnite.SDK.Data;
using SuccessStory.Views;
using CommonPluginsShared.Models;
using System.Windows.Media;
using System;
using System.Linq;
using Playnite.SDK.Models;
using SuccessStory.Clients;
using System.Threading.Tasks;
using System.Windows;

namespace SuccessStory
{
    public enum OrderAchievementType
    {
        AchievementName, AchievementDateUnlocked, AchievementRarety
    }

    public enum OrderType
    {
        Ascending, Descending
    }


    public class SuccessStorySettings : ObservableObject
    {
        #region Settings variables
        public bool MenuInExtensions { get; set; } = true;
        public bool EnableIntegrationButtonHeader { get; set; } = false;
        public bool EnableIntegrationButtonSide { get; set; } = true;

        public DateTime LastAutoLibUpdateAssetsDownload { get; set; } = DateTime.Now;

        public bool EnableTag { get; set; } = false;
        public bool AutoImport { get; set; } = true;

        public bool ShowHiddenDescription { get; set; } = false;

        public bool Auto100PercentCompleted { get; set; } = false;
        public CompletionStatus CompletionStatus100Percent { get; set; }

        // TODO TEMP
        public bool PurgeImageCache { get; set; } = false;
        public bool EnableImageCache { get; set; } = true;
        public bool IgnoreSettings { get; set; } = false;


        // TODO TEMP
        public bool IsRaretyUpdate { get; set; } = false;


        public bool EnableIntegrationInDescription { get; set; } = false;
        public bool EnableIntegrationInDescriptionWithToggle { get; set; } = false;

        public bool GraphicAllUnlockedByMonth { get; set; } = true;
        public bool GraphicAllUnlockedByDay { get; set; } = false;

        public bool IncludeHiddenGames { get; set; } = false;
        public bool DisplayChart { get; set; } = true;


        public string IconLocked { get; set; } = string.Empty;
        public string IconCustomLocked { get; set; } = string.Empty;
        public bool IconCustomOnlyMissing { get; set; } = true;


        private bool _EnableIntegrationViewItem = true;
        public bool EnableIntegrationViewItem { get => _EnableIntegrationViewItem; set => SetValue(ref _EnableIntegrationViewItem, value); }

        public bool IntegrationViewItemWithProgressBar { get; set; } = false;


        private bool _EnableIntegrationButton = true;
        public bool EnableIntegrationButton { get => _EnableIntegrationButton; set => SetValue(ref _EnableIntegrationButton, value); }

        private bool _EnableIntegrationButtonDetails = false;
        public bool EnableIntegrationButtonDetails { get => _EnableIntegrationButtonDetails; set => SetValue(ref _EnableIntegrationButtonDetails, value); }

        private bool _EnableIntegrationProgressBar = true;
        public bool EnableIntegrationProgressBar { get => _EnableIntegrationProgressBar; set => SetValue(ref _EnableIntegrationProgressBar, value); }

        public bool EnableIntegrationProgressBarIndicator { get; set; } = false;
        public bool EnableIntegrationProgressBarPercent { get; set; } = false;


        private bool _EnableIntegrationCompact = true;
        public bool EnableIntegrationCompact { get => _EnableIntegrationCompact; set => SetValue(ref _EnableIntegrationCompact, value); }

        public double IntegrationCompactHeight { get; set; } = 48;
        public bool IntegrationCompactShowDescription { get; set; } = true;


        private bool _EnableIntegrationCompactLocked = true;
        public bool EnableIntegrationCompactLocked { get => _EnableIntegrationCompactLocked; set => SetValue(ref _EnableIntegrationCompactLocked, value); }

        private bool _EnableIntegrationCompactUnlocked = true;
        public bool EnableIntegrationCompactUnlocked { get => _EnableIntegrationCompactUnlocked; set => SetValue(ref _EnableIntegrationCompactUnlocked, value); }

        public double IntegrationCompactPartialHeight { get; set; } = 48;
        public bool IntegrationCompactPartialDisplayLastest { get; set; } = true;
        public bool IntegrationCompactPartialDisplayLastestOneLine { get; set; } = false;
        public bool IntegrationCompactPartialShowDescription { get; set; } = true;


        private bool _EnableIntegrationChart = true;
        public bool EnableIntegrationChart { get => _EnableIntegrationChart; set => SetValue(ref _EnableIntegrationChart, value); }

        public double IntegrationChartHeight { get; set; } = 120;
        public bool EnableIntegrationAxisChart { get; set; } = true;
        public bool EnableIntegrationOrdinatesChart { get; set; } = false;
        public int IntegrationChartCountAbscissa { get; set; } = 11;
        public bool EnableIntegrationChartHideOptions { get; set; } = false;
        public bool EnableIntegrationChartAllPerdiod { get; set; } = false;
        public bool EnableIntegrationChartCutPeriod { get; set; } = false;


        private bool _EnableIntegrationUserStats = true;
        public bool EnableIntegrationUserStats { get => _EnableIntegrationUserStats; set => SetValue(ref _EnableIntegrationUserStats, value); }

        public double IntegrationUserStatsHeight { get; set; } = 120;


        private bool _EnableIntegrationList = true;
        public bool EnableIntegrationList { get => _EnableIntegrationList; set => SetValue(ref _EnableIntegrationList, value); }

        public double IntegrationListHeight { get; set; } = 200;
        public double IntegrationListIconHeight { get; set; } = 48;
        public int IntegrationListColCount { get; set; } = 1;


        public bool EnablePsn { get; set; } = false;
        public bool EnableSteam { get; set; } = false;
        public bool EnableGog { get; set; } = false;
        public bool EnableEpic { get; set; } = false;
        public bool EnableOrigin { get; set; } = false;
        public bool EnableXbox { get; set; } = false;
        public bool EnableRetroAchievements { get; set; } = false;
        public bool EnableRpcs3Achievements { get; set; } = false;

        public bool EnableOverwatchAchievements { get; set; } = false;
        public bool EnableSc2Achievements { get; set; } = false;
        public bool EnableWowAchievements { get; set; } = false;

        public List<CbData> WowRegions { get; set; } = new List<CbData>();
        private List<CbData> _WowRealms = new List<CbData>();
        public List<CbData> WowRealms { get => _WowRealms; set => SetValue(ref _WowRealms, value); }
        public string WowCharacter { get; set; } = "iryaël";

        public bool EnableSteamWithoutWebApi { get; set; } = false;
        public bool SteamIsPrivate { get; set; } = false;

        public string Rpcs3InstallationFolder { get; set; } = string.Empty;
        public List<Folder> Rpcs3InstallationFolders { get; set; } = new List<Folder>();

        public bool EnableRetroAchievementsView { get; set; } = false;
        public bool EnableOneGameView { get; set; } = true;

        public string RetroAchievementsUser { get; set; } = string.Empty;
        public string RetroAchievementsKey { get; set; } = string.Empty;
        public List<RaConsoleAssociated> RaConsoleAssociateds { get; set; } = new List<RaConsoleAssociated>();

        public bool EnableLocal { get; set; } = false;
        public List<Folder> LocalPath { get; set; } = new List<Folder>();

        public bool EnableManual { get; set; } = false;

        public bool EnableGenshinImpact { get; set; } = false;

        public bool EnableGuildWars2 { get; set; } = false;
        public string GuildWars2ApiKey { get; set; } = string.Empty;

        public string NameSorting { get; set; } = "LastActivity";
        public bool IsAsc { get; set; } = false;

        public bool EnableRaretyIndicator { get; set; } = true;
        public bool DisplayRarityValue { get; set; } = true;
        public double RarityUncommon { get; set; } = 30;
        public SolidColorBrush RarityUncommonColor { get; set; } = Brushes.DarkGray;
        public double RarityRare { get; set; } = 10;
        public SolidColorBrush RarityRareColor { get; set; } = Brushes.Gold;
        public bool UseUltraRare { get; set; } = false;
        public double RarityUltraRare { get; set; } = 2;
        public SolidColorBrush RarityUltraRareColor { get; set; } = Brushes.MediumPurple;

        public bool lvGamesIcon100Percent { get; set; } = true;
        public bool lvGamesIcon { get; set; } = true;
        public bool lvGamesName { get; set; } = true;
        public bool lvGamesLastSession { get; set; } = true;
        public bool lvGamesSource { get; set; } = true;
        public bool lvGamesProgression { get; set; } = true;

        public OrderAchievement IntegrationCompactOrderAchievement { get; set; } = new OrderAchievement();
        public OrderAchievement IntegrationCompactUnlockedOrderAchievement { get; set; } = new OrderAchievement();
        public OrderAchievement IntegrationCompactLockedOrderAchievement { get; set; } = new OrderAchievement();
        public OrderAchievement IntegrationListOrderAchievement { get; set; } = new OrderAchievement();

        public GameFeature AchievementFeature { get; set; } = null;

        public bool UseLocalised { get; set; } = false;
        #endregion

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        #region Variables exposed
        private bool _HasData { get; set; } = false;
        [DontSerialize]
        public bool HasData
        {
            get => _HasData;
            set
            {
                _HasData = value;
                OnPropertyChanged();
            }
        }

        private bool _Is100Percent { get; set; } = false;
        [DontSerialize]
        public bool Is100Percent
        {
            get => _Is100Percent;
            set
            {
                _Is100Percent = value;
                OnPropertyChanged();
            }
        }

        private int _Unlocked { get; set; } = 0;
        [DontSerialize]
        public int Unlocked
        {
            get => _Unlocked;
            set
            {
                _Unlocked = value;
                OnPropertyChanged();
            }
        }

        private int _Locked { get; set; } = 0;
        [DontSerialize]
        public int Locked
        {
            get => _Locked;
            set
            {
                _Locked = value;
                OnPropertyChanged();
            }
        }

        private int _Total { get; set; } = 0;
        [DontSerialize]
        public int Total
        {
            get => _Total;
            set
            {
                _Total = value;
                OnPropertyChanged();
            }
        }

        private int _Percent { get; set; } = 0;
        [DontSerialize]
        public int Percent
        {
            get => _Percent;
            set
            {
                _Percent = value;
                OnPropertyChanged();
            }
        }

        private string _EstimateTimeToUnlock { get; set; } = string.Empty;
        [DontSerialize]
        public string EstimateTimeToUnlock
        {
            get => _EstimateTimeToUnlock;
            set
            {
                _EstimateTimeToUnlock = value;
                OnPropertyChanged();
            }
        }

        private List<Achievements> _ListAchievements { get; set; } = new List<Achievements>();
        [DontSerialize]
        public List<Achievements> ListAchievements
        {
            get => _ListAchievements;
            set
            {
                _ListAchievements = value;
                OnPropertyChanged();
            }
        }
        #endregion  
    }


    public class SuccessStorySettingsViewModel : ObservableObject, ISettings
    {
        private readonly SuccessStory Plugin;
        private SuccessStorySettings EditingClone { get; set; }

        private SuccessStorySettings _Settings;
        public SuccessStorySettings Settings { get => _Settings; set => SetValue(ref _Settings, value); }


        public SuccessStorySettingsViewModel(SuccessStory plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            Plugin = plugin;

            // Load saved settings.
            SuccessStorySettings savedSettings = plugin.LoadPluginSettings<SuccessStorySettings>();

            // LoadPluginSettings returns null if not saved data is available.
            Settings = savedSettings ?? new SuccessStorySettings();

            if (Settings.WowRegions?.Count == 0)
            {
                Settings.WowRegions = new List<CbData> { new CbData { Name = "us" }, new CbData { Name = "kr" }, new CbData { Name = "eu" }, new CbData { Name = "tw" } };
            }

            // Set RA console list
            Task.Run(() =>
            {
                System.Threading.SpinWait.SpinUntil(() => API.Instance.Database.IsOpen, -1);
                Settings.RaConsoleAssociateds.ForEach(y =>
                {
                    API.Instance.Database.Platforms.ForEach(x =>
                    {
                        int RaConsoleId = RetroAchievements.FindConsole(x.Name);
                        Models.Platform Finded = y.Platforms.Find(z => z.Id == x.Id);
                        if (Finded == null)
                        {
                            y.Platforms.Add(new Models.Platform { Id = x.Id });
                        }
                    });
                    y.Platforms = y.Platforms.OrderBy(z => z.Name).ToList();
                });

                Application.Current.Dispatcher?.BeginInvoke((Action)delegate
                {
                    Settings.RaConsoleAssociateds = Settings.RaConsoleAssociateds.OrderBy(x => x.RaConsoleName).ToList();
                });
            });
        }

        // Code executed when settings view is opened and user starts editing values.
        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
        }

        // Code executed when user decides to cancel any changes made since BeginEdit was called.
        // This method should revert any changes made to Option1 and Option2.
        public void CancelEdit()
        {
            Settings = EditingClone;
        }

        // Code executed when user decides to confirm changes made since BeginEdit was called.
        // This method should save settings made to Option1 and Option2.
        public void EndEdit()
        {
            Settings.RarityUncommonColor = SuccessStorySettingsView.RarityUncommonColor;
            Settings.RarityRareColor = SuccessStorySettingsView.RarityRareColor;
            Settings.RarityUltraRareColor = SuccessStorySettingsView.RarityUltraRareColor;

            Settings.LocalPath = SuccessStorySettingsView.LocalPath;
            Settings.Rpcs3InstallationFolders = SuccessStorySettingsView.Rpcs3Path;

            Settings.CompletionStatus100Percent = SuccessStorySettingsView.completionStatus;

            Settings.RaConsoleAssociateds = SuccessStorySettingsView.RaConsoleAssociateds;
            Settings.RaConsoleAssociateds.ForEach(x => 
            {
                x.Platforms = x.Platforms.FindAll(y => y.IsSelected).ToList();
            });


            SuccessStory.SteamApi.Save();


            Plugin.SavePluginSettings(Settings);
            SuccessStory.PluginDatabase.PluginSettings = this;

            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                Plugin.topPanelItem.Visible = Settings.EnableIntegrationButtonHeader;
                Plugin.successStoryViewSidebar.Visible = Settings.EnableIntegrationButtonSide;
                Plugin.successStoryViewRaSidebar.Visible = (Settings.EnableIntegrationButtonSide && Settings.EnableRetroAchievementsView);
            }

            this.OnPropertyChanged();
        }

        // Code execute when user decides to confirm changes made since BeginEdit was called.
        // Executed before EndEdit is called and EndEdit is not called if false is returned.
        // List of errors is presented to user if verification fails.
        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }


    public class OrderAchievement
    {
        public OrderAchievementType OrderAchievementTypeFirst { get; set; } = OrderAchievementType.AchievementDateUnlocked;
        public OrderAchievementType OrderAchievementTypeSecond { get; set; } = OrderAchievementType.AchievementName;
        public OrderAchievementType OrderAchievementTypeThird { get; set; } = OrderAchievementType.AchievementRarety;
        public OrderType OrderTypeFirst { get; set; } = OrderType.Descending;
        public OrderType OrderTypeSecond { get; set; } = OrderType.Ascending;
        public OrderType OrderTypeThird { get; set; } = OrderType.Descending;
        public bool OrderGroupByUnlocked { get; set; } = false;
    }
}
