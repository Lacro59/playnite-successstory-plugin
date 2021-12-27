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

namespace SuccessStory.Clients
{
    abstract class GenericAchievements
    {
        internal static readonly ILogger logger = LogManager.GetLogger();
        internal static readonly IResourceProvider resources = new ResourceProvider();

        protected static IWebView _WebViewOffscreen;
        internal static IWebView WebViewOffscreen
        {
            get
            {
                if (_WebViewOffscreen == null)
                {
                    _WebViewOffscreen = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView();
                }
                return _WebViewOffscreen;
            }

            set
            {
                _WebViewOffscreen = value;
            }
        }

        internal static SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        protected bool? CachedConfigurationValidationResult { get; set; }
        protected bool? CachedIsConnectedResult { get; set; }

        protected string ClientName { get; }
        protected string LocalLang { get; }
        protected string LocalLangShort { get; }

        protected string LastErrorId { get; set; }
        protected string LastErrorMessage { get; set; }

        internal readonly string cookiesPath;



        public GenericAchievements(string ClientName, string LocalLang = "", string LocalLangShort = "")
        {
            this.ClientName = ClientName;
            this.LocalLang = LocalLang;
            this.LocalLangShort = LocalLangShort;

            cookiesPath = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientName}.json"));
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


        #region Cookies
        internal List<HttpCookie> GetCookies()
        {
            if (File.Exists(cookiesPath))
            {
                try
                {
                    return Serialization.FromJson<List<HttpCookie>>(
                        Encryption.DecryptFromFile(
                            cookiesPath,
                            Encoding.UTF8,
                            WindowsIdentity.GetCurrent().User.Value));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "Failed to load saved cookies");
                }
            }

            return null;
        }

        internal void SetCookies(List<HttpCookie> httpCookies)
        {
            FileSystem.CreateDirectory(Path.GetDirectoryName(cookiesPath));
            Encryption.EncryptToFile(
                cookiesPath,
                Serialization.ToJson(httpCookies),
                Encoding.UTF8,
                WindowsIdentity.GetCurrent().User.Value);
        }
        #endregion


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

        public virtual void ShowNotificationPluginNoAuthenticate(string Message, ExternalPlugin PluginSource)
        {
            LastErrorId = $"successStory-{ClientName.RemoveWhiteSpace().ToLower()}-noauthenticate";
            LastErrorMessage = Message;
            logger.Warn($"{ClientName} user is not authenticated");

            PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                $"successStory-{ClientName.RemoveWhiteSpace().ToLower()}-noauthenticate",
                $"SuccessStory\r\n{Message}",
                NotificationType.Error,
                () =>
                {
                    try
                    {
                        var plugin = API.Instance.Addons.Plugins.Find(x => x.Id == PlayniteTools.GetPluginId(PluginSource));
                        if (plugin != null)
                        {
                            plugin.OpenSettingsView();
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, "SuccessStory");
                    }
                }
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
            Common.LogError(ex, false, $"{ClientName}", true, "SuccessStory");
        }

        public virtual void ShowNotificationPluginWebError(Exception ex, string Url)
        {
            Common.LogError(ex, false, $"{ClientName} - Failed to load {Url}", true, "SuccessStory");
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
