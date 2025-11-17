using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPluginsShared;
using CommonPlayniteShared.PluginLibrary.XboxLibrary.Models;
using SuccessStory.Models;
using CommonPluginsShared.Models;
using CommonPluginsShared.Extensions;
using static CommonPluginsShared.PlayniteTools;
using CommonPlayniteShared.PluginLibrary.XboxLibrary.Services;
using SuccessStory.Models.Xbox;

namespace SuccessStory.Clients
{
    public class XboxAchievements : GenericAchievements
    {
        protected static readonly Lazy<XboxAccountClient> xboxAccountClient = new Lazy<XboxAccountClient>(() => new XboxAccountClient(API.Instance, PluginDatabase.Paths.PluginUserDataPath + "\\..\\" + PlayniteTools.GetPluginId(ExternalPlugin.XboxLibrary)));
        internal static XboxAccountClient XboxAccountClient => xboxAccountClient.Value;

        private static string AchievementsBaseUrl => @"https://achievements.xboxlive.com/users/xuid({0})/achievements";
        private static string TitleAchievementsBaseUrl => @"https://achievements.xboxlive.com/users/xuid({0})/titleachievements";


        public XboxAchievements() : base("Xbox", CodeLang.GetXboxLang(API.Instance.ApplicationSettings.Language))
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> AllAchievements = new List<Achievement>();

            if (IsConnected())
            {
                try
                {
                    AuthorizationData authData = XboxAccountClient.GetSavedXstsTokens();
                    if (authData == null)
                    {
                        ShowNotificationPluginNoAuthenticate(ExternalPlugin.XboxLibrary);
                        return gameAchievements;
                    }

                    AllAchievements = GetXboxAchievements(game, authData).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    ShowNotificationPluginError(ex);
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(ExternalPlugin.XboxLibrary);
            }


            gameAchievements.Items = AllAchievements;


            // Set source link
            if (gameAchievements.HasAchievements)
            {
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = game.Name,
                    Name = "Xbox",
                    Url = $"https://account.xbox.com/{LocalLang}/GameInfoHub?titleid={GetTitleId(game)}&selectedTab=achievementsTab&activetab=main:mainTab2"
                };
            }

            // Set rarity from Exophase
            if (gameAchievements.HasAchievements)
            {
                SuccessStory.ExophaseAchievements.SetRarety(gameAchievements, Services.SuccessStoryDatabase.AchievementSource.Xbox);
            }

            gameAchievements.SetRaretyIndicator();
            return gameAchievements;
        }

        #region Configuration

        public override bool ValidateConfiguration()
        {
            if (!PluginDatabase.PluginSettings.Settings.PluginState.XboxIsEnabled)
            {
                ShowNotificationPluginDisable(ResourceProvider.GetString("LOCSuccessStoryNotificationsXboxDisabled"));
                return false;
            }
            else
            {
                if (CachedConfigurationValidationResult == null)
                {
                    CachedConfigurationValidationResult = IsConnected();

                    if (!(bool)CachedConfigurationValidationResult)
                    {
                        ShowNotificationPluginNoAuthenticate(ExternalPlugin.XboxLibrary);
                    }
                }
                else if (!(bool)CachedConfigurationValidationResult)
                {
                    ShowNotificationPluginErrorMessage(ExternalPlugin.XboxLibrary);
                }

                return (bool)CachedConfigurationValidationResult;
            }
        }


        public override bool IsConnected()
        {
            if (CachedIsConnectedResult == null)
            {
                CachedIsConnectedResult = XboxAccountClient.GetIsUserLoggedIn().GetAwaiter().GetResult();
                if (!(bool)CachedIsConnectedResult && File.Exists(XboxAccountClient.liveTokensPath))
                {
                    XboxAccountClient.RefreshTokens().GetAwaiter().GetResult();
                    CachedIsConnectedResult = XboxAccountClient.GetIsUserLoggedIn().GetAwaiter().GetResult();
                }
            }

            return (bool)CachedIsConnectedResult;
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableXbox;
        }

        #endregion

        #region Xbox

        private string GetTitleId(Game game)
        {
            string titleId = string.Empty;
            if (game.GameId?.StartsWith("CONSOLE_") == true)
            {
                string[] consoleGameIdParts = game.GameId.Split('_');
                titleId = consoleGameIdParts[1];

                Common.LogDebug(true, $"{ClientName} - name: {game.Name} - gameId: {game.GameId} - titleId: {titleId}");
            }
            else if (!game.GameId.IsNullOrEmpty())
            {
                TitleHistoryResponse.Title libTitle = XboxAccountClient.GetTitleInfo(game.GameId).Result;
                titleId = libTitle.titleId;

                Common.LogDebug(true, $"{ClientName} - name: {game.Name} - gameId: {game.GameId} - titleId: {titleId}");
            }
            return titleId;
        }

