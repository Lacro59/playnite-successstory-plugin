using CommonPluginsShared;
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;
using SuccessStory.Models;
using Playnite.SDK.Models;
using Playnite.SDK.Data;
using System.IO;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Threading.Tasks;
using CommonPluginsShared.Models;
using CommonPluginsShared.Extensions;
using SuccessStory.Services;
using System.Threading;
using Playnite.SDK;
using SuccessStory.Models.RetroAchievements;
using CommonPlayniteShared.Common;

namespace SuccessStory.Clients
{
    // https://github.com/KrystianLesniak/retroachievements-api-net
    // https://github.com/RetroAchievements/retroachievements-api-js/issues/46
    // https://docs.retroachievements.org/developer-docs/game-identification.html
    public class RetroAchievements : GenericAchievements
    {
        #region Urls
        /// <summary>
        /// Base URL for the RetroAchievements API.
        /// </summary>
        private static string BaseUrl => @"https://retroachievements.org/API/";

        /// <summary>
        /// URL template for unlocked achievement badges.
        /// </summary>
        private static string BaseUrlUnlocked => @"https://s3-eu-west-1.amazonaws.com/i.retroachievements.org/Badge/{0}.png";

        /// <summary>
        /// URL template for locked achievement badges.
        /// </summary>
        private static string BaseUrlLocked => @"https://s3-eu-west-1.amazonaws.com/i.retroachievements.org/Badge/{0}_lock.png";
        #endregion

        /// <summary>
        /// Gets the configured RetroAchievements username.
        /// </summary>
        private static string User => PluginDatabase.PluginSettings.Settings.RetroAchievementsUser;

        /// <summary>
        /// Gets the configured RetroAchievements API key.
        /// </summary>
        private static string Key => PluginDatabase.PluginSettings.Settings.RetroAchievementsKey;

        /// <summary>
        /// Gets or sets the RetroAchievements game ID.
        /// </summary>
        public int GameId { get; set; } = 0;

        /// <summary>
        /// List of console IDs to exclude from hash-based matching.
        /// </summary>
        private static List<int> ConsoleExcludeHash => new List<int> { 2, 8, 12, 16, 21, 40, 41, 47, 49, 76 };

        /// <summary>
        /// Stores the name of the game as recognized by RetroAchievements.
        /// </summary>
        private string GameNameAchievements { get; set; } = string.Empty;


        public RetroAchievements() : base("RetroAchievements")
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> AllAchievements = new List<Achievement>();

            if (IsConfigured())
            {
                if (GameId == 0)
                {
                    int consoleID = GetConsoleId(game);
                    if (ConsoleExcludeHash.FindAll(x => x == consoleID)?.Count == 0)
                    {
                        GameId = GetGameIdByHash(game, consoleID);
                    }
                    if (GameId == 0)
                    {
                        GameId = GetGameIdByName(game, consoleID);
                    }
                }

                if (GameId != 0)
                {
                    AllAchievements = GetGameInfoAndUserProgress(GameId);
                    gameAchievements.RAgameID = GameId;
                }
                else
                {
                    return gameAchievements;
                }
            }
            else
            {
                ShowNotificationPluginNoConfiguration();
            }

            gameAchievements.Items = AllAchievements;


            // Set source link
            if (gameAchievements.HasAchievements)
            {
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = GameNameAchievements,
                    Name = "RetroAchievements",
                    Url = $"https://retroachievements.org/game/{gameAchievements.RAgameID}"
                };
            }

