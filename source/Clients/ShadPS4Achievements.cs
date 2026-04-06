using Playnite.SDK.Models;
using CommonPluginsShared;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using CommonPlayniteShared.Common;
using System.Globalization;
using static CommonPluginsShared.PlayniteTools;
using System.Text.RegularExpressions;
using Paths = CommonPlayniteShared.Common.Paths;
using Playnite.SDK.Data;

namespace SuccessStory.Clients
{
    
    public class ShadPS4Achievements : GenericAchievements
    {
        // PS4's RTC epoch is January 1, 2008 00:00:00 UTC
        private static readonly DateTime PS4Epoch = new DateTime(2008, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const int YearOffset = 2007; // The consistent difference we need to subtract

        public ShadPS4Achievements() : base("ShadPS4")
        {

        }

        private DateTime? ConvertPS4Timestamp(string timestamp)
        {
            try
            {
                if (string.IsNullOrEmpty(timestamp))
                    return null;

                // Handle newer 10-digit Unix timestamps (seconds since 1970)
                if (timestamp.Length <= 12)
                {
                    if (long.TryParse(timestamp, out long seconds))
                    {
                        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds).ToLocalTime();
                    }
                }

                // Parse the PS4 timestamp (Legacy 17-digit format)
                ulong tickValue = ulong.Parse(timestamp);

                // Divide by 1000 to get milliseconds instead of microseconds
                long milliseconds = (long)(tickValue / 1000);

                // Add milliseconds to PS4 epoch
                DateTime utcTime = PS4Epoch.AddMilliseconds(milliseconds);

                // Convert to local time
                DateTime localTime = utcTime.ToLocalTime();

                // Adjust the year by subtracting the offset
                return new DateTime(
                    localTime.Year - YearOffset,
                    localTime.Month,
                    localTime.Day,
                    localTime.Hour,
                    localTime.Minute,
                    localTime.Second,
                    localTime.Millisecond,
                    localTime.Kind
                );
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, false, PluginDatabase.PluginName);
                return null;
            }
        }

        private string GetRarityCachePath(string trophyPath)
        {
            return Path.Combine(trophyPath, "trophy00", "rarity_cache.json");
        }

        private RarityCache LoadRarityCache(string trophyPath)
        {
            string cachePath = GetRarityCachePath(trophyPath);
            if (File.Exists(cachePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(cachePath);
                    return Serialization.FromJson<RarityCache>(jsonContent);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    return null;
                }
            }
            return null;
        }

