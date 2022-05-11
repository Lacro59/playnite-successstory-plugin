using Playnite.SDK.Models;
using CommonPluginsShared;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static CommonPluginsShared.PlayniteTools;
using CommonPluginsStores.Origin;
using System.Collections.ObjectModel;
using CommonPluginsStores.Models;
using System.Linq;

namespace SuccessStory.Clients
{
    class OriginAchievements : GenericAchievements
    {
        protected static OriginApi _OriginAPI;
        internal static OriginApi OriginAPI
        {
            get
            {
                if (_OriginAPI == null)
                {
                    _OriginAPI = new OriginApi(PluginDatabase.PluginName);
                }
                return _OriginAPI;
            }

            set => _OriginAPI = value;
        }


        public OriginAchievements() : base("Origin", CodeLang.GetOriginLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language), CodeLang.GetOriginLangCountry(PluginDatabase.PlayniteApi.ApplicationSettings.Language))
        {
            OriginAPI.SetLanguage(PluginDatabase.PlayniteApi.ApplicationSettings.Language);
        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();

            if (IsConnected())
            {
                try
                {
                    GameInfos gameInfos = OriginAPI.GetGameInfos(game.GameId, null);
                    ObservableCollection<GameAchievement> originAchievements = OriginAPI.GetAchievements(gameInfos.Id2, OriginAPI.CurrentAccountInfos);
                    if (originAchievements?.Count > 0)
                    {
                        AllAchievements = originAchievements.Select(x => new Achievements
                        {
                            ApiName = x.Id,
                            Name = x.Name,
                            Description = x.Description,
                            UrlUnlocked = x.UrlUnlocked,
                            UrlLocked = x.UrlLocked,
                            DateUnlocked = x.DateUnlocked,
                            Percent = x.Percent
                        }).ToList();
                        gameAchievements.Items = AllAchievements;
                    }

                    // Set source link
                    if (gameAchievements.HasAchievements)
                    {
                        gameAchievements.SourcesLink = OriginAPI.GetAchievementsSourceLink(game.Name, gameInfos.Id, OriginAPI.CurrentAccountInfos);
                    }
                }
                catch (Exception ex)
                {
                    ShowNotificationPluginError(ex);
                    return gameAchievements;
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsOriginNoAuthenticate"), ExternalPlugin.OriginLibrary);
            }

            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            if (PlayniteTools.IsDisabledPlaynitePlugins("OriginLibrary"))
            {
                ShowNotificationPluginDisable(resources.GetString("LOCSuccessStoryNotificationsOriginDisabled"));
                return false;
            }
            else
            {
                if (CachedConfigurationValidationResult == null)
                {
                    CachedConfigurationValidationResult = IsConnected();

                    if (!(bool)CachedConfigurationValidationResult)
                    {
                        ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsOriginNoAuthenticate"), ExternalPlugin.OriginLibrary);
                    }
                }
                else if (!(bool)CachedConfigurationValidationResult)
                {
                    ShowNotificationPluginErrorMessage();
                }

                return (bool)CachedConfigurationValidationResult;
            }
        }

        public override bool IsConnected()
        {
            if (CachedIsConnectedResult == null)
            {
                try
                {
                    CachedIsConnectedResult = OriginAPI.IsUserLoggedIn;
                }
                catch (Exception ex)
                {
                    CachedIsConnectedResult = false;
                }
            }
            
            return (bool)CachedIsConnectedResult;
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableOrigin;
        }

        public override void ResetCachedConfigurationValidationResult()
        {
            CachedConfigurationValidationResult = null;
            OriginAPI.ResetIsUserLoggedIn();
        }

        public override void ResetCachedIsConnectedResult()
        {
            CachedIsConnectedResult = null;
            OriginAPI.ResetIsUserLoggedIn();
        }
        #endregion
    }
}
