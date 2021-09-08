using CommonPlayniteShared.Common;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace CommonPluginsStores
{
    public class UbisoftAPI
    {
        private static readonly ILogger logger = LogManager.GetLogger();


        public UbisoftAPI()
        {
        }


        public string GetInstallationPath()
        {
            var progs = Programs.GetUnistallProgramsList().FirstOrDefault(a => a.DisplayName == "Ubisoft Connect");
            if (progs == null)
            {
                return string.Empty;
            }
            else
            {
                return progs.InstallLocation;
            }
        }

        public string GetScreeshotsPath()
        {
            string ConfigPath = Path.Combine(Environment.GetEnvironmentVariable("AppData"), "..", "Local", "Ubisoft Game Launcher", "settings.yml");
            if (File.Exists(ConfigPath))
            {
                dynamic SettingsData = Serialization.FromYamlFile<dynamic>(ConfigPath);

                return ((string)SettingsData["misc"]["screenshot_root_path"]).Replace('/', Path.DirectorySeparatorChar);
            }

            return string.Empty;
        }
    }
}
