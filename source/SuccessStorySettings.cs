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
using CommonPluginsShared.Plugins;
using CommonPluginsStores;

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


    public class SuccessStorySettings : PluginSettings
    {
        #region Settings variables
        public bool EnableIntegrationButtonHeader { get; set; } = false;
        public bool EnableIntegrationButtonSide { get; set; } = true;

        public bool EnableGamerScore { get; set; } = true;

        [DontSerialize]
        public bool SteamGroupData { get; set; } = false;

        public bool ShowHiddenIcon { get; set; } = false;
        public bool ShowHiddenTitle { get; set; } = false;
        public bool ShowHiddenDescription { get; set; } = false;

        public bool Auto100PercentCompleted { get; set; } = false;
        public CompletionStatus CompletionStatus100Percent { get; set; }

        // TODO TEMP
        public bool PurgeImageCache { get; set; } = false;
        public bool EnableImageCache { get; set; } = true;
        public bool IgnoreSettings { get; set; } = false;


        // TODO TEMP
        public bool IsRaretyUpdate { get; set; } = false;

        // TODO TEMP
        public bool DeleteOldRaConsole { get; set; } = true;


        public bool EnableIntegrationInDescription { get; set; } = false;
        public bool EnableIntegrationInDescriptionWithToggle { get; set; } = false;

        public bool GraphicAllUnlockedByMonth { get; set; } = true;
        public bool GraphicAllUnlockedByDay { get; set; } = false;

        public bool IncludeHiddenGames { get; set; } = false;
        public bool DisplayChart { get; set; } = true;


        public string IconLocked { get; set; } = string.Empty;
        public string IconCustomLocked { get; set; } = string.Empty;
        public bool IconCustomOnlyMissing { get; set; } = true;


        private bool _enableIntegrationViewItem = true;
        public bool EnableIntegrationViewItem { get => _enableIntegrationViewItem; set => SetValue(ref _enableIntegrationViewItem, value); }

        public bool IntegrationViewItemWithProgressBar { get; set; } = false;


        private bool _enableIntegrationButton = true;
        public bool EnableIntegrationButton { get => _enableIntegrationButton; set => SetValue(ref _enableIntegrationButton, value); }

        private bool _enableIntegrationButtonDetails = false;
        public bool EnableIntegrationButtonDetails { get => _enableIntegrationButtonDetails; set => SetValue(ref _enableIntegrationButtonDetails, value); }

        private bool _enableIntegrationProgressBar = true;
        public bool EnableIntegrationProgressBar { get => _enableIntegrationProgressBar; set => SetValue(ref _enableIntegrationProgressBar, value); }

        public bool EnableIntegrationProgressBarIndicator { get; set; } = false;
        public bool EnableIntegrationProgressBarPercent { get; set; } = false;


        private bool _enableIntegrationCompact = true;
        public bool EnableIntegrationCompact { get => _enableIntegrationCompact; set => SetValue(ref _enableIntegrationCompact, value); }

        public double IntegrationCompactHeight { get; set; } = 48;
        public bool IntegrationCompactShowDescription { get; set; } = true;


        private bool _enableIntegrationCompactLocked = true;
        public bool EnableIntegrationCompactLocked { get => _enableIntegrationCompactLocked; set => SetValue(ref _enableIntegrationCompactLocked, value); }

        private bool _enableIntegrationCompactUnlocked = true;
        public bool EnableIntegrationCompactUnlocked { get => _enableIntegrationCompactUnlocked; set => SetValue(ref _enableIntegrationCompactUnlocked, value); }

        public double IntegrationCompactPartialHeight { get; set; } = 48;
        public bool IntegrationCompactPartialDisplayLastest { get; set; } = true;
        public bool IntegrationCompactPartialDisplayLastestOneLine { get; set; } = false;
        public bool IntegrationCompactPartialShowDescription { get; set; } = true;


        private bool _enableIntegrationChart = true;
        public bool EnableIntegrationChart { get => _enableIntegrationChart; set => SetValue(ref _enableIntegrationChart, value); }

        public double IntegrationChartHeight { get; set; } = 120;
        public bool EnableIntegrationAxisChart { get; set; } = true;
        public bool EnableIntegrationOrdinatesChart { get; set; } = false;
        public int IntegrationChartCountAbscissa { get; set; } = 11;
        public bool EnableIntegrationChartHideOptions { get; set; } = false;
        public bool EnableIntegrationChartAllPerdiod { get; set; } = false;
        public bool EnableIntegrationChartCutPeriod { get; set; } = false;


        private bool _enableIntegrationUserStats = true;
        public bool EnableIntegrationUserStats { get => _enableIntegrationUserStats; set => SetValue(ref _enableIntegrationUserStats, value); }

        public double IntegrationUserStatsHeight { get; set; } = 120;


        private bool _enableIntegrationList = true;
        public bool EnableIntegrationList { get => _enableIntegrationList; set => SetValue(ref _enableIntegrationList, value); }

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
        public bool EnableGameJolt { get; set; } = false;

        public bool EnableOverwatchAchievements { get; set; } = false;
        public bool EnableSc2Achievements { get; set; } = false;
        public bool EnableWowAchievements { get; set; } = false;

        public List<CbData> WowRegions { get; set; } = new List<CbData>();
        private List<CbData> wowRealms = new List<CbData>();
        public List<CbData> WowRealms { get => wowRealms; set => SetValue(ref wowRealms, value); }
        public string WowCharacter { get; set; } = string.Empty;

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
        public bool EnableWutheringWaves { get; set; } = false;
        public bool EnableHonkaiStarRail { get; set; } = false;

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
        private bool useUltraRare = false;
        public bool UseUltraRare { get => useUltraRare; set => SetValue(ref useUltraRare, value); }
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

        // TODO temp
        public SteamSettings SteamApiSettings { get; set; } = new SteamSettings();
        public EpicSettings EpicSettings { get; set; } = new EpicSettings();
        public GogSettings GogSettings { get; set; } = new GogSettings();

        public StoreSettings SteamStoreSettings { get; set; } = new StoreSettings();
        public StoreSettings EpicStoreSettings { get; set; } = new StoreSettings();
        public StoreSettings GogStoreSettings { get; set; } = new StoreSettings();
        public StoreSettings GameJoltStoreSettings { get; set; } = new StoreSettings();
        #endregion

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        #region Variables exposed
        private bool is100Percent = false;
        [DontSerialize]
        public bool Is100Percent { get => is100Percent; set => SetValue(ref is100Percent, value); }

        private AchRaretyStats common = new AchRaretyStats();
        [DontSerialize]
        public AchRaretyStats Common { get => common; set => SetValue(ref common, value); }

        private AchRaretyStats noCommon = new AchRaretyStats();
        [DontSerialize]
        public AchRaretyStats NoCommon { get => noCommon; set => SetValue(ref noCommon, value); }

        private AchRaretyStats rare = new AchRaretyStats();
        [DontSerialize]
        public AchRaretyStats Rare { get => rare; set => SetValue(ref rare, value); }

        private AchRaretyStats ultraRare = new AchRaretyStats();
        [DontSerialize]
        public AchRaretyStats UltraRare { get => ultraRare; set => SetValue(ref ultraRare, value); }

        private int unlocked = 0;
        [DontSerialize]
        public int Unlocked { get => unlocked; set => SetValue(ref unlocked, value); }

        private int locked = 0;
        [DontSerialize]
        public int Locked { get => locked; set => SetValue(ref locked, value); }

        private int total = 0;
        [DontSerialize]
        public int Total { get => total; set => SetValue(ref total, value); }

        private int totalGamerScore = 0;
        [DontSerialize]
        public int TotalGamerScore { get => totalGamerScore; set => SetValue(ref totalGamerScore, value); }

        private int percent = 0;
        [DontSerialize]
        public int Percent { get => percent; set => SetValue(ref percent, value); }

        private string estimateTimeToUnlock = string.Empty;
        [DontSerialize]
        public string EstimateTimeToUnlock { get => estimateTimeToUnlock; set => SetValue(ref estimateTimeToUnlock, value); }


        private List<Achievement> listAchievements = new List<Achievement>();
        [DontSerialize]
        public List<Achievement> ListAchievements { get => listAchievements; set => SetValue(ref listAchievements, value); }

        private List<Achievement> listAchUnlockDateAsc = new List<Achievement>();
        [DontSerialize]
        public List<Achievement> ListAchUnlockDateAsc { get => listAchUnlockDateAsc; set => SetValue(ref listAchUnlockDateAsc, value); }

        private List<Achievement> listAchUnlockDateDesc = new List<Achievement>();
        [DontSerialize]
        public List<Achievement> ListAchUnlockDateDesc { get => listAchUnlockDateDesc; set => SetValue(ref listAchUnlockDateDesc, value); }
        #endregion  
    }


    public class SuccessStorySettingsViewModel : ObservableObject, ISettings
    {
        private readonly SuccessStory Plugin;
        private SuccessStorySettings EditingClone { get; set; }

        private SuccessStorySettings _settings;
        public SuccessStorySettings Settings { get => _settings; set => SetValue(ref _settings, value); }


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
            Settings.RaConsoleAssociateds = Settings.RaConsoleAssociateds.OrderBy(x => x.RaConsoleName).ToList();

            // TODO temp
            if (Settings.SteamStoreSettings == null)
            {
                Settings.SteamStoreSettings = new StoreSettings
                {
                    UseApi = Settings.SteamApiSettings.UseApi,
                    UseAuth = Settings.SteamApiSettings.UseAuth
                };
            }
            if (Settings.EpicStoreSettings == null)
            {
                Settings.EpicStoreSettings = new StoreSettings
                {
                    UseAuth = Settings.EpicSettings.UseAuth
                };
            }
            if (Settings.GogStoreSettings == null)
            {
                Settings.GogStoreSettings = new StoreSettings
                {
                    UseAuth = Settings.GogSettings.UseAuth
                };
            }
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

            Settings.CompletionStatus100Percent = SuccessStorySettingsView.CompletionStatus;

            Settings.RaConsoleAssociateds = SuccessStorySettingsView.RaConsoleAssociateds;
            Settings.RaConsoleAssociateds.ForEach(x =>
            {
                x.SetSelectable();
            });


            // TODO
            // StoreAPI intialization
            SuccessStory.SteamApi.StoreSettings = Settings.SteamStoreSettings;
            if (Settings.EnableSteam)
            {
                SuccessStory.SteamApi.SaveCurrentUser();
                SuccessStory.SteamApi.CurrentAccountInfos = null;
                _ = SuccessStory.SteamApi.CurrentAccountInfos;
            }

            SuccessStory.EpicApi.StoreSettings = Settings.EpicStoreSettings;
            if (Settings.EnableEpic)
            {
                SuccessStory.EpicApi.SaveCurrentUser();
                SuccessStory.EpicApi.CurrentAccountInfos = null;
                _ = SuccessStory.EpicApi.CurrentAccountInfos;
            }

            SuccessStory.GogApi.StoreSettings = Settings.GogStoreSettings;
            if (Settings.EnableGog)
            {
                SuccessStory.GogApi.SaveCurrentUser();
                SuccessStory.GogApi.CurrentAccountInfos = null;
                _ = SuccessStory.GogApi.CurrentAccountInfos;
            }

            SuccessStory.GameJoltApi.StoreSettings = Settings.GameJoltStoreSettings;
            if (Settings.EnableGameJolt)
            {
                SuccessStory.GameJoltApi.SaveCurrentUser();
                SuccessStory.GameJoltApi.CurrentAccountInfos = null;
                _ = SuccessStory.GameJoltApi.CurrentAccountInfos;
            }


            Plugin.SavePluginSettings(Settings);
            SuccessStory.PluginDatabase.PluginSettings = this;

            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                Plugin.TopPanelItem.Visible = Settings.EnableIntegrationButtonHeader;
                Plugin.SidebarItem.Visible = Settings.EnableIntegrationButtonSide;
                Plugin.SidebarRaItem.Visible = Settings.EnableIntegrationButtonSide && Settings.EnableRetroAchievementsView;
            }

            OnPropertyChanged();
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

    // TODO TEMP
    public class SteamSettings
    {
        public bool UseApi { get; set; } = false;
        public bool UseAuth { get; set; } = false;
    }

    public class EpicSettings
    {
        public bool UseAuth { get; set; } = false;
    }

    public class GogSettings
    {
        public bool UseAuth { get; set; } = false;
    }
}
