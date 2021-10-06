using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using CommonPluginsShared;
using CommonPluginsStores;
using Playnite.SDK.Models;
using Playnite.SDK.Data;
using SuccessStory.Models;
using CommonPluginsShared.Models;

namespace SuccessStory.Clients
{
    class SteamEmulators : GenericAchievements
    {
        protected static SteamApi _steamApi;
        internal static SteamApi steamApi
        {
            get
            {
                if (_steamApi == null)
                {
                    _steamApi = new SteamApi();
                }
                return _steamApi;
            }

            set
            {
                _steamApi = value;
            }
        }

        private List<string> AchievementsDirectories = new List<string>();
        private int SteamId { get; set; } = 0;


        public SteamEmulators(List<Folder> LocalFolders) : base("SteamEmulators")
        {
            AchievementsDirectories.Add("%PUBLIC%\\Documents\\Steam\\CODEX");
            AchievementsDirectories.Add("%appdata%\\Steam\\CODEX");

            AchievementsDirectories.Add("%ProgramData%\\Steam");
            AchievementsDirectories.Add("%localappdata%\\SKIDROW");
            AchievementsDirectories.Add("%DOCUMENTS%\\SKIDROW");

            foreach (Folder folder in LocalFolders)
            {
                AchievementsDirectories.Add(folder.FolderPath);
            }
        }



        public override GameAchievements GetAchievements(Game game)
        {
            throw new NotImplementedException();
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            // The authentification is only for localised achievement
            return true;
        }

        public override bool EnabledInSettings()
        {
            // No necessary activation
            return true;
        }
        #endregion


        public int GetSteamId()
        {
            return SteamId;
        }


        #region SteamEmulator
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public GameAchievements GetAchievementsLocal(Game game, string apiKey, int SteamId = 0, bool IsManual = false)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();

            if (SteamId != 0)
            {
                this.SteamId = SteamId;
            }
            else
            {
                this.SteamId = steamApi.GetSteamId(game.Name);
            }


            AllAchievements = Get(this.SteamId, apiKey, IsManual);
            gameAchievements.Items = AllAchievements;


            return gameAchievements;
        }

