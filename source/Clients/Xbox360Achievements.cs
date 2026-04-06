using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Data;
using CommonPluginsShared;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommonPluginsShared.Extensions;
using CommonPlayniteShared.Common;
using SteamKit2.GC.TF2.Internal;
using CommonPluginsShared.Models;
using System.Threading.Tasks;
using System.Windows;

namespace SuccessStory.Clients
{
    public class Xbox360Achievements : GenericAchievements
    {
        private readonly IPlayniteAPI _playniteApi;
        private readonly ILogger _logger;
        private string _xeniaPath;
        private volatile bool _isInitialized;
        private readonly object _initLock = new object();
        private const string SUCCESS_STORY_GUID = "cebe6d32-8c46-4459-b993-5a5189d60788";
        private string _playniteAppData;
        private string _xeniaLogFilePath;
        private string _xeniaAchievementsDir;
        private string _successStoryDataDir;
        private string _xeniaJsonTempDir;

        public Xbox360Achievements() : base("Xbox360")
        {
            _playniteApi = API.Instance;
            _logger = LogManager.GetLogger();
            // _isInitialized defaults to false, no need to set explicitly
            // Do not read Xenia path or initialize paths at construction time to avoid probing
            // the user's system when Xbox360 support is disabled. Defer initialization until
            // the feature is actually used or when explicit Xenia path is provided.
            _playniteAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logger.Debug("Xbox360Achievements constructed (lazy init)");
        }

        // Overloaded constructor used by settings UI to provide API instance and optional xenia path
        public Xbox360Achievements(IPlayniteAPI apiInstance, string xeniaPath) : base("Xbox360")
        {
            _playniteApi = apiInstance ?? API.Instance;
            _logger = LogManager.GetLogger();
            // _isInitialized defaults to false, no need to set explicitly
            _playniteAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _xeniaPath = xeniaPath; // may be a folder path or exe path depending on caller
            _logger.Debug($"Xbox360Achievements constructed with provided xeniaPath: '{_xeniaPath}'");
        }

        // Ensure paths and environment are initialized lazily to avoid probing at startup
        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            lock (_initLock)
            {
                if (_isInitialized)
                {
                    return;
                }

                try
                {
                    // Prefer explicit ctor-provided _xeniaPath over settings
                    if (string.IsNullOrEmpty(_xeniaPath))
                    {
                        // Only fallback to settings if no explicit path was provided
                        try
                        {
                            var settings = SuccessStory.PluginDatabase?.PluginSettings?.Settings;
                            _xeniaPath = settings?.XeniaInstallationFolder;
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Error accessing SuccessStory settings in Xbox360Achievements.EnsureInitialized");
                        }
                    }

                    // InitializePaths now returns bool for success/failure
                    bool success = InitializePaths();
                    if (success)
                    {
                        _isInitialized = true;
                        _logger.Debug($"Xbox360Achievements initialized. XeniaPath='{_xeniaPath}'");
                    }
                    else
                    {
                        _logger.Warn("Xbox360Achievements InitializePaths failed");
                        _isInitialized = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Xbox360Achievements EnsureInitialized failed");
                    _isInitialized = false;
                }
            }
        }

