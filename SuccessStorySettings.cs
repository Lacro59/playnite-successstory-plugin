using Newtonsoft.Json;
using Playnite.SDK;
using SuccessStory.Models;
using System.Collections.Generic;

namespace SuccessStory
{
    public class SuccessStorySettings : ISettings
    {
        private readonly SuccessStory plugin;

        public bool EnableIntegrationInDescription { get; set; } = false;
        public bool EnableIntegrationInDescriptionWithToggle { get; set; } = false;

        public bool IncludeHiddenGames { get; set; } = false;

        public bool IntegrationShowTitle { get; set; } = true;
        public bool IntegrationShowGraphic { get; set; } = true;
        public bool IntegrationShowAchievements { get; set; } = true;
        public bool IntegrationTopGameDetails { get; set; } = true;
        public bool IntegrationToggleDetails { get; set; } = true;

        public bool EnableIntegrationInCustomTheme { get; set; } = false;

        public bool EnableIntegrationButton { get; set; } = false;
        public bool EnableIntegrationButtonDetails { get; set; } = false;

        public bool EnableIntegrationButtonHeader { get; set; } = false;

        public bool EnableSteam { get; set; } = false;
        public bool EnableGog { get; set; } = false;
        public bool EnableOrigin { get; set; } = false;

        public bool EnableLocal { get; set; } = false;

        public string NameSorting { get; set; } = "LastActivity";
        public bool IsAsc { get; set; } = false;

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
                EnableIntegrationInDescription = savedSettings.EnableIntegrationInDescription;
                EnableIntegrationInDescriptionWithToggle = savedSettings.EnableIntegrationInDescriptionWithToggle;

                IncludeHiddenGames = savedSettings.IncludeHiddenGames;

                IntegrationShowTitle = savedSettings.IntegrationShowTitle;
                IntegrationShowGraphic = savedSettings.IntegrationShowGraphic;
                IntegrationShowAchievements = savedSettings.IntegrationShowAchievements;
                IntegrationTopGameDetails = savedSettings.IntegrationTopGameDetails;
                IntegrationToggleDetails = savedSettings.IntegrationToggleDetails;

                EnableIntegrationInCustomTheme = savedSettings.EnableIntegrationInCustomTheme;

                EnableIntegrationButton = savedSettings.EnableIntegrationButton;
                EnableIntegrationButtonDetails = savedSettings.EnableIntegrationButtonDetails;

                EnableIntegrationButtonHeader = savedSettings.EnableIntegrationButtonHeader;

                EnableSteam = savedSettings.EnableSteam;
                EnableGog = savedSettings.EnableGog;
                EnableOrigin = savedSettings.EnableOrigin;

                EnableLocal = savedSettings.EnableLocal;

                NameSorting = savedSettings.NameSorting;
                IsAsc = savedSettings.IsAsc;
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
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(this);
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