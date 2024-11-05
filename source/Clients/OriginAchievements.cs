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
using Playnite.SDK;

namespace SuccessStory.Clients
{
    public class OriginAchievements : GenericAchievements
    {
        protected static readonly Lazy<OriginApi> originApi = new Lazy<OriginApi>(() => new OriginApi(PluginDatabase.PluginName));
        internal static OriginApi OriginApi => originApi.Value;


        public OriginAchievements() : base("EA", CodeLang.GetOriginLang(API.Instance.ApplicationSettings.Language), CodeLang.GetOriginLangCountry(API.Instance.ApplicationSettings.Language))
        {
            OriginApi.SetLanguage(API.Instance.ApplicationSettings.Language);
        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();

            if (IsConnected())
            {
                try
                {
                    GameInfos gameInfos = OriginApi.GetGameInfos(game.GameId, null);
                    if (gameInfos == null)
                    {
                        Logger.Warn($"No gameInfos for {game.GameId}");
                        return null;
                    }

                    ObservableCollection<GameAchievement> originAchievements = OriginApi.GetAchievements(gameInfos.Id2, OriginApi.CurrentAccountInfos);
                    if (originAchievements?.Count > 0)
                    {
                        AllAchievements = originAchievements.Select(x => new Achievements
                        {
                            ApiName = x.Id,
                            Name = x.Name,
                            Description = x.Description,
                            UrlUnlocked = x.UrlUnlocked,
                            UrlLocked = x.UrlLocked,
                            DateUnlocked = x.DateUnlocked.ToString().Contains(default(DateTime).ToString()) ? (DateTime?)null : x.DateUnlocked,
                            Percent = x.Percent,
                            GamerScore = x.GamerScore
                        }).ToList();
                        gameAchievements.Items = AllAchievements;
                    }

                    // Set source link
                    if (gameAchievements.HasAchievements)
                    {
                        gameAchievements.SourcesLink = OriginApi.GetAchievementsSourceLink(game.Name, gameInfos.Id, OriginApi.CurrentAccountInfos);
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
                ShowNotificationPluginNoAuthenticate(ExternalPlugin.OriginLibrary);
            }

            gameAchievements.SetRaretyIndicator();
            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            if (!PluginDatabase.PluginSettings.Settings.PluginState.OriginIsEnabled)
            {
                ShowNotificationPluginDisable(ResourceProvider.GetString("LOCSuccessStoryNotificationsOriginDisabled"));
                return false;
            }
            else
            {
                if (CachedConfigurationValidationResult == null)
                {
                    CachedConfigurationValidationResult = IsConnected();

                    if (!(bool)CachedConfigurationValidationResult)
                    {
                        ShowNotificationPluginNoAuthenticate(ExternalPlugin.OriginLibrary);
                    }
                }
                else if (!(bool)CachedConfigurationValidationResult)
                {
                    ShowNotificationPluginErrorMessage(ExternalPlugin.OriginLibrary);
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
                    CachedIsConnectedResult = OriginApi.IsUserLoggedIn;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, true);
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
            OriginApi.ResetIsUserLoggedIn();
        }

        public override void ResetCachedIsConnectedResult()
        {
            CachedIsConnectedResult = null;
            OriginApi.ResetIsUserLoggedIn();
        }
        #endregion
    }
}