        private List<Achievements> Get(int SteamId, string apiKey, bool IsManual)
        {
            List<Achievements> ReturnAchievements = new List<Achievements>();

            if (!IsManual)
            {
                // Search data local
                foreach (string DirAchivements in AchievementsDirectories)
                {
                    switch (DirAchivements.ToLower())
                    {
                        case ("%public%\\documents\\steam\\codex"):
                        case ("%appdata%\\steam\\codex"):
#if DEBUG
                            logger.Debug(Environment.ExpandEnvironmentVariables(DirAchivements) + $"\\{SteamId}\\achievements.ini");
#endif
                            if (File.Exists(Environment.ExpandEnvironmentVariables(DirAchivements) + $"\\{SteamId}\\achievements.ini"))
                            {
                                string line;

                                string Name = string.Empty;
                                DateTime? DateUnlocked = null;

                                StreamReader file = new StreamReader(Environment.ExpandEnvironmentVariables(DirAchivements) + $"\\{SteamId}\\achievements.ini");
                                while ((line = file.ReadLine()) != null)
                                {
                                    // Achievement name
                                    if (line.IndexOf("[") > -1)
                                    {
                                        Name = line.Replace("[", string.Empty).Replace("]", string.Empty).Trim();
                                    }
                                    // Achievement UnlockTime
                                    if (line.IndexOf("UnlockTime") > -1 && line.ToLower() != "unlocktime=0")
                                    {
                                        DateUnlocked = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(int.Parse(line.Replace("UnlockTime=", string.Empty)));
                                    }

                                    // End Achievement
                                    if (line.Trim() == string.Empty && DateUnlocked != null)
                                    {
                                        ReturnAchievements.Add(new Achievements
                                        {
                                            ApiName = Name,
                                            Name = string.Empty,
                                            Description = string.Empty,
                                            UrlUnlocked = string.Empty,
                                            UrlLocked = string.Empty,
                                            DateUnlocked = DateUnlocked
                                        });

                                        Name = string.Empty;
                                        DateUnlocked = null;
                                    }
                                }
                                file.Close();
                            }
                            break;

                        case "%programdata%\\steam":
#if DEBUG
                            logger.Debug(Environment.ExpandEnvironmentVariables("%ProgramData%\\Steam"));
#endif
                            if (Directory.Exists(Environment.ExpandEnvironmentVariables("%ProgramData%\\Steam")))
                            {
                                string[] dirsUsers = Directory.GetDirectories(Environment.ExpandEnvironmentVariables("%ProgramData%\\Steam"));
                                foreach (string dirUser in dirsUsers)
                                {
                                    if (File.Exists(dirUser + $"\\{SteamId}\\stats\\achievements.ini"))
                                    {
                                        string line;

                                        string Name = string.Empty;
                                        bool State = false;
                                        string sTimeUnlock = string.Empty;
                                        int timeUnlock = 0;
                                        DateTime? DateUnlocked = null;

                                        StreamReader file = new StreamReader(dirUser + $"\\{SteamId}\\stats\\achievements.ini");
                                        while ((line = file.ReadLine()) != null)
                                        {
                                            // Achievement name
                                            if (line.IndexOf("[") > -1)
                                            {
                                                Name = line.Replace("[", string.Empty).Replace("]", string.Empty).Trim();
                                                State = false;
                                                timeUnlock = 0;
                                                DateUnlocked = null;
                                            }

                                            if (Name != "Steam")
                                            {
                                                // State
                                                if (line.IndexOf("State") > -1 && line.ToLower() != "state = 0000000000")
                                                {
                                                    State = true;
                                                }

                                                // Unlock
                                                if (line.IndexOf("Time") > -1 && line.ToLower() != "time = 0000000000")
                                                {
                                                    sTimeUnlock = line.Replace("Time = ", string.Empty);
                                                    timeUnlock = BitConverter.ToInt32(StringToByteArray(line.Replace("Time = ", string.Empty)), 0);
                                                }
                                                if (line.IndexOf("CurProgress") > -1 && line.ToLower() != "curprogress = 0000000000")
                                                {
                                                    sTimeUnlock = line.Replace("CurProgress = ", string.Empty);
                                                    timeUnlock = BitConverter.ToInt32(StringToByteArray(line.Replace("CurProgress = ", string.Empty)), 0);
                                                }
                                                DateUnlocked = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(timeUnlock);

                                                // End Achievement
                                                if (timeUnlock != 0 && State)
                                                {
                                                    ReturnAchievements.Add(new Achievements
                                                    {
                                                        ApiName = Name,
                                                        Name = string.Empty,
                                                        Description = string.Empty,
                                                        UrlUnlocked = string.Empty,
                                                        UrlLocked = string.Empty,
                                                        DateUnlocked = DateUnlocked
                                                    });

                                                    Name = string.Empty;
                                                    State = false;
                                                    timeUnlock = 0;
                                                    DateUnlocked = null;
                                                }
                                            }
                                        }
                                        file.Close();
                                    }
                                }
                            }

                            break;

                        case "%localappdata%\\skidrow":
                            logger.Warn($"no treatment for {DirAchivements}");
                            break;

                        case "%documents%\\skidrow":
                            logger.Warn($"no treatment for {DirAchivements}");
                            break;

                        default:
                            if (ReturnAchievements.Count == 0)
                            {
                                if (!DirAchivements.ToLower().Contains("steamemu"))
                                {
                                    ReturnAchievements = GetSteamEmu(DirAchivements + $"\\{SteamId}\\SteamEmu");
                                }
                                else
                                {
                                    var DataPath = DirAchivements.Split('\\').ToList();
                                    int index = DataPath.FindIndex(x => x.ToLower() == "steamemu");
                                    string GameName = DataPath[index - 1];

                                    SteamApi steamApi = new SteamApi();
                                    int TempSteamId = steamApi.GetSteamId(GameName);

                                    if (TempSteamId == SteamId)
                                    {
                                        ReturnAchievements = GetSteamEmu(DirAchivements);
                                    }
                                }
                            }
                            break;
                    }
                }

                Common.LogDebug(true, $"{Serialization.ToJson(ReturnAchievements)}");

                if (ReturnAchievements == new List<Achievements>())
                {
                    logger.Error($"AchievementsLocal - No data for {SteamId}. ");
                    return new List<Achievements>();
                }
            }


            #region Get details achievements
            // List details acheviements
            string lang = CodeLang.GetSteamLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language);
            string url = string.Format(@"https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key={0}&appid={1}&l={2}", apiKey, SteamId, lang);