        private void SaveRarityCache(string trophyPath, RarityCache cache)
        {
            try
            {
                string cachePath = GetRarityCachePath(trophyPath);
                string jsonContent = Serialization.ToJson(cache);
                File.WriteAllText(cachePath, jsonContent);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        public static string GetGameDataPath(string installationFolder)
        {
            if (string.IsNullOrEmpty(installationFolder))
            {
                return null;
            }

            // Check for user/game_data (Legacy)
            string userGameData = Path.Combine(installationFolder, "user", "game_data");
            if (Directory.Exists(userGameData))
            {
                return userGameData;
            }

            // Check for launcher/game_data (Portable New)
            string launcherGameData = Path.Combine(installationFolder, "launcher", "game_data");
            if (Directory.Exists(launcherGameData))
            {
                return launcherGameData;
            }

            // Check for shadPS4/game_data (Default/AppData structure)
            string shadPS4GameData = Path.Combine(installationFolder, "shadPS4", "game_data");
            if (Directory.Exists(shadPS4GameData))
            {
                return shadPS4GameData;
            }

            return null;
        }

        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> AllAchievements = new List<Achievement>();

            if (IsConfigured())
            {
                string userGameDataPath = GetGameDataPath(PluginDatabase.PluginSettings.Settings.ShadPS4InstallationFolder);
                string titleId = FindGameTitleId(game);

                if (!string.IsNullOrEmpty(titleId) && !string.IsNullOrEmpty(userGameDataPath))
                {
                    string trophyPath = Path.Combine(userGameDataPath, titleId, "trophyfiles");
                    string xmlPath = Path.Combine(trophyPath, "trophy00", "Xml", "TROP.XML");

                    if (File.Exists(xmlPath))
                    {
                        XDocument trophyXml = XDocument.Load(xmlPath);
                        string gameName = trophyXml.Descendants("title-name").FirstOrDefault()?.Value.Trim();

                        // Load cached rarity data
                        RarityCache rarityCache = LoadRarityCache(trophyPath);
                        Dictionary<string, float> trophyRarities = new Dictionary<string, float>();
                        GameAchievements exophaseAchievements = null;

                        bool shouldFetchFromExophase = false;

                        // Check if we need to fetch from Exophase
                        if (rarityCache == null)
                        {
                            shouldFetchFromExophase = true;
                        }
                        else if ((DateTime.Now - rarityCache.LastUpdated).TotalDays > 30)
                        {
                            shouldFetchFromExophase = true;
                        }
                        else if (rarityCache.TrophyRarities == null || rarityCache.TrophyRarities.Count == 0)
                        {
                            shouldFetchFromExophase = true;
                        }

                        // Only fetch from Exophase if necessary
                        if (shouldFetchFromExophase)
                        {
                            Logger.Info($"Fetching trophy rarities from Exophase for {game.Name}");
                            List<SearchResult> searchResults = SuccessStory.ExophaseAchievements.SearchGame(game.Name);
                            SearchResult matchingGame = searchResults?.FirstOrDefault(x =>
                                x.Platforms.Any(p => p.Contains("PS4", StringComparison.OrdinalIgnoreCase)));

                            if (matchingGame != null)
                            {
                                exophaseAchievements = SuccessStory.ExophaseAchievements.GetAchievements(game, matchingGame);

                                // Create new cache
                                rarityCache = new RarityCache
                                {
                                    GameName = game.Name,
                                    LastUpdated = DateTime.Now,
                                    TrophyRarities = new Dictionary<string, float>()
                                };

                                // Store rarity data
                                if (exophaseAchievements?.Items != null)
                                {
                                    foreach (var achievement in exophaseAchievements.Items)
                                    {
                                        rarityCache.TrophyRarities[achievement.Name] = achievement.Percent;
                                    }

                                    // Only save cache if we actually got data
                                    if (rarityCache.TrophyRarities.Count > 0)
                                    {
                                        SaveRarityCache(trophyPath, rarityCache);
                                        Logger.Info($"Saved {rarityCache.TrophyRarities.Count} trophy rarities to cache for {game.Name}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Logger.Info($"Using cached trophy rarities for {game.Name}");
                        }

                        foreach (XElement trophyElement in trophyXml.Descendants("trophy"))
                        {   
                            string trophyId = trophyElement.Attribute("id")?.Value;
                            string trophyType = trophyElement.Attribute("ttype")?.Value;
                            bool isHidden = (trophyElement.Attribute("hidden")?.Value == "yes");
                            string name = trophyElement.Element("name")?.Value;
                            string description = trophyElement.Element("detail")?.Value;

                            // Check if trophy is unlocked
                            bool isUnlocked = trophyElement.Attribute("unlockstate") != null;
                            DateTime? unlockTime = null;

                            if (isUnlocked)
                            {
                                string timestamp = trophyElement.Attribute("timestamp")?.Value;
                                unlockTime = ConvertPS4Timestamp(timestamp);
                            }

                            // Calculate rarity/gamer score based on trophy type
                            float gamerScore = 15;
                            float rarity = 100;
                            switch (trophyType?.ToUpper())
                            {
                                case "B": // Bronze
                                    gamerScore = 15;
                                    // Try to get rarity from cache first
                                    if (rarityCache?.TrophyRarities != null &&
                                        rarityCache.TrophyRarities.TryGetValue(name, out float cachedRarity))
                                    {
                                        rarity = cachedRarity;
                                    }
                                    break;
                                case "S":
                                    gamerScore = 30;
                                    rarity = (float)PluginDatabase.PluginSettings.Settings.RarityUncommon;
                                    break;
                                case "G":
                                    gamerScore = 90;
                                    rarity = (float)PluginDatabase.PluginSettings.Settings.RarityRare;
                                    break;
                                case "P":
                                    gamerScore = 180;
                                    rarity = (float)PluginDatabase.PluginSettings.Settings.RarityUltraRare;
                                    break;
                            }

                            AllAchievements.Add(new Achievement
                            {
                                ApiName = trophyId,
                                Name = name,
                                Description = description,
                                IsHidden = isHidden,
                                DateUnlocked = unlockTime,
                                Percent = rarity,
                                GamerScore = gamerScore,
                                UrlUnlocked = GetTrophyIconPath(trophyPath, trophyId, true),
                                UrlLocked = GetTrophyIconPath(trophyPath, trophyId, false)
                            });
                        }

                        gameAchievements.Items.AddRange(AllAchievements);
                    }
                }
            }
            else
            {
                ShowNotificationPluginNoConfiguration();
            }

            gameAchievements.SetRaretyIndicator();
            PluginDatabase.AddOrUpdate(gameAchievements);
            return gameAchievements;
        }

        #region Configuration
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
                ShowNotificationPluginErrorMessage(ExternalPlugin.SuccessStory);
            }

            return (bool)CachedConfigurationValidationResult;
        }

        public override bool IsConfigured()
        {
            if (PluginDatabase.PluginSettings.Settings.ShadPS4InstallationFolder.IsNullOrEmpty())
            {
                Logger.Warn("No ShadPS4 configured folder");
                return false;
            }

            string userGameDataPath = GetGameDataPath(PluginDatabase.PluginSettings.Settings.ShadPS4InstallationFolder);
            if (string.IsNullOrEmpty(userGameDataPath))
            {
                Logger.Warn($"No ShadPS4 game_data folder found in {PluginDatabase.PluginSettings.Settings.ShadPS4InstallationFolder}");
                return false;
            }

            return true;
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableShadPS4Achievements;
        }
        #endregion

        #region ShadPS4        
        private string FindGameTitleId(Game game)
        {
            string userGameDataPath = GetGameDataPath(PluginDatabase.PluginSettings.Settings.ShadPS4InstallationFolder);
            if (string.IsNullOrEmpty(userGameDataPath))
            {
                return null;
            }

            // Get all title ID folders
            DirectoryInfo[] titleDirectories = new DirectoryInfo(userGameDataPath).GetDirectories();

            foreach (DirectoryInfo dir in titleDirectories)
            {
                // Check if this title ID folder contains trophy data
                string trophyPath = Path.Combine(dir.FullName, "trophyfiles", "trophy00", "Xml", "TROP.XML");
                if (File.Exists(trophyPath))
                {
                    try
                    {
                        // Load the TROP.XML to verify if it's the correct game
                        XDocument trophyXml = XDocument.Load(trophyPath);
                        string gameName = trophyXml.Descendants("title-name").FirstOrDefault()?.Value.Trim();

                        // Compare game names
                        if (gameName?.Equals(game.Name, StringComparison.OrdinalIgnoreCase) ?? false)
                        {
                            return dir.Name; // Return the title ID if we found a match
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }
            }

            Logger.Warn($"No trophy data found for {game.Name}");
            return null;
        }

        private string GetTrophyIconPath(string trophyPath, string trophyId, bool unlocked)
        {
            try
            {
                // Trophy icons are in the Icons folder and follow TROP###.PNG format
                string iconFileName = $"TROP{trophyId.PadLeft(3, '0')}.PNG";
                string iconPath = Path.Combine(trophyPath, "trophy00", "Icons", iconFileName);

                if (File.Exists(iconPath))
                {
                    // The cache path will be like shadps4/CUSA#####/TROP000.PNG
                    string titleId = new DirectoryInfo(trophyPath).Parent.Name;
                    string cacheDir = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "shadps4", titleId);

                    // Create cache directory if it doesn't exist
                    FileSystem.CreateDirectory(cacheDir);

                    string targetPath = Path.Combine(cacheDir, iconFileName);
                    if (!File.Exists(targetPath))
                    {
                        FileSystem.CopyFile(iconPath, targetPath, false);
                    }

                    // Return relative path for the plugin's use
                    return Path.Combine("shadps4", titleId, iconFileName);
                }

                Logger.Warn($"Trophy icon not found: {iconPath}");
                return null;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return null;
            }
        }
        #endregion
    }
}
