using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Playnite.SDK.Data;
using CommonPluginsShared;
using SuccessStory.Services;
using CommonPluginsShared.Converters;
using System.Net;
using CommonPluginsPlaynite.Common;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;

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
        /// Rarety indicator
        /// </summary>
        public float Percent { get; set; } = 100;

        public string Category { get; set; } = string.Empty;
        public string ParentCategory { get; set; } = string.Empty;

        [DontSerialize]
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
        [DontSerialize]
        public string ImageUnlocked
        {
            get
            {
                string TempUrlUnlocked = UrlUnlocked;
                if (TempUrlUnlocked?.IndexOf("rpcs3") > -1)
                {
                    TempUrlUnlocked = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, UrlUnlocked); ;
                }
                if (TempUrlUnlocked?.IndexOf("hidden_trophy") > -1)
                {
                    TempUrlUnlocked = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", UrlUnlocked);
                }

                string pathImageUnlocked = PlayniteTools.GetCacheFile(CacheUnlocked, "SuccessStory");
                if (pathImageUnlocked.IsNullOrEmpty() && !File.Exists(pathImageUnlocked))
                {
                    pathImageUnlocked = TempUrlUnlocked;
                }
                return pathImageUnlocked;
            }
        }

        [DontSerialize]
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
        [DontSerialize]
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
                    return ImageUnlocked;
                }
            }
        }


        /// <summary>
        /// Get the icon according to the achievement state
        /// </summary>
        [DontSerialize]
        public string Icon {
            get
            {
                if (DateUnlocked == default(DateTime) || DateUnlocked == null)
                {
                    return ImageLocked;
                }
                else
                {
                    return ImageUnlocked;
                }
            }
        }

        /// <summary>
        /// Indicates if there is no locked icon
        /// </summary>
        [DontSerialize]
        public bool IsGray
        {
            get
            {
                if (IsUnlock)
                {
                    return false;
                }

                return (UrlLocked.IsNullOrEmpty() || UrlLocked == UrlUnlocked);
            }
        }

        [DontSerialize]
        public bool EnableRaretyIndicator
        {
            get
            {
                return PluginDatabase.PluginSettings.Settings.EnableRaretyIndicator;
            }
        }

        [DontSerialize]
        public string NameWithDateUnlock
        {
            get
            {
                string NameWithDateUnlock = Name;

                if (DateUnlocked != null && DateUnlocked != default(DateTime) && DateUnlocked != new DateTime(1982, 12, 15, 0, 0, 0))
                {
                    var converter = new LocalDateTimeConverter();
                    NameWithDateUnlock += " (" + converter.Convert(DateUnlocked, null, null, null) + ")";
                }

                return NameWithDateUnlock;
            }
        }

        [DontSerialize]
        public TextBlock AchToolTipCompactList
        {
            get
            {
                TextBlock tooltip = new TextBlock();
                tooltip.Inlines.Add(new Run(NameWithDateUnlock)
                {
                    FontWeight = FontWeights.Bold
                });
                if (PluginDatabase.PluginSettings.Settings.IntegrationCompactShowDescription)
                {
                    tooltip.Inlines.Add(new LineBreak());
                    tooltip.Inlines.Add(new Run(Description));
                }

                return tooltip;
            }
        }

        [DontSerialize]
        public bool IsUnlock
        {
            get
            {
                return !(DateUnlocked == default(DateTime) || DateUnlocked == null);
            }
        }

        [DontSerialize]
        public DateTime? DateWhenUnlocked
        {
            get
            {
                if (DateUnlocked == default(DateTime) || DateUnlocked == new DateTime(1982, 12, 15, 0, 0, 0, 0))
                {
                    return null;
                }

                return DateUnlocked;
            }
            set
            {
                if (value == null)
                {
                    DateUnlocked = default(DateTime);
                }
                else
                {
                    DateUnlocked = value;
                }
            }
        }

        [DontSerialize]
        public string DateWhenUnlockedString
        {
            get
            {
                if (DateUnlocked == default(DateTime) || DateUnlocked == new DateTime(1982, 12, 15, 0, 0, 0, 0))
                {
                    return string.Empty;
                }

                var converter = new LocalDateTimeConverter();
                return (string)converter.Convert(DateUnlocked, null, null, null);
            }
        }


        private string GetNameFromUrl(string url)
        {
            string NameFromUrl = string.Empty;
            List<string> urlSplited = url.Split('/').ToList();

            if (url.IndexOf(".playstation.") > -1)
            {
                NameFromUrl = "playstation_" + Name.Replace(" ", "") + "_" + url.Substring(url.Length - 4).Replace(".png", string.Empty);
            }

            if (url.IndexOf(".xboxlive.com") > -1)
            {
                NameFromUrl = "xbox_" + Name.Replace(" ", "") + "_" + url.Substring(url.Length - 5);
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
            
            if (url.IndexOf("exophase") > -1)
            {
                NameFromUrl = "exophase_" + Name.Replace(" ", "");
            }
            
            if (url.IndexOf("overwatch") > -1)
            {
                NameFromUrl = "overwatch_" + Name.Replace(" ", "");
            }
            
            if (url.IndexOf("starcraft2") > -1)
            {
                NameFromUrl = "starcraft2_" + Name.Replace(" ", "");
            }

            if (!url.Contains("http"))
            {
                return url;
            }

            return NameFromUrl;
        }
    }
}
