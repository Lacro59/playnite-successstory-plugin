using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.Common;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using SuccessStory.Database;
using SuccessStory.Models;
using SuccessStory.PlayniteResources.XboxLibrary.Models;

namespace SuccessStory.Clients
{
    class XboxAchievements : GenericAchievements
    {
        private readonly string urlAchievements = @"https://achievements.xboxlive.com/users/xuid({0})/achievements";

        private readonly string liveTokensPath;
        private readonly string xstsLoginTokesPath;


        public XboxAchievements(IPlayniteAPI PlayniteApi, SuccessStorySettings settings, string PluginUserDataPath) : base(PlayniteApi, settings, PluginUserDataPath)
        {
            // TODO Use GetXboxLang with new PluginCommon
            LocalLang = CodeLang.GetEpicLang(Localization.GetPlayniteLanguageConfiguration(_PlayniteApi.Paths.ConfigurationPath));

            liveTokensPath = Path.Combine(_PluginUserDataPath + "\\..\\7e4fbb5e-2ae3-48d4-8ba0-6b30e7a4e287", "login.json");
            xstsLoginTokesPath = Path.Combine(_PluginUserDataPath + "\\..\\7e4fbb5e-2ae3-48d4-8ba0-6b30e7a4e287", "xsts.json");
        }

        public override GameAchievements GetAchievements(Game game)
        {
            List<Achievements> AllAchievements = new List<Achievements>();
            GameAchievements Result = new GameAchievements
            {
                Name = game.Name,
                HaveAchivements = false,
                Total = 0,
                Unlocked = 0,
                Locked = 0,
                Progression = 0,
                Achievements = AllAchievements
            };


            List<XboxAchievement> ListAchievements = new List<XboxAchievement>();
            try
            {
                ListAchievements = GetXboxAchievements(game.GameId).GetAwaiter().GetResult();

#if DEBUG
                logger.Debug("SuccessStory - Xbox achievements - " + JsonConvert.SerializeObject(ListAchievements));
#endif

                foreach (XboxAchievement xboxAchievement in ListAchievements)
                {
                    AllAchievements.Add(new Achievements
                    {
                        ApiName = string.Empty,
                        Name = xboxAchievement.name,
                        Description = (xboxAchievement.progression.timeUnlocked == default(DateTime)) ? xboxAchievement.lockedDescription : xboxAchievement.description,
                        IsHidden = xboxAchievement.isSecret,
                        Percent = 100,
                        DateUnlocked = xboxAchievement.progression.timeUnlocked,
                        UrlLocked = string.Empty,
                        UrlUnlocked = xboxAchievement.mediaAssets[0].url
                    });
                }

                Result = new GameAchievements
                {
                    Name = game.Name,
                    HaveAchivements = AllAchievements.HasItems(),
                    Total = AllAchievements.Count,
                    Unlocked = AllAchievements.FindAll(x => x.DateUnlocked != default(DateTime)).Count,
                    Locked = AllAchievements.FindAll(x => x.DateUnlocked == default(DateTime)).Count,
                    Progression = 0,
                    Achievements = AllAchievements
                };
                Result.Progression = (Result.Total != 0) ? (int)Math.Ceiling((double)(Result.Unlocked * 100 / Result.Total)) : 0;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", "Failed to Xbox profile achievements");
            }

            return Result;
        }


        private void SetAuthenticationHeaders(System.Net.Http.Headers.HttpRequestHeaders headers, AuthorizationData auth)
        {
            headers.Add("x-xbl-contract-version", "2");
            headers.Add("Authorization", $"XBL3.0 x={auth.DisplayClaims.xui[0].uhs};{auth.Token}");
            headers.Add("Accept-Language", LocalLang);
        }

