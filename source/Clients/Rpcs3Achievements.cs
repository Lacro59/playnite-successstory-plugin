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
using System.Threading;

namespace SuccessStory.Clients
{
    public class Rpcs3Achievements : GenericAchievements
    {
        public Rpcs3Achievements() : base("RPCS3")
        {

        }

        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> allAchievements = new List<Achievement>();


            if (IsConfigured())
            {
                List<string> trophyDirectories = FindTrophyGameFolder(game);
                string trophyFile = "TROPUSR.DAT";
                string trophyFileDetails = "TROPCONF.SFM";


                // Directory control
                if (trophyDirectories.Count == 0)
                {
                    Logger.Warn($"No Trophy directoy found for {game.Name}");
                    return gameAchievements;
                }

                foreach (string trophyDirectory in trophyDirectories)
                {
                    allAchievements = new List<Achievement>();

                    string trophyFilePath = Paths.FixPathLength(Path.Combine(trophyDirectory, trophyFile));
                    string trophyFileDetailsPath = Paths.FixPathLength(Path.Combine(trophyDirectory, trophyFileDetails));

                    if (!File.Exists(trophyFilePath))
                    {
                        Logger.Warn($"File {trophyFile} not found for {game.Name} in {trophyFilePath}");
                        continue;
                    }
                    if (!File.Exists(trophyFileDetailsPath))
                    {
                        Logger.Warn($"File {trophyFileDetails} not found for {game.Name} in {trophyFileDetailsPath}");
                        continue;
                    }


                    int trophyCount = 0;
                    List<string> trophyHexData = new List<string>();


                    // Trophies details
                    XDocument trophyDetailsXml = XDocument.Load(trophyFileDetailsPath);

                    string gameName = trophyDetailsXml.Descendants("title-name").FirstOrDefault().Value.Trim();

                    // eFMann - Get all trophies, including those in groups
                    Dictionary<string, string> groupsDict = trophyDetailsXml.Descendants("group")
                        .ToDictionary(
                            g => g.Attribute("id")?.Value,
                            g => g.Element("name")?.Value
                        );

                    if (trophyDetailsXml.Descendants("trophy").Count() <= 0)
                    {
                        Logger.Warn($"No trophy found found for {game.Name} in {trophyFileDetailsPath}");
                    }

                    foreach (XElement trophyXml in trophyDetailsXml.Descendants("trophy"))
                    {
                        _ = int.TryParse(trophyXml.Attribute("id").Value, out int TrophyDetailsId);
                        string trophyType = trophyXml.Attribute("ttype").Value;
                        string name = trophyXml.Element("name").Value;
                        string description = trophyXml.Element("detail").Value;

                        // Get group name from cache instead of querying XML
                        string groupId = trophyXml.Attribute("gid")?.Value;
                        string groupName = groupId != null && groupsDict.ContainsKey(groupId)
                            ? groupsDict[groupId]
                            : null;

                        int percent = 100;
                        float gamerScore = 15;
                        if (trophyType.IsEqual("S"))
                        {
                            percent = (int)PluginDatabase.PluginSettings.Settings.RarityUncommon;
                            gamerScore = 30;
                        }
                        if (trophyType.IsEqual("G"))
                        {
                            percent = (int)PluginDatabase.PluginSettings.Settings.RarityRare;
                            gamerScore = 90;
                        }
                        if (trophyType.IsEqual("P"))
                        {
                            percent = (int)PluginDatabase.PluginSettings.Settings.RarityUltraRare;
                            gamerScore = 180;
                        }

                        allAchievements.Add(new Achievement
                        {
                            ApiName = string.Empty,
                            Name = name,
                            Description = description,
                            UrlUnlocked = CopyTrophyFile(trophyDirectory, "TROP" + TrophyDetailsId.ToString("000") + ".png"),
                            UrlLocked = string.Empty,
                            DateUnlocked = default(DateTime),
                            Percent = percent,
                            GamerScore = gamerScore,

                            CategoryRpcs3 = trophyDirectories.Count > 1 ? gameName : null
                        });
                    }

                    trophyCount = allAchievements.Count;

                    // Trophies data
                    byte[] trophyByte = File.ReadAllBytes(trophyFilePath);
                    string hex = Tools.ToHex(trophyByte);
                    List<string> splitHex = hex.Split(new[] { "0000000400000050000000", "0000000600000060000000" }, StringSplitOptions.None).ToList();
                    trophyHexData = splitHex.Count >= trophyCount
                        ? splitHex.GetRange(splitHex.Count - trophyCount, trophyCount)
                        : new List<string>();

                    foreach (string hexData in trophyHexData)
                    {

                        if (hexData.Length < 58)
                        {
                            continue;
                        }

                        string stringHexId = hexData.Substring(0, 2);
                        int id = (int)long.Parse(stringHexId, NumberStyles.HexNumber);

                        if (id >= allAchievements.Count)
                        {
                            continue;
                        }

                        string unlocked = hexData.Substring(18, 8);
                        bool isUnlocked = unlocked == "00000001";

                        if (isUnlocked)
                        {
                            try
                            {
                                string dtHex = hexData.Substring(44, 14);
                                DateTime dt = new DateTime(long.Parse(dtHex, NumberStyles.AllowHexSpecifier) * 10L);
                                allAchievements[id].DateUnlocked = dt;

                                if (dt == DateTime.MinValue)
                                {
                                    dt = new DateTime(2000, 0, 0, 0, 0, 0);
                                }
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, false, PluginDatabase.PluginName);
                                allAchievements[id].DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                            }
                        }
                    }

                    allAchievements.ForEach(x => gameAchievements.Items.Add(x));
                }
            }
            else
            {
                ShowNotificationPluginNoConfiguration();
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
            if (PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolder.IsNullOrEmpty())
            {
                Logger.Warn("No RPCS3 configured folder");
                return false;
            }

            string trophyPath = Path.Combine(PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolder, "trophy");
            trophyPath = Paths.FixPathLength(trophyPath);
            if (!Directory.Exists(trophyPath))
            {
                Logger.Warn($"No RPCS3 trophy folder in {PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolder}");
                return false;
            }

            return true;
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableRpcs3Achievements;
        }
        #endregion


