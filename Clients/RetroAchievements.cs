using Newtonsoft.Json.Linq;
using Playnite.SDK;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;
using SuccessStory.Models;
using Newtonsoft.Json;
using Playnite.SDK.Models;
using System.IO;
using SuccessStory.Services;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    class RetroAchievements : GenericAchievements
    {
        private readonly string BaseUrl = "https://retroachievements.org/API/";
        private readonly string BaseUrlUnlocked = "https://s3-eu-west-1.amazonaws.com/i.retroachievements.org/Badge/{0}.png";
        private readonly string BaseUrlLocked = "https://s3-eu-west-1.amazonaws.com/i.retroachievements.org/Badge/{0}_lock.png";

        private readonly string BaseMD5List = "http://retroachievements.org/dorequest.php?r=hashlibrary&c={0}";

        private string User { get; set; }
        private string Key { get; set; }


        public RetroAchievements(IPlayniteAPI PlayniteApi, SuccessStorySettings settings, string PluginUserDataPath) : base(PlayniteApi, settings, PluginUserDataPath)
        {
            User = settings.RetroAchievementsUser;
            Key = settings.RetroAchievementsKey;
        }


        public override GameAchievements GetAchievements(Game game)
        {
            List<Achievements> AllAchievements = new List<Achievements>();
            string GameName = game.Name;
            string ClientId = game.PlayAction.EmulatorId.ToString();

            GameAchievements Result = SuccessStory.PluginDatabase.GetDefault(game);
            Result.Items = AllAchievements;


            if (User == string.Empty || Key == string.Empty)
            {
                logger.Error($"SuccessStory - No RetroAchievement configuration.");
                SuccessStoryDatabase.ListErrors.Add($"Error on RetroAchievement: no RetroAchievement configuration in settings menu of plugin.");
                return null;
            }

            // Load list console
            RA_Consoles ra_Consoles = GetConsoleIDs(_PluginUserDataPath);
            if (ra_Consoles != null && ra_Consoles != new RA_Consoles())
            {
                ra_Consoles.ListConsoles.Sort((x, y) => (y.Name).CompareTo(x.Name));
            }
            else
            {
                logger.Warn($"SuccessStory - No ra_Consoles find");
            }

            // List MD5
            List<RA_MD5List> ListMD5 = GetMD5List(_PluginUserDataPath, ra_Consoles);


            // Game Id
            int gameID = 0;
            gameID = GetGameIdByHash(game, ListMD5);
            if (gameID == 0)
            {
                gameID = GetGameIdByName(game, ra_Consoles);
            }

            // Get achievements
            if (gameID != 0)
            {
                AllAchievements = GetGameInfoAndUserProgress(gameID);
            }
            else
            {
                return Result;
            }

            Result.HaveAchivements = (AllAchievements.Count > 0);
            Result.Items = AllAchievements;
            Result.Total = AllAchievements.Count;
            Result.Unlocked = AllAchievements.FindAll(x => x.DateUnlocked != default(DateTime)).Count;
            Result.Locked = Result.Total - Result.Unlocked;
            Result.Progression = (Result.Total != 0) ? (int)Math.Ceiling((double)(Result.Unlocked * 100 / Result.Total)) : 0;


            return Result;
        }


        public override bool IsConfigured()
        {
            return (User != string.Empty && Key != string.Empty);
        }

        public override bool IsConnected()
        {
            throw new NotImplementedException();
        }


        private RA_Consoles GetConsoleIDs(string PluginUserDataPath)
        {
            string Target = "API_GetConsoleIDs.php";
            string url = string.Format(BaseUrl + Target + @"?z={0}&y={1}", User, Key);

            RA_Consoles resultObj = new RA_Consoles();

            string fileConsoles = PluginUserDataPath + "\\RA_Consoles.json";
            if (File.Exists(fileConsoles))
            {
                resultObj = JsonConvert.DeserializeObject<RA_Consoles>(File.ReadAllText(fileConsoles));
                return resultObj;
            }

            string ResultWeb = string.Empty;

            try
            {
                ResultWeb = Web.DownloadStringData(url).GetAwaiter().GetResult();
            }
            catch (WebException ex)
            {
                Common.LogError(ex, "SuccessStory", $"Failed to load from {url}");
            }


            if (!ResultWeb.IsNullOrEmpty())
            {
                try
                {
                    resultObj.ListConsoles = JsonConvert.DeserializeObject<List<RA_Console>>(ResultWeb);
                    File.WriteAllText(fileConsoles, JsonConvert.SerializeObject(resultObj));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", $"Failed to parse {ResultWeb}");
                }
            }

            return resultObj;
        }

        private List<RA_MD5List> GetMD5List(string PluginUserDataPath, RA_Consoles rA_Consoles) 
        {
            List<RA_MD5List> ListMD5 = new List<RA_MD5List>();

            // Cache
            string fileMD5List = PluginUserDataPath + "\\RA_MD5List.json";
            if (File.Exists(fileMD5List) && File.GetLastWriteTime(fileMD5List).AddDays(3) > DateTime.Now )
            {
                ListMD5 = JsonConvert.DeserializeObject<List<RA_MD5List>>(File.ReadAllText(fileMD5List));
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
                        RA_MD5ListResponse ResultMD5List = JsonConvert.DeserializeObject<RA_MD5ListResponse>(ResultWeb);

                        foreach (var obj in ResultMD5List.MD5List)
                        {
                            ListMD5.Add(new RA_MD5List { Id = (int)obj.Value, MD5 = obj.Key });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", $"Error GetConsoleIDs({rA_Console.ID}, {rA_Console.Name})");
                }
            }

            // Save
            if (ListMD5.Count > 0)
            {
                try
                {
                    File.WriteAllText(fileMD5List, JsonConvert.SerializeObject(ListMD5));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SuccessStory", $"Failed to save ListMD5");
                }
            }

            return ListMD5;
        }


        private int GetGameIdByName(Game game, RA_Consoles ra_Consoles)
        {
            string GameName = game.Name;

            // Search id console for the game
            string PlatformName = game.Platform.Name;
            int consoleID = 0;

            var FindConsole = ra_Consoles.ListConsoles.Find(x => PlatformName.ToLower().Contains(x.Name.ToLower()));
            if (FindConsole != null)
            {
                consoleID = FindConsole.ID;
            }

            if (consoleID != 0)
            {
                foreach (RA_Console ra_Console in ra_Consoles.ListConsoles)
                {
                    string NameConsole = ra_Console.Name.ToLower();
                    if (NameConsole == "snes")
                    {
                        NameConsole = "super nintendo";
                    }
                    if (NameConsole == "nes")
                    {
                        NameConsole = "nintendo";
                    }
                    if (NameConsole == "mega drive")
                    {
                        NameConsole = "sega genesis";
                    }

                    if (PlatformName.ToLower().IndexOf(NameConsole) > -1)
                    {
                        consoleID = ra_Console.ID;
                        break;
                    }
                }
            }

            // Search game id
            int gameID = 0;
            if (consoleID != 0)
            {
                RA_Games ra_Games = GetGameList(consoleID, _PluginUserDataPath);
                ra_Games.ListGames.Sort((x, y) => (y.Title).CompareTo(x.Title));
                foreach (RA_Game ra_Game in ra_Games.ListGames)
                {
                    string Title = ra_Game.Title.Trim().ToLower();
                    if (GameName.Trim().ToLower() == Title && gameID == 0)
                    {
                        logger.Info($"SuccessStory - Find for {GameName.Trim().ToLower()} / {Title} with {PlatformName} in {consoleID}");
                        gameID = ra_Game.ID;
                        break;
                    }

                    string[] TitleSplits = Title.Split('|');
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

                    TitleSplits = Title.Split('-');
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
                }
            }
            else
            {
                logger.Warn($"SuccessStory - No console find for {GameName} with {PlatformName}");
            }

            if (gameID == 0)
            {
                logger.Warn($"SuccessStory - No game find for {GameName} with {PlatformName} in {consoleID}");
            }

            return gameID;
        }

        private int GetGameIdByHash(Game game, List<RA_MD5List> rA_MD5Lists)
        {
            int GameId = 0;
            string HashMD5 = string.Empty;
            RA_MD5List rA_MD5List = null;
            string FilePath = _PlayniteApi.Database.GetFullFilePath(game.GameImagePath);

            if (FilePath.Contains(".zip"))
            {
                FilePath = ZipFileManafeExtract(FilePath);
            }

            if (!File.Exists(FilePath))
            {
                logger.Warn($"SuccessStory - No file found for RA hash - {FilePath}");
                ZipFileManafeRemove();
                return GameId;
            }
            
            HashMD5 = GetHash(FilePath, PlatformType.All);
            rA_MD5List = rA_MD5Lists.Find(x => x.MD5.ToLower() == HashMD5.ToLower());
            if (rA_MD5List != null)
            {
                ZipFileManafeRemove();
                logger.Info($"SuccessStory - Find for {game.Name} with {HashMD5} in PlatformType.All");
                return rA_MD5List.Id;
            }
            if (GameId == 0)
            {
                logger.Warn($"SuccessStory - No game find for {game.Name} with {HashMD5} in PlatformType.All");
            }

            HashMD5 = GetHash(FilePath, PlatformType.SNES);
            rA_MD5List = rA_MD5Lists.Find(x => x.MD5.ToLower() == HashMD5.ToLower());
            if (rA_MD5List != null)
            {
                ZipFileManafeRemove();
                logger.Info($"SuccessStory - Find for {game.Name} with {HashMD5} in PlatformType.SNES");
                return rA_MD5List.Id;
            }
            if (GameId == 0)
            {
                logger.Warn($"SuccessStory - No game find for {game.Name} with {HashMD5} in PlatformType.SNES");
            }

            HashMD5 = GetHash(FilePath, PlatformType.NES);
            rA_MD5List = rA_MD5Lists.Find(x => x.MD5.ToLower() == HashMD5.ToLower());
            if (rA_MD5List != null)
            {
                ZipFileManafeRemove();
                logger.Info($"SuccessStory - Find for {game.Name} with {HashMD5} in PlatformType.SNES");
                return rA_MD5List.Id;
            }
            if (GameId == 0)
            {
                logger.Warn($"SuccessStory - No game find for {game.Name} with {HashMD5} in PlatformType.SNES");
            }

            HashMD5 = GetHash(game.GameImagePath, PlatformType.Arcade);
            rA_MD5List = rA_MD5Lists.Find(x => x.MD5.ToLower() == HashMD5.ToLower());
            if (rA_MD5List != null)
            {
                ZipFileManafeRemove();
                logger.Info($"SuccessStory - Find for {game.Name} with {HashMD5} in PlatformType.Sega_CD_Saturn");
                return rA_MD5List.Id;
            }
            if (GameId == 0)
            {
                logger.Warn($"SuccessStory - No game find for {game.Name} with {HashMD5} in PlatformType.Sega_CD_Saturn");
            }

            HashMD5 = GetHash(FilePath, PlatformType.Famicom);
            rA_MD5List = rA_MD5Lists.Find(x => x.MD5.ToLower() == HashMD5.ToLower());
            if (rA_MD5List != null)
            {
                ZipFileManafeRemove();
                logger.Info($"SuccessStory - Find for {game.Name} with {HashMD5} in PlatformType.SNES");
                return rA_MD5List.Id;
            }
            if (GameId == 0)
            {
                logger.Warn($"SuccessStory - No game find for {game.Name} with {HashMD5} in PlatformType.SNES");
            }

            HashMD5 = GetHash(FilePath, PlatformType.Sega_CD_Saturn);
            rA_MD5List = rA_MD5Lists.Find(x => x.MD5.ToLower() == HashMD5.ToLower());
            if (rA_MD5List != null)
            {
                ZipFileManafeRemove();
                logger.Info($"SuccessStory - Find for {game.Name} with {HashMD5} in PlatformType.Sega_CD_Saturn");
                return rA_MD5List.Id;
            }
            if (GameId == 0)
            {
                logger.Warn($"SuccessStory - No game find for {game.Name} with {HashMD5} in PlatformType.Sega_CD_Saturn");
            }

            ZipFileManafeRemove();
            return GameId;
        }

        private string GetHash(string FilePath, PlatformType platformType)
        {
            byte[] byteSequence = File.ReadAllBytes(FilePath);
            long length = new FileInfo(FilePath).Length;

            byte[] byteSequenceFinal = byteSequence;

            if (platformType == PlatformType.SNES)
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

            if (platformType == PlatformType.NES)
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

            if (platformType == PlatformType.Famicom)
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

            if (platformType == PlatformType.Sega_CD_Saturn)
            {
                byteSequenceFinal = new byte[512];
                Buffer.BlockCopy(byteSequence, 0, byteSequenceFinal, 0, 512);
            }

            if (platformType == PlatformType.Arcade)
            {
                string FileName = Path.GetFileNameWithoutExtension(FilePath);
                byteSequenceFinal = Encoding.ASCII.GetBytes(FileName);
            }

            return GetMd5(byteSequenceFinal);
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
                return string.Empty;
            }
        }


        private string ZipFileManafeExtract(string FilePath)
        {
            ZipFileManafeRemove();

            string extractPath = Path.Combine(_PluginUserDataPath, "tempZip");
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
            string extractPath = Path.Combine(_PluginUserDataPath, "tempZip");
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }
        }


        private RA_Games GetGameList(int consoleID, string PluginUserDataPath)
        {
            string Target = "API_GetGameList.php";
            string url = string.Format(BaseUrl + Target + @"?z={0}&y={1}&i={2}", User, Key, consoleID);

            RA_Games resultObj = new RA_Games();

            string fileConsoles = PluginUserDataPath + "\\RA_Games_" + consoleID + ".json";
            if (File.Exists(fileConsoles))
            {
                resultObj = JsonConvert.DeserializeObject<RA_Games>(File.ReadAllText(fileConsoles));
                return resultObj;
            }

            string ResultWeb = string.Empty;
            try
            {
                ResultWeb = Web.DownloadStringData(url).GetAwaiter().GetResult();
            }
            catch (WebException ex)
            {
                Common.LogError(ex, "SuccessStory", $"Failed to load from {url}");
            }

            try
            {
                resultObj.ListGames = JsonConvert.DeserializeObject<List<RA_Game>>(ResultWeb);
                File.WriteAllText(fileConsoles, JsonConvert.SerializeObject(resultObj));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", $"Failed to parse {ResultWeb}");
            }

            return resultObj;
        }


        private List<Achievements> GetGameInfoAndUserProgress(int gameID)
        {
            List<Achievements> Achievements = new List<Achievements>();

            string Target = "API_GetGameInfoAndUserProgress.php";
            string url = string.Format(BaseUrl + Target + @"?z={0}&y={1}&u={0}&g={2}", User, Key, gameID);

            string ResultWeb = string.Empty;
            try
            {
                ResultWeb = Web.DownloadStringData(url).GetAwaiter().GetResult();
            }
            catch (WebException ex)
            {
                Common.LogError(ex, "SuccessStory", $"Failed to load from {url}");
                return Achievements;
            }

            try
            {
                JObject resultObj = new JObject();
                resultObj = JObject.Parse(ResultWeb);

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
                Common.LogError(ex, "SuccessStory", $"[{gameID}] Failed to parse {ResultWeb}");
                return Achievements;
            }

            return Achievements;
        }
    }

    enum PlatformType
    {
        All,
        SNES,
        Sega_CD_Saturn,
        NES,
        Famicom,
        Arcade
    }

    class RA_Consoles
    {
        public List<RA_Console> ListConsoles { get; set; }
    }
    class RA_Console
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    class RA_Games
    {
        public List<RA_Game> ListGames { get; set; }
    }
    class RA_Game
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public int ConsoleID { get; set; }
        public string ImageIcon { get; set; }
        public string ConsoleName { get; set; }
    }

    class RA_MD5ListResponse
    {
        public bool Success { get; set; }
        public JObject MD5List { get; set; }
    }
    class RA_MD5List
    {
        public string MD5 { get; set; }
        public int Id { get; set; }
    }
}
