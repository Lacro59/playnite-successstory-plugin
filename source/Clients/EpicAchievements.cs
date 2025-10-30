using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsStores.Epic;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static CommonPluginsShared.PlayniteTools;

namespace SuccessStory.Clients
{
    public class EpicAchievements : GenericAchievements
    {
        private EpicApi EpicApi => SuccessStory.EpicApi;


        public EpicAchievements() : base("Epic", CodeLang.GetEpicLang(API.Instance.ApplicationSettings.Language), CodeLang.GetGogLang(API.Instance.ApplicationSettings.Language))
        {

        }

        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> AllAchievements = new List<Achievement>();

            if (IsConnected())
            {
                try
                {
                    var assets = EpicApi.GetAssets();
                    var asset = assets.FirstOrDefault(x => x.AppName.IsEqual(game.GameId));
                    if (asset == null)
                    {
                        Logger.Warn($"No asset for the Epic game {game.Name}");
                        return gameAchievements;
                    }

                    ObservableCollection<GameAchievement> epicAchievements = EpicApi.GetAchievements(asset.Namespace, EpicApi.CurrentAccountInfos);
                    if (epicAchievements?.Count > 0)
                    {
                        AllAchievements = epicAchievements.Select(x => new Achievement
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
                    else
                    {
                        if (!EpicApi.IsUserLoggedIn)
                        {
                            ShowNotificationPluginNoAuthenticate(ExternalPlugin.SuccessStory);
                        }
                    }

                    // Set source link
                    if (gameAchievements.HasAchievements)
                    {
                        string productSlug = EpicApi.GetProductSlug(asset.Namespace);
                        gameAchievements.SourcesLink = EpicApi.GetAchievementsSourceLink(game.Name, productSlug, EpicApi.CurrentAccountInfos);
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
            if (!PluginDatabase.PluginSettings.Settings.PluginState.EpicIsEnabled)
            {
                ShowNotificationPluginDisable(ResourceProvider.GetString("LOCSuccessStoryNotificationsEpicDisabled"));
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
                CachedIsConnectedResult = EpicApi.IsUserLoggedIn;
            }

            return (bool)CachedIsConnectedResult;
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableEpic;
        }

        public override void ResetCachedConfigurationValidationResult()
        {
            CachedConfigurationValidationResult = null;
            EpicApi.ResetIsUserLoggedIn();
        }

        public override void ResetCachedIsConnectedResult()
        {
            CachedIsConnectedResult = null;
            EpicApi.ResetIsUserLoggedIn();
        }

        #endregion
    }
}