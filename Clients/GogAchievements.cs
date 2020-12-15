using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using CommonPlaynite.PluginLibrary.Services.GogLibrary;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    //https://gogapidocs.readthedocs.io/en/latest/
    class GogAchievements : GenericAchievements
    {
        private GogAccountClient gogAPI;


        public GogAchievements(IPlayniteAPI PlayniteApi, SuccessStorySettings settings, string PluginUserDataPath) : base(PlayniteApi, settings, PluginUserDataPath)
        {
            var view = PlayniteApi.WebViews.CreateOffscreenView();
            gogAPI = new GogAccountClient(view);
        }


        /// <summary>
        /// Get all achievements for a GOG game.
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        public override GameAchievements GetAchievements(Game game)
        {
            List<Achievements> AllAchievements = new List<Achievements>();
            string GameName = game.Name;
            string ClientId = game.GameId;
            bool HaveAchivements = false;
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            GameAchievements Result = SuccessStory.PluginDatabase.GetDefault(game);
            Result.Items = AllAchievements;

            string ResultWeb = string.Empty;

            // Only if user is logged. 
            if (gogAPI.GetIsUserLoggedIn())
            {
                string accessToken = gogAPI.GetAccountInfo().accessToken;

                string userId = gogAPI.GetAccountInfo().userId;
                string lang = CodeLang.GetGogLang(_PlayniteApi.ApplicationSettings.Language);

                // Achievements
                string url = string.Format(@"https://gameplay.gog.com/clients/{0}/users/{1}/achievements", ClientId, userId);

                try
                {
                    string urlLang = string.Format(@"https://www.gog.com/user/changeLanguage/{0}", lang.ToLower());
                    ResultWeb = DonwloadStringData(urlLang, url, accessToken).GetAwaiter().GetResult();
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                    {
                        var resp = (HttpWebResponse)ex.Response;
                        switch (resp.StatusCode)
                        {
                            case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                Common.LogError(ex, "SuccessStory", $"HTTP 503 to load from {url}");
                                break;
                            default:
                                Common.LogError(ex, "SuccessStory", $"Failed to load from {url}");
                                break;
                        }
                    }
                    return Result;
                }

                // Parse data
                if (ResultWeb != string.Empty)
                {
                    JObject resultObj = JObject.Parse(ResultWeb);
                    try
                    {
                        JArray resultItems = (JArray)resultObj["items"];
                        if (resultItems.Count > 0)
                        {
                            HaveAchivements = true;

                            for (int i = 0; i < resultItems.Count; i++)
                            {
                                Achievements temp = new Achievements
                                {
                                    ApiName = (string)resultItems[i]["achievement_key"],
                                    Name = (string)resultItems[i]["name"],
                                    Description = (string)resultItems[i]["description"],
                                    UrlUnlocked = (string)resultItems[i]["image_url_unlocked"],
                                    UrlLocked = (string)resultItems[i]["image_url_locked"],
                                    DateUnlocked = ((string)resultItems[i]["date_unlocked"] == null) ? default(DateTime) : (DateTime)resultItems[i]["date_unlocked"],
                                    Percent = (float)resultItems[i]["rarity"]
                                };

                                Total += 1;
                                if ((string)resultItems[i]["date_unlocked"] == null)
                                    Locked += 1;
                                else
                                    Unlocked += 1;

                                AllAchievements.Add(temp);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "SuccessStory", $"Failed to parse");
                        return Result;
                    }
                }
            }
            else
            {
                _PlayniteApi.Notifications.Add(new NotificationMessage(
                    "SuccessStory-Gog-NoAuthenticate",
                    $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsGogNoAuthenticate")}",
                    NotificationType.Error
                ));
                logger.Warn("SuccessStory - GOG user is not Authenticate");
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
            return gogAPI.GetIsUserLoggedIn();
        }


        /// <summary>
        /// Get achievements after change language.
        /// </summary>
        /// <param name="UrlChangeLang"></param>
        /// <param name="UrlAchievements"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<string> DonwloadStringData(string UrlChangeLang, string UrlAchievements, string token)
        {
            using (var client = new HttpClient())
            {
                string resultLang = await client.GetStringAsync(UrlChangeLang).ConfigureAwait(false);

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                string result = await client.GetStringAsync(UrlAchievements).ConfigureAwait(false);

                return result;
            }
        }
    }
}
