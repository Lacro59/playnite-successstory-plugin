using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Playnite.SDK.Data;
using CommonPluginsShared;
using SuccessStory.Services;
using CommonPluginsShared.Converters;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;
using System.Globalization;
using CommonPluginsControls.Controls;
using System.Windows.Media.Effects;

namespace SuccessStory.Models
{
    public class Achievements : ObservableObject
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;

        private string name;
        public string Name { get => name; set => name = value?.Trim(); }
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

        [DontSerialize]
        public string ImageCategoryIcon
        {
            get
            {
                string ImagePath = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", CategoryIcon);
                return File.Exists(ImagePath) ? ImagePath : ImageSourceManagerPlugin.GetImagePath(CategoryIcon);
            }
        }

        public int CategoryOrder { get; set; }
        public string CategoryIcon { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ParentCategory { get; set; } = string.Empty;

        public string CategoryRpcs3 { get; set; } = string.Empty;


        /// <summary>
        /// Image for unlocked achievement
        /// </summary>
        [DontSerialize]
        public string ImageUnlocked
        {
            get
            {
                string TempUrlUnlocked = UrlUnlocked;
                if (TempUrlUnlocked?.Contains("rpcs3", StringComparison.InvariantCultureIgnoreCase) ?? false)
                {
                    TempUrlUnlocked = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, UrlUnlocked);
                    return TempUrlUnlocked;
                }
                if (TempUrlUnlocked?.Contains("hidden_trophy", StringComparison.InvariantCultureIgnoreCase) ?? false)
                {
                    TempUrlUnlocked = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", UrlUnlocked);
                    return TempUrlUnlocked;
                }
                if (TempUrlUnlocked?.Contains("GenshinImpact", StringComparison.InvariantCultureIgnoreCase) ?? false)
                {
                    TempUrlUnlocked = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", UrlUnlocked);
                    return TempUrlUnlocked;
                }
                if (TempUrlUnlocked?.Contains("default_icon", StringComparison.InvariantCultureIgnoreCase) ?? false)
                {
                    TempUrlUnlocked = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", UrlUnlocked);
                    return TempUrlUnlocked;
                }
                if ((TempUrlUnlocked?.Contains("steamcdn-a.akamaihd.net", StringComparison.InvariantCultureIgnoreCase) ?? false) && TempUrlUnlocked.Length < 75)
                {
                    TempUrlUnlocked = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "default_icon.png");
                    return TempUrlUnlocked;
                }

                return ImageSourceManagerPlugin.GetImagePath(UrlUnlocked, 256);
            }
        }

        /// <summary>
        /// Image for locked achievement
        /// </summary>
        [DontSerialize]
        public string ImageLocked => UrlLocked != null && UrlLocked.Contains("steamcdn-a.akamaihd.net") && UrlLocked.Length < 75
                    ? ImageUnlocked
                    : !UrlLocked.IsNullOrEmpty() && UrlLocked != UrlUnlocked ? ImageSourceManagerPlugin.GetImagePath(UrlLocked, 256) : ImageUnlocked;


        [DontSerialize]
        public bool ImageUnlockedIsCached => HttpFileCachePlugin.FileWebIsCached(UrlUnlocked);
        [DontSerialize]
        public bool ImageLockedIsCached => HttpFileCachePlugin.FileWebIsCached(UrlLocked);


        /// <summary>
        /// Get the icon according to the achievement state
        /// </summary>
        [DontSerialize]
        public string Icon => IsUnlock ? ImageUnlocked : ImageLocked;

        /// <summary>
        /// Indicates if there is no locked icon
        /// </summary>
        [DontSerialize]
        //public bool IsGray => IsUnlock ? false : (UrlLocked.IsNullOrEmpty() || UrlLocked == UrlUnlocked);
        public bool IsGray
        {
            get
            {
                try
                {
                    return IsUnlock
                        ? false
                        : UrlLocked != null && UrlLocked.Contains("steamcdn-a.akamaihd.net") && UrlLocked.Length < 75
                            ? true
                            : UrlLocked.IsNullOrEmpty() || !UrlUnlocked.IsNullOrEmpty() || UrlLocked == UrlUnlocked;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                    return false;
                }
            }
        }

        [DontSerialize]
        public bool EnableRaretyIndicator => PluginDatabase.PluginSettings.Settings.EnableRaretyIndicator;

        [DontSerialize]
        public bool DisplayRaretyValue => NoRarety
                    ? false
                    : !PluginDatabase.PluginSettings.Settings.EnableRaretyIndicator
                    ? PluginDatabase.PluginSettings.Settings.EnableRaretyIndicator
                    : PluginDatabase.PluginSettings.Settings.DisplayRarityValue;

        public bool NoRarety { get; set; } = false;

        [DontSerialize]
        public string NameWithDateUnlock
        {
            get
            {
                string NameWithDateUnlock = Name;
                if (DateUnlocked != null && DateUnlocked != default(DateTime) && DateUnlocked != new DateTime(1982, 12, 15, 0, 0, 0))
                {
                    LocalDateTimeConverter converter = new LocalDateTimeConverter();
                    NameWithDateUnlock += " (" + converter.Convert(DateUnlocked, null, null, CultureInfo.CurrentCulture) + ")";
                }
                return NameWithDateUnlock;
            }
        }

        [DontSerialize]
        public object AchToolTipCompactList
        {
            get
            {
                StackPanel stackPanel = new StackPanel();

                TextBlockTrimmed textBlockTrimmed = new TextBlockTrimmed
                {
                    Text = NameWithDateUnlock,
                    FontWeight = FontWeights.Bold
                };
                if (!IsUnlock && IsHidden && !PluginDatabase.PluginSettings.Settings.ShowHiddenTitle)
                {
                    textBlockTrimmed.Effect = new BlurEffect
                    {
                        Radius = 4,
                        KernelType = KernelType.Box
                    };
                }
                _ = stackPanel.Children.Add(textBlockTrimmed);

                if (PluginDatabase.PluginSettings.Settings.IntegrationCompactShowDescription)
                {
                    TextBlock textBlock = new TextBlock
                    {
                        Text = Description
                    };
                    if (!IsUnlock && IsHidden && !PluginDatabase.PluginSettings.Settings.ShowHiddenDescription)
                    {
                        textBlock.Effect = new BlurEffect
                        {
                            Radius = 4,
                            KernelType = KernelType.Box
                        };
                    }
                    _ = stackPanel.Children.Add(textBlock);
                }

                return stackPanel;
            }
        }

        [DontSerialize]
        public object AchToolTipCompactPartial
        {
            get
            {
                StackPanel stackPanel = new StackPanel();

                TextBlockTrimmed textBlockTrimmed = new TextBlockTrimmed
                {
                    Text = NameWithDateUnlock,
                    FontWeight = FontWeights.Bold
                };
                if (!IsUnlock && IsHidden && !PluginDatabase.PluginSettings.Settings.ShowHiddenTitle)
                {
                    textBlockTrimmed.Effect = new BlurEffect
                    {
                        Radius = 4,
                        KernelType = KernelType.Box
                    };
                }
                _ = stackPanel.Children.Add(textBlockTrimmed);

                if (PluginDatabase.PluginSettings.Settings.IntegrationCompactPartialShowDescription)
                {
                    TextBlock textBlock = new TextBlock
                    {
                        Text = Description
                    };
                    if (!IsUnlock && IsHidden && !PluginDatabase.PluginSettings.Settings.ShowHiddenDescription)
                    {
                        textBlock.Effect = new BlurEffect
                        {
                            Radius = 4,
                            KernelType = KernelType.Box
                        };
                    }
                    _ = stackPanel.Children.Add(textBlock);
                }

                return stackPanel;
            }
        }

        [DontSerialize]
        public bool IsUnlock => !(DateUnlocked == default(DateTime) || DateUnlocked == null);

        private bool isVisible = true;
        [DontSerialize]
        public bool IsVisible { get => isVisible; set => SetValue(ref isVisible, value); }

        [DontSerialize]
        public DateTime? DateWhenUnlocked
        {
            get => DateUnlocked == default(DateTime) || DateUnlocked == new DateTime(1982, 12, 15, 0, 0, 0, 0) ? null : DateUnlocked;
            set => DateUnlocked = value == null ? (DateTime?)default(DateTime) : value;
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

                LocalDateTimeConverter converter = new LocalDateTimeConverter();
                return (string)converter.Convert(DateUnlocked, null, null, CultureInfo.CurrentCulture);
            }
        }


        public AchProgression Progression { get; set; }


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
        public string Progression => Value + " / " + Max;
    }
}
