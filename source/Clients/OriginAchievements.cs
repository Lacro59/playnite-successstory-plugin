using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPluginsShared;
using CommonPlayniteShared.PluginLibrary.OriginLibrary.Models;
using CommonPlayniteShared.PluginLibrary.OriginLibrary.Services;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using CommonPluginsShared.Models;
using static CommonPluginsShared.PlayniteTools;

namespace SuccessStory.Clients
{
    class OriginAchievements : GenericAchievements
    {
        protected static OriginAccountClient _OriginAPI;
        internal static OriginAccountClient OriginAPI
        {
            get
            {
                if (_OriginAPI == null)
                {
                    _OriginAPI = new OriginAccountClient(WebViewOffscreen);
                }
                return _OriginAPI;
            }

            set
            {
                _OriginAPI = value;
            }
        }

        private AuthTokenResponse _token;
        private AuthTokenResponse token
        {
            get
            {
                if (_token == null)
                {
                    _token = OriginAPI.GetAccessToken();
                }
                return _token;
            }
        }

        private const string UrlPersonas = @"https://gateway.ea.com/proxy/identity/pids/{0}/personas?namespaceName=cem_ea_id";
        private const string UrlGameStoreData = @"https://api2.origin.com/ecommerce2/public/supercat/{0}/{1}?country={2}";
        private const string UrlAchievements = @"https://achievements.gameservices.ea.com/achievements/personas/{0}/{1}/all?lang={2}&metadata=true&fullset=true";


        public OriginAchievements() : base("Origin", CodeLang.GetOriginLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language), CodeLang.GetOriginLangCountry(PluginDatabase.PlayniteApi.ApplicationSettings.Language))
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();


            if (IsConnected())
            {
                // Get informations from Origin plugin.
                string accessToken = token.access_token;
                string personasId = GetPersonas(token);
                string origineGameId = GetOrigineGameAchievementId(game.Id);

                if (personasId.IsNullOrEmpty())
                {
                    logger.Warn("No personasId");
                    gameAchievements.Items = AllAchievements;
                    return gameAchievements;
                }

                if (origineGameId.IsNullOrEmpty())
                {
                    logger.Warn($"No origineGameId for {game.Name}");
                    gameAchievements.Items = AllAchievements;
                    return gameAchievements;
                }

                // Achievements (default return in english)
                string Url = string.Format(UrlAchievements, personasId, origineGameId, LocalLang);
                using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
                {
                    try
                    {
                        webClient.Headers.Add("X-AuthToken", accessToken);
                        webClient.Headers.Add("accept", "application/vnd.origin.v3+json; x-cache/force-write");

                        string DownloadString = webClient.DownloadString(Url);
                        dynamic AchievementsData = Serialization.FromJson<dynamic>(DownloadString);

                        foreach (var item in AchievementsData["achievements"])
                        {
                            AllAchievements.Add(new Achievements
                            {
                                Name = (string)item.Value["name"],
                                Description = (string)item.Value["desc"],
                                UrlUnlocked = (string)item.Value["icons"]["208"],
                                UrlLocked = string.Empty,
                                DateUnlocked = ((string)item.Value["state"]["a_st"] == "ACTIVE") ? default(DateTime) : new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((int)item.Value["u"]).ToLocalTime(),
                                Percent = (float)item.Value["achievedPercentage"]
                            });
                        }
                    }
                    catch (WebException ex)
                    {
                        if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                        {
                            var resp = (HttpWebResponse)ex.Response;
                            switch (resp.StatusCode)
                            {
                                case HttpStatusCode.NotFound: // HTTP 404
                                    break;
                                default:
                                    ShowNotificationPluginWebError(ex, Url);
                                    break;
                            }

                            return gameAchievements;
                        }
                    }
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsOriginNoAuthenticate"), ExternalPlugin.OriginLibrary);
            }


            gameAchievements.Items = AllAchievements;


            // Set source link
            if (gameAchievements.HasAchivements)
            {
                string LangUrl = CodeLang.GetEpicLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language);

                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = gameAchievements.Name,
                    Name = "Origin",
                    Url = $"https://www.origin.com/fra/{LangUrl}/game-library/ogd/{game.GameId}/achievements"
                };
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
                    CachedIsConnectedResult = OriginAPI.GetIsUserLoggedIn();
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
        #endregion


        #region Origin
        /// <summary>
        /// Get usersId for achievement database.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private string GetPersonas(AuthTokenResponse token)
        {
            var client = new WebClient { Encoding = Encoding.UTF8 };
            var userId = OriginAPI.GetAccountInfo(OriginAPI.GetAccessToken()).pid.pidId;
            var url = string.Format(UrlPersonas, userId);

            client.Headers.Add("Authorization", token.token_type + " " + token.access_token);
            var stringData = client.DownloadString(url);

            dynamic objectData = Serialization.FromJson<dynamic>(stringData);

            return ((string)objectData["personas"]["personaUri"][0]).Replace("/pids/" + userId + "/personas/", string.Empty);
        }

        /// <summary>
        /// Get Origin gameId for achievement database.
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        private string GetOrigineGameAchievementId(Guid Id)
        {
            string GameId = PluginDatabase.PlayniteApi.Database.Games.Get(Id).GameId;
            GameStoreDataResponse StoreDetails = GetGameStoreData(GameId);
            return StoreDetails.platforms[0].achievementSetOverride;
        }

        /// <summary>
        /// Get game data from Origin.
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
        private GameStoreDataResponse GetGameStoreData(string gameId)
        {
            string url = string.Format(UrlGameStoreData, gameId, LocalLang, LocalLangShort);
            string stringData = Web.DownloadStringData(url).GetAwaiter().GetResult();
            return Serialization.FromJson<GameStoreDataResponse>(stringData);
        }
        #endregion
    }


    public class GameStoreDataResponse
    {
        public class I18n
        {
            public string longDescription;
            public string officialSiteURL;
            public string gameForumURL;
            public string displayName;
            public string packArtSmall;
            public string packArtMedium;
            public string packArtLarge;
            public string gameManualURL;
        }

        public class Platform
        {
            public string platform;
            public string multiplayerId;
            public DateTime releaseDate;
            // Add Orign game identifier for achievement.
            public string achievementSetOverride;
        }

        public string offerId;
        public string offerType;
        public string masterTitleId;
        public List<Platform> platforms;
        public string publisherFacetKey;
        public string developerFacetKey;
        public string genreFacetKey;
        public string imageServer;
        public string itemName;
        public string itemType;
        public string itemId;
        public I18n i18n;
        public string offerPath;
    }
}
