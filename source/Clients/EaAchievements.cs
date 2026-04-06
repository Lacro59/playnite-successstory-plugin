using Playnite.SDK.Models;
using CommonPluginsShared;
using CommonPluginsShared.Models;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using static CommonPluginsShared.PlayniteTools;
using CommonPluginsStores.Ea;
using System.Collections.ObjectModel;
using CommonPluginsStores.Models;
using System.Linq;
using Playnite.SDK;
using CommonPluginsShared.Extensions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    public class EaAchievements : GenericAchievements
    {
        protected static readonly Lazy<EaApi> eaApi = new Lazy<EaApi>(() => new EaApi(PluginDatabase.PluginName));
        internal static EaApi EaApi => eaApi.Value;

        // Fuzzy matching threshold: minimum proportion of words that must match (0.5 = 50%)
        private const double WORD_OVERLAP_THRESHOLD = 0.5;

        public EaAchievements() : base("EA", CodeLang.GetEaLang(API.Instance.ApplicationSettings.Language), CodeLang.GetCountryFromLast(API.Instance.ApplicationSettings.Language))
        {
            EaApi.SetLanguage(API.Instance.ApplicationSettings.Language);
        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> AllAchievements = new List<Achievement>();

            if (IsConnected())
            {
                try
                {
                    ObservableCollection<GameAchievement> originAchievements = EaApi.GetAchievements(game.GameId, EaApi.CurrentAccountInfos);

                    if (originAchievements?.Count > 0)
                    {
                        // Include all achievements (locked and unlocked)
                        AllAchievements = originAchievements
                            .Select(x => new Achievement
                        {
                            ApiName = x.Id,
                            Name = x.Name,
                            Description = x.Description,
                            UrlUnlocked = x.UrlUnlocked,
                            UrlLocked = x.UrlLocked,
                            DateUnlocked = x.DateUnlocked == default(DateTime) ? (DateTime?)null : x.DateUnlocked,
                            Percent = x.Percent,
                            GamerScore = x.GamerScore
                        }).ToList();
                        gameAchievements.Items = AllAchievements;
                    }

                    // Set source link
                    try
                    {
                        if (gameAchievements.HasAchievements)
                        {
                            gameAchievements.SourcesLink = new SourceLink
                            {
                                GameName = game.Name,
                                Name = "EA",
                                Url = "https://www.ea.com"
                            };

                            if (gameAchievements.Items == null || gameAchievements.Items.Any(x => x.UrlUnlocked.IsNullOrEmpty()))
                            {
                                var images = FetchExternalImages(game);
                                if (images.Count > 0)
                                {
                                    MapImagesToAchievements(game, gameAchievements, images);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"EaAchievements: Error fetching achievements for {game.Name}");
                    ShowNotificationPluginError(ex);
                    return gameAchievements;
                }
            }
            else
            {
                Logger.Warn($"EaAchievements: Not connected when fetching for {game.Name}");
                ShowNotificationPluginNoAuthenticate(ExternalPlugin.OriginLibrary);
            }

            gameAchievements.SetRaretyIndicator();
            if (gameAchievements.HasAchievements)
            {
                PluginDatabase.AddOrUpdate(gameAchievements);
            }
            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            if (!PluginDatabase.PluginSettings.Settings.PluginState.OriginIsEnabled)
            {
                ShowNotificationPluginDisable(ResourceProvider.GetString("LOCSuccessStoryNotificationsEaDisabled"));
                return false;
            }
            else
            {
                if (CachedConfigurationValidationResult == null)
                {
                    CachedConfigurationValidationResult = IsConnected();

                    if (!(bool)CachedConfigurationValidationResult)
                    {
                        ShowNotificationPluginNoAuthenticate(ExternalPlugin.OriginLibrary);
                    }
                }
                else if (!(bool)CachedConfigurationValidationResult)
                {
                    ShowNotificationPluginErrorMessage(ExternalPlugin.OriginLibrary);
                }

                return (bool)CachedConfigurationValidationResult;
            }
        }

        public override bool IsConnected()
        {
            if (CachedIsConnectedResult == null)
            {
                try
                {
                    CachedIsConnectedResult = EaApi.IsUserLoggedIn;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, true);
                    CachedIsConnectedResult = false;
                }
            }
            
            return (bool)CachedIsConnectedResult;
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableOrigin;
        }

        public override void ResetCachedConfigurationValidationResult()
        {
            CachedConfigurationValidationResult = null;
            EaApi.ResetIsUserLoggedIn();
        }

        public override void ResetCachedIsConnectedResult()
        {
            CachedIsConnectedResult = null;
            EaApi.ResetIsUserLoggedIn();
        }
        #endregion


        private Dictionary<string, string> FetchExternalImages(Game game)
        {
            Dictionary<string, string> images = new Dictionary<string, string>();

            // 0) Try to get images from the resolver cache first
            if (Services.AchievementImageResolver.TryGetImages(game, out var resolverImages) && resolverImages?.Count > 0)
            {
                images = resolverImages;
                Logger.Info($"Found {images.Count} images in resolver cache for {game.Name}");
                return images;
            }

            // 1) Try Exophase first (it's known to have EA images sometimes)
            try
            {
                if (SuccessStory.ExophaseAchievements != null)
                {
                    // Try with platform filter first
                    var exSearch = SuccessStory.ExophaseAchievements.SearchGame(game.Name, "Electronic Arts");
                    if (exSearch == null || exSearch.Count == 0)
                    {
                        exSearch = SuccessStory.ExophaseAchievements.SearchGame(game.Name);
                    }

                    if (exSearch != null && exSearch.Count > 0)
                    {
                        SearchResult exMatch = FindBestExophaseMatch(exSearch, game);
                        if (exMatch != null && !exMatch.Url.IsNullOrEmpty())
                        {
                            string exUrl = exMatch.Url;
                            if (exUrl.StartsWith("/")) exUrl = "https://www.exophase.com" + exUrl;
                            var exAch = Task.Run(() => SuccessStory.ExophaseAchievements.GetAchievements(game, exUrl)).GetAwaiter().GetResult();
                            if (exAch?.Items?.Count > 0)
                            {
                                string exBase = "https://www.exophase.com";
                                foreach (var i in exAch.Items.Where(i => !i.UrlUnlocked.IsNullOrEmpty()))
                                {
                                    string key = i.Name ?? string.Empty;
                                    string urlImg = i.UrlUnlocked;
                                    if (urlImg.StartsWith("/")) urlImg = exBase + urlImg;
                                    if (!images.ContainsKey(key) && !string.IsNullOrEmpty(urlImg)) images.Add(key, urlImg);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exSearch)
            {
                Common.LogError(exSearch, false, "Error while searching Exophase for EA images", true, PluginDatabase.PluginName);
            }

            // 2) If still no images, try TrueAchievements - Xbox origin
            if (images.Count == 0)
            {
                try
                {
                    var taGames = TrueAchievements.SearchGame(game, TrueAchievements.OriginData.Xbox);
                    if (taGames.Count > 0)
                    {
                        var match = taGames.First();
                        if (!match.GameUrl.IsNullOrEmpty()) images = TrueAchievements.GetDataImages(match.GameUrl);
                    }
                }
                catch (Exception exTaXbox)
                {
                    Common.LogError(exTaXbox, false, "Error while searching TrueAchievements Xbox for EA images", true, PluginDatabase.PluginName);
                }
            }

            // 3) Fallback TrueAchievements - Steam origin
            if (images.Count == 0)
            {
                try
                {
                    var taGamesSteam = TrueAchievements.SearchGame(game, TrueAchievements.OriginData.Steam);
                    if (taGamesSteam.Count > 0)
                    {
                        var match = taGamesSteam.First();
                        if (!match.GameUrl.IsNullOrEmpty()) images = TrueAchievements.GetDataImages(match.GameUrl);
                    }
                }
                catch (Exception exTa)
                {
                    Common.LogError(exTa, false, "Error while searching TrueAchievements for EA images", true, PluginDatabase.PluginName);
                }
            }

            return images;
        }

        private void MapImagesToAchievements(Game game, GameAchievements gameAchievements, Dictionary<string, string> images)
        {
            if (images == null || images.Count == 0 || gameAchievements == null) return;

            try
            {
                Services.AchievementImageResolver.RegisterImages(game, images);
            }
            catch (Exception exReg)
            {
                Common.LogError(exReg, false, "Error registering achievement images", true, PluginDatabase.PluginName);
            }

            var imagesNormalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in images)
            {
                string keyNorm = Normalize(kv.Key);
                if (!keyNorm.IsNullOrEmpty() && !imagesNormalized.ContainsKey(keyNorm)) imagesNormalized.Add(keyNorm, kv.Value);
            }

            if (gameAchievements.Items == null) return;

            // Pre-compute word sets for all images once (outside the achievement loop)
            var imageWordSets = imagesNormalized.ToDictionary(
                kv => kv.Key,
                kv => new HashSet<string>(kv.Key.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 2))
            );

            foreach (var ach in gameAchievements.Items)
            {
                bool assigned = false;
                string achNorm = Normalize(ach.Name);

                // Try exact match
                if (imagesNormalized.TryGetValue(achNorm, out string exactUrl))
                {
                    ach.UrlUnlocked = exactUrl;
                    ach.UrlLocked = exactUrl;
                    assigned = true;
                }

                // Try Contains match and fuzzy bidirectional matching (consolidated)
                if (!assigned && achNorm.Length >= 5) // Minimum length to avoid false positives
                {
                    var found = imagesNormalized.FirstOrDefault(x => 
                        x.Key.Length >= 5 && 
                        (x.Key.Contains(achNorm) || achNorm.Contains(x.Key) || 
                         achNorm.StartsWith(x.Key) || x.Key.StartsWith(achNorm)));
                    
                    if (!found.Equals(default(KeyValuePair<string, string>)))
                    {
                        ach.UrlUnlocked = found.Value;
                        ach.UrlLocked = found.Value;
                        assigned = true;
                        Logger.Debug($"EA: Fuzzy matched '{ach.Name}' to image key '{found.Key}'");
                    }
                }

                // Word overlap matching - use pre-computed word sets and reuse normalized value
                if (!assigned && !achNorm.IsNullOrEmpty())
                {
                    // Extract words from already-normalized achievement name
                    var achWords = achNorm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => w.Length > 2).ToList();

                    if (achWords.Count > 0)
                    {
                        foreach (var kv in imageWordSets)
                        {
                            int overlap = achWords.Count(w => kv.Value.Contains(w));
                            if (overlap >= Math.Ceiling(achWords.Count * WORD_OVERLAP_THRESHOLD))
                            {
                                ach.UrlUnlocked = imagesNormalized[kv.Key];
                                ach.UrlLocked = imagesNormalized[kv.Key];
                                assigned = true;
                                Logger.Debug($"EA: Word overlap matched '{ach.Name}' to image key (overlap: {overlap}/{achWords.Count})");
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static SearchResult FindBestExophaseMatch(List<SearchResult> searchResults, Game game)
        {
            if (searchResults == null || searchResults.Count == 0)
            {
                return null;
            }

            // Try exact EA platform match
            var match = searchResults.FirstOrDefault(x => 
                x.Platforms != null && 
                x.Platforms.Any(p => p.Equals("Electronic Arts", StringComparison.InvariantCultureIgnoreCase)));
            
            if (match == null)
            {
                // Try PC preference
                match = searchResults.FirstOrDefault(s => PrefersPc(s));
            }

            // Filter out Nintendo/Switch platforms
            var filtered = searchResults.Where(x => !IsNintendoOrSwitchPlatform(x)).ToList();
            if (filtered.Count > 0 && match == null)
            {
                // Try EA platform in filtered results
                match = filtered.FirstOrDefault(x => 
                    x.Platforms != null && 
                    x.Platforms.Any(p => p.Equals("Electronic Arts", StringComparison.InvariantCultureIgnoreCase)));
                
                if (match == null)
                {
                    // Try PC in filtered
                    match = filtered.FirstOrDefault(s => PrefersPc(s));
                }
                
                if (match == null)
                {
                    // Try normalized name match
                    string normalizedGame = NormalizeGameName(game.Name);
                    match = filtered.FirstOrDefault(x => NormalizeGameName(x.Name).IsEqual(normalizedGame));
                }
                
                if (match == null && filtered.Count > 0)
                {
                    match = filtered.First();
                }
            }

            // Fallback to any match
            if (match == null)
            {
                string normalizedGame = NormalizeGameName(game.Name);
                match = searchResults.FirstOrDefault(x => NormalizeGameName(x.Name).IsEqual(normalizedGame));
            }
            
            if (match == null && searchResults.Count > 0)
            {
                match = searchResults.First();
            }

            return match;
        }

        private static bool IsNintendoOrSwitchPlatform(SearchResult result)
        {
            if (result == null) return false;
            
            string name = result.Name ?? string.Empty;
            if (name.IndexOf("switch", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                name.IndexOf("nintendo", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                return true;
            }
            
            if (result.Platforms != null)
            {
                return result.Platforms.Any(p => 
                    p.IndexOf("switch", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                    p.IndexOf("nintendo", StringComparison.InvariantCultureIgnoreCase) >= 0);
            }
            
            return false;
        }

        private static bool PrefersPc(SearchResult s)
        {
            if (s == null) return false;

            bool platformMatch = s.Platforms?.Any(p => 
                p.Contains("PC", StringComparison.InvariantCultureIgnoreCase) || 
                p.Contains("Windows", StringComparison.InvariantCultureIgnoreCase) || 
                p.Contains("Origin", StringComparison.InvariantCultureIgnoreCase)) ?? false;

            bool nameMatch = s.Name?.Contains("PC", StringComparison.InvariantCultureIgnoreCase) ?? false;
            
            return platformMatch || nameMatch;
        }


        private static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            string result = s.RemoveDiacritics().Trim().ToLowerInvariant();
            result = Regex.Replace(result, @"[^a-z0-9\s]", "");
            result = Regex.Replace(result, @"\s+", " ").Trim();
            return result;
        }
    }
}
