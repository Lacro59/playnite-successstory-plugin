using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPluginsShared;
using CommonPlayniteShared.PluginLibrary.XboxLibrary.Models;
using SuccessStory.Models;
using CommonPluginsShared.Models;
using CommonPluginsShared.Extensions;
using static CommonPluginsShared.PlayniteTools;
using Playnite.SDK.Plugins;
using CommonPlayniteShared.PluginLibrary.XboxLibrary.Services;
using SuccessStory.Models.Xbox;

namespace SuccessStory.Clients
{
    public class XboxAchievements : GenericAchievements, IDisposable
    {
        private static readonly object _initLock = new object();
        private static readonly object _cacheLock = new object(); // For thread-safe cache operations
        private static readonly SemaphoreSlim _isConnectedSemaphore = new SemaphoreSlim(1, 1);
        private static readonly ConcurrentDictionary<string, string> _titleIdCache = new ConcurrentDictionary<string, string>();
        private static readonly int _titleIdCacheMaxSize = 500; // Prevent unbounded growth
        private static readonly HttpClient _sharedHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
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

                    // Run in background with timeout to avoid blocking UI thread and potential deadlocks indefinitely
                    using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        try
                        {
                            AllAchievements = Task.Run(async () => await GetXboxAchievements(game, authData, cts.Token).ConfigureAwait(false), cts.Token).GetAwaiter().GetResult() ?? new List<Achievement>();
                        }
                        catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
                        {
                            Logger.Warn($"Xbox achievements retrieval timed out for {game.Name} after 30 seconds.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, $"Error on GetXboxAchievements() for {game.Name}");
                            ShowNotificationPluginError(ex);
                        }
                    }
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

            if (AllAchievements.Count > 0)
            {
                gameAchievements.Items = AllAchievements;
            }
            else // AllAchievements.Count == 0
            {
                // Check if we have cached data - if so, this might indicate data loss
                var cachedData = SuccessStory.PluginDatabase.Get(game.Id, true);
                bool hadCachedAchievements = cachedData?.Items?.Count > 0;
                
                if (hadCachedAchievements)
                {
                    // Warn: we had data before but now it's empty (possible data loss)
                    Logger.Warn($"Xbox: No achievements found for {game.Name}, but cached data exists. Restoring from cache.");
                    gameAchievements.Items = cachedData.Items;
                }
                else
                {
                    // Info: legitimately no achievements for this title
                    Logger.Info($"Xbox: No achievements found for {game.Name} (title may not have achievements)");
                }
            }

            // Set source link AFTER populating Items
            if (gameAchievements.HasAchievements)
            {
                // Use cached title ID if available, otherwise use empty string
                // Full async fetch happens during GetXboxAchievements
                string titleId = string.Empty;
                if (game.GameId != null && _titleIdCache.TryGetValue(game.GameId, out string cachedTitleId))
                {
                    titleId = cachedTitleId;
                }
                
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = game.Name,
                    Name = "Xbox",
                    Url = !string.IsNullOrEmpty(titleId) 
                        ? $"https://account.xbox.com/{LocalLang}/GameInfoHub?titleid={titleId}&selectedTab=achievementsTab&activetab=main:mainTab2"
                        : $"https://account.xbox.com/{LocalLang}/Profile"
                };
            }

            // Set rarity from Exophase AFTER Items are populated — guarded to avoid crashing when Exophase integration is broken
            if (gameAchievements.HasAchievements)
            {
                try
                {
                    if (SuccessStory.ExophaseAchievements != null && SuccessStory.ExophaseAchievements.IsConnected())
                    {
                        SuccessStory.ExophaseAchievements.SetRarety(gameAchievements, Services.SuccessStoryDatabase.AchievementSource.Xbox);
                    }
                    else
                    {
                        Logger.Debug("Exophase not connected or unavailable - skipping rarity fetch for Xbox achievements.");
                    }
                }
                catch (Exception ex)
                {
                    // Log and continue — do not let Exophase failures crash Xbox achievement retrieval
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }

            gameAchievements.SetRaretyIndicator();

            // Only update if we actually have items to save (either new ones or restored ones)
            if (gameAchievements.HasAchievements)
            {
                PluginDatabase.AddOrUpdate(gameAchievements);
            }
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
            // Short-circuit if disposed
            if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
            {
                return false;
            }
            
            // Check cached result
            if (CachedIsConnectedResult == null)
            {
                // Sync-over-async: We run the full async flow on a background thread to avoid deadlocks.
                // Note: Calling .GetAwaiter().GetResult() from a thread with a synchronization context (like the UI thread)
                // still carries some risk. Prefer using IsConnectedAsync() whenever possible.
                lock (_initLock)
                {
                    if (CachedIsConnectedResult == null)
                    {
                        try
                        {
                            CachedIsConnectedResult = Task.Run(async () => await IsConnectedAsync().ConfigureAwait(false)).GetAwaiter().GetResult();
                        }
                        catch (ObjectDisposedException)
                        {
                            // Disposed during execution
                            CachedIsConnectedResult = false;
                            return false;
                        }
                        catch (OperationCanceledException)
                        {
                            // Cancelled during execution
                            CachedIsConnectedResult = false;
                            return false;
                        }
                    }
                }
            }

            return (bool)CachedIsConnectedResult;
        }

