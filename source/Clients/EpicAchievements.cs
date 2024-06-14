using CommonPlayniteShared.PluginLibrary.EpicLibrary.Services;
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
    class EpicAchievements : GenericAchievements
    {
        protected static EpicApi epicAPI;
        internal static EpicApi EpicAPI
        {
            get
            {
                if (epicAPI == null)
                {
                    epicAPI = new EpicApi(PluginDatabase.PluginName);
                }
                return epicAPI;
            }

            set => epicAPI = value;
        }


        public EpicAchievements() : base("Epic", CodeLang.GetEpicLang(API.Instance.ApplicationSettings.Language), CodeLang.GetGogLang(API.Instance.ApplicationSettings.Language))
        {
            EpicAPI.SetLanguage(API.Instance.ApplicationSettings.Language);
        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();

            if (IsConnected())
            {
                try
                {
                    ObservableCollection<GameAchievement> epicAchievements = EpicAPI.GetAchievements(game.Name, EpicAPI.CurrentAccountInfos);
                    if (epicAchievements?.Count > 0)
                    {
                        AllAchievements = epicAchievements.Select(x => new Achievements
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
                    else
                    {
                        if (!EpicAPI.IsUserLoggedIn)
                        {
                            ShowNotificationPluginNoAuthenticate(ResourceProvider.GetString("LOCSuccessStoryNotificationsEpicNoAuthenticate"), ExternalPlugin.EpicLibrary);
                        }
                    }

                    // Set source link
                    if (gameAchievements.HasAchievements)
                    {
                        gameAchievements.SourcesLink = EpicAPI.GetAchievementsSourceLink(game.Name, game.GameId, EpicAPI.CurrentAccountInfos);
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
                ShowNotificationPluginNoAuthenticate(ResourceProvider.GetString("LOCSuccessStoryNotificationsEpicNoAuthenticate"), ExternalPlugin.EpicLibrary);
            }

            gameAchievements.SetRaretyIndicator();
            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            if (PlayniteTools.IsDisabledPlaynitePlugins("EpicLibrary"))
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
                        ShowNotificationPluginNoAuthenticate(ResourceProvider.GetString("LOCSuccessStoryNotificationsEpicNoAuthenticate"), ExternalPlugin.EpicLibrary);
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
                CachedIsConnectedResult = EpicAPI.IsUserLoggedIn;
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
            EpicAPI.ResetIsUserLoggedIn();
        }

        public override void ResetCachedIsConnectedResult()
        {
            CachedIsConnectedResult = null;
            EpicAPI.ResetIsUserLoggedIn();
        }
        #endregion


        #region Epic
        private string GetProductSlug(string Name)
        {
            string ProductSlug = string.Empty;
            using (var client = new WebStoreClient())
            {
                var catalogs = client.QuerySearch(Name).GetAwaiter().GetResult();
                if (catalogs.HasItems())
                {
                    var catalog = catalogs.FirstOrDefault(a => a.title.IsEqual(Name, true));
                    if (catalog == null)
                    {
                        catalog = catalogs[0];
                    }

                    ProductSlug = catalog?.productSlug?.Replace("/home", string.Empty);
                    if (ProductSlug.IsNullOrEmpty())
                    {
                        Logger.Warn($"No ProductSlug for {Name}");
                    }
                }
            }
            return ProductSlug;
        }
        #endregion
    }
}
