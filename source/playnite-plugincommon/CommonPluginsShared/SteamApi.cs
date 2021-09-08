using CommonPluginsPlaynite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;

namespace CommonPluginsShared
{
    public class SteamApi
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly string urlSteamListApp = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";
        private readonly JObject SteamListApp = new JObject();


        public SteamApi(string PluginUserDataPath)
        {
            // Class variable
            string PluginCachePath = PlaynitePaths.DataCachePath;
            string PluginCacheFile = PluginCachePath + "\\SteamListApp.json";

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
                    SteamListApp = JObject.Parse(File.ReadAllText(PluginCacheFile));
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
        private JObject GetSteamAppListFromWeb(string PluginCacheFile)
        {
            string responseData = string.Empty;
            try
            {
                responseData = Web.DownloadStringData(urlSteamListApp).GetAwaiter().GetResult();
                if (responseData.IsNullOrEmpty() || responseData == "{\"applist\":{\"apps\":[]}}")
                {
                    responseData = JsonConvert.SerializeObject(new JObject());
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

            return JObject.Parse(responseData);
        }

        public int GetSteamId(string Name)
        {
            int SteamId = 0;
        
            try
            {
                if (SteamListApp?["applist"]?["apps"] != null)
                {
                    string SteamAppsListString = JsonConvert.SerializeObject(SteamListApp["applist"]["apps"]);
                    var SteamAppsList = JsonConvert.DeserializeObject<List<SteamApps>>(SteamAppsListString);
                    SteamAppsList.Sort((x, y) => x.AppId.CompareTo(y.AppId));

                    foreach (SteamApps Game in SteamAppsList)
                    {
                        string NameSteam = Common.NormalizeGameName(Game.Name);
                        string NameSearch = Common.NormalizeGameName(Name);

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
    }


    public class SteamApps
    {
        [JsonProperty("appid")]
        public int AppId { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
