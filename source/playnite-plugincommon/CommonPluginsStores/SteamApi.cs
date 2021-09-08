using CommonPluginsPlaynite;
using CommonPluginsShared;
using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CommonPluginsStores
{
    public class SteamApi
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly string urlSteamListApp = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";
        private readonly dynamic SteamListApp = null;

        private string InstallationPath { get; set; }


        public SteamApi()
        {
            // Class variable
            string PluginCachePath = PlaynitePaths.DataCachePath;
            string PluginCacheFile = PluginCachePath + "\\SteamListApp.json";


            InstallationPath = GetInstallationPath();


            // Load Steam list app
            try
            {
                if (!Directory.Exists(PluginCachePath))
                {
                    Directory.CreateDirectory(PluginCachePath);
                }

                // From cache if exists & not expired
                if (File.Exists(PluginCacheFile) && File.GetLastWriteTime(PluginCacheFile).AddDays(3) > DateTime.Now)
                {
                    Common.LogDebug(true, "GetSteamAppListFromCache");
                    SteamListApp = Serialization.FromJsonFile<dynamic>(PluginCacheFile);
                }
                // From web
                else
                {
                    Common.LogDebug(true, "GetSteamAppListFromWeb");
                    SteamListApp = GetSteamAppListFromWeb(PluginCacheFile);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "Error on load SteamListApp");
            }
        }

        // TODO transform to task and identified object and saved in playnite temp
        private dynamic GetSteamAppListFromWeb(string PluginCacheFile)
        {
            string responseData = string.Empty;
            try
            {
                responseData = Web.DownloadStringData(urlSteamListApp).GetAwaiter().GetResult();
                if (responseData.IsNullOrEmpty() || responseData == "{\"applist\":{\"apps\":[]}}")
                {
                    responseData = "{}";
                }
                else
                {
                    // Write file for cache usage
                    File.WriteAllText(PluginCacheFile, responseData);
                }
            }
            catch(Exception ex)
            {
                Common.LogError(ex, false, $"Failed to load from {urlSteamListApp}");
                responseData = "{\"applist\":{\"apps\":[]}}";
            }

            return Serialization.FromJson<dynamic>(responseData);
        }


        public int GetSteamId(string Name)
        {
            int SteamId = 0;
        
            try
            {
                if (SteamListApp?["applist"]?["apps"] != null)
                {
                    string SteamAppsListString = Serialization.ToJson(SteamListApp["applist"]["apps"]);
                    List<SteamApps> SteamAppsList = Serialization.FromJson<List<SteamApps>>(SteamAppsListString);
                    SteamAppsList.Sort((x, y) => x.AppId.CompareTo(y.AppId));

                    foreach (SteamApps Game in SteamAppsList)
                    {
                        string NameSteam = CommonPluginsShared.PlayniteTools.NormalizeGameName(Game.Name);
                        string NameSearch = CommonPluginsShared.PlayniteTools.NormalizeGameName(Name);

                        if (NameSteam == NameSearch)
                        {
                            return Game.AppId;
                        }
                    }
                }
                else
                {
                    logger.Warn($"No SteamListApp data");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error with {Name}");
            }
        
            if (SteamId == 0)
            {
                logger.Warn($"SteamId not find for {Name}");
            }
        
            return SteamId;
        }

        public string GetGameName(int SteamId)
        {
            string GameName = string.Empty;

            try
            {
                if (SteamListApp?["applist"]?["apps"] != null)
                {
                    string SteamAppsListString = Serialization.ToJson(SteamListApp["applist"]["apps"]);
                    List<SteamApps> SteamAppsList = Serialization.FromJson<List<SteamApps>>(SteamAppsListString);

                    string tempName = SteamAppsList.Find(x => x.AppId == SteamId)?.Name;

                    if (!tempName.IsNullOrEmpty())
                    {
                        GameName = tempName;
                    }
                }
                else
                {
                    logger.Warn($"No SteamListApp data");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return GameName;
        }


        public string GetUserSteamId()
        {
            try
            {
                string PluginSteamConfigFile = Path.Combine(PlaynitePaths.ExtensionsDataPath, "CB91DFC9-B977-43BF-8E70-55F46E410FAB", "config.json");

                if (File.Exists(PluginSteamConfigFile))
                {
                    dynamic SteamConfig = Serialization.FromJsonFile<dynamic>(PluginSteamConfigFile);

                    SteamID steamID = new SteamID();
                    steamID.SetFromUInt64((ulong)SteamConfig["UserId"]);

                    return steamID.AccountID.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return string.Empty;
            }
        }


        public string GetInstallationPath()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            {
                if (key?.GetValueNames().Contains("SteamPath") == true)
                {
                    return key.GetValue("SteamPath")?.ToString().Replace('/', '\\') ?? string.Empty;
                }
            }

            return string.Empty;
        }

        public string GetScreeshotsPath()
        {
            string PathScreeshotsFolder = string.Empty;

            if (!InstallationPath.IsNullOrEmpty())
            {
                string SteamId = GetUserSteamId();

                if (SteamId.IsNullOrEmpty())
                {
                    logger.Warn("No find SteamId");
                    return PathScreeshotsFolder;
                }


                PathScreeshotsFolder = Path.Combine(InstallationPath, "userdata", SteamId, "760", "remote");

                if (Directory.Exists(PathScreeshotsFolder))
                {
                    return PathScreeshotsFolder;
                }
                else
                {
                    logger.Warn("Folder Steam userdata not find");
                }
            }

            logger.Warn("No find Steam installation");
            return PathScreeshotsFolder;
        }
    }


    public class SteamApps
    {
        [SerializationPropertyName("appid")]
        public int AppId { get; set; }
        [SerializationPropertyName("name")]
        public string Name { get; set; }
    }

    public class SteamAchievementData
    {
        [SerializationPropertyName("rawname")]
        public string RawName { get; set; }
        [SerializationPropertyName("hidden")]
        public bool Hidden { get; set; }
        [SerializationPropertyName("closed")]
        public int Closed { get; set; }
        [SerializationPropertyName("unlock_time")]
        public int UnlockTime { get; set; }
        [SerializationPropertyName("icon_closed")]
        public string IconClosed { get; set; }
        [SerializationPropertyName("icon_open")]
        public string IconOpen { get; set; }
        [SerializationPropertyName("progress")]
        public dynamic Progress { get; set; }
        [SerializationPropertyName("name")]
        public string Name { get; set; }
        [SerializationPropertyName("desc")]
        public string Desc { get; set; }
    }
}
