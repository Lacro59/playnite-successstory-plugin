using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using CommonPluginsShared.Extensions;
using CommonPluginsShared;
using System.IO;
using System.Collections.Generic;
using Playnite.SDK.Data;
using System.Text;
using System.Security.Principal;
using CommonPlayniteShared.Common;
using static CommonPluginsShared.PlayniteTools;
using Playnite.SDK.Plugins;

namespace SuccessStory.Clients
{
    public abstract class GenericAchievements
    {
        internal static ILogger Logger => LogManager.GetLogger();

        // Access to the plugin's database.
        internal static SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;

        // Cached validation results for configuration and connection status.
        protected bool? CachedConfigurationValidationResult { get; set; }
        protected bool? CachedIsConnectedResult { get; set; }

        // Client details and language information.
        protected string ClientName { get; }
        protected string LocalLang { get; }
        protected string LocalLangShort { get; }

        // Error details for notifications.
        protected string LastErrorId { get; set; }
        protected string LastErrorMessage { get; set; }

        // Path to store cookies.
        internal CookiesTools CookiesTools { get; }
        internal string CookiesPath { get; }
        internal List<string> CookiesDomains { get; set; }


        // Constructor to initialize the client name and language settings.
        public GenericAchievements(string clientName, string localLang = "", string localLangShort = "")
        {
            ClientName = clientName;
            LocalLang = localLang;
            LocalLangShort = localLangShort;

            CookiesPath = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, CommonPlayniteShared.Common.Paths.GetSafePathName($"{clientName}.json"));
            CookiesTools = new CookiesTools(PluginDatabase.PluginName, clientName, CookiesPath, CookiesDomains);
		}

        #region Achievements

        /// <summary>
        /// Get all achievements for a game.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public GameAchievements GetAchievements(Guid Id)
        {
            Game game = API.Instance.Database.Games.Get(Id);
            if (game == null)
            {
                return new GameAchievements();
            }

            return GetAchievements(game);
        }

        /// <summary>
        /// Get all achievements for a game.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public abstract GameAchievements GetAchievements(Game game);

        #endregion

        #region Configuration

        /// <summary>
        /// Abstract method to validate the service-specific configuration and display error messages to the user.
        /// </summary>
        /// <returns>true if configuration is valid, false otherwise.</returns>
        public abstract bool ValidateConfiguration();

        /// <summary>
        /// Checks if the plugin is connected to the service.
        /// </summary>
        /// <returns>true if connected, false otherwise.</returns>
        public virtual bool IsConnected() => false;

        /// <summary>
        /// Checks if the plugin is configured correctly.
        /// </summary>
        /// <returns>true if configured, false otherwise.</returns>
        public virtual bool IsConfigured() => false;

        /// <summary>
        /// Checks if the plugin is enabled in the settings.
        /// </summary>
        /// <returns>true if enabled, false otherwise.</returns>
        public virtual bool EnabledInSettings() => false;

        #endregion

        // Resets the cached configuration validation result.
        public virtual void ResetCachedConfigurationValidationResult() => CachedConfigurationValidationResult = null;

        // Resets the cached connection result.
        public virtual void ResetCachedIsConnectedResult() => CachedIsConnectedResult = null;

        #region Cookies

        /// <summary>
        /// Gets the cookies from the saved file, if it exists.
        /// </summary>
        /// <returns>A list of HTTP cookies.</returns>
        internal List<HttpCookie> GetCookies() => CookiesTools.GetStoredCookies();

        /// <summary>
        /// Saves the cookies to a file by encrypting them.
        /// </summary>
        /// <param name="httpCookies">The list of cookies to save.</param>
        internal void SetCookies(List<HttpCookie> httpCookies) => CookiesTools.SetStoredCookies(httpCookies);

        #endregion

        #region Errors

        /// <summary>
        /// Displays a notification when the plugin is disabled.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        public virtual void ShowNotificationPluginDisable(string message)
        {
            LastErrorId = $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-disabled";
            LastErrorMessage = message;
            Logger.Warn($"{ClientName} is enable then disabled in Playnite");

            API.Instance.Notifications.Add(new NotificationMessage(
                $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-disabled",
                $"{PluginDatabase.PluginName}\r\n{message}",
                NotificationType.Error
            ));
        }

