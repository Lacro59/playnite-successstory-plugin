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
            List<Achievements> AllAchievements = new List<Achievements>();


            if (IsConfigured())
            {
                List<string> TrophyDirectories = FindTrophyGameFolder(game);
                string TrophyFile = "TROPUSR.DAT";
                string TrophyFileDetails = "TROPCONF.SFM";


                // Directory control
                if (TrophyDirectories.Count == 0)
                {
                    Logger.Warn($"No Trophy directoy found for {game.Name}");
                    return gameAchievements;
                }

                foreach (string TrophyDirectory in TrophyDirectories)
                {
                    AllAchievements = new List<Achievements>();

                    if (!File.Exists(Path.Combine(TrophyDirectory, TrophyFile)))
                    {
                        Logger.Warn($"File {TrophyFile} not found for {game.Name} in {Path.Combine(TrophyDirectory, TrophyFile)}");
                        continue;
                    }
                    if (!File.Exists(Path.Combine(TrophyDirectory, TrophyFileDetails)))
                    {
                        Logger.Warn($"File {TrophyFileDetails} not found for {game.Name} in {Path.Combine(TrophyDirectory, TrophyFileDetails)}");
                        continue;
                    }


                    int TrophyCount = 0;
                    List<string> TrophyHexData = new List<string>();


                    // Trophies details
                    XDocument TrophyDetailsXml = XDocument.Load(Path.Combine(TrophyDirectory, TrophyFileDetails));

                    string GameName = TrophyDetailsXml.Descendants("title-name").FirstOrDefault().Value.Trim();

                    foreach (XElement TrophyXml in TrophyDetailsXml.Descendants("trophy"))
                    {
                        _ = int.TryParse(TrophyXml.Attribute("id").Value, out int TrophyDetailsId);
                        string TrophyType = TrophyXml.Attribute("ttype").Value;
                        string Name = TrophyXml.Element("name").Value;
                        string Description = TrophyXml.Element("detail").Value;

                        int Percent = 100;
                        float GamerScore = 15;
                        if (TrophyType.IsEqual("S"))
                        {
                            Percent = 30;
                            GamerScore = 30;
                        }
                        if (TrophyType.IsEqual("G"))
                        {
                            Percent = 10;
                            GamerScore = 90;
                        }
                        if (TrophyType.IsEqual("P"))
                        {
                            Percent = 5;
                            GamerScore = 180;
                        }

                        AllAchievements.Add(new Achievements
                        {
                            ApiName = string.Empty,
                            Name = Name,
                            Description = Description,
                            UrlUnlocked = CopyTrophyFile(TrophyDirectory, "TROP" + TrophyDetailsId.ToString("000") + ".png"),
                            UrlLocked = string.Empty,
                            DateUnlocked = default(DateTime),
                            Percent = Percent,
                            GamerScore = GamerScore,

                            CategoryRpcs3 = TrophyDirectories.Count > 1 ? GameName : null
                        });
                    }


                    TrophyCount = AllAchievements.Count;


                    // Trophies data
                    byte[] TrophyByte = File.ReadAllBytes(Path.Combine(TrophyDirectory, TrophyFile));
                    string hex = Tools.ToHex(TrophyByte);

                    List<string> splitHex = hex.Split(new[] { "0000000600000060000000" }, StringSplitOptions.None).ToList();
                    for (int i = splitHex.Count - 1; i >= (splitHex.Count - TrophyCount); i--)
                    {
                        TrophyHexData.Add(splitHex[i]);
                    }
                    TrophyHexData.Reverse();

                    foreach (string HexData in TrophyHexData)
                    {
                        string stringHexId = HexData.Substring(0, 2);
                        int Id = (int)long.Parse(stringHexId, NumberStyles.HexNumber);

                        string Unlocked = HexData.Substring(18, 8);
                        bool IsUnlocked = Unlocked == "00000001";

                        // No unlock time
                        if (IsUnlocked)
                        {
                            try
                            {
                                string dtHex = HexData.Substring(44, 14);
                                DateTime dt = new DateTime(long.Parse(dtHex, NumberStyles.AllowHexSpecifier) * 10L);
                                AllAchievements[Id].DateUnlocked = dt;
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, false, PluginDatabase.PluginName);
                                AllAchievements[Id].DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                            }
                        }
                    }

                    AllAchievements.ForEach(x => gameAchievements.Items.Add(x));
                }
            }
            else
            {
                ShowNotificationPluginNoConfiguration(ResourceProvider.GetString("LOCSuccessStoryNotificationsRpcs3BadConfig"));
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
                    ShowNotificationPluginNoConfiguration(ResourceProvider.GetString("LOCSuccessStoryNotificationsRpcs3BadConfig"));
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
            if (PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolder.IsNullOrEmpty())
            {
                Logger.Warn("No RPCS3 configured folder");
                return false;
            }

            if (!Directory.Exists(Path.Combine(PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolder, "trophy")))
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
            List<string> TrophyGameFolder = new List<string>();

            List<string> FoldersPath = new List<string> { PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolder };
            PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolders?.ForEach(x => FoldersPath.Add(x.FolderPath));

            FoldersPath.ForEach(x =>
            {
                string TrophyFolder = Path.Combine(x, "trophy");
                string GameTrophyFolder = Path.Combine(game.InstallDirectory, "..", "TROPDIR");
                GameTrophyFolder = API.Instance.ExpandGameVariables(game, GameTrophyFolder, PlayniteTools.GetGameEmulator(game)?.InstallDir);

                Common.LogDebug(true, $"TrophyFolder: {TrophyFolder}");
                Common.LogDebug(true, $"GameTrophyFolder: {GameTrophyFolder}");

                try
                {
                    if (Directory.Exists(GameTrophyFolder))
                    {
                        _ = Parallel.ForEach(Directory.EnumerateDirectories(GameTrophyFolder), (objectDirectory, state) =>
                        {
                            DirectoryInfo di = new DirectoryInfo(objectDirectory);
                            string NameFolder = di.Name;

                            if (Directory.Exists(Path.Combine(TrophyFolder, NameFolder)))
                            {
                                TrophyGameFolder.Add(Path.Combine(TrophyFolder, NameFolder));
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });

            return TrophyGameFolder;
        }


        private string CopyTrophyFile(string TrophyDirectory, string TrophyFile)
        {
            DirectoryInfo di = new DirectoryInfo(TrophyDirectory);
            string NameFolder = di.Name;

            FileSystem.CreateDirectory(Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "rpcs3"));
            FileSystem.CreateDirectory(Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "rpcs3", NameFolder));

            try
            {
                if (!File.Exists(Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "rpcs3", NameFolder, TrophyFile)))
                {
                    File.Copy(Path.Combine(TrophyDirectory, TrophyFile), Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "rpcs3", NameFolder, TrophyFile));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return Path.Combine("rpcs3", NameFolder, TrophyFile);
        }
        #endregion
    }
}