        private bool InitializePaths()
        {
            if (string.IsNullOrEmpty(_xeniaPath))
            {
                _logger.Warn("Xbox360: Xenia path is null or empty");
                return false;
            }

            try
            {
                // Determine xeniaDir: use _xeniaPath directly if it's a directory, otherwise get its parent
                string xeniaDir;
                if (Directory.Exists(_xeniaPath))
                {
                    xeniaDir = _xeniaPath;
                }
                else if (_xeniaPath.EndsWith(Path.DirectorySeparatorChar.ToString()) || _xeniaPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    xeniaDir = _xeniaPath;
                }
                else
                {
                    xeniaDir = Path.GetDirectoryName(_xeniaPath);
                }
                
                if (string.IsNullOrEmpty(xeniaDir))
                {
                    _logger.Warn("Xbox360: Could not determine Xenia directory from path");
                    return false;
                }

                _xeniaLogFilePath = Path.Combine(xeniaDir, "xenia.log");
                _xeniaAchievementsDir = Path.Combine(xeniaDir, "Achievements") + Path.DirectorySeparatorChar;
                
                _successStoryDataDir = Directory.Exists(Path.Combine(_playniteAppData, "Playnite", "ExtensionsData", SUCCESS_STORY_GUID, "SuccessStory"))
                    ? Path.Combine(_playniteAppData, "Playnite", "ExtensionsData", SUCCESS_STORY_GUID, "SuccessStory") + Path.DirectorySeparatorChar
                    : Path.Combine(_playniteApi.Paths.ApplicationPath, "ExtensionsData", SUCCESS_STORY_GUID, "SuccessStory") + Path.DirectorySeparatorChar;
                
                _xeniaJsonTempDir = _xeniaAchievementsDir;

                // Validate required directories are set
                if (string.IsNullOrEmpty(_successStoryDataDir) || string.IsNullOrEmpty(_xeniaAchievementsDir))
                {
                    _logger.Warn("Xbox360: Required directories could not be initialized");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Xbox360: Error in InitializePaths");
                return false;
            }
        }

        public override GameAchievements GetAchievements(Game game)
        {
            _logger.Info($"Xbox360: Getting achievements for game: {game.Name}");
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            bool saved = false;

            if (!EnabledInSettings())
            {
                _logger.Info("Xbox360: Achievement tracking is disabled in settings");
                return gameAchievements;
            }

            EnsureInitialized();

            if (!_isInitialized)
            {
                _logger.Warn("Xbox360: EnsureInitialized failed, cannot get achievements");
                return gameAchievements;
            }

            try
            {
                if (IsConfigured())
                {
                    string gameId = game.Id.ToString();
                    if (!string.IsNullOrEmpty(gameId))
                    {
                        string successStoryJsonFile = Path.Combine(_successStoryDataDir, $"{gameId}.json");
                        string tempJsonFile = Path.Combine(_xeniaJsonTempDir, $"{gameId}.json");
                        string achievementTextFile = Path.Combine(_xeniaAchievementsDir, $"{gameId}.txt");

                        saved = ProcessAchievements(game, gameAchievements, successStoryJsonFile, tempJsonFile, achievementTextFile);

                        // ProcessAchievements handles all persistence via SaveAchievementsAtomically
                        // Trigger UI refresh if this is the selected game
                        if (saved && API.Instance.MainView.SelectedGames?.FirstOrDefault()?.Id == game.Id)
                        {
                            API.Instance.MainView.SelectGames(new List<Guid> { game.Id });
                        }
                    }
                }
                else
                {
                    ShowNotificationPluginNoConfiguration();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Xbox360: Error processing achievements for {game.Name}");
            }

            if (!saved)
            {
                _logger.Warn($"Xbox360: ProcessAchievements failed for {game.Name}, restoring cached data if available");
                // Restore old data if possible to return something meaningful
                gameAchievements = SuccessStory.PluginDatabase.Get(game.Id, true) ?? gameAchievements;
            }
            return gameAchievements;
        }

        private bool ProcessAchievements(Game game, GameAchievements gameAchievements, string successStoryJsonFile, string tempJsonFile, string achievementTextFile)
        {
            var achievements = new List<Achievement>();

            try
            {
                if (File.Exists(successStoryJsonFile))
                {
                    achievements = ParseExistingAchievements(successStoryJsonFile);
                }

                var unlockedAchievements = GetUnlockedAchievements(achievementTextFile);
                UpdateAchievementUnlockDates(achievements, unlockedAchievements);
                SaveAchievementsAtomically(achievements, successStoryJsonFile, game);

                _logger.Info($"Xbox360: Processed {unlockedAchievements.Count} unlocked achievements");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Xbox360: Failed to process achievements for {game.Name}");
                return false;
            }
        }

        private Dictionary<string, DateTime> GetUnlockedAchievements(string achievementTextFile)
        {
            var unlockedAchievements = new Dictionary<string, DateTime>();

            if (File.Exists(_xeniaLogFilePath))
            {
                DateTime logTimestamp = File.GetCreationTime(_xeniaLogFilePath);

                foreach (var line in File.ReadAllLines(_xeniaLogFilePath))
                {
                    if (line.Contains("Achievement unlocked:"))
                    {
                        var achievementName = line.Split(new[] { "Achievement unlocked:" }, StringSplitOptions.None)[1]
                            .Replace("\r", "")
                            .Replace("i> ", "")
                            .Trim();

                        achievementName = Regex.Replace(achievementName, @"i>\s+[A-F0-9]{8}", "");

                        if (!string.IsNullOrEmpty(achievementName))
                        {
                            unlockedAchievements[achievementName] = logTimestamp;

                            if (!File.Exists(achievementTextFile))
                            {
                                string entry = FormatAchievement(achievementName, logTimestamp);
                                File.WriteAllText(achievementTextFile, entry);
                            }
                            else if (!File.ReadAllText(achievementTextFile).Contains($"\"{achievementName}\""))
                            {
                                string entry = FormatAchievement(achievementName, logTimestamp);
                                File.AppendAllText(achievementTextFile, Environment.NewLine + entry);
                            }
                        }
                    }
                }
            }

            if (File.Exists(achievementTextFile))
            {
                var lines = File.ReadAllLines(achievementTextFile);
                for (int i = 0; i < lines.Length; i += 2)
                {
                    if (i + 1 < lines.Length && lines[i].StartsWith("Unlocked:"))
                    {
                        var match = Regex.Match(lines[i + 1].Trim(), @"""([^""]+)""\s+""([^""]+)""");
                        if (match.Success && DateTime.TryParse(match.Groups[2].Value, out DateTime unlockDate))
                        {
                            string name = match.Groups[1].Value;
                            if (!unlockedAchievements.ContainsKey(name))
                            {
                                unlockedAchievements[name] = unlockDate;
                            }
                        }
                    }
                }
            }

            return unlockedAchievements;
        }

        private string FormatAchievement(string name, DateTime date)
        {
            return $"Unlocked:\r\n\"{name}\" \"{date.ToString("yyyy-MM-ddTHH:mm:ss")}\"";
        }

        private List<Achievement> ParseExistingAchievements(string jsonPath)
        {
            try
            {
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var gameAchievements = Serialization.FromJson<GameAchievements>(jsonContent);
                    return gameAchievements.Items ?? new List<Achievement>();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Xbox360: Failed to parse existing achievements from {jsonPath}");
            }
            return new List<Achievement>();
        }

        private void UpdateAchievementUnlockDates(List<Achievement> achievements, Dictionary<string, DateTime> unlockedAchievements)
        {
            foreach (var achievement in achievements)
            {
                if (unlockedAchievements.ContainsKey(achievement.Name))
                {
                    achievement.DateUnlocked = unlockedAchievements[achievement.Name];
                }
            }

            achievements.Sort((a, b) => Nullable.Compare(a.DateUnlocked, b.DateUnlocked));
        }

        private void SaveAchievementsAtomically(List<Achievement> achievements, string finalPath, Game game)
        {
            string gameId = game.Id.ToString();
            string tempPath = Path.Combine(_xeniaAchievementsDir, $"{gameId}.temp.json");

            try
            {
                GameAchievements gameAchievements;
                if (File.Exists(finalPath))
                {
                    string existingJson = File.ReadAllText(finalPath);
                    gameAchievements = Serialization.FromJson<GameAchievements>(existingJson);

                    foreach (var newAchievement in achievements.Where(a => a.DateUnlocked.HasValue))
                    {
                        var existingAchievement = gameAchievements.Items.FirstOrDefault(x => x.Name == newAchievement.Name);
                        if (existingAchievement != null)
                        {
                            existingAchievement.DateUnlocked = newAchievement.DateUnlocked.Value;
                        }
                    }
                }
                else
                {
                    gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
                    gameAchievements.Items = achievements;
                }

                gameAchievements.DateLastRefresh = DateTime.UtcNow;
                gameAchievements.Name = game.Name;

                string json = Serialization.ToJson(gameAchievements);
                File.WriteAllText(tempPath, json);

                string verificationJson = File.ReadAllText(tempPath);
                var verificationAchievements = Serialization.FromJson<GameAchievements>(verificationJson);

                if (verificationAchievements != null)
                {
                    if (File.Exists(finalPath))
                    {
                        File.Delete(finalPath);
                    }
                    File.Copy(tempPath, finalPath);
                    gameAchievements.IsManual = true;
                    gameAchievements.SetRaretyIndicator();
                    SuccessStory.PluginDatabase.AddOrUpdate(gameAchievements);
                    SuccessStory.PluginDatabase.SetThemesResources(game);
                }
                else
                {
                    throw new Exception("Achievement data verification failed");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Xbox360: Failed to save achievements: {ex.Message}");
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
                throw;
            }
        }

        private void UpdateXeniaConfig(string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    File.WriteAllText(configPath, "show_achievement_notification = true\n");
                    return;
                }

                var configLines = File.ReadAllLines(configPath).ToList();
                bool hasAchievementSetting = configLines.Any(l => l.TrimStart().StartsWith("show_achievement_notification"));

                if (!hasAchievementSetting)
                {
                    configLines.Add("show_achievement_notification = true");
                    File.WriteAllLines(configPath, configLines);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Xbox360: Failed to update Xenia config: {ex.Message}");
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                throw;
            }
        }

        public override bool ValidateConfiguration()
        {
            if (CachedConfigurationValidationResult == null)
            {
                CachedConfigurationValidationResult = IsConfigured();

                if (!(bool)CachedConfigurationValidationResult)
                {
                    ShowNotificationPluginNoConfiguration();
                }
            }
            else if (!(bool)CachedConfigurationValidationResult)
            {
                ShowNotificationPluginErrorMessage(PlayniteTools.ExternalPlugin.SuccessStory);
            }

            return (bool)CachedConfigurationValidationResult;
        }

        public override bool IsConfigured()
        {
            EnsureInitialized();
            // Note: _xeniaPath is normalized to a directory by InitializeXeniaEnvironment if called,
            // but may still be a file path if only set via constructor. Check both file and directory existence.
            return !string.IsNullOrEmpty(_xeniaPath) && (System.IO.Directory.Exists(_xeniaPath) || System.IO.File.Exists(_xeniaPath));
        }

        public override bool EnabledInSettings()
        {
            try
            {
                return SuccessStory.PluginDatabase?.PluginSettings?.Settings?.EnableXbox360Achievements ?? false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error checking EnabledInSettings for Xbox 360 achievements");
                return false;
            }
        }

        public void InitializeXeniaEnvironment(string xeniaPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(xeniaPath))
                {
                    // Normalize path first
                    string baseDir = NormalizeToDirectory(xeniaPath);
                    
                    // Use lock to prevent other threads from observing partially-reset state
                    lock (_initLock)
                    {
                        _xeniaPath = baseDir;
                        _isInitialized = false;
                        EnsureInitialized();
                    }

                    // Try to ensure config contains achievement notifications
                    // (done outside lock since it's not critical for initialization state)
                    try
                    {
                        var cfg = Path.Combine(baseDir, "xenia_canary.config");
                        UpdateXeniaConfig(cfg);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Xbox360: Failed to update Xenia config during InitializeXeniaEnvironment");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Xbox360: InitializeXeniaEnvironment failed");
                throw;
            }
        }

        /// <summary>
        /// Normalizes a path to a directory path.
        /// If the path exists as a directory or has a trailing separator, returns it as-is.
        /// If the path exists as a file, returns its parent directory.
        /// If the path doesn't exist, assumes it's intended to be a directory path and returns it as-is.
        /// </summary>
        private string NormalizeToDirectory(string path)
        {
            if (Directory.Exists(path) || 
                path.EndsWith(Path.DirectorySeparatorChar.ToString()) || 
                path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                return path;
            }
            
            // Check if path is actually a file before getting its directory
            // This prevents "C:\Xenia" (non-existent dir) from becoming "C:\" (parent)
            if (File.Exists(path))
            {
                return Path.GetDirectoryName(path) ?? string.Empty;
            }
            
            // Path doesn't exist - assume it's a directory path
            // Note: This assumption may be incorrect if caller intended a non-existent file path
            return path;
        }
    }
}
