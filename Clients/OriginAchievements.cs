using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OriginLibrary.Models;
using OriginLibrary.Services;
using Playnite.Common.Web;
using Playnite.SDK;
using SuccessStory.Database;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SuccessStory.Clients
{
    class OriginAchievements
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        OriginAccountClient originAPI;

        /// <summary>
        /// Get all achievements for a origin game.
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        public GameAchievements GetAchievements(IPlayniteAPI PlayniteApi, Guid Id)
        {
            GameAchievements Result = new GameAchievements();

            List<Achievements> Achievements = new List<Achievements>();
            string GameName = PlayniteApi.Database.Games.Get(Id).Name;
            bool HaveAchivements = false;
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            var view = PlayniteApi.WebViews.CreateOffscreenView();
            originAPI = new OriginAccountClient(view);

            // Only if user is logged. 
            if (originAPI.GetIsUserLoggedIn())
            {
                string accessToken = originAPI.GetAccessToken().access_token;
                string personasId = GetPersonas(originAPI.GetAccessToken());
                string origineGameId = GetOrigineGameAchievementId(PlayniteApi, Id);
                string lang = resources.GetString("LOCLanguageCode");

                //string[] arrayLang = { "ar_SA", "ca_ES", "cs_CZ", "da_DK", "de_DE", "el_GR", "en_US", "es_ES", "fi_FL","fr_FR",
                //    "hu_HU", "it_IT", "ja_JP", "nl_NL", "no_NO", "pl_PL", "pt_BR", "pt_PT", "ro_RO","ru_RU", "sv_SE",
                //    "zh_CN", "zh_TW" };
                //if (!arrayLang.ContainsString(lang))
                //{
                //    lang = "en";
                //}

                // Achievements
                var url = string.Format(@"https://achievements.gameservices.ea.com/achievements/personas/{0}/{1}/all?lang={2}&metadata=true&fullset=true",
                    personasId, origineGameId, lang);

                logger.Debug($"SuccessStory - Origin.GetAchievements {url}");
                logger.Debug($"SuccessStory - Origin.GetAchievements {accessToken}");

                using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
                {
                    try
                    {
                        webClient.Headers.Add("X-AuthToken", accessToken);
                        webClient.Headers.Add("accept", "application/vnd.origin.v3+json; x-cache/force-write");

                        var stringData = webClient.DownloadString(url);

                        JObject AchievementsData = JObject.Parse(stringData);

                        foreach (var item in (JObject)AchievementsData["achievements"])
                        {
                            var val = item.Value;
                            HaveAchivements = true;

                            Achievements.Add(new Achievements
                            {
                                Name = (string)item.Value["name"],
                                Description = (string)item.Value["desc"],
                                UrlUnlocked = (string)item.Value["icons"]["208"],
                                UrlLocked = "",
                                DateUnlocked = ((string)item.Value["state"]["a_st"] == "ACTIVE") ? default(DateTime) : new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((int)item.Value["u"])
                            });

                            Total += 1;
                            if ((string)item.Value["state"]["a_st"] == "ACTIVE")
                            {
                                Locked += 1;
                            }
                            else
                            {
                                Unlocked += 1;
                            }
                        }
                    }
                    catch (WebException e)
                    {
                        if (e.Status == WebExceptionStatus.ProtocolError && e.Response != null)
                        {
                            var resp = (HttpWebResponse)e.Response;
                            switch (resp.StatusCode)
                            {
                                case HttpStatusCode.NotFound: // HTTP 404
                                    break;
                                default:
                                    logger.Error(e, $"SuccessStory - Failed to load from {url}");
                                    //PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error on OriginAchievements");
                                    AchievementsDatabase.ListErrors.Add("Error on OriginAchievements: " + e.Message);
                                    break;
                            }
                        }
                    }

                    webClient.Dispose();
                }
            }

            Result = new GameAchievements
            {
                Name = GameName,
                HaveAchivements = HaveAchivements,
                Total = Total,
                Unlocked = Unlocked,
                Locked = Locked,
                Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0,
                Achievements = Achievements
            };

            return Result;
        }

        /// <summary>
        /// Get usersId for achievement database.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        internal string GetPersonas(AuthTokenResponse token)
        {
            var client = new WebClient { Encoding = Encoding.UTF8 };
            var userId = originAPI.GetAccountInfo(originAPI.GetAccessToken()).pid.pidId;
            var url = string.Format(@"https://gateway.ea.com/proxy/identity/pids/{0}/personas?namespaceName=cem_ea_id", userId);

            logger.Debug($"SuccessStory - Origin.GetPersonas {url}");

            client.Headers.Add("Authorization", token.token_type + " " + token.access_token);
            var stringData = client.DownloadString(url);

            JObject objectData = JObject.Parse(stringData);

            return ((string)objectData["personas"]["personaUri"][0]).Replace("/pids/" + userId + "/personas/", "");
        }

        /// <summary>
        /// Get Origin gameId for achievement database.
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        internal string GetOrigineGameAchievementId(IPlayniteAPI PlayniteApi, Guid Id)
        {
            string GameId = PlayniteApi.Database.Games.Get(Id).GameId;
            GameStoreDataResponse StoreDetails = GetGameStoreData(GameId);

            return StoreDetails.platforms[0].achievementSetOverride;
        }

        internal static GameStoreDataResponse GetGameStoreData(string gameId)
        {
            string lang = resources.GetString("LOCLanguageCode");
            string langShort = resources.GetString("LOCLanguageCountry");
            var url = string.Format(@"https://api2.origin.com/ecommerce2/public/supercat/{0}/{1}?country={2}", gameId, lang, langShort);

            logger.Debug($"SuccessStory - Origin.GameStoreDataResponse {url}");

            var stringData = Encoding.UTF8.GetString(HttpDownloader.DownloadData(url));
            return JsonConvert.DeserializeObject<GameStoreDataResponse>(stringData);
        }
    }


    /// <summary>
    /// Add achievementSetOverride from original class.
    /// </summary>
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
