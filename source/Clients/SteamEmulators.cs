using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using CommonPluginsStores;
using CommonPluginsStores.Steam;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Converters;
using SuccessStory.Models;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Playnite.SDK;
using System.Collections.ObjectModel;
using CommonPluginsStores.Models;
using CommonPluginsStores.Steam.Models.SteamKit;

namespace SuccessStory.Clients
{
    public class SteamEmulators : GenericAchievements
    {
        private SteamApi SteamApi => SuccessStory.SteamApi;

        private List<string> AchievementsDirectories { get; set; } = new List<string>();
        private uint AppId { get; set; } = 0;

        private string Hyphenate(string str, int pos)
        {
            return string.Join("-", Regex.Split(str, @"(?<=\G.{" + pos + "})(?!$)"));
        }

        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }


        public SteamEmulators(List<Folder> LocalFolders) : base("SteamEmulators")
        {
            AchievementsDirectories.Add("%PUBLIC%\\Documents\\Steam\\CODEX");
            AchievementsDirectories.Add("%appdata%\\Steam\\CODEX");

            AchievementsDirectories.Add("%DOCUMENTS%\\VALVE");

            AchievementsDirectories.Add("%appdata%\\Goldberg SteamEmu Saves");
            AchievementsDirectories.Add("%appdata%\\SmartSteamEmu");
            AchievementsDirectories.Add("%DOCUMENTS%\\DARKSiDERS");

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


        public uint GetAppId()
        {
            return AppId;
        }


        #region SteamEmulator

        public GameAchievements GetAchievementsLocal(Game game, string apiKey, uint steamId = 0, bool isManual = false)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            GameAchievements gameAchievementsCached = SuccessStory.PluginDatabase.Get(game, true);

            AppId = steamId != 0 ? steamId : SteamApi.GetAppId(game.Name);
            SteamEmulatorData data = Get(game, AppId, apiKey, isManual);

            if (gameAchievementsCached == null)
            {
                gameAchievements.Items = data.Achievements;
                gameAchievements.ItemsStats = data.Stats;
                gameAchievements.SetRaretyIndicator();
                return gameAchievements;
            }
            else
            {
                if (gameAchievementsCached.Items.Count != data.Achievements.Count)
                {
                    gameAchievements.Items = data.Achievements;
                    gameAchievements.ItemsStats = data.Stats;
                    gameAchievements.SetRaretyIndicator();
                    return gameAchievements;
                }

                gameAchievementsCached.Items.ForEach(x =>
                {
                    Achievements finded = data.Achievements.Find(y => x.ApiName == y.ApiName);
                    if (finded != null)
                    {
                        x.Name = finded.Name;
                        if (x.DateUnlocked == null || x.DateUnlocked == default(DateTime))
                        {
                            x.DateUnlocked = finded.DateUnlocked;
                        }
                    }
                });
                gameAchievementsCached.ItemsStats = data.Stats;
                gameAchievementsCached.SetRaretyIndicator();
                return gameAchievementsCached;
            }
        }


