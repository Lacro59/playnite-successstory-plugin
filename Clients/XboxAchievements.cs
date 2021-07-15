using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPluginsShared;
using CommonPluginsPlaynite.Common;
using CommonPluginsPlaynite.PluginLibrary.XboxLibrary.Models;
using SuccessStory.Models;
using CommonPluginsShared.Models;

namespace SuccessStory.Clients
{
    class XboxAchievements : GenericAchievements
    {
        private readonly string urlAchievements = @"https://achievements.xboxlive.com/users/xuid({0})/achievements";

        private readonly string liveTokensPath;
        private readonly string xstsLoginTokesPath;

        private bool HasTestLogged = false;
        private bool loggedIn = false;

        private string titleId = string.Empty;


        public XboxAchievements() : base()
        {
            LocalLang = CodeLang.GetXboxLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language);

            liveTokensPath = Path.Combine(PluginDatabase.Paths.PluginUserDataPath + "\\..\\7e4fbb5e-2ae3-48d4-8ba0-6b30e7a4e287", "login.json");
            xstsLoginTokesPath = Path.Combine(PluginDatabase.Paths.PluginUserDataPath + "\\..\\7e4fbb5e-2ae3-48d4-8ba0-6b30e7a4e287", "xsts.json");
        }


        public override GameAchievements GetAchievements(Game game)
        {
            List<Achievements> AllAchievements = new List<Achievements>();
            GameAchievements Result = SuccessStory.PluginDatabase.GetDefault(game);
            Result.Items = AllAchievements;


            List<XboxAchievement> ListAchievements = new List<XboxAchievement>();
            try
            {
                ListAchievements = GetXboxAchievements(game.GameId, game.Name).GetAwaiter().GetResult();

                Common.LogDebug(true, Serialization.ToJson(ListAchievements));
                
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

                Result.Name = game.Name;
                Result.HaveAchivements = AllAchievements.HasItems();
                Result.Total = AllAchievements.Count;
                Result.Unlocked = AllAchievements.FindAll(x => x.DateUnlocked != default(DateTime)).Count;
                Result.Locked = AllAchievements.FindAll(x => x.DateUnlocked == default(DateTime)).Count;
                Result.Progression = (Result.Total != 0) ? (int)Math.Ceiling((double)(Result.Unlocked * 100 / Result.Total)) : 0;
                Result.Items = AllAchievements;

                if (Result.HaveAchivements)
                {
                    ExophaseAchievements exophaseAchievements = new ExophaseAchievements();
                    exophaseAchievements.SetRarety(Result);

                    Result.SourcesLink = new SourceLink
                    {
                        GameName = game.Name,
                        Name = "Xbox",
                        Url = $"https://account.xbox.com/fr-fr/GameInfoHub?titleid={titleId}&selectedTab=achievementsTab&activetab=main:mainTab2"
                    };
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "Failed to Xbox profile achievements");
            }

            return Result;
        }


        public override bool IsConfigured()
        {
            throw new NotImplementedException();
        }

        public override bool IsConnected()
        {
            return GetIsUserLoggedIn().GetAwaiter().GetResult();
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
                    logger.Warn("Xbox GetIsUserLoggedIn() - User is not authenticated - File not exist");
                    return false;
                }

