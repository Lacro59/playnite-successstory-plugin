using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPluginsShared;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Playnite.SDK.Plugins;

namespace SuccessStory.Clients
{
    class Rpcs3Achievements : GenericAchievements
    {
        public Rpcs3Achievements() : base()
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements Result = SuccessStory.PluginDatabase.GetDefault(game);
            Result.Items = new List<Achievements>();
            Result.HaveAchivements = true;

            string TrophyDirectory = FindTrophyGameFolder(game);
            string TrophyFile = "TROPUSR.DAT";
            string TrophyFileDetails = "TROPCONF.SFM";


            // Directory control
            if (TrophyDirectory.IsNullOrEmpty())
            {
                logger.Warn($"No Trophy directoy found for {game.Name}");
                return Result;
            }
            if (!File.Exists(Path.Combine(TrophyDirectory, TrophyFile)))
            {
                logger.Warn($"File {TrophyFile} not found for {game.Name} in {Path.Combine(TrophyDirectory, TrophyFile)}");
                return Result;
            }
            if (!File.Exists(Path.Combine(TrophyDirectory, TrophyFileDetails)))
            {
                logger.Warn($"File {TrophyFileDetails} not found for {game.Name} in {Path.Combine(TrophyDirectory, TrophyFileDetails)}");
                return Result;
            }


            int TrophyCount = 0;
            List<string> TrophyHexData = new List<string>();


            // Trophies details
            XDocument TrophyDetailsXml = XDocument.Load(Path.Combine(TrophyDirectory, TrophyFileDetails));

            foreach (XElement TrophyXml in TrophyDetailsXml.Descendants("trophy"))
            {
                Console.WriteLine(TrophyXml);

                int.TryParse(TrophyXml.Attribute("id").Value, out int TrophyDetailsId);
                string TrophyType = TrophyXml.Attribute("ttype").Value;
                string Name = TrophyXml.Element("name").Value;
                string Description = TrophyXml.Element("detail").Value;

                int Percent = 100;
                if (TrophyType.ToLower() == "s")
                {
                    Percent = 30;
                }
                if (TrophyType.ToLower() == "g")
                {
                    Percent = 10;
                }

                Result.Items.Add(new Achievements
                {
                    ApiName = string.Empty,
                    Name = Name,
                    Description = Description,
                    UrlUnlocked = CopyTrophyFile(TrophyDirectory, "TROP" + TrophyDetailsId.ToString("000") + ".png"),
                    UrlLocked = string.Empty,
                    DateUnlocked = default(DateTime),
                    Percent = Percent
                });
            }


            TrophyCount = Result.Items.Count;


            // Trophies data
            byte[] TrophyByte = File.ReadAllBytes(Path.Combine(TrophyDirectory, TrophyFile));
            string hex = Tools.ToHex(TrophyByte);

            List<string> splitHex = hex.Split(new[] { "0000000600000060000000" }, StringSplitOptions.None).ToList();
            for (int i = (splitHex.Count - 1); i >= (splitHex.Count - TrophyCount); i--)
            {
                //TrophyHexData.Add(splitHex[i].Substring(0, 192));
                TrophyHexData.Add(splitHex[i]);
            }
            TrophyHexData.Reverse();

            foreach (string HexData in TrophyHexData)
            {
                string stringHexId = HexData.Substring(0, 2);
                int Id = (int)Int64.Parse(stringHexId, System.Globalization.NumberStyles.HexNumber);

                string Unlocked = HexData.Substring(18, 8);
                bool IsUnlocked = (Unlocked == "00000001");

                // No unlock time
                if (IsUnlocked)
                {
                    Result.Items[Id].DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                }
            }

            Result.Total = Result.Items.Count;
            Result.Unlocked = Result.Items.FindAll(x => x.DateUnlocked != default(DateTime)).Count();
            Result.Locked = Result.Items.FindAll(x => x.DateUnlocked == default(DateTime)).Count();
            Result.Progression = (Result.Total != 0) ? (int)Math.Ceiling((double)(Result.Unlocked * 100 / Result.Total)) : 0;

            return Result;
        }


        public override bool IsConfigured()
        {
            if (PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolder.IsNullOrEmpty())
            {
                return false;
            }

            if (!Directory.Exists(Path.Combine(PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolder, "trophy")))
            {
                return false;
            }
            
            return true;
        }

        public override bool IsConnected()
        {
            throw new NotImplementedException();
        }


        private string FindTrophyGameFolder(Game game)
        {
            string TrophyGameFolder = string.Empty;
            string TrophyFolder = Path.Combine(PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolder, "trophy");
            string TempTrophyGameFolder = Directory.GetParent(game.InstallDirectory).FullName;

            try
            {
                if (Directory.Exists(Path.Combine(TempTrophyGameFolder, "TROPDIR")))
                {
                    Parallel.ForEach(Directory.EnumerateDirectories(Path.Combine(TempTrophyGameFolder, "TROPDIR")), (objectDirectory, state) =>
                    {
                        DirectoryInfo di = new DirectoryInfo(objectDirectory);
                        string NameFolder = di.Name;

                        if (Directory.Exists(Path.Combine(TrophyFolder, NameFolder)))
                        {
                            TrophyGameFolder = Path.Combine(TrophyFolder, NameFolder);
                            state.Break();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return TrophyGameFolder;
        }


        private string CopyTrophyFile(string TrophyDirectory, string TrophyFile)
        {
            DirectoryInfo di = new DirectoryInfo(TrophyDirectory);
            string NameFolder = di.Name;

            if (!Directory.Exists(Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "rpcs3")))
            {
                Directory.CreateDirectory(Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "rpcs3"));
            }
            if (!Directory.Exists(Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "rpcs3", NameFolder)))
            {
                Directory.CreateDirectory(Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "rpcs3", NameFolder));
            }

            try
            {
                File.Copy(Path.Combine(TrophyDirectory, TrophyFile), Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "rpcs3", NameFolder, TrophyFile));
            }
            catch
            {

            }

            return Path.Combine("rpcs3", NameFolder, TrophyFile);
        }

        public override bool ValidateConfiguration(IPlayniteAPI playniteAPI, Plugin plugin, SuccessStorySettings settings)
        {
            if (!IsConfigured())
            {
                logger.Warn("Bad RPCS3 configuration");
                playniteAPI.Notifications.Add(new NotificationMessage(
                    "SuccessStory-Rpcs3-NoConfig",
                    $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsRpcs3BadConfig")}",
                    NotificationType.Error,
                    () => plugin.OpenSettingsView()
                ));
                return false;
            }
            return true;
        }

        public override bool EnabledInSettings(SuccessStorySettings settings)
        {
            return settings.EnableRpcs3Achievements;
        }
    }
}