        #region RPCS3
        private List<string> FindTrophyGameFolder(Game game)
        {
            List<string> trophyGameFolder = new List<string>();

            List<string> foldersPath = new List<string> { PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolder };
            PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolders?.ForEach(x => foldersPath.Add(x.FolderPath));

            string path = API.Instance.ExpandGameVariables(game, Path.Combine(game.InstallDirectory, ".."), GetGameEmulator(game)?.InstallDir);
            List<string> file = Tools.FindFile(path, "TROPHY.TRP", true);

            if (file.Count() == 0)
            {
                Logger.Warn($"TROPHY.TRP not found for {game.Name}");
                return new List<string>();
            }
            else if (file.Count() > 1)
            {
                Logger.Warn($"TROPHY.TRP is multiple for {game.Name}");
                file.ForEach(x =>
                {
                    Logger.Warn(x);
                });
            }

            string fileTrophyTRP = file.First();
            string fileData = FileSystem.ReadFileAsStringSafe(fileTrophyTRP);
            Match match = Regex.Match(fileData, @"<npcommid>(.*?)<\/npcommid>", RegexOptions.IgnoreCase);
            string npcommid = match.Success ? match.Groups[1].Value : null;
            if (npcommid.IsNullOrEmpty())
            {
                Logger.Warn($"npcommid not found for {game.Name} in {fileTrophyTRP}");
            }

            foldersPath.ForEach(x =>
            {
                List<string> folders = new List<string>();
                string trophyPath = x + "\\trophy\\" + npcommid;
                if (Directory.Exists(trophyPath))
                {
                    folders.Add(trophyPath);
                    Logger.Warn($"Found TrophyPath: {trophyPath}");
                }

                if (folders.Count() == 0)
                {
                    Logger.Warn($"Trophy folder not found: {x}");
                }
                else if (folders.Count() > 1)
                {
                    Logger.Warn($"Trophy folder is multiple for {game.Name}");
                    folders.ForEach(y =>
                    {
                        Logger.Warn(y);
                    });
                }
                else
                {
                    trophyGameFolder.AddRange(folders.Where(y => !trophyGameFolder.Contains(y))); // Avoid duplicates
                }
            });

            return trophyGameFolder;
        }

        private string CopyTrophyFile(string trophyDirectory, string trophyFile)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(trophyDirectory);
                string nameFolder = di.Name;

                string parentDir = Paths.FixPathLength(Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "rpcs3"));
                string dir = Paths.FixPathLength(Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "rpcs3", nameFolder));

                FileSystem.CreateDirectory(parentDir);
                FileSystem.CreateDirectory(dir);

                string targetPath = Paths.FixPathLength(Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "rpcs3", nameFolder, trophyFile));
                string sourcePath = Paths.FixPathLength(Path.Combine(trophyDirectory, trophyFile));

                // Only copy if target doesn't exist
                if (!File.Exists(targetPath))
                {
                    int maxAttempts = 5;
                    int delayMs = 200;

                    for (int attempt = 0; attempt < maxAttempts; attempt++)
                    {
                        try
                        {
                            using (FileStream sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (FileStream destStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                            {
                                sourceStream.CopyTo(destStream);
                            }
                            break;
                        }
                        catch (IOException)
                        {
                            if (attempt == maxAttempts - 1)
                            {
                                throw;
                            }

                            Thread.Sleep(delayMs);
                        }
                    }
                }

                return Path.Combine("rpcs3", nameFolder, trophyFile);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return string.Empty;
            }
        }
        #endregion
    }
}