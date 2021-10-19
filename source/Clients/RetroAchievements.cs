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

namespace SuccessStory.Clients
{
    public enum PlatformType
    {
        All,
        SNES,
        Sega_CD_Saturn,
        NES,
        Famicom,
        Arcade
    }


    class RetroAchievements : GenericAchievements
    {
        private const string BaseUrl = @"https://retroachievements.org/API/";
        private const string BaseUrlUnlocked = @"https://s3-eu-west-1.amazonaws.com/i.retroachievements.org/Badge/{0}.png";
        private const string BaseUrlLocked = @"https://s3-eu-west-1.amazonaws.com/i.retroachievements.org/Badge/{0}_lock.png";

        private const string BaseMD5List = @"http://retroachievements.org/dorequest.php?r=hashlibrary&c={0}";

        private string User { get; set; }
        private string Key { get; set; }

        public int GameId { get; set; } = 0;


        private string GameNameAchievements = string.Empty;
        private string UrlAchievements = string.Empty;


        public RetroAchievements() : base("RetroAchievements")
        {
            User = PluginDatabase.PluginSettings.Settings.RetroAchievementsUser;
            Key = PluginDatabase.PluginSettings.Settings.RetroAchievementsKey;
        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();


            if (IsConfigured())
            {
                // Load list console
                RA_Consoles ra_Consoles = GetConsoleIDs();
                if (ra_Consoles != null && ra_Consoles != new RA_Consoles())
                {
                    ra_Consoles.ListConsoles.Sort((x, y) => (y.Name).CompareTo(x.Name));
                }
                else
                {
                    logger.Warn($"No ra_Consoles find");
                }

                // List MD5
                List<RA_MD5List> ListMD5 = new List<RA_MD5List>();
                try
                {
                    ListMD5 = GetMD5List(ra_Consoles);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }

                // Game Id
                if (GameId == 0)
                {
                    if (ListMD5.Count > 0)
                    {
                        GameId = GetGameIdByHash(game, ListMD5);
                    }

                    if (GameId == 0)
                    {
                        GameId = GetGameIdByName(game, ra_Consoles);
                    }
                }

                // Get achievements
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
                ShowNotificationPluginNoConfiguration(resources.GetString("LOCSuccessStoryNotificationsRetroAchievementsBadConfig"));
            }


            gameAchievements.Items = AllAchievements;


            // Set source link
            if (gameAchievements.HasAchivements)
            {
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = GameNameAchievements,
                    Name = "RetroAchievements",
                    Url = $"https://retroachievements.org/game/{gameAchievements.RAgameID}"
                };
            }

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
                    ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsRetroAchievementsBadConfig"));
                }
            }
            else if (!(bool)CachedConfigurationValidationResult)
            {
                ShowNotificationPluginErrorMessage();
            }

            return (bool)CachedConfigurationValidationResult;
        }

        public override bool IsConfigured()
        {
            User = PluginDatabase.PluginSettings.Settings.RetroAchievementsUser;
            Key = PluginDatabase.PluginSettings.Settings.RetroAchievementsKey;

            return User != string.Empty && Key != string.Empty;
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableRetroAchievements;
        }
        #endregion


        #region RetroAchievements
        private RA_Consoles GetConsoleIDs()
        {
            string Target = "API_GetConsoleIDs.php";
            string Url = string.Format(BaseUrl + Target + @"?z={0}&y={1}", User, Key);

            RA_Consoles resultObj = new RA_Consoles();

            string fileConsoles = PluginDatabase.Paths.PluginUserDataPath + "\\RA_Consoles.json";
            if (File.Exists(fileConsoles))
            {
                resultObj = Serialization.FromJsonFile<RA_Consoles>(fileConsoles);
                return resultObj;
            }

            string ResultWeb = string.Empty;

            try
            {
                ResultWeb = Web.DownloadStringData(Url).GetAwaiter().GetResult();
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false, $"Failed to load from {Url}");
            }


            if (!ResultWeb.IsNullOrEmpty())
            {
                try
                {
                    resultObj.ListConsoles = Serialization.FromJson<List<RA_Console>>(ResultWeb);
                    File.WriteAllText(fileConsoles, Serialization.ToJson(resultObj), Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to parse {ResultWeb}");
                }
            }

            return resultObj;
        }

        private List<RA_MD5List> GetMD5List(RA_Consoles rA_Consoles) 
        {
            List<RA_MD5List> ListMD5 = new List<RA_MD5List>();

            // Cache
            string fileMD5List = PluginDatabase.Paths.PluginUserDataPath + "\\RA_MD5List.json";
            if (File.Exists(fileMD5List) && File.GetLastWriteTime(fileMD5List).AddDays(3) > DateTime.Now )
            {
                ListMD5 = Serialization.FromJsonFile<List<RA_MD5List>>(fileMD5List);
                return ListMD5;
            }

            // Web            
            foreach (RA_Console rA_Console in rA_Consoles.ListConsoles)
            {
                int ConsoleId = rA_Console.ID;

                try
                {
                    string ResultWeb = Web.DownloadStringData(string.Format(BaseMD5List, ConsoleId)).GetAwaiter().GetResult();
                    if (!ResultWeb.Contains("\"MD5List\":[]"))
                    {
                        RA_MD5ListResponse ResultMD5List = Serialization.FromJson<RA_MD5ListResponse>(ResultWeb);

                        foreach (var obj in ResultMD5List.MD5List)
                        {
                            ListMD5.Add(new RA_MD5List { Id = (int)obj.Value, MD5 = obj.Name });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error GetConsoleIDs({rA_Console.ID}, {rA_Console.Name})");
                }
            }

            // Save
            if (ListMD5.Count > 0)
            {
                try
                {
                    File.WriteAllText(fileMD5List, Serialization.ToJson(ListMD5), Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to save ListMD5");
                }
            }

            return ListMD5;
        }


        private int GetGameIdByName(Game game, RA_Consoles ra_Consoles)
        {
            string GameName = game.Name;

            // Search id console for the game
            string PlatformName = game.Platforms.FirstOrDefault().Name;
            int consoleID = 0;


            if (PlatformName.IsEqual("Sega Genesis"))
            {
                PlatformName = "Mega Drive";
            }
            if (PlatformName.IsEqual("Super Nintendo Entertainment System"))
            {
                PlatformName = "SNES";
            }
            if (PlatformName.IsEqual("Nintendo Game Boy"))
            {
                PlatformName = "Game Boy";
            }
            if (PlatformName.IsEqual("Nintendo Game Boy Advance"))
            {
                PlatformName = "Game Boy Advance";
            }
            if (PlatformName.IsEqual("Nintendo Game Boy Color"))
            {
                PlatformName = "Game Boy Color";
            }
            if (PlatformName.IsEqual("Nintendo Entertainment System"))
            {
                PlatformName = "NES";
            }
            if (PlatformName.IsEqual("PC Engine SuperGrafx"))
            {
                PlatformName = "PC Engine";
            }
            if (PlatformName.IsEqual("Sega 32X"))
            {
                PlatformName = "32X";
            }
            if (PlatformName.IsEqual("Sega Master System"))
            {
                PlatformName = "Master System";
            }
            if (PlatformName.IsEqual("Sony PlayStation"))
            {
                PlatformName = "PlayStation";
            }
            if (PlatformName.IsEqual("SNK Neo Geo Pocket"))
            {
                PlatformName = "Neo Geo Pocket";
            }
            if (PlatformName.IsEqual("Sega Game Gear"))
            {
                PlatformName = "Game Gear";
            }
            if (PlatformName.IsEqual("Nintendo GameCube"))
            {
                PlatformName = "GameCube";
            }
            if (PlatformName.IsEqual("Nintendo Wii"))
            {
                PlatformName = "Wii";
            }
            if (PlatformName.IsEqual("Nintendo Wii U"))
            {
                PlatformName = "Wii U";
            }
            if (PlatformName.IsEqual("Sony PlayStation 2"))
            {
                PlatformName = "PlayStation 2";
            }
            if (PlatformName.IsEqual("Microsoft Xbox"))
            {
                PlatformName = "Xbox";
            }
            if (PlatformName.IsEqual("Magnavox Odyssey2"))
            {
                PlatformName = "Magnavox Odyssey 2";
            }
            if (PlatformName.IsEqual("PC (DOS)"))
            {
                PlatformName = "DOS";
            }
            if (PlatformName.IsEqual("Various"))
            {
                PlatformName = "Arcade";
            }
            if (PlatformName.IsEqual("MAME 2003 Plus"))
            {
                PlatformName = "Arcade";
            }
            if (PlatformName.IsEqual("Nintendo Virtual Boy"))
            {
                PlatformName = "Virtual Boy";
            }
            if (PlatformName.IsEqual("Sega SG 1000"))
            {
                PlatformName = "SG-1000";
            }
            if (PlatformName.IsEqual("Atari ST/STE/TT/Falcon"))
            {
                PlatformName = "Atari ST";
            }
            if (PlatformName.IsEqual("Sega Saturn"))
            {
                PlatformName = "Saturn";
            }
            if (PlatformName.IsEqual("Sega Dreamcast"))
            {
                PlatformName = "Dreamcast";
            }
            if (PlatformName.IsEqual("Sony PSP"))
            {
                PlatformName = "PlayStation Portable";
            }
            if (PlatformName.IsEqual("Coleco ColecoVision"))
            {
                PlatformName = "ColecoVision";
            }
            if (PlatformName.IsEqual("SNK Neo Geo CD"))
            {
                PlatformName = "Neo Geo CD";
            }


            var FindConsole = ra_Consoles.ListConsoles.Find(x => PlatformName.ToLower() == x.Name.ToLower());
            if (FindConsole != null)
            {
                consoleID = FindConsole.ID;
            }

            // Search game id
            int gameID = 0;
            if (consoleID != 0)
            {
                RA_Games ra_Games = GetGameList(consoleID);
                ra_Games.ListGames.Sort((x, y) => (y.Title).CompareTo(x.Title));

                foreach (RA_Game ra_Game in ra_Games.ListGames)
                {
                    string retroArchTitle = ra_Game.Title;
                    //TODO: Decide if editions should be removed here
                    string normalizedRetroArchTitle = PlayniteTools.NormalizeGameName(retroArchTitle, true);
                    string normalizedPlayniteTitle = PlayniteTools.NormalizeGameName(GameName, true);
                    if (normalizedPlayniteTitle == normalizedRetroArchTitle && gameID == 0)
                    {
                        logger.Info($"Find for {GameName.Trim().ToLower()} / {retroArchTitle} with {PlatformName} in {consoleID}");
                        gameID = ra_Game.ID;
                        break;
                    }

                    string[] TitleSplits = retroArchTitle.Split('|');
                    if (TitleSplits.Length > 1)
                    {
                        foreach (string TitleSplit in TitleSplits)
                        {
                            if (GameName.Trim().ToLower() == TitleSplit.Trim() && gameID == 0)
                            {
                                logger.Info($"SuccessStory - Find for {GameName.Trim().ToLower()} / {TitleSplit.Trim()} with {PlatformName} in {consoleID}");
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
                            if (GameName.Trim().ToLower() == TitleSplit.Trim() && gameID == 0)
                            {
                                logger.Info($"Find for {GameName.Trim().ToLower()} / {TitleSplit.Trim()} with {PlatformName} in {consoleID}");
                                gameID = ra_Game.ID;
                                break;
                            }
                        }
                    }

                    if (normalizedPlayniteTitle == normalizedRetroArchTitle && gameID == 0)
                    {
                        logger.Info($"Find for {normalizedPlayniteTitle} / {normalizedRetroArchTitle} with {PlatformName} in {consoleID}");
                        gameID = ra_Game.ID;
                        break;
                    }
                }
            }
            else
            {
                logger.Warn($"No console find for {GameName} with {PlatformName}");
            }

            if (gameID == 0)
            {
                logger.Warn($"No game find for {GameName} with {PlatformName} in {consoleID}");
            }

            return gameID;
        }

        private int GetGameIdByHash(Game game, List<RA_MD5List> rA_MD5Lists)
        {
            int GameId = 0;
            string HashMD5 = string.Empty;
            RA_MD5List rA_MD5List = null;
            string FilePath = PluginDatabase.PlayniteApi.ExpandGameVariables(game, game.Roms.FirstOrDefault().Path);

            if (!File.Exists(FilePath))
            {
                return GameId;
            }
            
            if (FilePath.Contains(".rar") && FilePath.Contains(".7z"))
            {
                return GameId;
            }
            if (FilePath.Contains(".zip"))
            {
                // Exclude for performance
                FileInfo fi = new FileInfo(FilePath);
                if (fi.Length > 10000000)
                {
                    return GameId;
                }
                else
                {
                    FilePath = ZipFileManafeExtract(FilePath);
                }              
            }

            if (!File.Exists(FilePath))
            {
                logger.Warn($"No file found for RA hash - {FilePath}");
                ZipFileManafeRemove();
                return GameId;
            }
            else
            {
                // Exclude for performance
                FileInfo fi = new FileInfo(FilePath);
                if (fi.Length > 800000000)
                {
                    logger.Warn($"The file is too long - {FilePath}");
                    return GameId;
                }
            }
            
            HashMD5 = GetHash(FilePath, PlatformType.All);
            rA_MD5List = rA_MD5Lists.Find(x => x.MD5.ToLower() == HashMD5.ToLower());
            if (rA_MD5List != null)
            {
                ZipFileManafeRemove();
                logger.Info($"Find for {game.Name} with {HashMD5} in PlatformType.All");
                return rA_MD5List.Id;
            }
            if (GameId == 0)
            {
                logger.Warn($"No game find for {game.Name} with {HashMD5} in PlatformType.All");
            }

            HashMD5 = GetHash(FilePath, PlatformType.SNES);
            rA_MD5List = rA_MD5Lists.Find(x => x.MD5.ToLower() == HashMD5.ToLower());
            if (rA_MD5List != null)
            {
                ZipFileManafeRemove();
                logger.Info($"Find for {game.Name} with {HashMD5} in PlatformType.SNES");
                return rA_MD5List.Id;
            }
            if (GameId == 0)
            {
                logger.Warn($"No game find for {game.Name} with {HashMD5} in PlatformType.SNES");
            }

            HashMD5 = GetHash(FilePath, PlatformType.NES);
            rA_MD5List = rA_MD5Lists.Find(x => x.MD5.ToLower() == HashMD5.ToLower());
            if (rA_MD5List != null)
            {
                ZipFileManafeRemove();
                logger.Info($"Find for {game.Name} with {HashMD5} in PlatformType.SNES");
                return rA_MD5List.Id;
            }
            if (GameId == 0)
            {
                logger.Warn($"No game find for {game.Name} with {HashMD5} in PlatformType.SNES");
            }

            HashMD5 = GetHash(FilePath, PlatformType.Arcade);
            rA_MD5List = rA_MD5Lists.Find(x => x.MD5.ToLower() == HashMD5.ToLower());
            if (rA_MD5List != null)
            {
                ZipFileManafeRemove();
                logger.Info($"Find for {game.Name} with {HashMD5} in PlatformType.Sega_CD_Saturn");
                return rA_MD5List.Id;
            }
            if (GameId == 0)
            {
                logger.Warn($"No game find for {game.Name} with {HashMD5} in PlatformType.Sega_CD_Saturn");
            }

            HashMD5 = GetHash(FilePath, PlatformType.Famicom);
            rA_MD5List = rA_MD5Lists.Find(x => x.MD5.ToLower() == HashMD5.ToLower());
            if (rA_MD5List != null)
            {
                ZipFileManafeRemove();
                logger.Info($"Find for {game.Name} with {HashMD5} in PlatformType.SNES");
                return rA_MD5List.Id;
            }
            if (GameId == 0)
            {
                logger.Warn($"No game find for {game.Name} with {HashMD5} in PlatformType.SNES");
            }

            HashMD5 = GetHash(FilePath, PlatformType.Sega_CD_Saturn);
            rA_MD5List = rA_MD5Lists.Find(x => x.MD5.ToLower() == HashMD5.ToLower());
            if (rA_MD5List != null)
            {
                ZipFileManafeRemove();
                logger.Info($"Find for {game.Name} with {HashMD5} in PlatformType.Sega_CD_Saturn");
                return rA_MD5List.Id;
            }
            if (GameId == 0)
            {
                logger.Warn($"No game find for {game.Name} with {HashMD5} in PlatformType.Sega_CD_Saturn");
            }

            ZipFileManafeRemove();
            return GameId;
        }

        private string GetHash(string FilePath, PlatformType platformType)
        {
            try
            {
                byte[] byteSequence = File.ReadAllBytes(FilePath);
                long length = new FileInfo(FilePath).Length;

                byte[] byteSequenceFinal = byteSequence;

                if (platformType == PlatformType.SNES)
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

                if (platformType == PlatformType.NES)
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

                if (platformType == PlatformType.Famicom)
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

                if (platformType == PlatformType.Sega_CD_Saturn)
                {
                    try
                    {
                        byteSequenceFinal = new byte[512];
                        Buffer.BlockCopy(byteSequence, 0, byteSequenceFinal, 0, 512);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex,true);
                    }
                }

                if (platformType == PlatformType.Arcade)
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
                Common.LogError(ex, false);
                return string.Empty;
            }
        }

        private static string GetMd5(byte[] byteSequence)
        {
            try
            {
                using (var md5 = MD5.Create())
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


        private string ZipFileManafeExtract(string FilePath)
        {
            ZipFileManafeRemove();

            string extractPath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "tempZip");
            ZipFile.ExtractToDirectory(FilePath, extractPath);

            string FilePathReturn = string.Empty;
            Parallel.ForEach(Directory.EnumerateFiles(extractPath, "*.*"), (objectFile) => 
            {
                FilePathReturn = objectFile;

                string FileExtension = Path.GetExtension(objectFile).ToLower(); ;

                if (FileExtension == "nes")
                {
                    return;
                }
            });

            return FilePathReturn;
        }

        private void ZipFileManafeRemove()
        {
            string extractPath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "tempZip");
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }
        }


        private RA_Games GetGameList(int consoleID)
        {
            string Target = "API_GetGameList.php";
            string url = string.Format(BaseUrl + Target + @"?z={0}&y={1}&i={2}", User, Key, consoleID);

            RA_Games resultObj = new RA_Games();

            string fileConsoles = PluginDatabase.Paths.PluginUserDataPath + "\\RA_Games_" + consoleID + ".json";
            if (File.Exists(fileConsoles))
            {
                resultObj = Serialization.FromJsonFile<RA_Games>(fileConsoles);
                return resultObj;
            }

            string ResultWeb = string.Empty;
            try
            {
                ResultWeb = Web.DownloadStringData(url).GetAwaiter().GetResult();
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false, $"Failed to load from {url}");
            }

            try
            {
                resultObj.ListGames = Serialization.FromJson<List<RA_Game>>(ResultWeb);
                File.WriteAllText(fileConsoles, Serialization.ToJson(resultObj), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to parse {ResultWeb}");
            }

            return resultObj;
        }


        private List<Achievements> GetGameInfoAndUserProgress(int gameID)
        {
            List<Achievements> Achievements = new List<Achievements>();

            string Target = "API_GetGameInfoAndUserProgress.php";
            UrlAchievements = string.Format(BaseUrl + Target + @"?z={0}&y={1}&u={0}&g={2}", User, Key, gameID);

            string ResultWeb = string.Empty;
            try
            {
                ResultWeb = Web.DownloadStringData(UrlAchievements).GetAwaiter().GetResult();
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false, $"Failed to load from {UrlAchievements}");
                return Achievements;
            }

            try
            {
                dynamic resultObj = Serialization.FromJson<dynamic>(ResultWeb);

                GameNameAchievements = (string)resultObj["Title"];
                int NumDistinctPlayersCasual = (int)resultObj["NumDistinctPlayersCasual"];

                if (resultObj["Achievements"] != null)
                {
                    foreach (var item in resultObj["Achievements"])
                    {
                        foreach (var it in item)
                        {
                            Achievements.Add(new Achievements
                            {
                                Name = (string)it["Title"],
                                Description = (string)it["Description"],
                                UrlLocked = string.Format(BaseUrlLocked, (string)it["BadgeName"]),
                                UrlUnlocked = string.Format(BaseUrlUnlocked, (string)it["BadgeName"]),
                                DateUnlocked = (it["DateEarned"] == null) ? default(DateTime) : Convert.ToDateTime((string)it["DateEarned"]),
                                Percent = (int)it["NumAwarded"] * 100 / NumDistinctPlayersCasual
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"[{gameID}] Failed to parse {ResultWeb}");
                return Achievements;
            }

            return Achievements;
        }
        #endregion
    }


    public class RA_Consoles
    {
        public List<RA_Console> ListConsoles { get; set; }
    }

    public class RA_Console
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    public class RA_Games
    {
        public List<RA_Game> ListGames { get; set; }
    }

    public class RA_Game
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public int ConsoleID { get; set; }
        public string ImageIcon { get; set; }
        public string ConsoleName { get; set; }
    }

    public class RA_MD5ListResponse
    {
        public bool Success { get; set; }
        public dynamic MD5List { get; set; }
    }

    public class RA_MD5List
    {
        public string MD5 { get; set; }
        public int Id { get; set; }
    }
}
