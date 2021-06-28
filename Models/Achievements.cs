using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using CommonPluginsShared;
using SuccessStory.Services;
using System.Net;
using CommonPluginsPlaynite.Common;
using System.Text.RegularExpressions;

namespace SuccessStory.Models
{
    public class Achievements : ObservableObject
    {
        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        public string Name { get; set; }
        public string ApiName { get; set; } = string.Empty;
        public string Description { get; set; }
        public string UrlUnlocked { get; set; }
        public string UrlLocked { get; set; }
        public DateTime? DateUnlocked { get; set; }
        public bool IsHidden { get; set; } = false;
        /// <summary>
        /// Rarety
        /// </summary>
        public float Percent { get; set; } = 100;

        [JsonIgnore]
        public string CacheUnlocked {
            get
            {
                string ImageFileName = string.Empty;

                if (!UrlUnlocked.IsNullOrEmpty()) {
                    int maxLenght = (Name.Replace(" ", "").Length >= 10) ? 10 : Name.Replace(" ", "").Length;

                    ImageFileName = GetNameFromUrl(UrlUnlocked);
                    ImageFileName += "_" + Name.Replace(" ", "").Substring(0, maxLenght);
                    ImageFileName = string.Concat(ImageFileName.Split(Path.GetInvalidFileNameChars()));
                    ImageFileName += "_Unlocked";
                }

                return Regex.Replace(WebUtility.HtmlDecode(Paths.GetSafeFilename(ImageFileName)), @"[^\u0020-\u007E]", string.Empty);
            }
        }
        /// <summary>
        /// Image for unlocked achievement
        /// </summary>
        [JsonIgnore]
        public string ImageUnlocked
        {
            get
            {
                string TempUrlUnlocked = UrlUnlocked;
                if (TempUrlUnlocked.IndexOf("rpcs3") > -1)
                {
                    TempUrlUnlocked = Path.Combine(PluginDatabase.PluginUserDataPath, UrlUnlocked); ;
                }

                string pathImageUnlocked = PlayniteTools.GetCacheFile(CacheUnlocked, "SuccessStory");
                if (pathImageUnlocked.IsNullOrEmpty() && !File.Exists(pathImageUnlocked))
                {
                    pathImageUnlocked = TempUrlUnlocked;
                }
                return pathImageUnlocked;
            }
        }

        [JsonIgnore]
        public string CacheLocked
        {
            get
            {
                string ImageFileName = string.Empty;

                if (!UrlLocked.IsNullOrEmpty())
                {
                    int maxLenght = (Name.Replace(" ", "").Length >= 10) ? 10 : Name.Replace(" ", "").Length;

                    ImageFileName = GetNameFromUrl(UrlLocked);
                    ImageFileName += "_" + Name.Replace(" ", "").Substring(0, maxLenght);
                    ImageFileName = string.Concat(ImageFileName.Split(Path.GetInvalidFileNameChars()));
                    ImageFileName += "_Locked";
                }

                return Regex.Replace(WebUtility.HtmlDecode(Paths.GetSafeFilename(ImageFileName)), @"[^\u0020-\u007E]", string.Empty);
            }
        }
        /// <summary>
        /// Image for locked achievement
        /// </summary>
        [JsonIgnore]
        public string ImageLocked
        {
            get
            {
                if (!UrlLocked.IsNullOrEmpty() && UrlLocked != UrlUnlocked)
                {
                    string pathImageLocked = PlayniteTools.GetCacheFile(CacheLocked, "SuccessStory");
                    if (pathImageLocked.IsNullOrEmpty() && !File.Exists(pathImageLocked))
                    {
                        pathImageLocked = UrlLocked;
                    }
                    return pathImageLocked;
                }
                else
                {
                    string pathImageUnlocked = PlayniteTools.GetCacheFile(CacheUnlocked, "SuccessStory");
                    if (pathImageUnlocked.IsNullOrEmpty() && !File.Exists(pathImageUnlocked))
                    {
                        pathImageUnlocked = UrlUnlocked;
                    }
                    return pathImageUnlocked;
                }
            }
        }


        private string GetNameFromUrl(string url)
        {
            string NameFromUrl = string.Empty;
            List<string> urlSplited = url.Split('/').ToList();

            if (url.IndexOf(".xboxlive.com") > -1)
            {
                NameFromUrl = "xbox_" + Name.Replace(" ", "");
            }

            if (url.IndexOf("steamcommunity") > -1)
            {                
                NameFromUrl = "steam_" + ApiName;
                if (urlSplited.Count >= 8)
                {
                    NameFromUrl += "_" + urlSplited[7];
                }
            }

            if (url.IndexOf(".gog.com") > -1)
            {
                NameFromUrl = "gog_" + ApiName;
            }

            if (url.IndexOf(".ea.com") > -1)
            {
                NameFromUrl = "ea_" + Name.Replace(" ", "");
            }
            
            if (url.IndexOf("retroachievements") > -1)
            {
                NameFromUrl = "ra_" + Name.Replace(" ", "");
            }

            if (!url.Contains("http"))
            {
                return url;
            }

            return NameFromUrl;
        }
    }
}
