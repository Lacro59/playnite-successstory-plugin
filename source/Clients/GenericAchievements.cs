using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using CommonPluginsShared.Extensions;
using CommonPluginsShared;

namespace SuccessStory.Clients
{
    abstract class GenericAchievements
    {
        internal static readonly ILogger logger = LogManager.GetLogger();
        internal static readonly IResourceProvider resources = new ResourceProvider();

        internal static SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        protected bool? CachedConfigurationValidationResult { get; set; }
        protected bool? CachedIsConnectedResult { get; set; }

        protected string ClientName { get; }
        protected string LocalLang { get; }
        protected string LocalLangShort { get; }

        protected string LastErrorId { get; set; }
        protected string LastErrorMessage { get; set; }



        public GenericAchievements(string ClientName, string LocalLang = "", string LocalLangShort = "")
        {
            this.ClientName = ClientName;
            this.LocalLang = LocalLang;
        }


        #region Achievements
        /// <summary>
        /// Get all achievements for a game.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public GameAchievements GetAchievements(Guid Id)
        {
            Game game = PluginDatabase.PlayniteApi.Database.Games.Get(Id);
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
        /// Override to validate service-specific config and display error messages to the user
        /// </summary>
        /// <param name="playniteAPI"></param>
        /// <param name="plugin"></param>
        /// <returns>false when there are errors, true if everything's good</returns>
        public abstract bool ValidateConfiguration();


        public virtual bool IsConnected()
        {
            return false;
        }

        public virtual bool IsConfigured()
        {
            return false;
        }

        public virtual bool EnabledInSettings()
        {
            return false;
        }
        #endregion


        public virtual void ResetCachedConfigurationValidationResult()
        {
            CachedConfigurationValidationResult = null;
        }

        public virtual void ResetCachedIsConnectedResult()
        {
            CachedIsConnectedResult = null;
        }


        #region Errors
        public virtual void ShowNotificationPluginDisable(string Message)
        {
            LastErrorId = $"successStory-{ClientName.RemoveWhiteSpace().ToLower()}-disabled";
            LastErrorMessage = Message;
            logger.Warn($"{ClientName} is enable then disabled in Playnite");

            PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                $"successStory-{ClientName.RemoveWhiteSpace().ToLower()}-disabled",
                $"SuccessStory\r\n{Message}",
                NotificationType.Error
            ));
        }

        public virtual void ShowNotificationPluginNoAuthenticate(string Message)
        {
            LastErrorId = $"successStory-{ClientName.RemoveWhiteSpace().ToLower()}-noauthenticate";
            LastErrorMessage = Message;
            logger.Warn($"{ClientName} user is not authenticated");

            PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                $"successStory-{ClientName.RemoveWhiteSpace().ToLower()}-noauthenticate",
                $"SuccessStory\r\n{Message}",
                NotificationType.Error
            ));
        }

        public virtual void ShowNotificationPluginNoConfiguration(string Message)
        {
            LastErrorId = $"successStory-{ClientName.RemoveWhiteSpace().ToLower()}-noconfig";
            LastErrorMessage = Message;
            logger.Warn($"{ClientName} is not configured");

            PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                $"successStory-{ClientName.RemoveWhiteSpace().ToLower()}-noconfig",
                $"SuccessStory\r\n{Message}",
                NotificationType.Error,
                () => PluginDatabase.Plugin.OpenSettingsView()
            ));
        }

        public virtual void ShowNotificationPluginError(Exception ex)
        {
            Common.LogError(ex, false, $"{ClientName}");

            PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                $"successStory-{ClientName.RemoveWhiteSpace().ToLower()}-error",
                $"SuccessStory\r\n{ex.Message}",
                NotificationType.Error
            ));
        }

        public virtual void ShowNotificationPluginWebError(Exception ex, string Url)
        {
            Common.LogError(ex, false, $"{ClientName} - Failed to load {Url}");

            PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                $"successStory-{ClientName.RemoveWhiteSpace().ToLower()}-weberror",
                $"SuccessStory\r\n{ex.Message}",
                NotificationType.Error
            ));
        }


        public virtual void ShowNotificationPluginErrorMessage()
        {
            if (!LastErrorMessage.IsNullOrEmpty())
            {
                PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                    LastErrorId,
                    $"SuccessStory\r\n{LastErrorMessage}",
                    NotificationType.Error
                ));
            }
        }
        #endregion
    }
}
