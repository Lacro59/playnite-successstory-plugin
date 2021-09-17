using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPluginsShared;
using CommonPluginsPlaynite.PluginLibrary.Services.GogLibrary;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CommonPluginsShared.Models;
using Playnite.SDK.Plugins;

namespace SuccessStory.Clients
{
    //https://gogapidocs.readthedocs.io/en/latest/
    class GogAchievements : GenericAchievements
    {
        private GogAccountClient gogAPI;


        public GogAchievements() : base()
        {
            var view = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView();
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
            string url = string.Empty;
            string userName = string.Empty;
            string userId = string.Empty;

            // Only if user is logged. 
            if (gogAPI.GetIsUserLoggedIn())
            {
                string accessToken = gogAPI.GetAccountInfo().accessToken;

                var AccountInfo = gogAPI.GetAccountInfo();
                userId = AccountInfo.userId;
                userName = AccountInfo.username;
                string lang = CodeLang.GetGogLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language);

                // Achievements
                url = string.Format(@"https://gameplay.gog.com/clients/{0}/users/{1}/achievements", ClientId, userId);

                try
                {
                    string urlLang = string.Format(@"https://www.gog.com/user/changeLanguage/{0}", lang.ToLower());
                    ResultWeb = Web.DownloadStringData(url, accessToken, urlLang).GetAwaiter().GetResult();
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                    {
                        var resp = (HttpWebResponse)ex.Response;
                        switch (resp.StatusCode)
                        {
                            case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                Common.LogError(ex, false, $"HTTP 503 to load from {url}");
                                break;
                            default:
                                Common.LogError(ex, false, $"Failed to load from {url}");
                                break;
                        }
                    }
                    return Result;
                }

                // Parse data
                if (ResultWeb != string.Empty)
                {
                    dynamic resultObj = Serialization.FromJson<dynamic>(ResultWeb);
                    try
                    {
                        dynamic resultItems = resultObj["items"];
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
                        Common.LogError(ex, false, $"Failed to parse");
                        return Result;
                    }
                }
            }
            else
            {
                PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                    "SuccessStory-Gog-NoAuthenticate",
                    $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsGogNoAuthenticate")}",
                    NotificationType.Error
                ));
                logger.Warn("GOG user is not Authenticate");
            }

            Result.Name = GameName;
            Result.HaveAchivements = HaveAchivements;
            Result.Total = Total;
            Result.Unlocked = Unlocked;
            Result.Locked = Locked;
            Result.Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;
            Result.Items = AllAchievements;

            if (Result.HaveAchivements)
            {
                Result.SourcesLink = new SourceLink
                {
                    GameName = GameName,
                    Name = "GOG",
                    Url = $"https://www.gog.com/u/{userName}/game/{ClientId}?sort=user_unlock_date&sort_user_id={userId}"
                };
            }

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

        public override bool ValidateConfiguration(IPlayniteAPI playniteAPI, Plugin plugin, SuccessStorySettings settings)
        {
            if (PlayniteTools.IsDisabledPlaynitePlugins("GogLibrary"))
            {
                logger.Warn("GOG is enable then disabled");
                playniteAPI.Notifications.Add(new NotificationMessage(
                    "SuccessStory-GOG-disabled",
                    $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsGogDisabled")}",
                    NotificationType.Error,
                    () => plugin.OpenSettingsView()
                ));
                return false;
            }
            else
            {
                if (CachedConfigurationValidationResult == null)
                {
                    CachedConfigurationValidationResult = IsConnected();
                }

                if (!(bool)CachedConfigurationValidationResult)
                {
                    logger.Warn("Gog user is not authenticate");
                    playniteAPI.Notifications.Add(new NotificationMessage(
                        "SuccessStory-Gog-NoAuthenticated",
                        $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsGogNoAuthenticate")}",
                        NotificationType.Error,
                        () => plugin.OpenSettingsView()
                    ));
                    return false;
                }
            }
            return true;
        }
        public override bool EnabledInSettings(SuccessStorySettings settings)
        {
            return settings.EnableGog;
        }
    }
}