        private async Task<bool> GetIsUserLoggedIn()
        {
            try
            {
                if (!File.Exists(xstsLoginTokesPath))
                {
                    logger.Debug("SuccessStory - Xbox GetIsUserLoggedIn() - User is not authenticated - File not exist");
                    return false;
                }

                var tokens = Serialization.FromJsonFile<AuthorizationData>(xstsLoginTokesPath);
                using (var client = new HttpClient())
                {
                    SetAuthenticationHeaders(client.DefaultRequestHeaders, tokens);
                    var requestData = new ProfileRequest()
                    {
                        settings = new List<string> { "GameDisplayName" },
                        userIds = new List<ulong> { ulong.Parse(tokens.DisplayClaims.xui[0].xid) }
                    };
                    
                    var response = client.PostAsync(
                        @"https://profile.xboxlive.com/users/batch/profile/settings",
                        new StringContent(Serialization.ToJson(requestData), Encoding.UTF8, "application/json")).Result;

                    logger.Debug($"SuccessStory - Xbox GetIsUserLoggedIn() - {response.StatusCode}");

                    return response.StatusCode == System.Net.HttpStatusCode.OK;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to check Xbox user loging status");
                return false;
            }
        }

        private async Task Authenticate(string accessToken)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-xbl-contract-version", "1");

                //  Authenticate
                var authRequestData = new AthenticationRequest();
                authRequestData.Properties.RpsTicket = accessToken;
                var authPostContent = Serialization.ToJson(authRequestData, true);

                var authResponse = await client.PostAsync(
                    @"https://user.auth.xboxlive.com/user/authenticate",
                    new StringContent(authPostContent, Encoding.UTF8, "application/json"));
                var authResponseContent = await authResponse.Content.ReadAsStringAsync();
                var authTokens = Serialization.FromJson<AuthorizationData>(authResponseContent);

                // Authorize
                var atrzRequrestData = new AuhtorizationRequest();
                atrzRequrestData.Properties.UserTokens = new List<string> { authTokens.Token };
                var atrzPostContent = Serialization.ToJson(atrzRequrestData, true);

                var atrzResponse = await client.PostAsync(
                    @"https://xsts.auth.xboxlive.com/xsts/authorize",
                    new StringContent(atrzPostContent, Encoding.UTF8, "application/json"));
                var atrzResponseContent = await atrzResponse.Content.ReadAsStringAsync();
                var atrzTokens = Serialization.FromJson<AuthorizationData>(atrzResponseContent);

                FileSystem.WriteStringToFile(xstsLoginTokesPath, atrzResponseContent);
            }
        }

        private async Task RefreshTokens()
        {
            var tokens = Serialization.FromJsonFile<AuthenticationData>(liveTokensPath);

            var query = HttpUtility.ParseQueryString(string.Empty);
            query.Add("grant_type", "refresh_token");
            query.Add("client_id", "0000000048093EE3");
            query.Add("scope", "service::user.auth.xboxlive.com::MBI_SSL");
            query.Add("refresh_token", tokens.RefreshToken);

            var refreshUrl = @"https://login.live.com/oauth20_token.srf?" + query.ToString();
            using (var client = new HttpClient())
            {
                var refreshResponse = await client.GetAsync(refreshUrl);
                if (refreshResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var responseContent = await refreshResponse.Content.ReadAsStringAsync();
                    var response = Serialization.FromJson<RefreshTokenResponse>(responseContent);
                    tokens.AccessToken = response.access_token;
                    tokens.RefreshToken = response.refresh_token;
                    FileSystem.WriteStringToFile(liveTokensPath, Serialization.ToJson(tokens));
                    await Authenticate(tokens.AccessToken);
                }
            }
        }


        private async Task<TitleHistoryResponse.Title> GetTitleInfo(string pfn)
        {
            var tokens = Serialization.FromJsonFile<AuthorizationData>(xstsLoginTokesPath);
            using (var client = new HttpClient())
            {
                SetAuthenticationHeaders(client.DefaultRequestHeaders, tokens);
                var requestData = new Dictionary<string, List<string>>
                {
                    { "pfns", new List<string> { pfn } },
                    { "windowsPhoneProductIds", new List<string>() },
                };

                var response = await client.PostAsync(
                           @"https://titlehub.xboxlive.com/titles/batch/decoration/detail",
                           new StringContent(Serialization.ToJson(requestData), Encoding.UTF8, "application/json"));

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new Exception("Title info not available.");
                }
                else if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception("User is not authenticated.");
                }

