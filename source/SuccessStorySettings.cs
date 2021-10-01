using Playnite.SDK;
using CommonPluginsShared;
using System.Collections.Generic;
using System.Threading.Tasks;
using SuccessStory.Services;
using SuccessStory.Models;
using Playnite.SDK.Data;
using SuccessStory.Views;
using CommonPluginsShared.Models;

namespace SuccessStory
{
    public class SuccessStorySettings : ObservableObject
    {
        #region Settings variables
        public bool MenuInExtensions { get; set; } = true;

        public bool EnableTag { get; set; } = false;


        public bool Auto100PercentCompleted { get; set; } = false;

        public bool EnableImageCache { get; set; } = true;
        public bool IgnoreSettings { get; set; } = false;

        public bool EnableIntegrationInDescription { get; set; } = false;
        public bool EnableIntegrationInDescriptionWithToggle { get; set; } = false;

        public bool GraphicAllUnlockedByMonth { get; set; } = true;
        public bool GraphicAllUnlockedByDay { get; set; } = false;

        public bool IncludeHiddenGames { get; set; } = false;


        public bool EnableIntegrationButtonHeader { get; set; } = false;


        private bool _EnableIntegrationViewItem { get; set; } = true;
        public bool EnableIntegrationViewItem
        {
            get => _EnableIntegrationViewItem;
            set
            {
                _EnableIntegrationViewItem = value;
                OnPropertyChanged();
            }
        }

        public bool IntegrationViewItemWithProgressBar { get; set; } = false;


        private bool _EnableIntegrationButton { get; set; } = true;
        public bool EnableIntegrationButton
        {
            get => _EnableIntegrationButton;
            set
            {
                _EnableIntegrationButton = value;
                OnPropertyChanged();
            }
        }

        private bool _EnableIntegrationButtonDetails { get; set; } = false;
        public bool EnableIntegrationButtonDetails
        {
            get => _EnableIntegrationButtonDetails;
            set
            {
                _EnableIntegrationButtonDetails = value;
                OnPropertyChanged();
            }
        }


        private bool _EnableIntegrationProgressBar { get; set; } = true;
        public bool EnableIntegrationProgressBar
        {
            get => _EnableIntegrationProgressBar;
            set
            {
                _EnableIntegrationProgressBar = value;
                OnPropertyChanged();
            }
        }

        public bool EnableIntegrationProgressBarIndicator { get; set; } = false;
        public bool EnableIntegrationProgressBarPercent { get; set; } = false;


        private bool _EnableIntegrationCompact { get; set; } = true;
        public bool EnableIntegrationCompact
        {
            get => _EnableIntegrationCompact;
            set
            {
                _EnableIntegrationCompact = value;
                OnPropertyChanged();
            }
        }

        public double IntegrationCompactHeight { get; set; } = 48;
        public bool IntegrationCompactShowDescription { get; set; } = true;


        private bool _EnableIntegrationCompactLocked { get; set; } = true;
        public bool EnableIntegrationCompactLocked
        {
            get => _EnableIntegrationCompactLocked;
            set
            {
                _EnableIntegrationCompactLocked = value;
                OnPropertyChanged();
            }
        }

        private bool _EnableIntegrationCompactUnlocked { get; set; } = true;
        public bool EnableIntegrationCompactUnlocked
        {
            get => _EnableIntegrationCompactUnlocked;
            set
            {
                _EnableIntegrationCompactUnlocked = value;
                OnPropertyChanged();
            }
        }

        public double IntegrationCompactPartialHeight { get; set; } = 48;
        public bool IntegrationCompactPartialDisplayLastest { get; set; } = true;
        public bool IntegrationCompactPartialDisplayLastestOneLine { get; set; } = false;
        public bool IntegrationCompactPartialShowDescription { get; set; } = true;


        private bool _EnableIntegrationChart { get; set; } = true;
        public bool EnableIntegrationChart
        {
            get => _EnableIntegrationChart;
            set
            {
                _EnableIntegrationChart = value;
                OnPropertyChanged();
            }
        }

        public double IntegrationChartHeight { get; set; } = 120;
        public bool EnableIntegrationAxisChart { get; set; } = true;
        public bool EnableIntegrationOrdinatesChart { get; set; } = false;
        public int IntegrationChartCountAbscissa { get; set; } = 11;
        public bool EnableIntegrationChartHideOptions { get; set; } = false;
        public bool EnableIntegrationChartAllPerdiod { get; set; } = false;
        public bool EnableIntegrationChartCutPeriod { get; set; } = false;


        private bool _EnableIntegrationUserStats { get; set; } = true;
        public bool EnableIntegrationUserStats
        {
            get => _EnableIntegrationUserStats;
            set
            {
                _EnableIntegrationUserStats = value;
                OnPropertyChanged();
            }
        }

        public double IntegrationUserStatsHeight { get; set; } = 120;


        private bool _EnableIntegrationList { get; set; } = true;
        public bool EnableIntegrationList
        {
            get => _EnableIntegrationList;
            set
            {
                _EnableIntegrationList = value;
                OnPropertyChanged();
            }
        }

        public double IntegrationListHeight { get; set; } = 200;
        public int IntegrationListColCount { get; set; } = 1;



        public bool EnablePsn { get; set; } = false;
        public bool EnableSteam { get; set; } = false;
        public bool EnableGog { get; set; } = false;
        public bool EnableOrigin { get; set; } = false;
        public bool EnableXbox { get; set; } = false;
        public bool EnableRetroAchievements { get; set; } = false;
        public bool EnableRpcs3Achievements { get; set; } = false;

        public bool EnableOverwatchAchievements { get; set; } = false;
        public bool EnableSc2Achievements { get; set; } = false;

        public bool EnableSteamWithoutWebApi { get; set; } = false;
        public bool SteamIsPrivate { get; set; } = false;

        public string Rpcs3InstallationFolder { get; set; } = string.Empty;

        public bool EnableRetroAchievementsView { get; set; } = false;
        public bool EnableOneGameView { get; set; } = true;

        public string RetroAchievementsUser { get; set; } = string.Empty;
        public string RetroAchievementsKey { get; set; } = string.Empty;

        public bool EnableLocal { get; set; } = false;
        public List<Folder> LocalPath { get; set; } = new List<Folder>();

        public bool EnableManual { get; set; } = false;

        public string NameSorting { get; set; } = "LastActivity";
        public bool IsAsc { get; set; } = false;

        public bool EnableRaretyIndicator { get; set; } = true;

        public bool lvGamesIcon100Percent { get; set; } = true;
        public bool lvGamesIcon { get; set; } = true;
        public bool lvGamesName { get; set; } = true;
        public bool lvGamesLastSession { get; set; } = true;
        public bool lvGamesSource { get; set; } = true;
        public bool lvGamesProgression { get; set; } = true;
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
        public SuccessStorySettings Settings
        {
            get => _Settings;
            set
            {
                _Settings = value;
                OnPropertyChanged();
            }
        }


        public SuccessStorySettingsViewModel(SuccessStory plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            Plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<SuccessStorySettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new SuccessStorySettings();
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

            if (SuccessStorySettingsView.tokenSource != null)
            {
                SuccessStorySettingsView.WithoutMessage = true;
                SuccessStorySettingsView.tokenSource.Cancel();
            }
        }

        // Code executed when user decides to confirm changes made since BeginEdit was called.
        // This method should save settings made to Option1 and Option2.
        public void EndEdit()
        {
            Settings.LocalPath = SuccessStorySettingsView.LocalPath;

            Plugin.SavePluginSettings(Settings);
            SuccessStory.PluginDatabase.PluginSettings = this;
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
}