        private List<GameStats> ReadStatsINI(string pathFile, List<GameStats> gameStats)
        {
            try
            {
                string line;
                string Name = string.Empty;
                double Value = 0;

                StreamReader file = new StreamReader(pathFile);
                while ((line = file.ReadLine()) != null)
                {
                    // Achievement name
                    if (!line.IsEqual("[Stats]"))
                    {
                        string[] data = line.Split('=');
                        if (data.Count() > 1 && !data[0].IsNullOrEmpty() && !data[0].IsEqual("STACount"))
                        {
                            Name = data[0];
                            try
                            {
                                Value = BitConverter.ToInt32(StringToByteArray(data[1]), 0);
                            }
                            catch
                            {
                                _ = double.TryParse(data[1], out Value);
                            }

                            gameStats.Add(new GameStats
                            {
                                Name = Name,
                                Value = Value
                            });

                            Name = string.Empty;
                            Value = 0;
                        }
                    }
                }
                file.Close();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return gameStats;
        }

        private List<Achievements> ReadAchievementsINI(string pathFile, List<Achievements> ReturnAchievements)
        {
            bool isType2 = false;
            bool isType3 = false;

            try
            {
                string line;
                StreamReader file = new StreamReader(pathFile);
                while ((line = file.ReadLine()) != null)
                {
                    if (line.IsEqual("[Time]"))
                    {
                        isType2 = true;
                        break;
                    }
                    if (line.IsEqual("achieved=true"))
                    {
                        isType3 = true;
                        break;
                    }
                }
                file.Close();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            if (!isType2 && !isType3)
            {
                return ReadAchievementsINI_type1(pathFile, ReturnAchievements);
            }
            else if (isType3)
            {
                return ReadAchievementsINI_type3(pathFile, ReturnAchievements);
            }
            else
            {
                return ReadAchievementsINI_type2(pathFile, ReturnAchievements);
            }
        }

        private List<Achievements> ReadAchievementsINI_type1(string pathFile, List<Achievements> ReturnAchievements)
        {
            try
            {
                string line;

                string Name = string.Empty;
                bool State = false;
                string sTimeUnlock = string.Empty;
                int timeUnlock = 0;
                DateTime? DateUnlocked = null;

                StreamReader file = new StreamReader(pathFile);
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
                            if (line.Contains("Time = "))
                            {
                                sTimeUnlock = line.Replace("Time = ", string.Empty);
                                timeUnlock = BitConverter.ToInt32(StringToByteArray(line.Replace("Time = ", string.Empty)), 0);
                            }
                            if (line.Contains("Time="))
                            {
                                sTimeUnlock = line.Replace("Time=", string.Empty);
                                sTimeUnlock = sTimeUnlock.Substring(0, sTimeUnlock.Length - 2);

                                char[] ca = sTimeUnlock.ToCharArray();
                                StringBuilder sb = new StringBuilder(sTimeUnlock.Length);
                                for (int i = 0; i < sTimeUnlock.Length; i += 2)
                                {
                                    sb.Insert(0, ca, i, 2);
                                }
                                sTimeUnlock = sb.ToString();

                                timeUnlock = int.Parse(sTimeUnlock, System.Globalization.NumberStyles.HexNumber);
                            }
                        }
                        if (line.IndexOf("CurProgress") > -1 && line.ToLower() != "curprogress = 0000000000")
                        {
                            sTimeUnlock = line.Replace("CurProgress = ", string.Empty);
                            timeUnlock = BitConverter.ToInt32(StringToByteArray(line.Replace("CurProgress = ", string.Empty)), 0);
                        }

                        DateUnlocked = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(timeUnlock).ToLocalTime();

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
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return ReturnAchievements;
        }

        private List<Achievements> ReadAchievementsINI_type2(string pathFile, List<Achievements> ReturnAchievements)
        {
            try
            {
                string line;
                bool startAchievement = false;

                string Name = string.Empty;
                string sTimeUnlock = string.Empty;
                int timeUnlock = 0;
                DateTime? DateUnlocked = null;

                StreamReader file = new StreamReader(pathFile);
                while ((line = file.ReadLine()) != null)
                {
                    if (line.IsEqual("[Time]"))
                    {
                        startAchievement = true;
                    }
                    else if (startAchievement)
                    {
                        var data = line.Split('=');
                        Name = data[0];
                        sTimeUnlock = data[1];
                        timeUnlock = BitConverter.ToInt32(StringToByteArray(sTimeUnlock), 0);
                        DateUnlocked = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(timeUnlock).ToLocalTime();

                        if (timeUnlock != 0)
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
                            timeUnlock = 0;
                            DateUnlocked = null;
                        }
                    }
                }
                file.Close();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return ReturnAchievements;
        }

        private List<Achievements> ReadAchievementsINI_type3(string pathFile, List<Achievements> ReturnAchievements)
        {
            try
            {
                string line;

                string Name = string.Empty;
                bool State = false;
                string sTimeUnlock = string.Empty;
                int timeUnlock = 0;
                DateTime? DateUnlocked = null;

                StreamReader file = new StreamReader(pathFile);
                while ((line = file.ReadLine()) != null)
                {
                    // Achievement name
                    if (line.IndexOf("[") > -1)
                    {
                        Name = line.Replace("[", string.Empty).Replace("]", string.Empty).Trim();
                        State = true;
                        timeUnlock = 0;
                        DateUnlocked = null;
                    }

                    // Unlock
                    if (line.IndexOf("timestamp") > -1)
                    {
                        sTimeUnlock = line.Replace("timestamp=", string.Empty);
                        timeUnlock = int.Parse(sTimeUnlock);
                        DateUnlocked = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(timeUnlock).ToLocalTime();
                    }

                    if (line == string.Empty)
                    {
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
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return ReturnAchievements;
        }

        private List<Achievements> ReadAchievementsStatsINI(string pathFile, List<Achievements> ReturnAchievements)
        {
            try
            {
                string line;
                bool startAchievement = false;

                string Name = string.Empty;
                string sTimeUnlock = string.Empty;
                int timeUnlock = 0;
                DateTime? DateUnlocked = null;

                StreamReader file = new StreamReader(pathFile);
                while ((line = file.ReadLine()) != null)
                {
                    if (line.IsEqual("[ACHIEVEMENTS]"))
                    {
                        startAchievement = true;
                    }
                    else if (startAchievement)
                    {
                        if (!line.Trim().IsNullOrEmpty())
                        {
                            string[] data = line.Split('=');
                            Name = data[0].Trim();
                            sTimeUnlock = data.Last().Trim();
                            timeUnlock = int.Parse(sTimeUnlock.Replace("{unlocked = true, time = ", string.Empty).Replace("}", string.Empty));
                            DateUnlocked = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(timeUnlock).ToLocalTime();

                            if (timeUnlock != 0)
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
                                timeUnlock = 0;
                                DateUnlocked = null;
                            }
                        }
                    }
                }
                file.Close();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return ReturnAchievements;
        }


        private SteamEmulatorData Get(Game game, uint appId, string apiKey, bool isManual)
        {
            List<Achievements> ReturnAchievements = new List<Achievements>();
            List<GameStats> ReturnStats = new List<GameStats>();

            try
            {
                #region Get local achievements
                if (!isManual)
                {
                    // Search data local
                    foreach (string DirAchivements in AchievementsDirectories)
                    {
                        switch (DirAchivements.ToLower())
                        {
                            case "%public%\\documents\\steam\\codex":
                            case "%appdata%\\steam\\codex":
                                if (File.Exists(Environment.ExpandEnvironmentVariables(DirAchivements) + $"\\{appId}\\achievements.ini"))
                                {
                                    string line;

                                    string Name = string.Empty;
                                    DateTime? DateUnlocked = null;

                                    StreamReader file = new StreamReader(Environment.ExpandEnvironmentVariables(DirAchivements) + $"\\{appId}\\achievements.ini");
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
                                            DateUnlocked = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(int.Parse(line.Replace("UnlockTime=", string.Empty))).ToLocalTime();
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

                                if (File.Exists(Environment.ExpandEnvironmentVariables(DirAchivements) + $"\\{appId}\\stats.ini"))
                                {
                                    ReturnStats = ReadStatsINI(Environment.ExpandEnvironmentVariables(DirAchivements) + $"\\{appId}\\stats.ini", ReturnStats);
                                }

                                break;

                            case "%documents%\\valve":
                                if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"\\VALVE\\{appId}\\ALI213\\Stats\\Achievements.Bin"))
                                {
                                    string line;
                                    string Name = string.Empty;
                                    bool State = false;
                                    string sTimeUnlock = string.Empty;
                                    int timeUnlock = 0;
                                    DateTime? DateUnlocked = null;

                                    string pathFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"\\VALVE\\{appId}\\ALI213\\Stats\\Achievements.Bin";
                                    StreamReader file = new StreamReader(pathFile);

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
                                            if (line.ToLower() == "haveachieved=1")
                                            {
                                                State = true;
                                            }

                                            // Unlock
                                            if (line.IndexOf("HaveAchievedTime") > -1 && line.ToLower() != "haveachievedtime=0000000000")
                                            {
                                                if (line.Contains("HaveAchievedTime="))
                                                {
                                                    sTimeUnlock = line.Replace("HaveAchievedTime=", string.Empty);
                                                    timeUnlock = Int32.Parse(sTimeUnlock);
                                                }
                                            }

                                            DateUnlocked = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(timeUnlock).ToLocalTime();

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
                                }

                                break;

                            case "%appdata%\\goldberg steamemu saves":
                                if (File.Exists(Environment.ExpandEnvironmentVariables(DirAchivements) + $"\\{appId}\\achievements.json"))
                                {
                                    string Name = string.Empty;
                                    DateTime? DateUnlocked = null;

                                    string jsonText = File.ReadAllText(Environment.ExpandEnvironmentVariables(DirAchivements) + $"\\{appId}\\achievements.json");
                                    foreach (dynamic achievement in Serialization.FromJson<dynamic>(jsonText))
                                    {
                                        Name = achievement.Path;

                                        dynamic elements = achievement.First;
                                        dynamic unlockedTimeToken = elements.SelectToken("earned_time");

                                        if (unlockedTimeToken.Value > 0)
                                        {
                                            DateUnlocked = new DateTime(1970, 1, 1).AddSeconds(unlockedTimeToken.Value);
                                        }

                                        if (Name != string.Empty && DateUnlocked != null)
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
                                }

                                break;

                            case "%appdata%\\smartsteamemu":
                                if (File.Exists(Environment.ExpandEnvironmentVariables(DirAchivements) + $"\\{appId}\\stats.bin"))
                                {
                                    string Name = string.Empty;
                                    int header = 0;
                                    byte[] headerbyte = new byte[4];
                                    byte[] statbyte = new byte[24];
                                    byte[] namebyte = new byte[4];
                                    byte[] datebyte = new byte[4];
                                    Dictionary<string, string> achnames = new Dictionary<string, string>();
                                    List<byte[]> stats = new List<byte[]>();
                                    DateTime? DateUnlocked = null;
                                    int statcount = 0;
                                    Crc32 crc = new Crc32();

                                    byte[] allData = File.ReadAllBytes(Environment.ExpandEnvironmentVariables(DirAchivements) + $"\\{appId}\\stats.bin");
                                    statcount = (allData.Length - 4) / 24;

                                    //logger.Warn($"Count of achievements unlocked is {statcount}.");
                                    Buffer.BlockCopy(allData, 0, headerbyte, 0, 4);
                                    //Array.Reverse(headerbyte);
                                    header = BitConverter.ToInt32(headerbyte, 0);
                                    //logger.Warn($"header was found as {header}");
                                    allData = allData.Skip(4).Take(allData.Length - 4).ToArray();

                                    for (int c = 24, j = 0; j < statcount; j++)
                                    {
                                        //Buffer.BlockCopy(allData, i, statbyte, 0, 24);
                                        stats.Add(allData.Take(c).ToArray());
                                        allData = allData.Skip(c).Take(allData.Length - c).ToArray();
                                    }

                                    if (stats.Count != header)
                                    {
                                        Logger.Error("Invalid File");
                                    }
                                    string language = CodeLang.GetSteamLang(API.Instance.ApplicationSettings.Language);
                                    string site = string.Format(@"https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key={0}&appid={1}&l={2}", apiKey, appId, language);

                                    string Results = string.Empty;
                                    try
                                    {
                                        Results = Web.DownloadStringData(site).GetAwaiter().GetResult();
                                    }
                                    catch (WebException ex)
                                    {
                                        if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                                        {
                                            HttpWebResponse resp = (HttpWebResponse)ex.Response;
                                            switch (resp.StatusCode)
                                            {
                                                case HttpStatusCode.BadRequest: // HTTP 400
                                                    break;
                                                case HttpStatusCode.ServiceUnavailable: // HTTP 503
                                                    break;
                                                default:
                                                    Common.LogError(ex, false, $"Failed to load from {site}", true, PluginDatabase.PluginName);
                                                    break;
                                            }
                                        }
                                    }

                                    if (Results != string.Empty && Results.Length > 50)
                                    {
                                        dynamic resultObj = Serialization.FromJson<dynamic>(Results);
                                        dynamic resultItems = null;
                                        try
                                        {
                                            resultItems = resultObj["game"]?["availableGameStats"]?["achievements"];
                                            for (int i = 0; i < resultItems?.Count; i++)
                                            {
                                                string achname = resultItems[i]["name"];
                                                byte[] bn = Encoding.ASCII.GetBytes(achname);
                                                string hash = string.Empty;
                                                foreach (byte b in crc.ComputeHash(bn))
                                                {
                                                    hash += b.ToString("x2").ToUpper();
                                                }
                                                hash = Hyphenate(hash, 2);
                                                achnames.Add(hash, achname);
                                            }
                                        }
                                        catch
                                        {
                                            Logger.Error($"Error getting achievement names");
                                        }
                                    }

                                    for (int i = 0; i < stats.Count; i++)
                                    {
                                        try
                                        {
                                            Buffer.BlockCopy(stats[i], 0, namebyte, 0, 4);
                                            Array.Reverse(namebyte);
                                            Buffer.BlockCopy(stats[i], 8, datebyte, 0, 4);
                                            Name = BitConverter.ToString(namebyte);

                                            if (achnames.ContainsKey(Name))
                                            {
                                                Name = achnames[Name];
                                                int Date = BitConverter.ToInt32(datebyte, 0);
                                                DateUnlocked = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Date).ToLocalTime();
                                                if (Name != string.Empty && DateUnlocked != null)
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
                                                }
                                                Name = string.Empty;
                                                DateUnlocked = null;
                                            }
                                            else
                                            {
                                                Logger.Warn($"No matches found for crc in stats.bin.");
                                            }
                                        }
                                        catch
                                        {
                                            Logger.Error($"Stats.bin file format incorrect for SSE");
                                        }

                                        Array.Clear(namebyte, 0, namebyte.Length);
                                        Array.Clear(datebyte, 0, datebyte.Length);
                                    }
                                }
                                break;

                            case "%documents%\\skidrow":
                            case "%documents%\\darksiders":
                                string skidrowfile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"\\SKIDROW\\{appId}\\SteamEmu\\UserStats\\achiev.ini";
                                string darksidersfile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"\\DARKSiDERS\\{appId}\\SteamEmu\\UserStats\\achiev.ini";
                                string emu = "";

                                if (File.Exists(skidrowfile))
                                {
                                    emu = skidrowfile;
                                }
                                else if (File.Exists(darksidersfile))
                                {
                                    emu = darksidersfile;
                                }

                                if (!(emu == ""))
                                {
                                    Logger.Warn($"File found at {emu}");
                                    string line;
                                    string Name = string.Empty;
                                    DateTime? DateUnlocked = null;
                                    List<List<string>> achlist = new List<List<string>>();
                                    StreamReader r = new StreamReader(emu);

                                    while ((line = r.ReadLine()) != null)
                                    {
                                        // Achievement Name
                                        if (line.IndexOf("[AchievementsUnlockTimes]") > -1)
                                        {
                                            string nextline = r.ReadLine();
                                            while (nextline.IndexOf("[") == -1)
                                            {
                                                achlist.Add(new List<string>(nextline.Split('=')));
                                                nextline = r.ReadLine();
                                            }

                                            foreach (List<string> l in achlist)
                                            {
                                                Name = l[0];
                                                DateUnlocked = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(int.Parse(l[1])).ToLocalTime();
                                                if (Name != string.Empty && DateUnlocked != null)
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
                                        }
                                    }
                                    r.Close();
                                }

                                break;

                            case "%programdata%\\steam":
                                if (Directory.Exists(Environment.ExpandEnvironmentVariables("%ProgramData%\\Steam")))
                                {
                                    string[] dirsUsers = Directory.GetDirectories(Environment.ExpandEnvironmentVariables("%ProgramData%\\Steam"));
                                    foreach (string dirUser in dirsUsers)
                                    {
                                        if (File.Exists(dirUser + $"\\{appId}\\stats\\achievements.ini"))
                                        {
                                            ReturnAchievements = ReadAchievementsINI(dirUser + $"\\{appId}\\stats\\achievements.ini", ReturnAchievements);
                                        }

                                        if (File.Exists(dirUser + $"\\{appId}\\stats\\stats.ini"))
                                        {
                                            ReturnStats = ReadStatsINI(dirUser + $"\\{appId}\\stats\\stats.ini", ReturnStats);
                                        }
                                    }
                                }

                                break;

                            case "%localappdata%\\skidrow":
                                Logger.Warn($"No treatment for {DirAchivements}");
                                break;

                            default:
                                if (ReturnAchievements.Count == 0)
                                {
                                    Folder finded = PluginDatabase.PluginSettings.Settings.LocalPath.Find(x => x.FolderPath.IsEqual(DirAchivements));
                                    Guid.TryParse(finded?.GameId, out Guid GameId);

                                    if (File.Exists(DirAchivements + "\\user_stats.ini") && GameId != default && GameId == game.Id)
                                    {
                                        ReturnAchievements = ReadAchievementsStatsINI(DirAchivements + "\\user_stats.ini", ReturnAchievements);
                                    }
                                    else
                                    {
                                        if (!DirAchivements.Contains("steamemu", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            if (File.Exists(DirAchivements + $"\\{appId}\\stats\\achievements.ini"))
                                            {
                                                ReturnAchievements = ReadAchievementsINI(DirAchivements + $"\\{appId}\\stats\\achievements.ini", ReturnAchievements);

                                                if (File.Exists(DirAchivements + $"\\{appId}\\stats\\stats.ini"))
                                                {
                                                    ReturnStats = ReadStatsINI(DirAchivements + $"\\{appId}\\stats\\stats.ini", ReturnStats);
                                                }

                                            }
                                            else if (GameId != default && GameId == game.Id && (finded?.HasGame ?? false))
                                            {
                                                if (File.Exists(DirAchivements + $"\\stats\\achievements.ini"))
                                                {
                                                    ReturnAchievements = ReadAchievementsINI(DirAchivements + $"\\stats\\achievements.ini", ReturnAchievements);

                                                    if (File.Exists(DirAchivements + $"\\stats\\stats.ini"))
                                                    {
                                                        ReturnStats = ReadStatsINI(DirAchivements + $"\\stats\\stats.ini", ReturnStats);
                                                    }
                                                }
                                                if (File.Exists(DirAchivements + $"\\achievements.ini"))
                                                {
                                                    ReturnAchievements = ReadAchievementsINI(DirAchivements + $"\\achievements.ini", ReturnAchievements);

                                                    if (File.Exists(DirAchivements + $"\\stats.ini"))
                                                    {
                                                        ReturnStats = ReadStatsINI(DirAchivements + $"\\stats.ini", ReturnStats);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                ReturnAchievements = GetSteamEmu(DirAchivements + $"\\{appId}\\SteamEmu");
                                            }
                                        }
                                        else
                                        {
                                            List<string> DataPath = DirAchivements.Split('\\').ToList();
                                            int index = DataPath.FindIndex(x => x.IsEqual("steamemu"));
                                            string GameName = DataPath[index - 1];

                                            uint TempSteamId = SteamApi.GetAppId(GameName);
                                            if (TempSteamId == appId)
                                            {
                                                ReturnAchievements = GetSteamEmu(DirAchivements);
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    }

                    Common.LogDebug(true, $"{Serialization.ToJson(ReturnAchievements)}");

                    if (ReturnAchievements == new List<Achievements>())
                    {
                        Logger.Warn($"No data for {appId}");
                        return new SteamEmulatorData { Achievements = new List<Achievements>(), Stats = new List<GameStats>() };
                    }
                }
                #endregion

                #region Get achievements
                ObservableCollection<GameAchievement> steamAchievements = SteamApi.GetAchievementsSchema(appId);
                steamAchievements?.ForEach(x =>
                {
                    bool isFind = false;
                    for (int j = 0; j < ReturnAchievements.Count; j++)
                    {
                        if (ReturnAchievements[j].ApiName.IsEqual(x.Id))
                        {
                            Achievements temp = new Achievements
                            {
                                ApiName = x.Id,
                                Name = x.Name,
                                Description = x.Description,
                                UrlUnlocked = x.UrlUnlocked,
                                UrlLocked = x.UrlLocked,
                                DateUnlocked = x.DateUnlocked,
                                GamerScore = x.GamerScore
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
                            ApiName = x.Id,
                            Name = x.Name,
                            Description = x.Description,
                            UrlUnlocked = x.UrlUnlocked,
                            UrlLocked = x.UrlLocked,
                            DateUnlocked = default
                        });
                    }
                });
                #endregion

                #region Get stats
                if (ReturnStats.Count > 0 && !apiKey.IsNullOrEmpty())
                {
                    SteamSchema steamSchema = SteamKit.GetSchemaForGame(apiKey, appId, CodeLang.GetSteamLang(API.Instance.ApplicationSettings.Language));
                    steamSchema?.Stats?.ForEach(x =>
                    {
                        bool isFind = false;
                        for (int j = 0; j < ReturnStats.Count; j++)
                        {
                            if (ReturnStats[j].Name.IsEqual(x.Name))
                            {
                                GameStats temp = new GameStats
                                {
                                    Name = x.Name,
                                    Value = ReturnStats[j].Value
                                };

                                isFind = true;
                                ReturnStats[j] = temp;
                                j = ReturnStats.Count;
                            }
                        }

                        if (!isFind)
                        {
                            ReturnStats.Add(new GameStats
                            {
                                Name = x.Name,
                                Value = x.DefaultValue
                            });
                        }
                    });
                }
                #endregion

                // Delete empty (SteamEmu)
                ReturnAchievements = ReturnAchievements.Select(x => x).Where(x => !string.IsNullOrEmpty(x.UrlLocked)).ToList();

                return new SteamEmulatorData { Achievements = ReturnAchievements, Stats = ReturnStats };
            }
            catch (Exception ex)
            {
                Common.LogError(ex, true, $"Failed to parse");
                return new SteamEmulatorData { Achievements = new List<Achievements>(), Stats = new List<GameStats>() };
            }
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
                            string[] data = line.Split('=');

                            DateUnlocked = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(int.Parse(data[1])).ToLocalTime();
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


    public class SteamEmulatorData
    {
        public List<Achievements> Achievements { get; set; }
        public List<GameStats> Stats { get; set; }
    }
}
