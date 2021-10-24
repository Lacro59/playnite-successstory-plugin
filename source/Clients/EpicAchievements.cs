using CommonPlayniteShared.Common;
using CommonPlayniteShared.PluginLibrary.EpicLibrary;
using CommonPlayniteShared.PluginLibrary.EpicLibrary.Services;
using CommonPluginsShared;
using CommonPluginsShared.Models;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    class EpicAchievements : GenericAchievements
    {
        protected static EpicAccountClient _EpicAPI;
        internal static EpicAccountClient EpicAPI
        {
            get
            {
                if (_EpicAPI == null)
                {
                    _EpicAPI = new EpicAccountClient(
                        PluginDatabase.PlayniteApi, 
                        PluginDatabase.Paths.PluginUserDataPath + "\\..\\00000002-DBD1-46C6-B5D0-B1BA559D10E4\\tokens.json"
                    );
                }
                return _EpicAPI;
            }

            set
            {
                _EpicAPI = value;
            }
        }

        private const string UrlAchievements = @"https://www.epicgames.com/store/{0}/achievements/{1}";


        public EpicAchievements() : base("Epic", CodeLang.GetGogLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language))
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();


            string Url = string.Empty;
            string ResultWeb = string.Empty;


            if (IsConnected())
            {
                try
                {
                    var tokens = EpicAPI.loadTokens();

                    string ProductSlug = GetProductSlug(game.Name);
                    Url = string.Format(UrlAchievements, LocalLang, ProductSlug);
                    
                    ResultWeb = Web.DownloadStringData(Url, tokens.access_token).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404"))
                    {
                        logger.Warn($"Error 404 for {game.Name}");
                    }
                    else
                    {
                        ShowNotificationPluginError(ex);
                    }

                    return gameAchievements;
                }

                if (ResultWeb != string.Empty && !ResultWeb.Contains("<span>404</span>", StringComparison.InvariantCultureIgnoreCase))
                {
                    try
                    {
                        int indexStart = ResultWeb.IndexOf("window.__REACT_QUERY_INITIAL_QUERIES__ =");
                        int indexEnd = ResultWeb.IndexOf("window.server_rendered");

                        int length = ResultWeb.Length 
                            - (indexStart + "window.__REACT_QUERY_INITIAL_QUERIES__ =".Length)
                            - (ResultWeb.Length - indexEnd);


                        string JsonDataString = ResultWeb.Substring(
                            indexStart + "window.__REACT_QUERY_INITIAL_QUERIES__ =".Length,
                            length
                        );


                        indexEnd = JsonDataString.IndexOf(";");
                        length = JsonDataString.Length - (JsonDataString.Length - indexEnd);
                        JsonDataString = JsonDataString.Substring(0, length);


                        EpicData epicData = Serialization.FromJson<EpicData>(JsonDataString);


                        // Achievements data
                        var achievemenstData = epicData.queries
                                    .Where(x => (Serialization.ToJson(x.state.data)).Contains("\"achievements\":[{\"achievement\"", StringComparison.InvariantCultureIgnoreCase))
                                    .FirstOrDefault();

                        EpicAchievementsData epicAchievementsData = Serialization.FromJson<EpicAchievementsData>(Serialization.ToJson(achievemenstData.state.data));

                        if (epicAchievementsData != null && epicAchievementsData.Achievement.productAchievementsRecordBySandbox.achievements?.Count > 0)
                        {
                            foreach (var ach in epicAchievementsData.Achievement.productAchievementsRecordBySandbox.achievements)
                            {
                                Achievements temp = new Achievements
                                {
                                    ApiName = ach.achievement.name,
                                    Name = ach.achievement.unlockedDisplayName,
                                    Description = ach.achievement.unlockedDescription,
                                    UrlUnlocked = ach.achievement.unlockedIconLink,
                                    UrlLocked = ach.achievement.lockedIconLink,
                                    DateUnlocked = default(DateTime),
                                    Percent = ach.achievement.rarity.percent
                                };

                                AllAchievements.Add(temp);
                            }
                        }

                        // Owned achievement
                        var achievemenstOwnedData = epicData.queries
                            .Where(x => (Serialization.ToJson(x.state.data)).Contains("\"playerAchievements\":[{\"playerAchievement\"", StringComparison.InvariantCultureIgnoreCase))
                            .FirstOrDefault();

                        if (achievemenstOwnedData != null)
                        {
                            EpicAchievementsOwnedData epicAchievementsOwnedData = Serialization.FromJson<EpicAchievementsOwnedData>(Serialization.ToJson(achievemenstOwnedData.state.data));

                            if (epicAchievementsOwnedData != null && epicAchievementsOwnedData.PlayerAchievement.playerAchievementGameRecordsBySandbox.records.FirstOrDefault()?.playerAchievements?.Count() > 0)
                            {
                                foreach (var ach in epicAchievementsOwnedData.PlayerAchievement.playerAchievementGameRecordsBySandbox.records.FirstOrDefault().playerAchievements)
                                {
                                    var owned = AllAchievements.Find(x => x.ApiName == ach.playerAchievement.achievementName);
                                    if (owned != null)
                                    {
                                        owned.DateUnlocked = ach.playerAchievement.unlockDate;
                                    }
                                    else
                                    {
                                    }
                                }
                            }
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
                    logger.Warn($"Error 404 for {game.Name}");
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsGogNoAuthenticate"));
            }


            gameAchievements.Items = AllAchievements;


            // Set source link
            if (gameAchievements.HasAchivements)
            {
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = gameAchievements.Name,
                    Name = "Epic",
                    Url = Url
                };
            }


            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            if (PlayniteTools.IsDisabledPlaynitePlugins("EpicLibrary"))
            {
                ShowNotificationPluginDisable(resources.GetString("LOCSuccessStoryNotificationsEpicDisabled"));
                return false;
            }
            else
            {
                if (CachedConfigurationValidationResult == null)
                {
                    CachedConfigurationValidationResult = IsConnected();

                    if (!(bool)CachedConfigurationValidationResult)
                    {
                        ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsEpicNoAuthenticate"));
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
                CachedIsConnectedResult = EpicAPI.GetIsUserLoggedIn();
            }

            return (bool)CachedIsConnectedResult;
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableEpic;
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
                    var catalog = catalogs.FirstOrDefault(a => a.title.Equals(Name, StringComparison.InvariantCultureIgnoreCase));
                    if (catalog == null)
                    {
                        catalog = catalogs[0];
                    }

                    ProductSlug = catalog.productSlug;
                }
            }

            return ProductSlug;
        }
        #endregion
    }
}