                var cont = await response.Content.ReadAsStringAsync();
                var titleHistory = Serialization.FromJson<TitleHistoryResponse>(cont);
                return titleHistory.titles.First();
            }
        }

        private async Task<List<XboxAchievement>> GetXboxAchievements(string pfn = "")
        {
            if (!File.Exists(xstsLoginTokesPath))
            {
                logger.Warn("SuccessStory - Xbox - User is not authenticated - File not exist");
                _PlayniteApi.Notifications.Add(new NotificationMessage(
                    "SuccessStory - SuccessStory-Xbox-notAuthenticated",
                    $"SuccessStory - {resources.GetString("LOCSucessStoryNotificationsXboxNotAuthenticate")}",
                    NotificationType.Error
                ));

                return new List<XboxAchievement>();
            }
            else
            {
                var loggedIn = await GetIsUserLoggedIn();
                if (!loggedIn && File.Exists(liveTokensPath))
                {
                    await RefreshTokens();
                }

                if (!await GetIsUserLoggedIn())
                {
                    logger.Warn("SuccessStory - Xbox - User is not authenticated");
                    _PlayniteApi.Notifications.Add(new NotificationMessage(
                        "SuccessStory-Xbox-notAuthenticated",
                        $"SuccessStory - {resources.GetString("LOCSucessStoryNotificationsXboxNotAuthenticate")}",
                        NotificationType.Error
                    ));

                    return new List<XboxAchievement>();
                }
            }


            string titleId = string.Empty;
            if (!pfn.IsNullOrEmpty())
            {
                var libTitle = GetTitleInfo(pfn).Result;
                titleId = libTitle.titleId;
            }

            var tokens = Serialization.FromJsonFile<AuthorizationData>(xstsLoginTokesPath);
            string url = string.Format(urlAchievements + $"?titleId={titleId}", tokens.DisplayClaims.xui[0].xid);
            if (!titleId.IsNullOrEmpty())
            {
                url = string.Format(urlAchievements, tokens.DisplayClaims.xui[0].xid);
            }

            using (var client = new HttpClient())
            {
                SetAuthenticationHeaders(client.DefaultRequestHeaders, tokens);
                var response = client.GetAsync(url).Result;
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger.Warn($"SuccessStory - Xbox - User is not authenticated - {response.StatusCode}");
                    _PlayniteApi.Notifications.Add(new NotificationMessage(
                        "SuccessStory-Xbox-notAuthenticated",
                        $"SuccessStory - {resources.GetString("LOCSucessStoryNotificationsXboxNotAuthenticate")}",
                        NotificationType.Error
                    ));

                    return new List<XboxAchievement>();
                }

                string cont = await response.Content.ReadAsStringAsync();
                string contConvert = JsonConvert.SerializeObject(JObject.Parse(cont)["achievements"]);
                return JsonConvert.DeserializeObject<List<XboxAchievement>>(contConvert); ;
            }
        }
    }

    public class XboxAchievement
    {
        public string id { get; set; }
        public string serviceConfigId { get; set; }
        public string name { get; set; }
        public List<TitleAssociations> titleAssociations { get; set; }
        public string progressState { get; set; }
        public Progression progression { get; set; }
        public List<MediaAssets> mediaAssets { get; set; }
        public List<string> platforms { get; set; }
        public bool isSecret { get; set; }
        public string description { get; set; }
        public string lockedDescription { get; set; }
        public string productId { get; set; }
        public string achievementType { get; set; }
        public string participationType { get; set; }
    }

    public class TitleAssociations
    {
        public string name { get; set; }
        public int id { get; set; }
    }

    public class Progression
    {
        public List<Requirements> requirements { get; set; }
        public DateTime timeUnlocked { get; set; }
    }
    
    public class Requirements
    {
        public string id { get; set; }
        public string current { get; set; }
        public string target { get; set; }
        public string operationType { get; set; }
        public string valueType { get; set; }
        public string ruleParticipationType { get; set; }
    }

    public class MediaAssets
    {
        public string name { get; set; }
        public string type { get; set; }
        public string url { get; set; }
    }
}
