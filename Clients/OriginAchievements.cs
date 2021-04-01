using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPluginsShared;
using CommonPluginsPlaynite.PluginLibrary.OriginLibrary.Models;
using CommonPluginsPlaynite.PluginLibrary.OriginLibrary.Services;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace SuccessStory.Clients
{
    class OriginAchievements : GenericAchievements
    {
        OriginAccountClient originAPI;

        public OriginAchievements(IPlayniteAPI PlayniteApi, SuccessStorySettings settings, string PluginUserDataPath) : base(PlayniteApi, settings, PluginUserDataPath)
        {
            var view = PlayniteApi.WebViews.CreateOffscreenView();
            originAPI = new OriginAccountClient(view);
        }


        /// <summary>
        /// Get all achievements for a Origin game.
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        public override GameAchievements GetAchievements(Game game)
        {
            List<Achievements> AllAchievements = new List<Achievements>();
            string GameName = game.Name;
            bool HaveAchivements = false;
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            GameAchievements Result = SuccessStory.PluginDatabase.GetDefault(game);
            Result.Items = AllAchievements;

            // Only if user is logged. 
            if (originAPI.GetIsUserLoggedIn())
            {
                // Get informations from Origin plugin.
                string accessToken = originAPI.GetAccessToken().access_token;
                string personasId = GetPersonas(originAPI.GetAccessToken());
                string origineGameId = GetOrigineGameAchievementId(_PlayniteApi, game.Id);

                Common.LogDebug(true, $"Origin token: {accessToken}");

                string lang = CodeLang.GetOriginLang(_PlayniteApi.ApplicationSettings.Language);
                // Achievements (default return in english)
                var url = string.Format(@"https://achievements.gameservices.ea.com/achievements/personas/{0}/{1}/all?lang={2}&metadata=true&fullset=true",
                    personasId, origineGameId, lang);

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

                            AllAchievements.Add(new Achievements
                            {
                                Name = (string)item.Value["name"],
                                Description = (string)item.Value["desc"],
                                UrlUnlocked = (string)item.Value["icons"]["208"],
                                UrlLocked = string.Empty,
                                DateUnlocked = ((string)item.Value["state"]["a_st"] == "ACTIVE") ? default(DateTime) : new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((int)item.Value["u"]),
                                Percent = (float)item.Value["achievedPercentage"]
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
                                    Common.LogError(ex, false, $"Failed to load from {url}. ");
                                    break;
                            }
                            return Result;
                        }
                    }
                }
            }

            Result.Name = GameName;
            Result.HaveAchivements = HaveAchivements;
            Result.Total = Total;
            Result.Unlocked = Unlocked;
            Result.Locked = Locked;
            Result.Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;
            Result.Items = AllAchievements;

            return Result;
        }


        public override bool IsConfigured()
        {
            throw new NotImplementedException();
        }

        public override bool IsConnected()
        {
            return originAPI.GetIsUserLoggedIn();
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

            client.Headers.Add("Authorization", token.token_type + " " + token.access_token);
            var stringData = client.DownloadString(url);

            JObject objectData = JObject.Parse(stringData);

            return ((string)objectData["personas"]["personaUri"][0]).Replace("/pids/" + userId + "/personas/", string.Empty);
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
            GameStoreDataResponse StoreDetails = GetGameStoreData(GameId, PlayniteApi);

            return StoreDetails.platforms[0].achievementSetOverride;
        }

        internal static GameStoreDataResponse GetGameStoreData(string gameId, IPlayniteAPI PlayniteApi)
        {
            string lang = CodeLang.GetOriginLang(PlayniteApi.ApplicationSettings.Language);
            string langShort = CodeLang.GetOriginLangCountry(PlayniteApi.ApplicationSettings.Language);

            var url = string.Format(@"https://api2.origin.com/ecommerce2/public/supercat/{0}/{1}?country={2}", gameId, lang, langShort);

            string stringData = Web.DownloadStringData(url).GetAwaiter().GetResult();
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
