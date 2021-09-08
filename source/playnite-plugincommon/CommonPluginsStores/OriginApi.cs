using CommonPluginsPlaynite;
using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsStores
{
    public class OriginApi
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly string urlOriginListApp = @"https://api3.origin.com/supercat/FR/fr_FR/supercat-PCWIN_MAC-FR-fr_FR.json.gz";
        private readonly List<GameStoreDataResponseAppsList> OriginListApp = new List<GameStoreDataResponseAppsList>();


        public OriginApi(string PluginUserDataPath)
        {
            // Class variable
            string PluginCachePath = PlaynitePaths.DataCachePath;
            string PluginCacheFile = PluginCachePath + "\\OriginListApp.json";

            // Load Origin list app
            try
            {
                if (!Directory.Exists(PluginCachePath))
                {
                    Directory.CreateDirectory(PluginCachePath);
                }

                // From cache if exists & not expired
                if (File.Exists(PluginCacheFile) && File.GetLastWriteTime(PluginCacheFile).AddDays(3) > DateTime.Now)
                {
                    Common.LogDebug(true, "GetOriginAppListFromCache");
                    OriginListApp = Serialization.FromJsonFile<List<GameStoreDataResponseAppsList>>(PluginCacheFile);
                }
                // From web
                else
                {
                    Common.LogDebug(true, "GetOriginAppListFromWeb");
                    OriginListApp = GetOriginAppListFromWeb(PluginCacheFile);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        private List<GameStoreDataResponseAppsList> GetOriginAppListFromWeb(string PluginCacheFile)
        {
            string responseData = string.Empty;
            try
            {
                string result = Web.DownloadStringDataWithGz(urlOriginListApp).GetAwaiter().GetResult();
                dynamic resultObject = Serialization.FromJson<dynamic>(result);
                responseData = Serialization.ToJson(resultObject["offers"]);

                // Write file for cache usage
                File.WriteAllText(PluginCacheFile, Serialization.ToJson(resultObject["offers"]), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to load from {urlOriginListApp}");
            }

            return Serialization.FromJson<List<GameStoreDataResponseAppsList>>(responseData);
        }

        public string GetOriginId(string Name)
        {
            GameStoreDataResponseAppsList findGame = OriginListApp.Find(x => x.masterTitle.ToLower() == Name.ToLower());

            Common.LogDebug(true, $"Find Origin data for {Name} - {Serialization.ToJson(findGame)}");

            if (findGame != null)
            {
                return findGame.offerId ?? string.Empty;
            }

            return string.Empty;
        }
    }


    public class GameStoreDataResponseAppsList
    {
        public string offerId;
        public string offerType;
        public string masterTitleId;
        public string publisherFacetKey;
        public string developerFacetKey;
        public string genreFacetKey;
        public string imageServer;
        public string itemName;
        public string itemType;
        public string itemId;
        public string offerPath;
        public string masterTitle;
    }
}
