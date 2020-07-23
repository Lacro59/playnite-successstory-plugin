using Newtonsoft.Json.Linq;
using Playnite.Common.Web;
using Playnite.SDK;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using SuccessStory.Models;
using Newtonsoft.Json;
using SuccessStory.Database;
using Playnite.SDK.Models;
using System.IO;

namespace SuccessStory.Clients
{
    class RetroAchievements
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private string BaseUrl = "https://retroachievements.org/API/";
        private string BaseUrlUnlocked = "https://s3-eu-west-1.amazonaws.com/i.retroachievements.org/Badge/{0}.png";
        private string BaseUrlLocked = "https://s3-eu-west-1.amazonaws.com/i.retroachievements.org/Badge/{0}_lock.png";

        private string User { get; set; }
        private string Key { get; set; }


        public RetroAchievements(SuccessStorySettings settings)
        {
            User = settings.RetroAchievementsUser;
            Key = settings.RetroAchievementsKey;
        }

        public GameAchievements GetAchievements(IPlayniteAPI PlayniteApi, Guid Id, string PluginUserDataPath)
        {
            List<Achievements> Achievements = new List<Achievements>();
            Game game = PlayniteApi.Database.Games.Get(Id);
            string GameName = game.Name;
            string ClientId = game.PlayAction.EmulatorId.ToString();
            bool HaveAchivements = false;
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            GameAchievements Result = new GameAchievements
            {
                Name = GameName,
                HaveAchivements = HaveAchivements,
                IsEmulators = true,
                Total = Total,
                Unlocked = Unlocked,
                Locked = Locked,
                Progression = 0,
                Achievements = Achievements
            };


            if (User == "" || Key == "")
            {
                logger.Error($"SuccessStory - No RetroAchievement configuration.");
                AchievementsDatabase.ListErrors.Add($"Error on RetroAchievement: no RetroAchievement configuration in settings menu of plugin.");
                return null;
            }

            // Load list console
            RA_Consoles ra_Consoles = GetConsoleIDs(PluginUserDataPath);
            ra_Consoles.ListConsoles.Sort((x, y) => (y.Name).CompareTo(x.Name));

            // Search id console for the game
            string PlatformName = game.Platform.Name;
            int consoleID = 0;
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

                if (PlatformName.ToLower().IndexOf(NameConsole) > - 1)
                {
                    consoleID = ra_Console.ID;
                    break;
                }
            }

            // Search game id
            int gameID = 0;
            if (consoleID != 0)
            {
                RA_Games ra_Games = GetGameList(consoleID, PluginUserDataPath);
                ra_Games.ListGames.Sort((x, y) => (y.Title).CompareTo(x.Title));
                foreach (RA_Game ra_Game in ra_Games.ListGames)
                {
                    //if (GameName.ToLower().IndexOf(ra_Game.Title.ToLower()) > -1 || ra_Game.Title.ToLower().IndexOf(GameName.ToLower()) > -1)
                    var temp = ra_Game.Title.Split('|');

                    foreach (string Title in temp)
                    {
                        if (GameName.ToLower().IndexOf(Title.Trim().ToLower()) > -1)
                        {
                            logger.Debug($"SuccessStory - Find for {GameName} / {Title.Trim()} with {PlatformName} in {consoleID}");
                            gameID = ra_Game.ID;
                            break;
                        }
                    }
                }
            }
            else
            {
                logger.Info($"SuccessStory - No console find for {GameName} with {PlatformName}");
                return Result;
            }

            // Get achievements
            if (gameID != 0)
            {
                Achievements = GetGameInfoAndUserProgress(gameID);
            }
            else
            {
                logger.Info($"SuccessStory - No game find for {GameName} with {PlatformName} in {consoleID}");
                return Result;
            }

            Result.HaveAchivements = (Achievements.Count > 0);
            Result.Achievements = Achievements;
            Result.Total = Achievements.Count;
            Result.Unlocked = Achievements.FindAll(x => x.DateUnlocked != default(DateTime)).Count;
            Result.Locked = Result.Total - Result.Unlocked;
            Result.Progression = (Result.Total != 0) ? (int)Math.Ceiling((double)(Result.Unlocked * 100 / Result.Total)) : 0;


            return Result;
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

            string ResultWeb = "";
            try
            {
                ResultWeb = HttpDownloader.DownloadString(url, Encoding.UTF8);
            }
            catch (WebException ex)
            {
                Common.LogError(ex, "SuccessStory", $"Failed to load from {url}");
            }


            try
            {
                resultObj.ListConsoles = JsonConvert.DeserializeObject<List<RA_Console>>(ResultWeb);
                File.WriteAllText(fileConsoles, JsonConvert.SerializeObject(resultObj));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory", $"Failed to parse {ResultWeb}");
            }

            return resultObj;
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

            string ResultWeb = "";
            try
            {
                ResultWeb = HttpDownloader.DownloadString(url, Encoding.UTF8);
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

            string ResultWeb = "";
            try
            {
                ResultWeb = HttpDownloader.DownloadString(url, Encoding.UTF8);
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
                                DateUnlocked = (it["DateEarned"] == null) ? default(DateTime) : Convert.ToDateTime((string)it["DateEarned"])
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

}