        public override async Task<bool> IsConnectedAsync()
        {
            // Short-circuit if disposed
            if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
            {
                return false;
            }
            
            if (CachedIsConnectedResult == null)
            {
                try
                {
                    await _isConnectedSemaphore.WaitAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    // Semaphore disposed
                    return false;
                }
                
                try
                {
                    // Double-check after acquiring lock
                    if (CachedIsConnectedResult == null)
                    {
                        bool isAuthenticated = await XboxAccountClient.GetIsUserLoggedIn().ConfigureAwait(false);
                        if (!isAuthenticated && File.Exists(XboxAccountClient.liveTokensPath))
                        {
                            await XboxAccountClient.RefreshTokens().ConfigureAwait(false);
                            isAuthenticated = await XboxAccountClient.GetIsUserLoggedIn().ConfigureAwait(false);
                        }

                        if (!isAuthenticated)
                        {
                            Logger.Warn($"{ClientName} user is not authenticated");
                        }

                        // Update cached result
                        CachedIsConnectedResult = isAuthenticated;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Disposed during execution
                    CachedIsConnectedResult = false;
                    return false;
                }
                finally
                {
                    try
                    {
                        _isConnectedSemaphore.Release();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Already disposed, ignore
                    }
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

        private async Task<string> GetTitleIdAsync(Game game)
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
                try
                {
                    if (_titleIdCache.TryGetValue(game.GameId, out string cachedTitleId))
                    {
                        return cachedTitleId;
                    }

                    var titleInfo = await XboxAccountClient.GetTitleInfo(game.GameId).ConfigureAwait(false);
                    if (titleInfo != null && !string.IsNullOrEmpty(titleInfo.titleId))
                    {
                        // Thread-safe cache size check and clear
                        lock (_cacheLock)
                        {
                            if (_titleIdCache.Count >= _titleIdCacheMaxSize)
                            {
                                Logger.Debug($"Xbox title ID cache size limit reached ({_titleIdCacheMaxSize}), clearing cache");
                                _titleIdCache.Clear();
                            }
                            _titleIdCache.TryAdd(game.GameId, titleInfo.titleId);
                        }
                        
                        return titleInfo.titleId;
                    }
                    else
                    {
                        Logger.Warn($"{ClientName} - No title info found for {game.GameId}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"{ClientName} - Error fetching title info for {game.GameId}");
                }

                Common.LogDebug(true, $"{ClientName} - name: {game.Name} - gameId: {game.GameId} - titleId: {titleId}");
            }
            return titleId;
        }



        private async Task<TContent> GetSerializedContentFromUrl<TContent>(string url, AuthorizationData authData, string contractVersion, CancellationToken cancellationToken) where TContent : class
        {
            Common.LogDebug(true, $"{ClientName} - url: {url}");

            // Create per-request message to avoid thread-safety issues with shared client headers
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Add("User-Agent", Web.UserAgent);
                SetAuthenticationHeaders(request.Headers, authData, contractVersion);

                using (HttpResponseMessage response = await _sharedHttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
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

                string cont = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Common.LogDebug(true, cont);

                return Serialization.FromJson<TContent>(cont);
                }
            }
        }


        private async Task<List<Achievement>> GetXboxAchievements(Game game, AuthorizationData authorizationData, CancellationToken cancellationToken)
        {
            var getAchievementMethods = new List<Func<Game, AuthorizationData, CancellationToken, Task<List<Achievement>>>>
            {
                GetXboxOneAchievements
            };

            // Conditionally add Xbox360 support if enabled
            if (PluginDatabase?.PluginSettings?.Settings?.EnableXbox360Achievements == true)
            {
                getAchievementMethods.Add(GetXbox360Achievements);
                Logger.Info("Xbox360 achievements enabled - adding to fetch methods");
            }

            if (game.Platforms != null && game.Platforms.Any(p => p.SpecificationId == "xbox360") && getAchievementMethods.Contains(GetXbox360Achievements))
            {
                getAchievementMethods.Reverse();
            }

            foreach (Func<Game, AuthorizationData, CancellationToken, Task<List<Achievement>>> getAchievementsMethod in getAchievementMethods)
            {
                try
                {
                    List<Achievement> result = await getAchievementsMethod.Invoke(game, authorizationData, cancellationToken).ConfigureAwait(false);
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

            return new List<Achievement>();
        }

        /// <summary>
        /// Gets achievements for games that have come out on or since the Xbox One. This includes recent PC releases and Xbox Series X/S games.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="authorizationData"></param>
        /// <returns></returns>
        private async Task<List<Achievement>> GetXboxOneAchievements(Game game, AuthorizationData authorizationData, CancellationToken cancellationToken)
        {
            if (authorizationData is null)
            {
                throw new ArgumentNullException(nameof(authorizationData));
            }

            string xuid = authorizationData.DisplayClaims.xui[0].xid;

            Common.LogDebug(true, $"GetXboxAchievements() - name: {game.Name} - gameId: {game.GameId}");
            
            string titleId = await GetTitleIdAsync(game).ConfigureAwait(false);

            string url = string.Format(AchievementsBaseUrl, xuid) + $"?titleId={titleId}&maxItems=1000";
            if (titleId.IsNullOrEmpty())
            {
                url = string.Format(AchievementsBaseUrl, xuid) + "?maxItems=10000";
                Logger.Warn($"{ClientName} - Bad request");
            }

            XboxOneAchievementResponse response = await GetSerializedContentFromUrl<XboxOneAchievementResponse>(url, authorizationData, "2", cancellationToken).ConfigureAwait(false);

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
        private async Task<List<Achievement>> GetXbox360Achievements(Game game, AuthorizationData authorizationData, CancellationToken cancellationToken)
        {
            if (authorizationData is null)
            {
                throw new ArgumentNullException(nameof(authorizationData));
            }

            string xuid = authorizationData.DisplayClaims.xui[0].xid;

            Common.LogDebug(true, $"GetXbox360Achievements() - name: {game.Name} - gameId: {game.GameId}");

            string titleId = await GetTitleIdAsync(game).ConfigureAwait(false);

            if (titleId.IsNullOrEmpty())
            {
                Common.LogDebug(true, $"Couldn't find title ID for game name: {game.Name} - gameId: {game.GameId}");
                return new List<Achievement>();
            }

            // gets the player-unlocked achievements
            string unlockedAchievementsUrl = string.Format(AchievementsBaseUrl, xuid) + $"?titleId={titleId}&maxItems=1000";
            Task<Xbox360AchievementResponse> getUnlockedAchievementsTask = GetSerializedContentFromUrl<Xbox360AchievementResponse>(unlockedAchievementsUrl, authorizationData, "1", cancellationToken);

            // gets all of the game's achievements, but they're all marked as locked
            string allAchievementsUrl = string.Format(TitleAchievementsBaseUrl, xuid) + $"?titleId={titleId}&maxItems=1000";
            Task<Xbox360AchievementResponse> getAllAchievementsTask = GetSerializedContentFromUrl<Xbox360AchievementResponse>(allAchievementsUrl, authorizationData, "1", cancellationToken);

            await Task.WhenAll(getUnlockedAchievementsTask, getAllAchievementsTask).ConfigureAwait(false);

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

        #region IDisposable

        private static int _disposed = 0;

        /// <summary>
        /// Cleanup method to dispose of static resources. Should be called when plugin unloads.
        /// </summary>
        public static void Cleanup()
        {
            // Use Interlocked for thread-safe disposal check
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                try
                {
                    _isConnectedSemaphore?.Dispose();
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
                
                try
                {
                    _sharedHttpClient?.Dispose();
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
                
                try
                {
                    _titleIdCache?.Clear();
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }
        }

        /// <summary>
        /// Instance dispose method. This is empty because all resources are static.
        /// Call the static Cleanup() method when the plugin unloads to release resources.
        /// This interface implementation exists for compatibility with disposal patterns.
        /// </summary>
        public void Dispose()
        {
            // All resources are static (_sharedHttpClient, _isConnectedSemaphore)
            // They must be disposed via the static Cleanup() method, not per-instance
            // This is intentional for the plugin lifecycle where resources are shared
        }

        #endregion
    }
}