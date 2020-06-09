using GogLibrary.Services;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using PluginCommon;
using SuccessStory.Database;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    //https://gogapidocs.readthedocs.io/en/latest/
    class GogAchievements
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private GogAccountClient gogAPI;


        /// <summary>
        /// Get achievements after change language.
        /// </summary>
        /// <param name="UrlChangeLang"></param>
        /// <param name="UrlAchievements"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal async Task<string> DonwloadStringData(string UrlChangeLang, string UrlAchievements, string token)
        {
            using (var client = new HttpClient())
            {
                string resultLang = await client.GetStringAsync(UrlChangeLang).ConfigureAwait(false);

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                string result = await client.GetStringAsync(UrlAchievements).ConfigureAwait(false);

                return result;
            }
        }

        /// <summary>
        /// Get all achievements for a GOG game.
        /// </summary>
        /// <param name="PlayniteApi"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        public GameAchievements GetAchievements(IPlayniteAPI PlayniteApi, Guid Id)
        {
            List<Achievements> Achievements = new List<Achievements>();
            string GameName = PlayniteApi.Database.Games.Get(Id).Name;
            string ClientId = PlayniteApi.Database.Games.Get(Id).GameId;
            bool HaveAchivements = false;
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            GameAchievements Result = new GameAchievements
            {
                Name = GameName,
                HaveAchivements = HaveAchivements,
                Total = Total,
                Unlocked = Unlocked,
                Locked = Locked,
                Progression = 0,
                Achievements = Achievements
            };

            string ResultWeb = "";

            var view = PlayniteApi.WebViews.CreateOffscreenView();
            gogAPI = new GogAccountClient(view);

            // Only if user is logged. 
            if (gogAPI.GetIsUserLoggedIn())
            {
                string accessToken = gogAPI.GetAccountInfo().accessToken;

                string userId = gogAPI.GetAccountInfo().userId;
                string lang = CodeLang.GetGogLang(Localization.GetPlayniteLanguageConfiguration(PlayniteApi.Paths.ConfigurationPath));

                // Only languages available
                string[] arrayLang = { "de", "en", "fr", "ru", "zh", "zh-Hans" };
                if (!arrayLang.ContainsString(lang))
                {
                    lang = "en";
                }

                // Achievements
                string url = string.Format(@"https://gameplay.gog.com/clients/{0}/users/{1}/achievements",
                    ClientId, userId);

                try
                {
                    string urlLang = string.Format(@"https://www.gog.com/user/changeLanguage/{0}", lang.ToLower());
                    ResultWeb = DonwloadStringData(urlLang, url, accessToken).GetAwaiter().GetResult();
                }
                // TODO Environnement
                //catch (Exception e) when (!Environment.IsDebugBuild)
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                    {
                        var resp = (HttpWebResponse)ex.Response;
                        switch (resp.StatusCode)
                        {
                            case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                logger.Error(ex, $"SuccessStory - HTTP 503 to load from {url}");
                                break;
                            default:
                                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                                logger.Error(ex, $"SuccessStory [{LineNumber}] - Failed to load from {url}");
                                //AchievementsDatabase.ListErrors.Add($"Error on GogAchievements [{LineNumber}]: " + ex.Message);
                                break;
                        }
                    }
                    return Result;
                }

                if (ResultWeb != "")
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
                                    Name = (string)resultItems[i]["name"],
                                    Description = (string)resultItems[i]["description"],
                                    UrlUnlocked = (string)resultItems[i]["image_url_unlocked"],
                                    UrlLocked = (string)resultItems[i]["image_url_locked"],
                                    DateUnlocked = ((string)resultItems[i]["date_unlocked"] == null) ? default(DateTime) : (DateTime)resultItems[i]["date_unlocked"]
                                };

                                Total += 1;
                                if ((string)resultItems[i]["date_unlocked"] == null)
                                    Locked += 1;
                                else
                                    Unlocked += 1;

                                Achievements.Add(temp);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                        logger.Error(ex, $"SuccessStory [{LineNumber}] - Failed to parse.");
                        //AchievementsDatabase.ListErrors.Add($"Error on GogAchievements [{LineNumber}]: " + ex.Message);
                        return Result;
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
    }
}