        private async Task<TContent> GetSerializedContentFromUrl<TContent>(string url, AuthorizationData authData, string contractVersion) where TContent : class
        {
            Common.LogDebug(true, $"{ClientName} - url: {url}");

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", Web.UserAgent);
                SetAuthenticationHeaders(client.DefaultRequestHeaders, authData, contractVersion);

                HttpResponseMessage response = client.GetAsync(url).Result;
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        Logger.Warn($"{ClientName} - User is not authenticated - {response.StatusCode}");
                        API.Instance.Notifications.Add(new NotificationMessage(
                            $"{PluginDatabase.PluginName}-Xbox-notAuthenticate",
                            $"{PluginDatabase.PluginName}\r\n{ResourceProvider.GetString("LOCSuccessStoryNotificationsXboxNotAuthenticate")}",
                            NotificationType.Error
                        ));
                    }
                    else
                    {
                        Logger.Warn($"{ClientName} - Error on GetXboxAchievements() - {response.StatusCode}");
                        API.Instance.Notifications.Add(new NotificationMessage(
                            $"{PluginDatabase.PluginName}-Xbox-webError",
                            $"{PluginDatabase.PluginName}\r\nXbox achievements: {ResourceProvider.GetString("LOCImportError")}",
                            NotificationType.Error
                        ));
                    }

