using Playnite.SDK.Models;
using CommonPluginsShared;
using SuccessStory.Models;
using System;
using System.Linq;
using static CommonPluginsShared.PlayniteTools;
using CommonPluginsStores.Gog;
using System.Collections.ObjectModel;
using CommonPluginsStores.Models;
using System.Collections.Generic;
using Playnite.SDK;

namespace SuccessStory.Clients
{
    public class GogAchievements : GenericAchievements
    {
        private GogApi GogApi => SuccessStory.GogApi;


        public GogAchievements() : base("GOG", CodeLang.GetGogLang(API.Instance.ApplicationSettings.Language))
        {
            GogApi.SetLanguage(API.Instance.ApplicationSettings.Language);
        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> AllAchievements = new List<Achievement>();

            if (IsConnected())
            {
                try
                {
                    ObservableCollection<GameAchievement> gogAchievements = GogApi.GetAchievements(game.GameId, GogApi.CurrentAccountInfos);
                    if (gogAchievements?.Count > 0)
                    {
                        AllAchievements = gogAchievements.Select(x => new Achievement
                        {
                            ApiName = x.Id,
                            Name = x.Name,
                            Description = x.Description,
                            UrlUnlocked = x.UrlUnlocked,
                            UrlLocked = x.UrlLocked,
                            DateUnlocked = x.DateUnlocked.ToString().Contains(default(DateTime).ToString()) ? (DateTime?)null : x.DateUnlocked,
                            Percent = x.Percent,
                            GamerScore = x.GamerScore,
                            IsHidden = x.IsHidden
                        }).ToList();
                        gameAchievements.Items = AllAchievements;
                    }
                    else
                    {
                        if (!GogApi.IsUserLoggedIn)
                        {
                            ShowNotificationPluginNoAuthenticate(ExternalPlugin.SuccessStory);
                        }
                    }

                    // Set source link
                    if (gameAchievements.HasAchievements)
                    {
                        gameAchievements.SourcesLink = GogApi.GetAchievementsSourceLink(game.Name, game.GameId, GogApi.CurrentAccountInfos);
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
                ShowNotificationPluginNoAuthenticate(ExternalPlugin.SuccessStory);
            }

            gameAchievements.SetRaretyIndicator();
            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            if (!PluginDatabase.PluginSettings.Settings.PluginState.GogIsEnabled)
            {
                ShowNotificationPluginDisable(ResourceProvider.GetString("LOCSuccessStoryNotificationsGogDisabled"));
                return false;
            }
            else
            {
                if (CachedConfigurationValidationResult == null)
                {
                    CachedConfigurationValidationResult = IsConnected();

                    if (!(bool)CachedConfigurationValidationResult)
                    {
                        ShowNotificationPluginNoAuthenticate(ExternalPlugin.SuccessStory);
                    }
                    else
                    {
                        CachedConfigurationValidationResult = IsConfigured();

                        if (!(bool)CachedConfigurationValidationResult)
                        {
                            ShowNotificationPluginNoConfiguration();
                        }
                    }
                }
                else if (!(bool)CachedConfigurationValidationResult)
                {
                    ShowNotificationPluginErrorMessage(ExternalPlugin.SuccessStory);
                }

                return (bool)CachedConfigurationValidationResult;
            }
        }

        public override bool IsConnected()
        {
            if (CachedIsConnectedResult == null)
            {
                CachedIsConnectedResult = GogApi.IsUserLoggedIn;
            }

            return (bool)CachedIsConnectedResult;
        }

        public override bool IsConfigured()
        {
            return IsConnected();
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableGog;
        }

        public override void ResetCachedConfigurationValidationResult()
        {
            CachedConfigurationValidationResult = null;
            GogApi.ResetIsUserLoggedIn();
        }

        public override void ResetCachedIsConnectedResult()
        {
            CachedIsConnectedResult = null;
            GogApi.ResetIsUserLoggedIn();
        }
        #endregion
    }
}
