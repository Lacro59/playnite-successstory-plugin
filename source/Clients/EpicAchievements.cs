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
using System.Diagnostics;
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
            var swOverall = Stopwatch.StartNew();
            Logger.Info($"Epic.GetAchievements START - {game.Name}");
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> AllAchievements = new List<Achievement>();

            if (IsConnected())
            {
                try
                {
                    var assets = EpicApi.GetAssets();

                    var asset = assets.FirstOrDefault(x => x.AppName.IsEqual(game.GameId));
                    string targetNamespace = asset?.Namespace ?? game.GameId;

                    if (asset == null)
                    {
                        Logger.Warn($"No asset for the Epic game {game.Name}. Using GameId {game.GameId} as namespace.");
                    }

                    var epicAchievements = EpicApi.GetAchievements(targetNamespace, EpicApi.CurrentAccountInfos);
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
                        Logger.Debug($"No achievements found for {game.Name} on Epic");
                    }

                    // Set source link
                    if (gameAchievements.HasAchievements && gameAchievements.SourcesLink == null)
                    {
                        var swSlug = Stopwatch.StartNew();
                        string productSlug = EpicApi.GetProductSlug(targetNamespace);
                        swSlug.Stop();
                        Logger.Debug($"Epic.GetProductSlug: {swSlug.ElapsedMilliseconds}ms");
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
                return gameAchievements;
            }

            gameAchievements.SetRaretyIndicator();
            PluginDatabase.AddOrUpdate(gameAchievements);

            swOverall.Stop();
            Logger.Info($"Epic.GetAchievements STOP - {game.Name} - {swOverall.ElapsedMilliseconds}ms");
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