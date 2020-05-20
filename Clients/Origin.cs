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
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    class Origin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        OriginAccountClient originAPI;

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

            if (originAPI.GetIsUserLoggedIn())
            {
                string accessToken = originAPI.GetAccessToken().access_token;
                string personasId = GetPersonas(originAPI.GetAccessToken());
                string origineGameId = GetOrigineGameId(PlayniteApi, Id);
                string lang = "en_IE";

                // Achievements
                //https://achievements.gameservices.ea.com/achievements/personas/{userID}/{gameID}/all?lang=fr_FR&metadata=true&fullset=true

                var url = string.Format(@"https://achievements.gameservices.ea.com/achievements/personas/{0}/{1}/all?lang={2}&metadata=true&fullset=true",
                    personasId, origineGameId, lang);


                var client = new WebClient();
                client.Headers.Add("X-AuthToken", accessToken);
                client.Headers.Add("accept", "application/vnd.origin.v3+json; x-cache/force-write");

                try
                {
                    var stringData = client.DownloadString(url);

                    JObject AchievementsData = JObject.Parse(stringData);

                    //{
                    //    "icons": {
                    //        "416": "https://achievements.gameservices.ea.com/achievements/icons/50072_193612_50844-22-416.png",
                    //        "208": "https://achievements.gameservices.ea.com/achievements/icons/50072_193612_50844-22-208.png",
                    //        "40": "https://achievements.gameservices.ea.com/achievements/icons/50072_193612_50844-22-40.png"
                    //            },
                    //    "complete": false,
                    //    "tn": [],
                    //    "requirements": [],
                    //    "conditions": [],
                    //    "xp_t": "15",
                    //    "rp_t": "0",
                    //    "hidden": false,
                    //    "s": -1,
                    //    "tags": {},
                    //    "tt": 0,
                    //    "tc": -1,
                    //    "achievedPercentage": "18.90",
                    //    "state": {
                    //        "a_st": "ACTIVE" // "a_st": "COMPLETED"
                    //    },
                    //    "stateModifiers": [],
                    //    "pt": -1,
                    //    "p": 0,
                    //    "t": 1,
                    //    "cnt": 0,
                    //    "xp": 15,
                    //    "rp": 0,
                    //    "up": 0,
                    //    "u": 1589983293,
                    //    "e": -1,
                    //    "name": "Getting the Hang of It",
                    //    "desc": "Achieve Gold 3 Rank in Ultimate Team Squad Battles",
                    //    "howto": "Achieve Gold 3 Rank in Ultimate Team Squad Battles",
                    //    "img": "22"
                    //}

                    foreach (var item in (JObject)AchievementsData["achievements"])
                    {
                        var val = item.Value;
                        HaveAchivements = true;

                        Achievements.Add(new Achievements
                        {
                            Name = (string)item.Value["name"],
                            Description = (string)item.Value["desc"],
                            UrlUnlocked = (string)item.Value["icons"]["208"],
                            UrlLocked = (string)item.Value["icons"]["208"],
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
                                logger.Error(e, $"Failed to load from {url}");
                                PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "SuccessStory error");
                                break;
                        }
                    }
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

        public string GetPersonas(AuthTokenResponse token)
        {
            var client = new WebClient();
            var userId = originAPI.GetAccountInfo(originAPI.GetAccessToken()).pid.pidId;
            var url = string.Format(@"https://gateway.ea.com/proxy/identity/pids/{0}/personas?namespaceName=cem_ea_id", userId);
            client.Headers.Add("Authorization", token.token_type + " " + token.access_token);
            var stringData = client.DownloadString(url);

            // Utility uri ?
            //"personas" : {
            //    "personaUri" : [ "/pids/2299676828/personas/307126092" ]
            //}
            JObject objectData = JObject.Parse(stringData);

            return ((string)objectData["personas"]["personaUri"][0]).Replace("/pids/" + userId + "/personas/", "");
        }



        private string GetOrigineGameId(IPlayniteAPI PlayniteApi, Guid Id)
        {
            string GameId = PlayniteApi.Database.Games.Get(Id).GameId;
            GameStoreDataResponse StoreDetails = GetGameStoreData(GameId);

            return StoreDetails.platforms[0].achievementSetOverride;
        }


        public static GameStoreDataResponse GetGameStoreData(string gameId)
        {
            string lang = "en_IE";
            string langShort = "IE";
            var url = string.Format(@"https://api2.origin.com/ecommerce2/public/supercat/{0}/{1}?country={2}", gameId, lang, langShort);
            var stringData = Encoding.UTF8.GetString(HttpDownloader.DownloadData(url));
            return JsonConvert.DeserializeObject<GameStoreDataResponse>(stringData);
        }
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