            gameAchievements.SetRaretyIndicator();
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
                    ShowNotificationPluginNoAuthenticate(PlayniteTools.ExternalPlugin.None);
                }
            }
            else if (!(bool)CachedConfigurationValidationResult)
            {
                ShowNotificationPluginErrorMessage(PlayniteTools.ExternalPlugin.None);
            }

            return (bool)CachedConfigurationValidationResult;
        }

        public override bool IsConfigured()
        {
            return User != string.Empty && Key != string.Empty;
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableRetroAchievements;
        }
        #endregion


        #region RetroAchievements
        /// <summary>
        /// Retrieves the list of supported consoles from the RetroAchievements API.
        /// </summary>
        /// <returns>List of <see cref="RaConsole"/> objects.</returns>
        public static List<RaConsole> GetConsoleIDs()
        {
            List<RaConsole> resultObj = new List<RaConsole>();
            string target = "API_GetConsoleIDs.php";
            string url = string.Format(BaseUrl + target + @"?z={0}&y={1}", User, Key);

            string fileConsoles = PluginDatabase.Paths.PluginUserDataPath + "\\RA_Consoles.json";
            if (File.Exists(fileConsoles) && File.GetLastWriteTime(fileConsoles).AddDays(5) > DateTime.Now && Serialization.TryFromJsonFile(fileConsoles, out resultObj))
            {
                return resultObj;
            }

            string response = string.Empty;

            try
            {
                response = Web.DownloadStringData(url).GetAwaiter().GetResult();
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false, $"Failed to load from {url}", true, PluginDatabase.PluginName);
            }


            if (!response.IsNullOrEmpty())
            {
                _ = Serialization.TryFromJson(response, out List<RaConsole> data);
                resultObj = data ?? new List<RaConsole>();

                if (resultObj?.Count == 0)
                {
                    Exception ex = new Exception($"Failed to parse {response}");
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
                else
                {
                    File.WriteAllText(fileConsoles, Serialization.ToJson(resultObj), Encoding.UTF8);
                }
            }

            return resultObj;
        }

        /// <summary>
        /// Finds the RetroAchievements console ID for a given platform name.
        /// </summary>
        /// <param name="platformName">The platform name to match.</param>
        /// <returns>The console ID if found; otherwise, 0.</returns>
        public static int FindConsole(string platformName)
        {
            List<RaConsole> raConsoles = GetConsoleIDs();
            int consoleID = 0;

            #region Normalize
            if (platformName.IsEqual("Sega Genesis"))
            {
                platformName = "Mega Drive";
            }
            if (platformName.IsEqual("Nintendo SNES"))
            {
                platformName = "SNES";
            }
            if (platformName.IsEqual("Super Nintendo Entertainment System"))
            {
                platformName = "SNES";
            }
            if (platformName.IsEqual("Nintendo Game Boy"))
            {
                platformName = "Game Boy";
            }
            if (platformName.IsEqual("Nintendo Game Boy Advance"))
            {
                platformName = "Game Boy Advance";
            }
            if (platformName.IsEqual("Nintendo Game Boy Color"))
            {
                platformName = "Game Boy Color";
            }
            if (platformName.IsEqual("Nintendo Entertainment System"))
            {
                platformName = "NES";
            }
            if (platformName.IsEqual("PC Engine SuperGrafx"))
            {
                platformName = "PC Engine";
            }
            if (platformName.IsEqual("Sega 32X"))
            {
                platformName = "32X";
            }
            if (platformName.IsEqual("Sega Master System"))
            {
                platformName = "Master System";
            }
            if (platformName.IsEqual("Sony PlayStation"))
            {
                platformName = "PlayStation";
            }
            if (platformName.IsEqual("SNK Neo Geo Pocket"))
            {
                platformName = "Neo Geo Pocket";
            }
            if (platformName.IsEqual("Sega Game Gear"))
            {
                platformName = "Game Gear";
            }
            if (platformName.IsEqual("Nintendo GameCube"))
            {
                platformName = "GameCube";
            }
            if (platformName.IsEqual("Nintendo Wii"))
            {
                platformName = "Wii";
            }
            if (platformName.IsEqual("Nintendo Wii U"))
            {
                platformName = "Wii U";
            }
            if (platformName.IsEqual("Sony PlayStation 2"))
            {
                platformName = "PlayStation 2";
            }
            if (platformName.IsEqual("Microsoft Xbox"))
            {
                platformName = "Xbox";
            }
            if (platformName.IsEqual("Magnavox Odyssey2"))
            {
                platformName = "Magnavox Odyssey 2";
            }
            if (platformName.IsEqual("PC (DOS)"))
            {
                platformName = "DOS";
            }
            if (platformName.IsEqual("Various"))
            {
                platformName = "Arcade";
            }
            if (platformName.IsEqual("MAME 2003 Plus"))
            {
                platformName = "Arcade";
            }
            if (platformName.IsEqual("Nintendo Virtual Boy"))
            {
                platformName = "Virtual Boy";
            }
            if (platformName.IsEqual("Sega SG 1000"))
            {
                platformName = "SG-1000";
            }
            if (platformName.IsEqual("Atari ST/STE/TT/Falcon"))
            {
                platformName = "Atari ST";
            }
            if (platformName.IsEqual("Sega Saturn"))
            {
                platformName = "Saturn";
            }
            if (platformName.IsEqual("Sega Dreamcast"))
            {
                platformName = "Dreamcast";
            }
            if (platformName.IsEqual("Sony PlayStation Portable"))
            {
                platformName = "PlayStation Portable";
            }
            if (platformName.IsEqual("Sony PSP"))
            {
                platformName = "PlayStation Portable";
            }
            if (platformName.IsEqual("Coleco ColecoVision"))
            {
                platformName = "ColecoVision";
            }
            if (platformName.IsEqual("SNK Neo Geo CD"))
            {
                platformName = "Neo Geo CD";
            }
            if (platformName.Contains("Grafx"))
            {
                platformName = "PC Engine";
            }
            #endregion

            RaConsole raConsole = raConsoles.Find(x => platformName.IsEqual(x.Name));
            if (raConsole != null)
            {
                consoleID = raConsole.ID;
            }

            return consoleID;
        }

        /// <summary>
        /// Attempts to find the RetroAchievements game ID by matching the game name.
        /// </summary>
        /// <param name="game">The game to search for.</param>
        /// <param name="consoleID">The console ID to search within.</param>
        /// <returns>The game ID if found; otherwise, 0.</returns>
        private int GetGameIdByName(Game game, int consoleID)
        {
            Logger.Info($"GetGameIdByName({game.Name}, {consoleID})");

            string gameName = game.Name;
            string platformName = game.Platforms.FirstOrDefault().Name;

            // Search game id
            int gameID = 0;
            if (consoleID != 0)
            {
                List<RaGame> ra_Games = GetGameList(consoleID);
                ra_Games.Sort((x, y) => y.Title.CompareTo(x.Title));

                foreach (RaGame ra_Game in ra_Games)
                {
                    string retroArchTitle = ra_Game.Title;
                    //TODO: Decide if editions should be removed here
                    string normalizedRetroArchTitle = PlayniteTools.NormalizeGameName(retroArchTitle, true);
                    string normalizedPlayniteTitle = PlayniteTools.NormalizeGameName(gameName, true);
                    if (normalizedPlayniteTitle.IsEqual(normalizedRetroArchTitle))
                    {
                        Logger.Info($"Find for {gameName} [{ra_Game.ID}] / {retroArchTitle} with {platformName} in {consoleID}");
                        gameID = ra_Game.ID;
                        break;
                    }

                    string[] TitleSplits = retroArchTitle.Split('|');
                    if (TitleSplits.Length > 1)
                    {
                        foreach (string TitleSplit in TitleSplits)
                        {
                            if (gameName.IsEqual(TitleSplit) && gameID == 0)
                            {
                                Logger.Info($"Find for {gameName} [{ra_Game.ID}] / {TitleSplit} with {platformName} in {consoleID}");
                                gameID = ra_Game.ID;
                                break;
                            }
                        }
                    }

                    TitleSplits = retroArchTitle.Split('-');
                    if (TitleSplits.Length > 1)
                    {
                        foreach (string TitleSplit in TitleSplits)
                        {
                            if (gameName.IsEqual(TitleSplit) && gameID == 0)
                            {
                                Logger.Info($"Find for {gameName} [{ra_Game.ID}] / {TitleSplit} with {platformName} in {consoleID}");
                                gameID = ra_Game.ID;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                Logger.Warn($"No console found for {gameName} with {platformName}");
            }

            if (gameID == 0)
            {
                Logger.Warn($"No game found for {gameName} with {platformName} in {consoleID}");
            }

            return gameID;
        }

        /// <summary>
        /// Gets the RetroAchievements console ID associated with the specified game.
        /// </summary>
        /// <param name="game">The game for which to find the console ID.</param>
        /// <returns>The console ID if found; otherwise, 0.</returns>
        private int GetConsoleId(Game game)
        {
            Platform platform = game.Platforms.FirstOrDefault();
            int consoleId = 0;
            IEnumerable<RaConsoleAssociated> consolesAssociated = PluginDatabase.PluginSettings.Settings.RaConsoleAssociateds.Where(x => x.Platforms.Find(y => y.Id == platform.Id) != null);
            if (consolesAssociated.Count() == 0)
            {
                string message = string.Format(ResourceProvider.GetString("LOCSuccessStoryNotificationsRetroAchievementsNoConsoleId"), game.Name, platform.Name);
                Logger.Warn($"No ConsoleId find for {game.Name} with platforms {platform.Name}");
                API.Instance.Notifications.Add(new NotificationMessage(
                    $"{PluginDatabase.PluginName}-{ClientName}-NoConsoleId",
                    $"{PluginDatabase.PluginName}\r\n{message}",
                    NotificationType.Error,
                    () => PluginDatabase.Plugin.OpenSettingsView()
                ));
                return consoleId;
            }
            else if (consolesAssociated.Count() > 1)
            {
                string message = string.Format(ResourceProvider.GetString("LOCCommonNotificationTooMuchData"), $"{ClientName} - {game.Name}");
                ShowNotificationPluginTooMuchData(message, PlayniteTools.ExternalPlugin.SuccessStory);
            }

            RaConsoleAssociated consoleAssociated = consolesAssociated.First();
            consoleId = consoleAssociated?.RaConsoleId ?? 0;
            string consoleName = consoleAssociated?.RaConsoleName ?? string.Empty;
            Logger.Info($"Find ConsoleId {consoleId}{(consoleName.IsNullOrEmpty() ? string.Empty : $"/{consoleName}")} for {game.Name} with Platforms {platform.Name}");
            return consoleId;
        }

        /// <summary>
        /// Attempts to find the RetroAchievements game ID by matching the ROM hash.
        /// </summary>
        /// <param name="game">The game to search for.</param>
        /// <param name="consoleID">The console ID to search within.</param>
        /// <returns>The game ID if found; otherwise, 0.</returns>
        private int GetGameIdByHash(Game game, int consoleID)
        {
            Logger.Info($"GetGameIdByHash({game.Name}, {consoleID})");

            // Search game id
            int gameId = 0;
            if (consoleID != 0)
            {
                List<RaGame> ra_Games = GetGameList(consoleID);
                ra_Games.Sort((x, y) => y.Title.CompareTo(x.Title));

                string hashMD5 = string.Empty;

                RaMd5List rA_MD5List = null;
                List<RaMd5List> rA_MD5Lists = new List<RaMd5List>();
                ra_Games.ForEach(x =>
                {
                    int Id = x.ID;
                    x.Hashes.ForEach(y =>
                    {
                        rA_MD5Lists.Add(new RaMd5List { Id = Id, MD5 = y });
                    });
                });

                string FilePath = API.Instance.ExpandGameVariables(game, game.Roms.FirstOrDefault().Path, PlayniteTools.GetGameEmulator(game)?.InstallDir);

                if (!File.Exists(FilePath))
                {
                    return gameId;
                }

                if (FilePath.Contains(".rar", StringComparison.OrdinalIgnoreCase) || FilePath.Contains(".7z", StringComparison.OrdinalIgnoreCase))
                {
                    return gameId;
                }
                if (FilePath.Contains(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    // Exclude for performance
                    FileInfo fi = new FileInfo(FilePath);
                    if (fi.Length > 20000000)
                    {
                        return gameId;
                    }
                    else
                    {
                        FilePath = ZipFileManageExtract(FilePath);
                    }
                }

                if (!File.Exists(FilePath))
                {
                    Logger.Warn($"No file found for RA hash - {FilePath}");
                    ZipFileManageRemove();
                    return gameId;
                }
                else
                {
                    // Exclude for performance
                    FileInfo fi = new FileInfo(FilePath);
                    if (fi.Length > 300000000)
                    {
                        Logger.Warn($"Hash impossible - The file is too long - {FilePath}");
                        return gameId;
                    }
                }


                string ext = Path.GetExtension(FilePath);

                if (ext.IsEqual(".nds"))
                {
                    hashMD5 = GetHash(FilePath, RaPlatformType.NDS);
                    rA_MD5List = rA_MD5Lists.Find(x => x.MD5.IsEqual(hashMD5));
                    if (rA_MD5List != null)
                    {
                        ZipFileManageRemove();
                        Logger.Info($"Find for {game.Name} with {hashMD5} in PlatformType.NDS");
                        return rA_MD5List.Id;
                    }
                    if (gameId == 0)
                    {
                        Logger.Warn($"No game find for {game.Name} with {hashMD5} in PlatformType.NDS");
                    }
                }

                hashMD5 = GetHash(FilePath, RaPlatformType.All);
                rA_MD5List = rA_MD5Lists.Find(x => x.MD5.IsEqual(hashMD5));
                if (rA_MD5List != null)
                {
                    ZipFileManageRemove();
                    Logger.Info($"Find for {game.Name} with {hashMD5} in PlatformType.All");
                    return rA_MD5List.Id;
                }
                if (gameId == 0)
                {
                    Logger.Warn($"No game found for {game.Name} with {hashMD5} in PlatformType.NDS");
                }

                hashMD5 = GetHash(FilePath, RaPlatformType.SNES);
                rA_MD5List = rA_MD5Lists.Find(x => x.MD5.IsEqual(hashMD5));
                if (rA_MD5List != null)
                {
                    ZipFileManageRemove();
                    Logger.Info($"Find for {game.Name} with {hashMD5} in PlatformType.SNES");
                    return rA_MD5List.Id;
                }
                if (gameId == 0)
                {
                    Logger.Warn($"No game find for {game.Name} with {hashMD5} in PlatformType.SNES");
                }

                hashMD5 = GetHash(FilePath, RaPlatformType.NES);
                rA_MD5List = rA_MD5Lists.Find(x => x.MD5.IsEqual(hashMD5));
                if (rA_MD5List != null)
                {
                    ZipFileManageRemove();
                    Logger.Info($"Find for {game.Name} with {hashMD5} in PlatformType.SNES");
                    return rA_MD5List.Id;
                }
                if (gameId == 0)
                {
                    Logger.Warn($"No game find for {game.Name} with {hashMD5} in PlatformType.SNES");
                }

                hashMD5 = GetHash(FilePath, RaPlatformType.Arcade);
                rA_MD5List = rA_MD5Lists.Find(x => x.MD5.IsEqual(hashMD5));
                if (rA_MD5List != null)
                {
                    ZipFileManageRemove();
                    Logger.Info($"Find for {game.Name} with {hashMD5} in PlatformType.Sega_CD_Saturn");
                    return rA_MD5List.Id;
                }
                if (gameId == 0)
                {
                    Logger.Warn($"No game find for {game.Name} with {hashMD5} in PlatformType.Sega_CD_Saturn");
                }

                hashMD5 = GetHash(FilePath, RaPlatformType.Famicom);
                rA_MD5List = rA_MD5Lists.Find(x => x.MD5.IsEqual(hashMD5));
                if (rA_MD5List != null)
                {
                    ZipFileManageRemove();
                    Logger.Info($"Find for {game.Name} with {hashMD5} in PlatformType.SNES");
                    return rA_MD5List.Id;
                }
                if (gameId == 0)
                {
                    Logger.Warn($"No game find for {game.Name} with {hashMD5} in PlatformType.SNES");
                }

                hashMD5 = GetHash(FilePath, RaPlatformType.Sega_CD_Saturn);
                rA_MD5List = rA_MD5Lists.Find(x => x.MD5.IsEqual(hashMD5));
                if (rA_MD5List != null)
                {
                    ZipFileManageRemove();
                    Logger.Info($"Find for {game.Name} with {hashMD5} in PlatformType.Sega_CD_Saturn");
                    return rA_MD5List.Id;
                }
                if (gameId == 0)
                {
                    Logger.Warn($"No game find for {game.Name} with {hashMD5} in PlatformType.Sega_CD_Saturn");
                }

                ZipFileManageRemove();
            }

            return gameId;
        }

        /// <summary>
        /// Computes the MD5 hash of a game file, applying platform-specific rules.
        /// </summary>
        /// <param name="FilePath">The path to the game file.</param>
        /// <param name="platformType">The platform type for hash calculation.</param>
        /// <returns>The MD5 hash as a string.</returns>
        private string GetHash(string FilePath, RaPlatformType platformType)
        {
            try
            {
                byte[] byteSequence = File.ReadAllBytes(FilePath);
                long length = new FileInfo(FilePath).Length;

                byte[] byteSequenceFinal = byteSequence;

                if (platformType == RaPlatformType.NDS)
                {
                    try
                    {
                        NDS nds = new NDS(FilePath);
                        byteSequenceFinal = nds.getByteToHash();
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, true);
                    }
                }

                if (platformType == RaPlatformType.SNES)
                {
                    try
                    {
                        if (length > 512)
                        {
                            byteSequenceFinal = new byte[byteSequence.Length - 512];
                            Buffer.BlockCopy(byteSequence, 512, byteSequenceFinal, 0, byteSequenceFinal.Length);
                        }
                        else
                        {
                            byteSequenceFinal = byteSequence;
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, true);
                    }
                }

                if (platformType == RaPlatformType.NES)
                {
                    try
                    {
                        string hexData = Tools.ToHex(byteSequence);

                        //$4E $45 $53 $1A
                        if (hexData.ToLower().IndexOf("4e45531a") == 0)
                        {
                            byteSequenceFinal = new byte[byteSequence.Length - 16];
                            Buffer.BlockCopy(byteSequence, 16, byteSequenceFinal, 0, byteSequenceFinal.Length);
                        }
                        else
                        {
                            byteSequenceFinal = byteSequence;
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, true);
                    }
                }

                if (platformType == RaPlatformType.Famicom)
                {
                    try
                    {
                        string hexData = Tools.ToHex(byteSequence);

                        //$46 $44 $53 $1A
                        if (hexData.ToLower().IndexOf("4644531a") == 0)
                        {
                            byteSequenceFinal = new byte[byteSequence.Length - 16];
                            Buffer.BlockCopy(byteSequence, 16, byteSequenceFinal, 0, byteSequenceFinal.Length);
                        }
                        else
                        {
                            byteSequenceFinal = byteSequence;
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, true);
                    }
                }

                if (platformType == RaPlatformType.Sega_CD_Saturn)
                {
                    try
                    {
                        byteSequenceFinal = new byte[512];
                        Buffer.BlockCopy(byteSequence, 0, byteSequenceFinal, 0, 512);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, true);
                    }
                }

                if (platformType == RaPlatformType.Arcade)
                {
                    try
                    {
                        string FileName = Path.GetFileNameWithoutExtension(FilePath);
                        byteSequenceFinal = Encoding.ASCII.GetBytes(FileName);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, true);
                    }
                }

                return GetMd5(byteSequenceFinal);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return string.Empty;
            }
        }

        /// <summary>
        /// Computes the MD5 hash for a byte array.
        /// </summary>
        /// <param name="byteSequence">The byte array to hash.</param>
        /// <returns>The MD5 hash as a string.</returns>
        private static string GetMd5(byte[] byteSequence)
        {
            try
            {
                using (MD5 md5 = MD5.Create())
                {
                    byte[] md5Byte = md5.ComputeHash(byteSequence);
                    return BitConverter.ToString(md5Byte).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, true);
                return string.Empty;
            }
        }

        /// <summary>
        /// Extracts the first file from a ZIP archive for hash calculation.
        /// </summary>
        /// <param name="filePath">The path to the ZIP file.</param>
        /// <returns>The path to the extracted file.</returns>
        private string ZipFileManageExtract(string filePath)
        {
            string extractPath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "tempZip");
            try
            {
                ZipFileManageRemove();
                ZipFile.ExtractToDirectory(filePath, extractPath);
            }
            catch { }

            string FilePathReturn = string.Empty;
            _ = Parallel.ForEach(Directory.EnumerateFiles(extractPath, "*.*", SearchOption.AllDirectories), (objectFile) =>
            {
                FilePathReturn = objectFile;
            });

            return FilePathReturn;
        }

        /// <summary>
        /// Removes the temporary directory used for ZIP extraction.
        /// </summary>
        private void ZipFileManageRemove()
        {
            string extractPath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "tempZip");
            FileSystem.DeleteDirectory(extractPath);
        }

        /// <summary>
        /// Retrieves the list of games for a specific console from the RetroAchievements API.
        /// </summary>
        /// <param name="consoleID">The console ID.</param>
        /// <returns>List of <see cref="RaGame"/> objects.</returns>
        private List<RaGame> GetGameList(int consoleID)
        {
            string target = "API_GetGameList.php";
            string url = string.Format(BaseUrl + target + @"?z={0}&y={1}&i={2}&h=1", User, Key, consoleID);

            List<RaGame> resultObj = new List<RaGame>();

            string fileConsoles = PluginDatabase.Paths.PluginUserDataPath + "\\RA_Games_" + consoleID + ".json";
            if (File.Exists(fileConsoles) && File.GetLastWriteTime(fileConsoles).AddDays(5) > DateTime.Now && Serialization.TryFromJsonFile(fileConsoles, out resultObj))
            {
                return resultObj;
            }

            string response = string.Empty;
            try
            {
                response = Web.DownloadStringData(url).GetAwaiter().GetResult();
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false, $"Failed to load from {url}", true, PluginDatabase.PluginName);
            }

            try
            {
                resultObj = Serialization.FromJson<List<RaGame>>(response);
                File.WriteAllText(fileConsoles, Serialization.ToJson(resultObj), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to parse {response}", true, PluginDatabase.PluginName);
            }

            return resultObj;
        }

        /// <summary>
        /// Retrieves achievement information and user progress for a specific game.
        /// </summary>
        /// <param name="gameID">The RetroAchievements game ID.</param>
        /// <returns>List of <see cref="Achievement"/> objects.</returns>
        private List<Achievement> GetGameInfoAndUserProgress(int gameID)
        {
            List<Achievement> achievements = new List<Achievement>();

            string target = "API_GetGameInfoAndUserProgress.php";
            string url = string.Format(BaseUrl + target + @"?z={0}&y={1}&u={0}&g={2}", User, Key, gameID);

            string response;
            try
            {
                response = Web.DownloadStringData(url).GetAwaiter().GetResult();
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false, $"Failed to load from {url}", true, PluginDatabase.PluginName);
                return achievements;
            }

            try
            {
                dynamic resultObj = Serialization.FromJson<dynamic>(response);

                GameNameAchievements = (string)resultObj["Title"];
                int numDistinctPlayersCasual = resultObj["NumDistinctPlayersCasual"] == null ? 0 : (int)resultObj["NumDistinctPlayersCasual"];

                if (resultObj["Achievements"] != null && !response.Contains("\"Achievements\":{}"))
                {
                    foreach (dynamic item in resultObj["Achievements"])
                    {
                        foreach (dynamic it in item)
                        {
                            achievements.Add(new Achievement
                            {
                                Name = (string)it["Title"],
                                Description = (string)it["Description"],
                                UrlLocked = string.Format(BaseUrlLocked, (string)it["BadgeName"]),
                                UrlUnlocked = string.Format(BaseUrlUnlocked, (string)it["BadgeName"]),
                                DateUnlocked = (it["DateEarned"] == null) ? (DateTime?) null : Convert.ToDateTime((string)it["DateEarned"]),
                                Percent = it["NumAwarded"] == null || (int)it["NumAwarded"] == 0 || numDistinctPlayersCasual == 0 ? 100 : (int)it["NumAwarded"] * 100 / numDistinctPlayersCasual,
                                GamerScore = it["Points"] == null ? 0 : (int)it["Points"]
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"[{gameID}] Failed to parse {response}", true, PluginDatabase.PluginName);
                return achievements;
            }

            return achievements;
        }
        #endregion
    }
}
