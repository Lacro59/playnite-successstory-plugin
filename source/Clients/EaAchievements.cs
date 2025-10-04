using Playnite.SDK.Models;
using CommonPluginsShared;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static CommonPluginsShared.PlayniteTools;
using CommonPluginsStores.Ea;
using System.Collections.ObjectModel;
using CommonPluginsStores.Models;
using System.Linq;
using Playnite.SDK;

namespace SuccessStory.Clients
{
    public class EaAchievements : GenericAchievements
    {
        protected static readonly Lazy<EaApi> eaApi = new Lazy<EaApi>(() => new EaApi(PluginDatabase.PluginName));
        internal static EaApi EaApi => eaApi.Value;


        public EaAchievements() : base("EA", CodeLang.GetEaLang(API.Instance.ApplicationSettings.Language), CodeLang.GetCountryFromLast(API.Instance.ApplicationSettings.Language))
        {
            EaApi.SetLanguage(API.Instance.ApplicationSettings.Language);
        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> AllAchievements = new List<Achievement>();

            if (IsConnected())
            {
                try
                {
                    GameInfos gameInfos = EaApi.GetGameInfos(game.GameId, null);
                    if (gameInfos == null)
                    {
                        Logger.Warn($"No gameInfos for {game.GameId}");
                        return null;
                    }

                    ObservableCollection<GameAchievement> originAchievements = EaApi.GetAchievements(gameInfos.Id2, EaApi.CurrentAccountInfos);
                    if (originAchievements?.Count > 0)
                    {
                        AllAchievements = originAchievements.Select(x => new Achievement
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
                        gameAchievements.SourcesLink = EaApi.GetAchievementsSourceLink(game.Name, gameInfos.Id, EaApi.CurrentAccountInfos);
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
                ShowNotificationPluginDisable(ResourceProvider.GetString("LOCSuccessStoryNotificationsEaDisabled"));
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
                    CachedIsConnectedResult = EaApi.IsUserLoggedIn;
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
            EaApi.ResetIsUserLoggedIn();
        }

        public override void ResetCachedIsConnectedResult()
        {
            CachedIsConnectedResult = null;
            EaApi.ResetIsUserLoggedIn();
        }
        #endregion
    }
}