                var tokens = Playnite.SDK.Data.Serialization.FromJsonFile<AuthorizationData>(xstsLoginTokesPath);
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:86.0) Gecko/20100101 Firefox/86.0");
                    SetAuthenticationHeaders(client.DefaultRequestHeaders, tokens);
                    var requestData = new ProfileRequest()
                    {
                        settings = new List<string> { "GameDisplayName" },
                        userIds = new List<ulong> { ulong.Parse(tokens.DisplayClaims.xui[0].xid) }
                    };
                    
                    var response = await client.PostAsync(
                        @"https://profile.xboxlive.com/users/batch/profile/settings",
                        new StringContent(Playnite.SDK.Data.Serialization.ToJson(requestData), Encoding.UTF8, "application/json"));

                    Common.LogDebug(true, $"Xbox GetIsUserLoggedIn() - {response.StatusCode}");

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
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:86.0) Gecko/20100101 Firefox/86.0");
                client.DefaultRequestHeaders.Add("x-xbl-contract-version", "1");

                //  Authenticate
                var authRequestData = new AthenticationRequest();
                authRequestData.Properties.RpsTicket = accessToken;
                var authPostContent = Playnite.SDK.Data.Serialization.ToJson(authRequestData, true);

                var authResponse = await client.PostAsync(
                    @"https://user.auth.xboxlive.com/user/authenticate",
                    new StringContent(authPostContent, Encoding.UTF8, "application/json"));
                var authResponseContent = await authResponse.Content.ReadAsStringAsync();
                var authTokens = Playnite.SDK.Data.Serialization.FromJson<AuthorizationData>(authResponseContent);

                // Authorize
                var atrzRequrestData = new AuhtorizationRequest();
                atrzRequrestData.Properties.UserTokens = new List<string> { authTokens.Token };
                var atrzPostContent = Playnite.SDK.Data.Serialization.ToJson(atrzRequrestData, true);

                var atrzResponse = await client.PostAsync(
                    @"https://xsts.auth.xboxlive.com/xsts/authorize",
                    new StringContent(atrzPostContent, Encoding.UTF8, "application/json"));
                var atrzResponseContent = await atrzResponse.Content.ReadAsStringAsync();
                var atrzTokens = Playnite.SDK.Data.Serialization.FromJson<AuthorizationData>(atrzResponseContent);

                FileSystem.WriteStringToFile(xstsLoginTokesPath, atrzResponseContent);
            }
        }

        private async Task RefreshTokens()
        {
            var tokens = Playnite.SDK.Data.Serialization.FromJsonFile<AuthenticationData>(liveTokensPath);

            var query = HttpUtility.ParseQueryString(string.Empty);
            query.Add("grant_type", "refresh_token");
            query.Add("client_id", "0000000048093EE3");
            query.Add("scope", "service::user.auth.xboxlive.com::MBI_SSL");
            query.Add("refresh_token", tokens.RefreshToken);

            var refreshUrl = @"https://login.live.com/oauth20_token.srf?" + query.ToString();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:86.0) Gecko/20100101 Firefox/86.0");

                var refreshResponse = await client.GetAsync(refreshUrl);
                if (refreshResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var responseContent = await refreshResponse.Content.ReadAsStringAsync();
                    var response = Playnite.SDK.Data.Serialization.FromJson<RefreshTokenResponse>(responseContent);
                    tokens.AccessToken = response.access_token;
                    tokens.RefreshToken = response.refresh_token;
                    FileSystem.WriteStringToFile(liveTokensPath, Playnite.SDK.Data.Serialization.ToJson(tokens));
                    await Authenticate(tokens.AccessToken);
                }
            }
        }


        private async Task<TitleHistoryResponse.Title> GetTitleInfo(string pfn)
        {
            var tokens = Playnite.SDK.Data.Serialization.FromJsonFile<AuthorizationData>(xstsLoginTokesPath);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:86.0) Gecko/20100101 Firefox/86.0");
                SetAuthenticationHeaders(client.DefaultRequestHeaders, tokens);
                var requestData = new Dictionary<string, List<string>>
                {
                    { "pfns", new List<string> { pfn } },
                    { "windowsPhoneProductIds", new List<string>() },
                };

                var response = await client.PostAsync(
                           @"https://titlehub.xboxlive.com/titles/batch/decoration/detail",
                           new StringContent(Playnite.SDK.Data.Serialization.ToJson(requestData), Encoding.UTF8, "application/json"));

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new Exception("Title info not available.");
                }
                else if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception("User is not authenticated.");
                }

                var cont = await response.Content.ReadAsStringAsync();
                var titleHistory = Playnite.SDK.Data.Serialization.FromJson<TitleHistoryResponse>(cont);
                return titleHistory.titles.First();
            }
        }

        private async Task<List<XboxAchievement>> GetXboxAchievements(string gameId, string name)
        {
            Common.LogDebug(true, $"GetXboxAchievements() - name: {name} - gameId: {gameId}");

            if (!File.Exists(xstsLoginTokesPath))
            {
                logger.Warn("XboxAchievements - User is not authenticated - File not exist");
                PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                    "SuccessStory-Xbox-notAuthenticate",
                    $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsXboxNotAuthenticate")}",
                    NotificationType.Error
                ));

                return new List<XboxAchievement>();
            }
            else
            {
                if (!HasTestLogged)
                {
                    loggedIn = await GetIsUserLoggedIn();

                    if (!(bool)loggedIn && File.Exists(liveTokensPath))
                    {
                        await RefreshTokens();
                        loggedIn = await GetIsUserLoggedIn();
                    }
                    HasTestLogged = true;
                }

                if (!loggedIn)
                {
                    logger.Warn("XboxAchievements - User is not authenticated");
                    PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                        "SuccessStory-Xbox-notAuthenticate",
                        $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsXboxNotAuthenticate")}",
                        NotificationType.Error
                    ));

                    return new List<XboxAchievement>();
                }
            }

            titleId = string.Empty;
            if (gameId?.StartsWith("CONSOLE_") == true)
            {
                var consoleGameIdParts = gameId.Split('_');
                titleId = consoleGameIdParts[1];

                Common.LogDebug(true, $"XboxAchievements - name: {name} - gameId: {gameId} - titleId: {titleId}");
            }
            else if (!gameId.IsNullOrEmpty())
            {
                var libTitle = GetTitleInfo(gameId).Result;

                Common.LogDebug(true, $"XboxAchievements - name: {name} - gameId: {gameId} - titleId: {titleId}");

                titleId = libTitle.titleId;
            }

            var tokens = Playnite.SDK.Data.Serialization.FromJsonFile<AuthorizationData>(xstsLoginTokesPath);
            string url = string.Format(urlAchievements + $"?titleId={titleId}&maxItems=1000", tokens.DisplayClaims.xui[0].xid);
            if (titleId.IsNullOrEmpty())
            {
                url = string.Format(urlAchievements + $"?maxItems=10000", tokens.DisplayClaims.xui[0].xid);
                logger.Warn($"XboxAchievements - Bad request");
            }

            Common.LogDebug(true, $"XboxAchievements - url: {url}");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:86.0) Gecko/20100101 Firefox/86.0");
                SetAuthenticationHeaders(client.DefaultRequestHeaders, tokens);
                var response = client.GetAsync(url).Result;
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        logger.Warn($"XboxAchievements - User is not authenticated - {response.StatusCode}");
                        PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                            "SuccessStory-Xbox-notAuthenticate",
                            $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsXboxNotAuthenticate")}",
                            NotificationType.Error
                        ));
                    }
                    else
                    {
                        logger.Warn($"XboxAchievements - Error on GetXboxAchievements() - {response.StatusCode}");
                        PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                            "SuccessStory-Xbox-webError",
                            $"SuccessStory\r\nXbox achievements: {resources.GetString("LOCImportError")}",
                            NotificationType.Error
                        ));
                    }

                    return new List<XboxAchievement>();
                }

                string cont = await response.Content.ReadAsStringAsync();
                string contConvert = Serialization.ToJson(Serialization.FromJson<dynamic>(cont)["achievements"]);

                var ListAchievements = Serialization.FromJson<List<XboxAchievement>>(contConvert);
                if (titleId.IsNullOrEmpty())
                {
                    ListAchievements = ListAchievements.Where(x => x.titleAssociations.First().name.ToLower() == name.ToLower()).ToList();

                    Common.LogDebug(true, $"Not find with {gameId} for {name} - {ListAchievements.Count}");
                }                
                else
                {
                    Common.LogDebug(true, $"Find with {titleId} & {gameId} for {name} - {ListAchievements.Count}");
                }

                return ListAchievements;
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