            string ResultWeb = string.Empty;
            try
            {
                ResultWeb = Web.DownloadStringData(url).GetAwaiter().GetResult();
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.BadRequest: // HTTP 400
                            break;
                        case HttpStatusCode.ServiceUnavailable: // HTTP 503
                            break;
                        default:
                            Common.LogError(ex, false, $"Failed to load from {url}");
                            break;
                    }
                    return new List<Achievements>();
                }
            }

            if (ResultWeb != string.Empty && ResultWeb.Length > 50)
            {
                dynamic resultObj = Serialization.FromJson<dynamic>(ResultWeb);
                dynamic resultItems = null;

                try
                {
                    resultItems = resultObj["game"]["availableGameStats"]["achievements"];

                    for (int i = 0; i < resultItems.Count; i++)
                    {
                        bool isFind = false;
                        for (int j = 0; j < ReturnAchievements.Count; j++)
                        {
                            if (ReturnAchievements[j].ApiName.ToLower() == ((string)resultItems[i]["name"]).ToLower())
                            {
                                Achievements temp = new Achievements
                                {
                                    ApiName = (string)resultItems[i]["name"],
                                    Name = (string)resultItems[i]["displayName"],
                                    Description = (string)resultItems[i]["description"],
                                    UrlUnlocked = (string)resultItems[i]["icon"],
                                    UrlLocked = (string)resultItems[i]["icongray"],
                                    DateUnlocked = ReturnAchievements[j].DateUnlocked
                                };

                                isFind = true;
                                ReturnAchievements[j] = temp;
                                j = ReturnAchievements.Count;
                            }
                        }

                        if (!isFind)
                        {
                            ReturnAchievements.Add(new Achievements
                            {
                                ApiName = (string)resultItems[i]["name"],
                                Name = (string)resultItems[i]["displayName"],
                                Description = (string)resultItems[i]["description"],
                                UrlUnlocked = (string)resultItems[i]["icon"],
                                UrlLocked = (string)resultItems[i]["icongray"],
                                DateUnlocked = default(DateTime)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, true, $"Failed to parse");
                    return new List<Achievements>();
                }
            }
            #endregion


            // Delete empty (SteamEmu)
            ReturnAchievements = ReturnAchievements.Select(x => x).Where(x => !string.IsNullOrEmpty(x.UrlLocked)).ToList();


            Common.LogDebug(true, $"{Serialization.ToJson(ReturnAchievements)}");
            return ReturnAchievements;
        }

        private List<Achievements> GetSteamEmu(string DirAchivements)
        {
            List<Achievements> ReturnAchievements = new List<Achievements>();

            if (File.Exists(DirAchivements + $"\\stats.ini"))
            {
                bool IsGoodSection = false;
                string line;

                string Name = string.Empty;
                DateTime? DateUnlocked = null;

                StreamReader file = new StreamReader(DirAchivements + $"\\stats.ini");
                while ((line = file.ReadLine()) != null)
                {
                    if (IsGoodSection)
                    {
                        // End list achievements unlocked
                        if (line.IndexOf("[Achievements]") > -1)
                        {
                            IsGoodSection = false;
                        }
                        else
                        {
                            var data = line.Split('=');

                            DateUnlocked = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(int.Parse(data[1]));
                            Name = data[0];

                            ReturnAchievements.Add(new Achievements
                            {
                                ApiName = Name,
                                Name = string.Empty,
                                Description = string.Empty,
                                UrlUnlocked = string.Empty,
                                UrlLocked = string.Empty,
                                DateUnlocked = DateUnlocked
                            });
                        }
                    }

                    // Start list achievements unlocked
                    if (line.IndexOf("[AchievementsUnlockTimes]") > -1)
                    {
                        IsGoodSection = true;
                    }
                }
                file.Close();
            }

            return ReturnAchievements;
        }
        #endregion
    }
}
