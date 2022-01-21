using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Playnite.SDK.Data;
using CommonPluginsShared;
using SuccessStory.Services;
using CommonPluginsShared.Converters;
using System.Net;
using CommonPlayniteShared.Common;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;
using System.Globalization;

namespace SuccessStory.Models
{
    public class Achievements : ObservableObject
    {
        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        private string _name;
        public string Name { get { return _name; } set { _name = value?.Trim(); } }
        public string ApiName { get; set; } = string.Empty;
        public string Description { get; set; }
        public string UrlUnlocked { get; set; }
        public string UrlLocked { get; set; }
        public DateTime? DateUnlocked { get; set; }
        public bool IsHidden { get; set; } = false;
        /// <summary>
        /// Rarity indicator
        /// </summary>
        public float Percent { get; set; } = 100;

        public string Category { get; set; } = string.Empty;
        public string ParentCategory { get; set; } = string.Empty;

        public string CategoryRpcs3 { get; set; } = string.Empty;

        [DontSerialize]
        public string CacheUnlocked
        {
            get
            {
                string ImageFileName = string.Empty;

                if (!UrlUnlocked.IsNullOrEmpty())
                {
                    int maxLenght = (Name.Replace(" ", "").Length >= 10) ? 10 : Name.Replace(" ", "").Length;

                    ImageFileName = GetNameFromUrl(UrlUnlocked);
                    ImageFileName += "_" + Name.Replace(" ", "").Substring(0, maxLenght);
                    ImageFileName = string.Concat(ImageFileName.Split(Path.GetInvalidFileNameChars()));
                    ImageFileName += "_Unlocked.png";
                }

                return Regex.Replace(WebUtility.HtmlDecode(CommonPlayniteShared.Common.Paths.GetSafePathName(ImageFileName)), @"[^\u0020-\u007E]", string.Empty);
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
                var Options = new
                {
                    CachedFileIfMissing = true,
                    Url = UrlUnlocked
                };

                string TempUrlUnlocked = UrlUnlocked;
                if (TempUrlUnlocked?.IndexOf("rpcs3") > -1)
                {
                    TempUrlUnlocked = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, UrlUnlocked);
                    Options = null;
                }
                if (TempUrlUnlocked?.IndexOf("hidden_trophy") > -1)
                {
                    TempUrlUnlocked = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", UrlUnlocked);
                    Options = null;
                }

                string pathImageUnlocked = PlayniteTools.GetCacheFile(CacheUnlocked, "SuccessStory", Options);
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
                    ImageFileName += "_Locked.png";
                }

                return Regex.Replace(WebUtility.HtmlDecode(CommonPlayniteShared.Common.Paths.GetSafePathName(ImageFileName)), @"[^\u0020-\u007E]", string.Empty);
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
                    var Options = new
                    {
                        CachedFileIfMissing = true,
                        Url = UrlLocked
                    };

                    string pathImageLocked = PlayniteTools.GetCacheFile(CacheLocked, "SuccessStory", Options);
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
        public string Icon
        {
            get
            {
                return IsUnlock ? ImageUnlocked : ImageLocked;
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
                return UrlLocked.IsNullOrEmpty() || UrlLocked == UrlUnlocked;
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
        public bool DisplayRaretyValue
        {
            get
            {
                if (!PluginDatabase.PluginSettings.Settings.EnableRaretyIndicator)
                {
                    return PluginDatabase.PluginSettings.Settings.EnableRaretyIndicator;
                }
                return PluginDatabase.PluginSettings.Settings.DisplayRarityValue;
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
                    NameWithDateUnlock += " (" + converter.Convert(DateUnlocked, null, null, CultureInfo.CurrentCulture) + ")";
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

        private bool isVisible = true;
        [DontSerialize]
        public bool IsVisible
        {
            get => isVisible;
            set
            {
                isVisible = value;
                OnPropertyChanged();
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
                return (string)converter.Convert(DateUnlocked, null, null, CultureInfo.CurrentCulture);
            }
        }


        public AchProgression Progression { get; set; }


        private string GetNameFromUrl(string url)
        {
            string NameFromUrl = string.Empty;
            List<string> urlSplited = url.Split('/').ToList();

            int Length = 5;
            if (url.Length > 10)
            {
                Length = 10;
            }
            if (url.Length > 15)
            {
                Length = 15;
            }

            if (url.IndexOf("epicgames.com") > -1)
            {
                NameFromUrl = "epic_" + Name.Replace(" ", "") + "_" + url.Substring(url.Length - Length).Replace(".png", string.Empty);
            }

            if (url.IndexOf(".playstation.") > -1)
            {
                NameFromUrl = "playstation_" + Name.Replace(" ", "") + "_" + url.Substring(url.Length - Length).Replace(".png", string.Empty);
            }

            if (url.IndexOf(".xboxlive.com") > -1)
            {
                NameFromUrl = "xbox_" + Name.Replace(" ", "") + "_" + url.Substring(url.Length - Length);
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


        [DontSerialize]
        public string IconText => PluginDatabase.PluginSettings.Settings.IconLocked;
        [DontSerialize]
        public string IconCustom
        {
            get
            {
                if (PluginDatabase.PluginSettings.Settings.IconCustomOnlyMissing)
                {
                    if (IsGray)
                    {
                        return PluginDatabase.PluginSettings.Settings.IconCustomLocked;
                    }
                }
                else
                {
                    return PluginDatabase.PluginSettings.Settings.IconCustomLocked;
                }

                return string.Empty;
            }
        }
    }

    public class AchProgression
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double Value { get; set; }

        [DontSerialize]
        public string Progression
        {
            get
            {
                return Value + " / " + Max;
            }
        }
    }
}
