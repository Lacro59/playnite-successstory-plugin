using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
using SuccessStory.Models;
using SuccessStory.Views;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SuccessStory
{
    public class SuccessStorySettings : ISettings
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly SuccessStory plugin;

        public bool EnableCheckVersion { get; set; } = true;
        public bool MenuInExtensions { get; set; } = true;
        public bool EnableImageCache { get; set; } = false;
        public bool IgnoreSettings { get; set; } = false;

        public bool EnableIntegrationInDescription { get; set; } = false;
        public bool EnableIntegrationInDescriptionWithToggle { get; set; } = false;

        public bool EnableIntegrationInDescriptionOnlyIcon { get; set; } = false;

        public bool GraphicAllUnlockedByMonth { get; set; } = true;
        public bool GraphicAllUnlockedByDay { get; set; } = false;

        public bool IncludeHiddenGames { get; set; } = false;

        public bool IntegrationShowTitle { get; set; } = true;
        public bool IntegrationShowGraphic { get; set; } = true;
        public bool IntegrationShowAchievements { get; set; } = true;
        public bool IntegrationShowAchievementsCompactLocked { get; set; } = true;
        public bool IntegrationShowAchievementsCompactUnlocked { get; set; } = true;
        public bool IntegrationTopGameDetails { get; set; } = true;
        public bool IntegrationToggleDetails { get; set; } = true;

        public bool EnableIntegrationAxisGraphic { get; set; } = true;
        public bool EnableIntegrationOrdinatesGraphic { get; set; } = false;
        public double IntegrationShowAchievementsHeight { get; set; } = 200;
        public double IntegrationShowGraphicHeight { get; set; } = 120;
        public int IntegrationGraphicOptionsCountAbscissa { get; set; } = 11;

        public bool EnableIntegrationInCustomTheme { get; set; } = false;

        public bool EnableIntegrationButton { get; set; } = false;
        public bool EnableIntegrationButtonDetails { get; set; } = false;

        public bool EnableIntegrationButtonHeader { get; set; } = false;

        public bool IntegrationShowProgressBar { get; set; } = false;
        public bool IntegrationShowProgressBarIndicator { get; set; } = false;
        public bool IntegrationShowProgressBarPercent { get; set; } = false;


        public bool EnableSteam { get; set; } = false;
        public bool EnableGog { get; set; } = false;
        public bool EnableOrigin { get; set; } = false;
        public bool EnableXbox { get; set; } = false;
        public bool EnableRetroAchievements { get; set; } = false;

        public bool EnableRetroAchievementsView { get; set; } = false;

        public string RetroAchievementsUser { get; set; } = string.Empty;
        public string RetroAchievementsKey { get; set; } = string.Empty;

        public bool EnableLocal { get; set; } = false;

        public string NameSorting { get; set; } = "LastActivity";
        public bool IsAsc { get; set; } = false;

        public bool EnableRaretyIndicator { get; set; } = true;

        public bool lvGamesIcon100Percent { get; set; } = true;
        public bool lvGamesIcon { get; set; } = true;
        public bool lvGamesName { get; set; } = true;
        public bool lvGamesLastSession { get; set; } = true;
        public bool lvGamesSource { get; set; } = true;
        public bool lvGamesProgression { get; set; } = true;

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonIgnore` ignore attribute.
        [JsonIgnore]
        public bool OptionThatWontBeSaved { get; set; } = false;

        // Parameterless constructor must exist if you want to use LoadPluginSettings method.
        public SuccessStorySettings()
        {
        }

        public SuccessStorySettings(SuccessStory plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<SuccessStorySettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                EnableCheckVersion = savedSettings.EnableCheckVersion;
                MenuInExtensions = savedSettings.MenuInExtensions;
                EnableImageCache = savedSettings.EnableImageCache;

                EnableIntegrationInDescription = savedSettings.EnableIntegrationInDescription;
                EnableIntegrationInDescriptionWithToggle = savedSettings.EnableIntegrationInDescriptionWithToggle;

                EnableIntegrationInDescriptionOnlyIcon = savedSettings.EnableIntegrationInDescriptionOnlyIcon;

                GraphicAllUnlockedByMonth = savedSettings.GraphicAllUnlockedByMonth;
                GraphicAllUnlockedByDay = savedSettings.GraphicAllUnlockedByDay;

                IncludeHiddenGames = savedSettings.IncludeHiddenGames;

                IntegrationShowTitle = savedSettings.IntegrationShowTitle;
                IntegrationShowGraphic = savedSettings.IntegrationShowGraphic;
                IntegrationShowAchievements = savedSettings.IntegrationShowAchievements;
                IntegrationShowAchievementsCompactLocked = savedSettings.IntegrationShowAchievementsCompactLocked;
                IntegrationShowAchievementsCompactUnlocked = savedSettings.IntegrationShowAchievementsCompactUnlocked;
                IntegrationTopGameDetails = savedSettings.IntegrationTopGameDetails;
                IntegrationToggleDetails = savedSettings.IntegrationToggleDetails;

                EnableIntegrationAxisGraphic = savedSettings.EnableIntegrationAxisGraphic;
                EnableIntegrationOrdinatesGraphic = savedSettings.EnableIntegrationOrdinatesGraphic;
                IntegrationShowAchievementsHeight = savedSettings.IntegrationShowAchievementsHeight;
                IntegrationShowGraphicHeight = savedSettings.IntegrationShowGraphicHeight;
                IntegrationGraphicOptionsCountAbscissa = savedSettings.IntegrationGraphicOptionsCountAbscissa;

                EnableIntegrationInCustomTheme = savedSettings.EnableIntegrationInCustomTheme;

                EnableIntegrationButton = savedSettings.EnableIntegrationButton;
                EnableIntegrationButtonDetails = savedSettings.EnableIntegrationButtonDetails;

                EnableIntegrationButtonHeader = savedSettings.EnableIntegrationButtonHeader;

                IntegrationShowProgressBar = savedSettings.IntegrationShowProgressBar;
                IntegrationShowProgressBarIndicator = savedSettings.IntegrationShowProgressBarIndicator;
                IntegrationShowProgressBarPercent = savedSettings.IntegrationShowProgressBarPercent;

                EnableSteam = savedSettings.EnableSteam;
                EnableGog = savedSettings.EnableGog;
                EnableOrigin = savedSettings.EnableOrigin;
                EnableXbox = savedSettings.EnableXbox;
                EnableRetroAchievements = savedSettings.EnableRetroAchievements;

                EnableRetroAchievementsView = savedSettings.EnableRetroAchievementsView;

                RetroAchievementsUser = savedSettings.RetroAchievementsUser;
                RetroAchievementsKey = savedSettings.RetroAchievementsKey;

                EnableLocal = savedSettings.EnableLocal;

                NameSorting = savedSettings.NameSorting;
                IsAsc = savedSettings.IsAsc;

                EnableRaretyIndicator = savedSettings.EnableRaretyIndicator;

                lvGamesIcon100Percent = savedSettings.lvGamesIcon100Percent;
                lvGamesIcon = savedSettings.lvGamesIcon;
                lvGamesName = savedSettings.lvGamesName;
                lvGamesLastSession = savedSettings.lvGamesLastSession;
                lvGamesSource = savedSettings.lvGamesSource;
                lvGamesProgression = savedSettings.lvGamesProgression;
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.

        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.

            if (SuccessStorySettingsView.tokenSource != null)
            {
                SuccessStorySettingsView.WithoutMessage = true;
                SuccessStorySettingsView.tokenSource.Cancel();
            }
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(this);

            PlayniteUiHelper.ResetToggle();
            SuccessStory.successStoryUI.RemoveElements();
            var TaskIntegrationUI = Task.Run(() =>
            {
                SuccessStory.successStoryUI.AddElements();
                SuccessStory.successStoryUI.RefreshElements(SuccessStory.GameSelected);
            });
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}