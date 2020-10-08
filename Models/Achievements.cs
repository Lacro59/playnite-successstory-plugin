using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using PluginCommon;

namespace SuccessStory.Models
{
    public class Achievements
    {
        public string Name { get; set; }
        public string ApiName { get; set; } = string.Empty;
        public string Description { get; set; }
        public string UrlUnlocked { get; set; }
        public string UrlLocked { get; set; }
        public DateTime? DateUnlocked { get; set; }
        public bool IsHidden { get; set; } = false;
        public float Percent { get; set; } = 100;

        [JsonIgnore]
        public string CacheUnlocked {
            get
            {
                string ImageFileName = string.Empty;

                if (!UrlUnlocked.IsNullOrEmpty()) {
                    List<string> urlSplited = UrlUnlocked.Split('/').ToList();
                    ImageFileName = urlSplited[2] + "_" + Name.Replace(" ", "");
                    ImageFileName = string.Concat(ImageFileName.Split(Path.GetInvalidFileNameChars()));
                    ImageFileName += "_Unlocked";
                }

                return ImageFileName;
            }
        }
        [JsonIgnore]
        public string ImageUnlocked
        {
            get
            {
                string pathImageUnlocked = PlayniteTools.GetCacheFile(CacheUnlocked);
                if (pathImageUnlocked.IsNullOrEmpty())
                {
                    pathImageUnlocked = UrlUnlocked;
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
                    List<string> urlSplited = UrlLocked.Split('/').ToList();
                    ImageFileName = urlSplited[2] + "_" + Name.Replace(" ", "");
                    ImageFileName = string.Concat(ImageFileName.Split(Path.GetInvalidFileNameChars()));
                    ImageFileName += "_Locked";
                }

                return ImageFileName;
            }
        }
        [JsonIgnore]
        public string ImageLocked
        {
            get
            {
                if (!UrlLocked.IsNullOrEmpty() && UrlLocked != UrlUnlocked)
                {
                    string pathImageLocked = PlayniteTools.GetCacheFile(CacheLocked);
                    if (pathImageLocked.IsNullOrEmpty())
                    {
                        pathImageLocked = UrlUnlocked;
                    }
                    return pathImageLocked;
                }
                else
                {
                    return ImageUnlocked;
                }
            }
        }
    }
}
