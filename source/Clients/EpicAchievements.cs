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
    public class EpicAchievements : GenericAchievements
    {
        private EpicApi EpicApi => SuccessStory.EpicApi;


        public EpicAchievements() : base("Epic", CodeLang.GetEpicLang(API.Instance.ApplicationSettings.Language), CodeLang.GetGogLang(API.Instance.ApplicationSettings.Language))
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();

            if (IsConnected())
            {
                try
                {
                    string productSlug = string.Empty;
                    string gameNameApostrophe = game.Name.Replace("'", "");
                    game.Links?.ForEach(x =>
                    {
                        productSlug = EpicApi.GetProductSlugByUrl(x.Url, gameNameApostrophe).IsNullOrEmpty() ? productSlug : EpicApi.GetProductSlugByUrl(x.Url, gameNameApostrophe);
                    });

                    if (productSlug.IsNullOrEmpty())
                    {
                        productSlug = EpicApi.GetProductSlug(PlayniteTools.NormalizeGameName(gameNameApostrophe));
                    }

                    if (productSlug.IsNullOrEmpty())
                    {
                        Logger.Warn($"No ProductSlug for {game.Name}");
                        return null;
                    }

                    string nameSpace = EpicApi.GetNameSpace(NormalizeGameName(gameNameApostrophe), productSlug);
                    if (nameSpace.IsNullOrEmpty())
                    {
                        Logger.Warn($"No NameSpace for the Epic game {game.Name}");
                    }
                    else
                    {
                        ObservableCollection<GameAchievement> epicAchievements = EpicApi.GetAchievements(nameSpace, EpicApi.CurrentAccountInfos);
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
                                Percent = x.Percent,
                                GamerScore = x.GamerScore
                            }).ToList();
                            gameAchievements.Items = AllAchievements;
                        }
                        else
                        {
                            if (!EpicApi.IsUserLoggedIn)
                            {
                                ShowNotificationPluginNoAuthenticate(ResourceProvider.GetString("LOCSuccessStoryNotificationsEpicNoAuthenticate"), ExternalPlugin.EpicLibrary);
                            }
                        }
                    }

                    // Set source link
                    if (gameAchievements.HasAchievements)
                    {
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