                    return null;
                }

                string cont = await response.Content.ReadAsStringAsync();
                Common.LogDebug(true, cont);

                return Serialization.FromJson<TContent>(cont);
            }
        }


        private async Task<List<Achievement>> GetXboxAchievements(Game game, AuthorizationData authorizationData)
        {
            List<Func<Game, AuthorizationData, Task<List<Achievement>>>> getAchievementMethods = new List<Func<Game, AuthorizationData, Task<List<Achievement>>>>
            {
                GetXboxOneAchievements,
                GetXbox360Achievements
            };

            if (game.Platforms != null && game.Platforms.Any(p => p.SpecificationId == "xbox360"))
            {
                getAchievementMethods.Reverse();
            }

            foreach (Func<Game, AuthorizationData, Task<List<Achievement>>> getAchievementsMethod in getAchievementMethods)
            {
                try
                {
                    List<Achievement> result = await getAchievementsMethod.Invoke(game, authorizationData);
                    if (result != null && result.Any())
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("User is not authenticated", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ShowNotificationPluginNoAuthenticate(ExternalPlugin.XboxLibrary);
                    }
                    else
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets achievements for games that have come out on or since the Xbox One. This includes recent PC releases and Xbox Series X/S games.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="authorizationData"></param>
        /// <returns></returns>
        private async Task<List<Achievement>> GetXboxOneAchievements(Game game, AuthorizationData authorizationData)
        {
            if (authorizationData is null)
            {
                throw new ArgumentNullException(nameof(authorizationData));
            }

            string xuid = authorizationData.DisplayClaims.xui[0].xid;

            Common.LogDebug(true, $"GetXboxAchievements() - name: {game.Name} - gameId: {game.GameId}");
            
            string titleId = GetTitleId(game);

            string url = string.Format(AchievementsBaseUrl, xuid) + $"?titleId={titleId}&maxItems=1000";
            if (titleId.IsNullOrEmpty())
            {
                url = string.Format(AchievementsBaseUrl, xuid) + "?maxItems=10000";
                Logger.Warn($"{ClientName} - Bad request");
            }

            XboxOneAchievementResponse response = await GetSerializedContentFromUrl<XboxOneAchievementResponse>(url, authorizationData, "2");

            List<XboxOneAchievement> relevantAchievements;
            if (titleId.IsNullOrEmpty())
            {
                relevantAchievements = response.achievements.Where(x => x.titleAssociations.First().name.IsEqual(game.Name, true)).ToList();
                Common.LogDebug(true, $"Not found with {game.GameId} for {game.Name} - {relevantAchievements.Count}");
            }
            else
            {
                relevantAchievements = response.achievements;
                Common.LogDebug(true, $"Find with {titleId} & {game.GameId} for {game.Name} - {relevantAchievements.Count}");
            }

            List<Achievement> achievements = relevantAchievements.Select(ConvertToAchievement).ToList();
            return achievements;
        }

        /// <summary>
        /// Gets achievements for Xbox 360 and Games for Windows Live
        /// </summary>
        /// <param name="game"></param>
        /// <param name="authorizationData"></param>
        /// <returns></returns>
        private async Task<List<Achievement>> GetXbox360Achievements(Game game, AuthorizationData authorizationData)
        {
            if (authorizationData is null)
            {
                throw new ArgumentNullException(nameof(authorizationData));
            }

            string xuid = authorizationData.DisplayClaims.xui[0].xid;

            Common.LogDebug(true, $"GetXbox360Achievements() - name: {game.Name} - gameId: {game.GameId}");

            string titleId = GetTitleId(game);

            if (titleId.IsNullOrEmpty())
            {
                Common.LogDebug(true, $"Couldn't find title ID for game name: {game.Name} - gameId: {game.GameId}");
                return new List<Achievement>();
            }

            // gets the player-unlocked achievements
            string unlockedAchievementsUrl = string.Format(AchievementsBaseUrl, xuid) + $"?titleId={titleId}&maxItems=1000";
            Task<Xbox360AchievementResponse> getUnlockedAchievementsTask = GetSerializedContentFromUrl<Xbox360AchievementResponse>(unlockedAchievementsUrl, authorizationData, "1");

            // gets all of the game's achievements, but they're all marked as locked
            string allAchievementsUrl = string.Format(TitleAchievementsBaseUrl, xuid) + $"?titleId={titleId}&maxItems=1000";
            Task<Xbox360AchievementResponse> getAllAchievementsTask = GetSerializedContentFromUrl<Xbox360AchievementResponse>(allAchievementsUrl, authorizationData, "1");

            await Task.WhenAll(getUnlockedAchievementsTask, getAllAchievementsTask);

            Dictionary<int, Xbox360Achievement> mergedAchievements = getUnlockedAchievementsTask.Result.achievements.ToDictionary(x => x.id);
            foreach (Xbox360Achievement a in getAllAchievementsTask.Result.achievements)
            {
                if (mergedAchievements.ContainsKey(a.id))
                {
                    continue;
                }

                mergedAchievements.Add(a.id, a);
            }

            List<Achievement> achievements = mergedAchievements.Values.Select(ConvertToAchievement).ToList();

            return achievements;
        }


        private static Achievement ConvertToAchievement(XboxOneAchievement xboxAchievement)
        {
            return new Achievement
            {
                ApiName = string.Empty,
                Name = xboxAchievement.name,
                Description = (xboxAchievement.progression.timeUnlocked == default) ? xboxAchievement.lockedDescription : xboxAchievement.description,
                IsHidden = xboxAchievement.isSecret,
                Percent = 100,
                DateUnlocked = xboxAchievement.progression.timeUnlocked.ToString().Contains(default(DateTime).ToString()) ? (DateTime?)null : xboxAchievement.progression.timeUnlocked,
                UrlLocked = string.Empty,
                UrlUnlocked = xboxAchievement.mediaAssets[0].url,
                GamerScore = float.Parse(xboxAchievement.rewards?.FirstOrDefault(x => x.type.IsEqual("Gamerscore"))?.value ?? "0")
            };
        }

        private static Achievement ConvertToAchievement(Xbox360Achievement xboxAchievement)
        {
            bool unlocked = xboxAchievement.unlocked || xboxAchievement.unlockedOnline;

            return new Achievement
            {
                ApiName = string.Empty,
                Name = xboxAchievement.name,
                Description = unlocked ? xboxAchievement.lockedDescription : xboxAchievement.description,
                IsHidden = xboxAchievement.isSecret,
                Percent = 100,
                DateUnlocked = unlocked ? xboxAchievement.timeUnlocked : (DateTime?)null,
                UrlLocked = string.Empty,
                UrlUnlocked = $"https://image-ssl.xboxlive.com/global/t.{xboxAchievement.titleId:x}/ach/0/{xboxAchievement.imageId:x}",
                GamerScore = xboxAchievement.gamerscore
            };
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="auth"></param>
        /// <param name="contractVersion">1 for Xbox 360 era API, 2 for Xbox One or Series X/S</param>
        private void SetAuthenticationHeaders(System.Net.Http.Headers.HttpRequestHeaders headers, AuthorizationData auth, string contractVersion)
        {
            headers.Add("x-xbl-contract-version", contractVersion);
            headers.Add("Authorization", $"XBL3.0 x={auth.DisplayClaims.xui[0].uhs};{auth.Token}");
            headers.Add("Accept-Language", LocalLang);
        }
        
        #endregion
    }
}