        /// <summary>
        /// Displays a notification when the plugin is not authenticated.
        /// </summary>
        /// <param name="pluginSource">The external plugin source.</param>
        public virtual void ShowNotificationPluginNoAuthenticate(ExternalPlugin pluginSource)
        {
            string message = string.Format(ResourceProvider.GetString("LOCCommonStoresNoAuthenticate"), ClientName);
            LastErrorId = $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-noauthenticate";
            LastErrorMessage = message;
            Logger.Warn($"{ClientName}: User is not authenticated");

            API.Instance.Notifications.Add(new NotificationMessage(
                $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-noauthenticate",
                $"{PluginDatabase.PluginName}\r\n{message}",
                NotificationType.Error,
                () =>
                {
                    try
                    {
                        ShowPluginSettings(pluginSource);
                        foreach (GenericAchievements achievementProvider in SuccessStoryDatabase.AchievementProviders.Values)
                        {
                            achievementProvider.ResetCachedConfigurationValidationResult();
                            achievementProvider.ResetCachedIsConnectedResult();
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }
            ));
        }

        /// <summary>
        /// Displays a notification when too much data is received from the plugin source.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="pluginSource">The external plugin source.</param>
        public virtual void ShowNotificationPluginTooMuchData(string message, ExternalPlugin pluginSource)
        {
            LastErrorId = $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-toomuchdata";
            LastErrorMessage = message;
            Logger.Warn(message);

            API.Instance.Notifications.Add(new NotificationMessage(
                $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-toomuchdata",
                $"{PluginDatabase.PluginName}\r\n{message}",
                NotificationType.Error,
                () =>
                {
                    try
                    {
                        ShowPluginSettings(pluginSource);
                        foreach (GenericAchievements achievementProvider in SuccessStoryDatabase.AchievementProviders.Values)
                        {
                            achievementProvider.ResetCachedConfigurationValidationResult();
                            achievementProvider.ResetCachedIsConnectedResult();
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }
            ));
        }

        /// <summary>
        /// Displays a notification when the plugin is misconfigured.
        /// </summary>
        public virtual void ShowNotificationPluginNoConfiguration()
        {
            string message = string.Format(ResourceProvider.GetString("LOCCommonStoreBadConfiguration"), ClientName);
            LastErrorId = $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-noconfig";
            LastErrorMessage = message;
            Logger.Warn($"{ClientName} is not configured");

            API.Instance.Notifications.Add(new NotificationMessage(
                $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-noconfig",
                $"{PluginDatabase.PluginName}\r\n{message}",
                NotificationType.Error,
                () => {
                    _ = PluginDatabase.Plugin.OpenSettingsView();
                    foreach (GenericAchievements achievementProvider in SuccessStoryDatabase.AchievementProviders.Values)
                    {
                        achievementProvider.ResetCachedConfigurationValidationResult();
                        achievementProvider.ResetCachedIsConnectedResult();
                    }
                }
            ));
        }

        /// <summary>
        /// Displays a generic error notification.
        /// </summary>
        /// <param name="ex">The exception to display.</param>
        public virtual void ShowNotificationPluginError(Exception ex)
        {
            Common.LogError(ex, false, $"{ClientName}", true, PluginDatabase.PluginName);
        }

        /// <summary>
        /// Displays an error when loading a URL fails.
        /// </summary>
        /// <param name="ex">The exception to display.</param>
        /// <param name="url">The URL that failed to load.</param>
        public virtual void ShowNotificationPluginWebError(Exception ex, string url)
        {
            Common.LogError(ex, false, $"{ClientName} - Failed to load {url}", true, PluginDatabase.PluginName);
        }

        /// <summary>
        /// Displays the last error message.
        /// </summary>
        /// <param name="pluginSource">The external plugin source.</param>
        public virtual void ShowNotificationPluginErrorMessage(ExternalPlugin pluginSource)
        {
            if (!LastErrorMessage.IsNullOrEmpty())
            {
                API.Instance.Notifications.Add(new NotificationMessage(
                    LastErrorId,
                    $"{PluginDatabase.PluginName}\r\n{LastErrorMessage}",
                NotificationType.Error,
                () =>
                {
                    try
                    {
                        ShowPluginSettings(pluginSource);
                        foreach (GenericAchievements achievementProvider in SuccessStoryDatabase.AchievementProviders.Values)
                        {
                            achievementProvider.ResetCachedConfigurationValidationResult();
                            achievementProvider.ResetCachedIsConnectedResult();
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }
                ));
            }
        }

        #endregion
    }
}