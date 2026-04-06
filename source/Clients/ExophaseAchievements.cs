using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    public class ExophaseAchievements : GenericAchievements
    {
        #region Urls

        private string UrlApi => @"https://api.exophase.com";
        private string UrlExophaseSearch => UrlApi + "/public/archive/games?q={0}&sort=added";
        private string UrlExophaseSearchPlatform => UrlApi + "/public/archive/platform/{1}?q={0}&sort=added";

        private string UrlExophase => @"https://www.exophase.com";
        private string UrlExophaseLogin => $"{UrlExophase}/login";
        private string UrlExophaseLogout => $"{UrlExophase}/logout";
        private string UrlExophaseAccount => $"{UrlExophase}/account";

        #endregion

        public static List<string> Platforms = new List<string>
        {
            ResourceProvider.GetString("LOCAll"),
            "Apple",
            "Blizzard",
            "Electronic Arts",
            "Epic",
            "GOG",
            "Google Play",
            "Nintendo",
            "PSN",
            "Retro",
            "Stadia",
            "Steam",
            "Ubisoft",
            "Xbox"
        };


        private static readonly HttpClient _sharedHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        // Cache expiration in days
        private const int CACHE_EXPIRATION_DAYS = 7;

        private static readonly SemaphoreSlim _bgFetchSemaphore = new SemaphoreSlim(2);
        private static CancellationTokenSource _bgFetchCts = new CancellationTokenSource();
        private static readonly object _bgFetchCtsLock = new object();

        static ExophaseAchievements()
        {
            _sharedHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        }

        public ExophaseAchievements() : base("Exophase")
        {
            CookiesDomains = new List<string> { ".exophase.com" };
		}

        private static string GetCacheDirectory()
        {
            return Path.Combine(PluginDatabase.Paths.PluginCachePath, "ExophaseImages");
        }

        private static string GenerateCacheKey(string url)
        {
            string safeUrl = url ?? string.Empty;
            string cacheKey = Regex.Replace(safeUrl, "[^a-zA-Z0-9_-]", "_");
            if (cacheKey.Length > 100)
            {
                cacheKey = safeUrl.MD5();
            }
            return cacheKey;
        }


        public override GameAchievements GetAchievements(Game game)
        {
            throw new NotImplementedException();
        }

        public GameAchievements GetAchievements(Game game, string url, CancellationToken cancellationToken = default)
        {
            return GetAchievements(game, new SearchResult { Name = game.Name, Url = url }, cancellationToken);
        }

        public async Task<GameAchievements> GetAchievementsAsync(Game game, string url)
        {
            return await GetAchievementsAsync(game, new SearchResult { Name = game.Name, Url = url });
        }

        /// <summary>
        /// Gets achievements for a game synchronously.
        /// WARNING: This method uses sync-over-async and MUST only be called from background threads.
        /// Use GetAchievementsAsync when possible to avoid deadlocks.
        /// </summary>
        /// <param name="game">The game to get achievements for</param>
        /// <param name="searchResult">The search result containing the URL</param>
        /// <returns>Game achievements</returns>
        /// <exception cref="InvalidOperationException">Thrown if called from a thread with a synchronization context</exception>
        public GameAchievements GetAchievements(Game game, SearchResult searchResult, CancellationToken cancellationToken = default)
        {
            // Enforce background thread usage to prevent deadlocks
            if (SynchronizationContext.Current != null)
            {
                throw new InvalidOperationException(
                    "GetAchievements (sync) must be called from a background thread. " +
                    "Use GetAchievementsAsync or wrap this call in Task.Run() to avoid deadlocks.");
            }
            
            // Synchronous wrapper for backward compatibility
            return GetAchievementsAsync(game, searchResult, cancellationToken).GetAwaiter().GetResult();
        }

        public async Task<GameAchievements> GetAchievementsAsync(Game game, SearchResult searchResult, CancellationToken cancellationToken = default)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> allAchievements = new List<Achievement>();

            try
            {
                string dataExophaseLocalised = string.Empty;
                string dataExophase = string.Empty;

                // Normalize fetch URL and prepare cache key
                string fetchUrl = searchResult.Url ?? string.Empty;
                if (fetchUrl.StartsWith("/"))
                {
                    fetchUrl = UrlExophase.TrimEnd('/') + fetchUrl;
                }
                if (!fetchUrl.Contains("/achievements", StringComparison.InvariantCultureIgnoreCase))
                {
                    fetchUrl = fetchUrl.TrimEnd('/') + "/achievements/";
                }
                string cacheKeyUrl = fetchUrl;
                try
                {
                    var cacheDir = GetCacheDirectory();
                    if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);
                    string cacheKey = GenerateCacheKey(cacheKeyUrl);
                    string cacheFile = Path.Combine(cacheDir, cacheKey + ".json");
                    if (File.Exists(cacheFile))
                    {
                        var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(cacheFile);
                        if (age.TotalDays <= CACHE_EXPIRATION_DAYS)
                        {
                            // load cached achievements quickly
                            var jsonCache = File.ReadAllText(cacheFile);
                            try
                            {
                                var cached = Serialization.FromJson<Dictionary<string, Achievement>>(jsonCache);
                                if (cached != null && cached.Count > 0)
                                {
                                    gameAchievements.Items = cached.Values.ToList();
                                    // Register images to resolver
                                    var imagesDict = cached.ToDictionary(kv => kv.Key, kv => kv.Value.UrlUnlocked);
                                    Services.AchievementImageResolver.RegisterImages(game, imagesDict);
                                    return gameAchievements;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Warn($"Exophase cache deserialization failed, trying old format (may result in incomplete data): {ex.Message}");
                                var cachedOld = Serialization.FromJson<Dictionary<string, string>>(jsonCache);
                                if (cachedOld != null && cachedOld.Count > 0)
                                {
                                    var cachedList = cachedOld.Select(kv => new Achievement { Name = kv.Key, UrlUnlocked = kv.Value }).ToList();
                                    gameAchievements.Items = cachedList;
                                    Services.AchievementImageResolver.RegisterImages(game, cachedOld);
                                    return gameAchievements;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "Error reading Exophase cache", true, PluginDatabase.PluginName);
                }

                // Fetch data asynchronously
                try
                {
                    // Try WebView fetch first to obtain fully rendered page (bypasses Cloudflare/JS)
                    try
                    {
                        var webData = await Web.DownloadSourceDataWebView(fetchUrl, GetCookies(), true, CookiesDomains, true, true, "ul.achievement, ul.trophy, ul.challenge", cancellationToken);
                        if (!string.IsNullOrEmpty(webData.Item1))
                        {
                            dataExophase = webData.Item1;
                        }
                    }
                    catch (Exception exWebView)
                    {
                        Logger.Debug($"Exophase WebView fetch failed, falling back to HTTP: {exWebView.Message}");
                        // WebView fetch failed; fall back to HTTP methods
                    }

                    // Fallback: try HTTP client then simple download
                    if (string.IsNullOrEmpty(dataExophase))
                    {
                        string fetched = null;
                        try
                        {
                            var resp = await _sharedHttpClient.GetAsync(fetchUrl, cancellationToken);
                            if (resp.IsSuccessStatusCode)
                            {
                                fetched = await resp.Content.ReadAsStringAsync();
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Cancelled
                        }
                        catch (Exception ex)
                        {
                            Logger.Debug($"Exophase HTTP client fetch failed: {ex.Message}");
                        }

                        if (!string.IsNullOrEmpty(fetched))
                        {
                            dataExophase = fetched;
                        }
                    }

                    // Final fallback
                    if (string.IsNullOrEmpty(dataExophase))
                    {
                        try
                        {
                            dataExophase = await Web.DownloadStringData(fetchUrl);
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, $"Exophase HTTP fetch failed for {searchResult.Url}, scheduling background WebView fetch", true, PluginDatabase.PluginName);

                            // Background fetch: parse, cache and register images without blocking
                            ScheduleBackgroundFetch(fetchUrl, cacheKeyUrl, game);

                            dataExophase = string.Empty;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "Error fetching Exophase page", true, PluginDatabase.PluginName);
                    dataExophase = string.Empty;
                }

                // Check if the fetched page contains achievement lists; some Exophase pages don't include 'Achievements' in <title>
                bool hasAchievementLists = false;
                if (!string.IsNullOrEmpty(dataExophase))
                {
                    try
                    {
                        var parserCheck = new HtmlParser();
                        var docCheck = parserCheck.Parse(dataExophase);
                        hasAchievementLists = (docCheck.QuerySelectorAll("ul.achievement, ul.trophy, ul.challenge")?.Length ?? 0) > 0;
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"Exophase achievement list check failed: {ex.Message}");
                        hasAchievementLists = false;
                    }
                }

                // If no achievement lists found, schedule background fetch for next run
                if (!hasAchievementLists)
                {
                    Logger.Debug($"Exophase: No achievement lists found for {searchResult.Url}, scheduling background fetch");
                    ScheduleBackgroundFetch(fetchUrl, cacheKeyUrl, game);
                    dataExophase = string.Empty;
                }

                if (PluginDatabase.PluginSettings.Settings.UseLocalised && !IsConnected())
                {
                    Logger.Warn($"Exophase is disconnected");
                    string message = string.Format(ResourceProvider.GetString("LOCCommonStoresNoAuthenticate"), ClientName);
                    API.Instance.Notifications.Add(new NotificationMessage(
                        $"{PluginDatabase.PluginName}-Exophase-disconnected",
                        $"{PluginDatabase.PluginName}\r\n{message}",
                        NotificationType.Error,
                        () => PluginDatabase.Plugin.OpenSettingsView()
                    ));
                }
                else if (PluginDatabase.PluginSettings.Settings.UseLocalised)
                {
                     try
                     {
                         dataExophaseLocalised = await Web.DownloadStringData(fetchUrl);
                     }
                     catch (Exception ex)
                     {
                         Common.LogError(ex, false, $"Exophase localized fetch failed for {searchResult.Url}", true, PluginDatabase.PluginName);
                         dataExophaseLocalised = string.Empty;
                     }
                    // If localized page contains a notice message, skip retries
                    // If localized page contains a notice message, skip retries and fallback to default
                    if (!string.IsNullOrEmpty(dataExophaseLocalised) && dataExophaseLocalised.Contains("Notice Message App"))
                    {
                        Common.LogDebug(true, $"Exophase localized page contained 'Notice Message App'. Falling back to non-localized data.");
                        dataExophaseLocalised = null;
                    }
                 }

                List<Achievement> All = ParseData(dataExophase);
                List<Achievement> AllLocalised = dataExophaseLocalised.IsNullOrEmpty() ? new List<Achievement>() : ParseData(dataExophaseLocalised);

                // After parsing, cache achievements to disk for future runs (using canonical format)
                try
                {
                    // Build canonical cache format: Dictionary<string, Achievement>
                    var achievementsDict = All.Where(a => !a.Name.IsNullOrEmpty())
                        .GroupBy(a => a.Name)
                        .ToDictionary(g => g.Key, g => g.First());
                    
                    if (achievementsDict.Count > 0)
                    {
                        var cacheDir = GetCacheDirectory();
                        if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);
                        // Use the same normalized URL for cache key as used in reads
                        string cacheKey = GenerateCacheKey(cacheKeyUrl);
                        string cacheFile = Path.Combine(cacheDir, cacheKey + ".json");
                        File.WriteAllText(cacheFile, Serialization.ToJson(achievementsDict));
                        
                        // Register images separately (requires name->url map)
                        var imagesDict = achievementsDict
                            .Where(kv => !kv.Value.UrlUnlocked.IsNullOrEmpty())
                            .ToDictionary(kv => kv.Key, kv => kv.Value.UrlUnlocked);
                        if (imagesDict.Count > 0)
                        {
                            Services.AchievementImageResolver.RegisterImages(game, imagesDict);
                        }
                    }
                }
                catch (Exception exCache)
                {
                    Common.LogError(exCache, false, "Error caching Exophase images", true, PluginDatabase.PluginName);
                }

                for (int i = 0; i < All?.Count; i++)
                {
                    allAchievements.Add(new Achievement
                    {
                        Name = AllLocalised.Count > 0 ? AllLocalised[i].Name : All[i].Name,
                        ApiName = All[i].Name,
                        UrlUnlocked = All[i].UrlUnlocked,
                        Description = AllLocalised.Count > 0 ? AllLocalised[i].Description : All[i].Description,
                        DateUnlocked = All[i].DateUnlocked,
                        Percent = All[i].Percent,
                        GamerScore = All[i].GamerScore
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            gameAchievements.Items = allAchievements;

            // Set source link
            if (gameAchievements.HasAchievements)
            {
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = searchResult.Name.IsNullOrEmpty() ? searchResult.Name : searchResult.Name,
                    Name = "Exophase",
                    Url = searchResult.Url
                };
            }

            return gameAchievements;
        }


        #region Configuration

        public override bool ValidateConfiguration()
        {
            // The authentification is only for localised achievement
            return true;
        }

        public override bool IsConnected()
        {
            if (CachedIsConnectedResult == null)
            {
                CachedIsConnectedResult = GetIsUserLoggedIn();
            }
            return (bool)CachedIsConnectedResult;
        }

        public override bool EnabledInSettings()
        {
            // No necessary activation
            return true;
        }

        #endregion

        #region Exophase

        public Task Login()
        {
            try
            {
                FileSystem.DeleteFile(CookiesPath);
                ResetCachedIsConnectedResult();

                WebViewSettings webViewSettings = new WebViewSettings
                {
                    WindowWidth = 580,
                    WindowHeight = 700,
                    JavaScriptEnabled = true,
                    // This is needed otherwise captcha won't pass
                    UserAgent = Web.UserAgent
                };

                using (IWebView webView = API.Instance.WebViews.CreateView(webViewSettings))
                {
                    webView.LoadingChanged += (s, e) =>
                    {
                        string address = webView.GetCurrentAddress();
                        if (address.StartsWith(UrlExophaseAccount, StringComparison.InvariantCultureIgnoreCase) && !address.StartsWith(UrlExophaseLogout, StringComparison.InvariantCultureIgnoreCase))
                        {
                            var cookies = webView.GetCookies();
                            if (cookies?.Count > 0)
                            {
                                SetCookies(cookies);
                                CachedIsConnectedResult = true;
                            }
                            else
                            {
                                Logger.Warn("Exophase Login: No cookies found in WebView.");
                            }
                            webView.Close();
                        }
                    };

                    webView.DeleteDomainCookies(CookiesDomains.First());
                    webView.Navigate(UrlExophaseLogin);
                    _ = webView.OpenDialog();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
            return Task.CompletedTask;
        }

        private bool GetIsUserLoggedIn()
        {
            if (SynchronizationContext.Current != null)
            {
                throw new InvalidOperationException(
                    "GetIsUserLoggedIn must be called from a background thread. " +
                    "Use Task.Run() to avoid deadlocks.");
            }

            var data = Web.DownloadSourceDataWebView(UrlExophaseAccount, GetCookies(), true, CookiesDomains).GetAwaiter().GetResult();
            bool isConnected = data.Item1.Contains("column-username", StringComparison.InvariantCultureIgnoreCase);

            if (isConnected)
            {
                SetCookies(data.Item2);
            }

            return isConnected;
        }


        public List<SearchResult> SearchGame(string name, string platforms = "")
        {
            List<SearchResult> listSearchGames = new List<SearchResult>();
            try
			{
				string urlSearch = platforms.IsNullOrEmpty() || platforms.IsEqual(ResourceProvider.GetString("LOCAll"))
					? string.Format(UrlExophaseSearch, WebUtility.UrlEncode(name))
					: string.Format(UrlExophaseSearchPlatform, WebUtility.UrlEncode(name), platforms);

                var dataText = Web.DownloadJsonDataWebView(urlSearch, GetCookies()).GetAwaiter().GetResult();
				string json = dataText.Item1;

                if (!Serialization.TryFromJson(json, out ExophaseSearchResult exophaseScheachResult))
                {
                    Logger.Warn($"No Exophase result for {name}");
                    Logger.Warn($"{json}");
                    return listSearchGames;
                }

                List<List> listExophase = exophaseScheachResult?.Games?.List;
                if (listExophase != null)
                {
                    listSearchGames = listExophase.Select(x => new SearchResult
                    {
                        Url = x.EndpointAwards,
                        Name = x.Title,
                        UrlImage = x.Images.O ?? x.Images.L ?? x.Images.M,
                        Platforms = x.Platforms.Select(p => p.Name).ToList(),
                        AchievementsCount = x.TotalAwards ?? 0
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on SearchGame({name})", true, PluginDatabase.PluginName);
            }

            return listSearchGames;
        }


        private string GetAchievementsPageUrl(GameAchievements gameAchievements, Services.SuccessStoryDatabase.AchievementSource source)
        {
            bool usedSplit = false;

            string sourceLinkName = gameAchievements.SourcesLink?.Name;
            if (sourceLinkName == "Exophase")
            {
                return gameAchievements.SourcesLink.Url;
            }

            List<SearchResult> searchResults = SearchGame(gameAchievements.Name);
            if (searchResults.Count == 0)
            {
                Logger.Warn($"No game found for {gameAchievements.Name} in GetAchievementsPageUrl()");

                Thread.Sleep(1000);
                searchResults = SearchGame(CommonPluginsShared.PlayniteTools.NormalizeGameName(gameAchievements.Name));
                if (searchResults.Count == 0)
                {
                    Logger.Warn($"No game found for {CommonPluginsShared.PlayniteTools.NormalizeGameName(gameAchievements.Name)} in GetAchievementsPageUrl()");

                    Thread.Sleep(1000);
                    searchResults = SearchGame(Regex.Match(gameAchievements.Name, @"^.*(?=[:-])").Value);
                    usedSplit = true;
                    if (searchResults.Count == 0)
                    {
                        Logger.Warn($"No game found for {Regex.Match(gameAchievements.Name, @"^.*(?=[:-])").Value} in GetAchievementsPageUrl()");
                        return null;
                    }
                }
            }

            string normalizedGameName = usedSplit ? CommonPluginsShared.PlayniteTools.NormalizeGameName(Regex.Match(gameAchievements.Name, @"^.*(?=[:-])").Value) : CommonPluginsShared.PlayniteTools.NormalizeGameName(gameAchievements.Name);
            SearchResult searchResult = searchResults.Find(x => CommonPluginsShared.PlayniteTools.NormalizeGameName(x.Name) == normalizedGameName && PlatformAndProviderMatch(x, gameAchievements, source));

            if (searchResult == null)
            {
                Logger.Warn($"No matching game found for {gameAchievements.Name} in GetAchievementsPageUrl()");
            }

            return searchResult?.Url;
        }


        /// <summary>
        /// Set achievement rarity via Exophase web scraping.
        /// </summary>
        /// <param name="gameAchievements"></param>
        /// <param name="source"></param>
        public void SetRarety(GameAchievements gameAchievements, Services.SuccessStoryDatabase.AchievementSource source, CancellationToken cancellationToken = default)
        {
            string achievementsUrl = GetAchievementsPageUrl(gameAchievements, source);
            if (achievementsUrl.IsNullOrEmpty())
            {
                Logger.Warn($"No Exophase (rarity) url found for {gameAchievements.Name} - {gameAchievements.Id}");
                return;
            }

            try
            {
                GameAchievements exophaseAchievements = GetAchievements(gameAchievements.Game, achievementsUrl, cancellationToken);
                var missingMatches = new List<string>();
                
                foreach (var y in exophaseAchievements.Items)
                {
                    Achievement achievement = gameAchievements.Items.Find(x => x.ApiName.IsEqual(y.ApiName));
                    if (achievement == null)
                    {
                        achievement = gameAchievements.Items.Find(x => x.Name.IsEqual(y.Name));
                        if (achievement == null)
                        {
                            achievement = gameAchievements.Items.Find(x => x.Name.IsEqual(y.ApiName));
                        }
                    }

                    if (achievement != null)
                    {
                        achievement.ApiName = y.ApiName;
                        achievement.Percent = y.Percent;
                        achievement.GamerScore = StoreApi.CalcGamerScore(y.Percent);

                        if (PluginDatabase.PluginSettings.Settings.UseLocalised && IsConnected())
                        {
                            achievement.Name = y.Name;
                            achievement.Description = y.Description;
                        }
                    }
                    else
                    {
                        // Collect missing names and log a single summary after processing to avoid flooding logs
                        try
                        {
                            if (!string.IsNullOrEmpty(y.Name))
                            {
                                missingMatches.Add(y.Name);
                            }
                        }
                        catch (Exception ex) { Logger.Debug($"Exophase missing match collection failed: {ex.Message}"); }
                    }
                }

                if (missingMatches.Count > 0)
                {
                    // limit output length
                    int maxShow = 10;
                    string sample = string.Join(", ", missingMatches.Take(maxShow));
                    if (missingMatches.Count > maxShow)
                    {
                        sample += ", ...";
                    }
                    Logger.Warn($"No Exophase (rarity) matching achievements found for {gameAchievements.Name} - {gameAchievements.Id} in {achievementsUrl}: {missingMatches.Count} missing. Examples: {sample}");
                }

                PluginDatabase.AddOrUpdate(gameAchievements);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private static bool PlatformAndProviderMatch(SearchResult exophaseGame, GameAchievements playniteGame, Services.SuccessStoryDatabase.AchievementSource achievementSource)
        {
            switch (achievementSource)
            {
                //PC: match service
                case Services.SuccessStoryDatabase.AchievementSource.Steam:
                    return exophaseGame.Platforms.Contains("Steam", StringComparer.InvariantCultureIgnoreCase);

                case Services.SuccessStoryDatabase.AchievementSource.GOG:
                    return exophaseGame.Platforms.Contains("GOG", StringComparer.InvariantCultureIgnoreCase);

                case Services.SuccessStoryDatabase.AchievementSource.EA:
                    return exophaseGame.Platforms.Contains("Electronic Arts", StringComparer.InvariantCultureIgnoreCase);

                case Services.SuccessStoryDatabase.AchievementSource.RetroAchievements:
                    return exophaseGame.Platforms.Contains("Retro", StringComparer.InvariantCultureIgnoreCase);

                case Services.SuccessStoryDatabase.AchievementSource.Overwatch:
                case Services.SuccessStoryDatabase.AchievementSource.Starcraft2:
                case Services.SuccessStoryDatabase.AchievementSource.Wow:
                    return exophaseGame.Platforms.Contains("Blizzard", StringComparer.InvariantCultureIgnoreCase);

                //Console: match platform
                case Services.SuccessStoryDatabase.AchievementSource.Playstation:
                case Services.SuccessStoryDatabase.AchievementSource.Xbox:
                case Services.SuccessStoryDatabase.AchievementSource.RPCS3:
                    return PlatformsMatch(exophaseGame, playniteGame);

                case Services.SuccessStoryDatabase.AchievementSource.Epic:
                case Services.SuccessStoryDatabase.AchievementSource.GenshinImpact:
                case Services.SuccessStoryDatabase.AchievementSource.GuildWars2:
                case Services.SuccessStoryDatabase.AchievementSource.None:
                case Services.SuccessStoryDatabase.AchievementSource.Local:
                default:
                    return false;
            }
        }

        private static Dictionary<string, string[]> PlaynitePlatformSpecificationIdToExophasePlatformName => new Dictionary<string, string[]>
        {
            { "xbox360", new[]{"Xbox 360"} },
            { "xbox_one", new[]{"Xbox One"} },
            { "xbox_series", new[]{"Xbox Series"} },
            { "xbox_game_pass", new []{"Windows 8", "Windows 10", "Windows 11", "GFWL", "Xbox 360", "Xbox One", "Xbox Series" } },
            { "pc_windows", new []{"Windows 8", "Windows 10", "Windows 11" /* future proofing */, "GFWL"} },
            { "sony_playstation3", new[]{"PS3"} },
            { "sony_playstation4", new[]{"PS4"} },
            { "sony_playstation5", new[]{"PS5"} },
            { "sony_vita", new[]{"PS Vita"} },
        };

        private static bool PlatformsMatch(SearchResult exophaseGame, GameAchievements playniteGame)
        {
            foreach (Platform playnitePlatform in playniteGame.Platforms)
            {
                string sourceName = string.Empty;
                string key = string.Empty;
                try
                {
                    sourceName = API.Instance.Database.Games.Get(playniteGame.Id)?.Source?.Name;
                    key = sourceName == "Xbox Game Pass" ? "xbox_game_pass" : playnitePlatform.SpecificationId;
                    if (!PlaynitePlatformSpecificationIdToExophasePlatformName.TryGetValue(key, out string[] exophasePlatformNames))
                    {
                        continue;
                    }

                    if (exophaseGame?.Platforms?.IntersectsExactlyWith(exophasePlatformNames) ?? false)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on PlatformsMatch with {sourceName} - {key}");
                }
            }
            return false;
        }

        #endregion


        private List<Achievement> ParseData(string data)
        {
            HtmlParser parser = new HtmlParser();
            IHtmlDocument htmlDocument = parser.Parse(data);

            List<Achievement> allAchievements = new List<Achievement>();
            IHtmlCollection<IElement> sectionAchievements = htmlDocument.QuerySelectorAll("ul.achievement, ul.trophy, ul.challenge");
            string gameName = htmlDocument.QuerySelector("h2.me-2 a")?.GetAttribute("title");

            if (sectionAchievements == null || sectionAchievements.Count() == 0)
            {
                Logger.Warn("Exophase data is not parsed");
                return new List<Achievement>();
            }
            else
            {
                foreach (IElement section in sectionAchievements)
                {
                    foreach (IElement searchAchievements in section.QuerySelectorAll("li"))
                    {
                        try
                        {
                            string sFloat = searchAchievements.GetAttribute("data-average")
                                ?.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                                ?.Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

                            _ = float.TryParse(sFloat, out float Percent);

                            string urlUnlocked = searchAchievements.QuerySelector("img")?.GetAttribute("src");
                            string name = WebUtility.HtmlDecode(searchAchievements.QuerySelector("a")?.InnerHtml);
                            string description = WebUtility.HtmlDecode(searchAchievements.QuerySelector("div.award-description p")?.InnerHtml);
                            bool isHidden = searchAchievements.GetAttribute("class").IndexOf("secret") > -1;

                            allAchievements.Add(new Achievement
                            {
                                Name = name,
                                UrlUnlocked = urlUnlocked,
                                Description = description,
                                DateUnlocked = default(DateTime),
                                Percent = Percent,
                                GamerScore = StoreApi.CalcGamerScore(Percent)
                            });
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        }
                    }
                }
            }

            return allAchievements;
        }




        private void ScheduleBackgroundFetch(string fetchUrl, string searchResultUrl, Game game)
        {
            Task.Run(async () =>
            {
                CancellationToken token;
                lock (_bgFetchCtsLock)
                {
                    if (_bgFetchCts == null || _bgFetchCts.IsCancellationRequested)
                    {
                        Logger.Debug($"Exophase background fetch skipped - shutdown in progress or completed");
                        return;
                    }
                    token = _bgFetchCts.Token;
                }
                
                try
                {
                    await _bgFetchSemaphore.WaitAsync(token);
                }
                catch (OperationCanceledException)
                {
                    Logger.Debug($"Exophase background fetch cancelled before starting for {searchResultUrl}");
                    return;
                }
                catch (ObjectDisposedException)
                {
                    Logger.Debug($"Exophase background fetch semaphore disposed");
                    return;
                }
                
                try
                {
                    token.ThrowIfCancellationRequested();
                    
                    var webDataBg = await Web.DownloadSourceDataWebView(fetchUrl, GetCookies(), true, CookiesDomains);
                    if (webDataBg.Item1.IsNullOrEmpty())
                    {
                        Logger.Warn($"Exophase background fetch: no data from {fetchUrl}");
                        return;
                    }

                    var parsed = ParseData(webDataBg.Item1);
                    CacheAndApplyImages(parsed, searchResultUrl, game);
                }
                catch (OperationCanceledException)
                {
                    Logger.Debug($"Exophase background fetch cancelled for {searchResultUrl}");
                }
                catch (Exception bgEx)
                {
                    Common.LogError(bgEx, false, $"Exophase background fetch failed for {searchResultUrl}", true, PluginDatabase.PluginName);
                }
                finally
                {
                    try
                    {
                        _bgFetchSemaphore.Release();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Already disposed, ignore
                    }
                }
            });
        }

        private void CacheAndApplyImages(List<Achievement> parsed, string cacheKeyUrl, Game game)
        {
            // Filter out empty Name/UrlUnlocked and de-duplicate by Name
            var achievementsDict = new Dictionary<string, Achievement>();
            var imagesDict = new Dictionary<string, string>();
            var imagesNormalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var a in parsed)
            {
                if (a.Name.IsNullOrEmpty()) continue;
                if (!achievementsDict.ContainsKey(a.Name))
                {
                    achievementsDict.Add(a.Name, a);
                    if (!a.UrlUnlocked.IsNullOrEmpty())
                    {
                        imagesDict.Add(a.Name, a.UrlUnlocked);
                        
                        string keyNorm = a.Name.RemoveDiacritics().ToLowerInvariant().Trim();
                        if (!imagesNormalized.ContainsKey(keyNorm))
                        {
                            imagesNormalized.Add(keyNorm, a.UrlUnlocked);
                        }
                    }
                }
            }

            if (achievementsDict.Count > 0)
            {
                var cacheDir = GetCacheDirectory();
                if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

                string cacheKey = GenerateCacheKey(cacheKeyUrl);
                string cacheFile = Path.Combine(cacheDir, cacheKey + ".json");
                File.WriteAllText(cacheFile, Serialization.ToJson(achievementsDict));

                if (imagesDict.Count > 0)
                {
                    Services.AchievementImageResolver.RegisterImages(game, imagesDict);
                }

                try
                {
                    // Also apply images to any existing GameAchievements in the plugin DB so UI updates immediately
                    var existing = PluginDatabase.Get(game, true);
                    if (existing != null && existing.Items != null && existing.Items.Count > 0)
                    {
                        bool changed = false;
                        foreach (var it in existing.Items)
                        {
                            if (it == null) continue;
                            string name = it.Name ?? string.Empty;
                            string keyNorm = name.RemoveDiacritics().ToLowerInvariant().Trim();
                            
                            if (keyNorm.IsNullOrEmpty()) continue;

                            if (imagesNormalized.TryGetValue(keyNorm, out string url))
                            {
                                if (it.UrlUnlocked != url)
                                {
                                    it.UrlUnlocked = url;
                                    changed = true;
                                }
                            }
                        }

                        if (changed)
                        {
                            PluginDatabase.AddOrUpdate(existing);
                        }
                    }
                }
                catch (Exception exUpdate)
                {
                    Common.LogError(exUpdate, false, $"Exophase background: failed to apply images to DB for {game.Name}", true, PluginDatabase.PluginName);
                }
            }
        }


        #region Shutdown

        /// <summary>
        /// Shutdown method to cancel background tasks and dispose resources. Should be called when plugin unloads.
        /// Note: This operation is terminal for the static background fetcher. Background fetches cannot be restarted without reloading the domain/application.
        /// </summary>
        public static void Shutdown()
        {
            lock (_bgFetchCtsLock)
            {
                try
                {
                    _bgFetchCts?.Cancel();
                }
                catch (ObjectDisposedException ex)
                {
                    Logger.Debug($"Background fetch cancel: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Error cancelling background fetch tasks: {ex.Message}");
                }
                
                try
                {
                    _bgFetchCts?.Dispose();
                }
                catch (ObjectDisposedException ex)
                {
                    Logger.Debug($"Background fetch dispose: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Error disposing cancellation token source: {ex.Message}");
                }
            }
            
            // Do not dispose semaphore as it is static and reused across reloads
            /*
            try
            {
                _bgFetchSemaphore?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
            catch (Exception ex)
            {
                Logger.Warn($"Error disposing semaphore: {ex.Message}");
            }
            */
        }

        #endregion
    }
}