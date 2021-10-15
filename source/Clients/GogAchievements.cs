using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPluginsShared;
using CommonPlayniteShared.PluginLibrary.Services.GogLibrary;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Net;
using CommonPluginsShared.Models;
using CommonPlayniteShared.PluginLibrary.GogLibrary.Models;

namespace SuccessStory.Clients
{
    // https://gogapidocs.readthedocs.io/en/latest/
    class GogAchievements : GenericAchievements
    {
        protected static GogAccountClient _GogAPI;
        internal static GogAccountClient GogAPI
        {
            get
            {
                if (_GogAPI == null)
                {
                    _GogAPI = new GogAccountClient(WebViewOffscreen);
                }
                return _GogAPI;
            }

            set
            {
                _GogAPI = value;
            }
        }

        protected static AccountBasicRespose _AccountInfo;
        internal static AccountBasicRespose AccountInfo
        {
            get
            {
                if (_AccountInfo == null)
                {
                    _AccountInfo = GogAPI.GetAccountInfo();
                }
                return _AccountInfo;
            }

            set
            {
                _AccountInfo = value;
            }
        }

        private const string UrlGogAchievements = @"https://gameplay.gog.com/clients/{0}/users/{1}/achievements";
        private const string UrlGogLang = @"https://www.gog.com/user/changeLanguage/{0}";


        public GogAchievements() : base("GOG", CodeLang.GetGogLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language))
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();

            string ClientId = game.GameId;

            string ResultWeb = string.Empty;
            string Url = string.Empty;

            string AccessToken = string.Empty;
            string UserId = string.Empty;
            string UserName = string.Empty;


            if (IsConnected())
            {
                AccessToken = AccountInfo.accessToken;
                UserId = AccountInfo.userId;
                UserName = AccountInfo.username;

                // Achievements
                Url = string.Format(UrlGogAchievements, ClientId, UserId);

                try
                {
                    string UrlLang = string.Format(UrlGogLang, LocalLang.ToLower());
                    ResultWeb = Web.DownloadStringData(Url, AccessToken, UrlLang).GetAwaiter().GetResult();
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                    {
                        var resp = (HttpWebResponse)ex.Response;
                        switch (resp.StatusCode)
                        {
                            case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                ShowNotificationPluginWebError(ex, Url);
                                break;
                            default:
                                ShowNotificationPluginWebError(ex, Url);
                                break;
                        }
                    }
                    return gameAchievements;
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

                                AllAchievements.Add(temp);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowNotificationPluginError(ex);
                        return gameAchievements;
                    }
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
                    Name = "GOG",
                    Url = $"https://www.gog.com/u/{UserName}/game/{ClientId}?sort=user_unlock_date&sort_user_id={UserId}"
                };
            }


            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            if (PlayniteTools.IsDisabledPlaynitePlugins("GogLibrary"))
            {
                ShowNotificationPluginDisable(resources.GetString("LOCSuccessStoryNotificationsGogDisabled"));
                return false;
            }
            else
            {
                if (CachedConfigurationValidationResult == null)
                {
                    CachedConfigurationValidationResult = IsConnected();

                    if (!(bool)CachedConfigurationValidationResult)
                    {
                        ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsGogNoAuthenticate"));
                    }
                    else
                    {
                        CachedConfigurationValidationResult = IsConfigured();

                        if (!(bool)CachedConfigurationValidationResult)
                        {
                            ShowNotificationPluginNoConfiguration(resources.GetString("LOCSuccessStoryNotificationsGogBadConfig"));
                        }
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
                CachedIsConnectedResult = GogAPI.GetIsUserLoggedIn();
            }

            return (bool)CachedIsConnectedResult;
        }

        public override bool IsConfigured()
        {
            string AccessToken = AccountInfo?.accessToken;
            string UserId = AccountInfo?.userId;
            string UserName = AccountInfo?.username;

            return !AccessToken.IsNullOrEmpty() && !UserId.IsNullOrEmpty() && !UserName.IsNullOrEmpty();
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableGog;
        }
        #endregion
    }
}
