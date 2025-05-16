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
using System.Globalization;
using CommonPluginsControls.Controls;
using System.Windows.Media.Effects;

namespace SuccessStory.Models
{
    /// <summary>
    /// Represents a single achievement, including its metadata, unlock state, images, and display logic.
    /// </summary>
    public class Achievement : ObservableObject
    {
        /// <summary>
        /// Reference to the plugin database instance.
        /// </summary>
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;

        private string _name;
        /// <summary>
        /// Gets or sets the localized name of the achievement.
        /// </summary>
        public string Name { get => _name; set => _name = value?.Trim(); }

        private string _nameEn;
        /// <summary>
        /// Gets or sets the English name of the achievement.
        /// </summary>
        public string NameEn { get => _nameEn; set => _nameEn = value?.Trim(); }

        /// <summary>
        /// Gets or sets the API name or identifier for the achievement.
        /// </summary>
        public string ApiName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the achievement.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the URL for the unlocked achievement image.
        /// </summary>
        public string UrlUnlocked { get; set; }

        /// <summary>
        /// Gets or sets the URL for the locked achievement image.
        /// </summary>
        public string UrlLocked { get; set; }

        // TODO
        private DateTime? _dateUnlocked;
        /// <summary>
        /// Gets or sets the date and time when the achievement was unlocked.
        /// </summary>
        public DateTime? DateUnlocked
        {
            get => _dateUnlocked == default(DateTime) ? null : _dateUnlocked;
            set => _dateUnlocked = value is DateTime dt ? dt.ToUniversalTime() : value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the achievement is hidden.
        /// </summary>
        public bool IsHidden { get; set; } = false;

        /// <summary>
        /// Rarity indicator.
        /// </summary>
        public float Percent { get; set; } = 100;

        /// <summary>
        /// Gets or sets the gamerscore or points value of the achievement.
        /// </summary>
        public float GamerScore { get; set; } = 0;

        /// <summary>
        /// Gets the icon path for the achievement's category.
        /// </summary>
        [DontSerialize]
        public string ImageCategoryIcon
        {
            get
            {
                string imagePath = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", CategoryIcon);
                return File.Exists(imagePath) ? imagePath : ImageSourceManagerPlugin.GetImagePath(CategoryIcon);
            }
        }

        /// <summary>
        /// Gets or sets the display order for the achievement's category.
        /// </summary>
        public int CategoryOrder { get; set; }

        /// <summary>
        /// Gets or sets the icon name for the achievement's category.
        /// </summary>
        public string CategoryIcon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the category name of the achievement.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parent category name of the achievement.
        /// </summary>
        public string ParentCategory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the RPCS3-specific category name.
        /// </summary>
        public string CategoryRpcs3 { get; set; } = string.Empty;

        private static string[] LocalResourceGames => new[]
        {
            "rpcs3", "hidden_trophy", "GenshinImpact", "WutheringWaves", "HonkaiStarRail", "ZenlessZoneZero", "default_icon"
        };

        /// <summary>
        /// Gets the local or remote image path for the unlocked achievement.
        /// </summary>
        [DontSerialize]
        public string ImageUnlocked
        {
            get
            {
                string tempUrlUnlocked = UrlUnlocked;
                if (tempUrlUnlocked?.Contains("rpcs3", StringComparison.InvariantCultureIgnoreCase) ?? false)
                {
                    tempUrlUnlocked = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, UrlUnlocked);
                    return tempUrlUnlocked;
                }
                if (LocalResourceGames.Any(game => tempUrlUnlocked?.Contains(game, StringComparison.InvariantCultureIgnoreCase) == true))
                {
                    tempUrlUnlocked = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", UrlUnlocked);
                    return tempUrlUnlocked;
                }
                if ((tempUrlUnlocked?.Contains("steamcdn-a.akamaihd.net", StringComparison.InvariantCultureIgnoreCase) ?? false) && tempUrlUnlocked.Length < 75)
                {
                    tempUrlUnlocked = Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "default_icon.png");
                    return tempUrlUnlocked;
                }

                return ImageSourceManagerPlugin.GetImagePath(UrlUnlocked, 256);
            }
        }

        /// <summary>
        /// Gets the local or remote image path for the locked achievement.
        /// </summary>
        [DontSerialize]
        public string ImageLocked => UrlLocked != null && UrlLocked.Contains("steamcdn-a.akamaihd.net") && UrlLocked.Length < 75
                    ? ImageUnlocked
                    : !UrlLocked.IsNullOrEmpty() && UrlLocked != UrlUnlocked ? ImageSourceManagerPlugin.GetImagePath(UrlLocked, 256) : ImageUnlocked;

        /// <summary>
        /// Gets a value indicating whether the unlocked image is cached locally.
        /// </summary>
        [DontSerialize]
        public bool ImageUnlockedIsCached => HttpFileCachePlugin.FileWebIsCached(UrlUnlocked);

        /// <summary>
        /// Gets a value indicating whether the locked image is cached locally.
        /// </summary>
        [DontSerialize]
        public bool ImageLockedIsCached => HttpFileCachePlugin.FileWebIsCached(UrlLocked);

        /// <summary>
        /// Gets the icon path according to the achievement's unlock state.
        /// </summary>
        [DontSerialize]
        public string Icon => IsUnlock ? ImageUnlocked : ImageLocked;

        /// <summary>
        /// Gets a value indicating whether the locked icon should be displayed in gray.
        /// </summary>
        [DontSerialize]
        public bool IsGray => !IsUnlock && ((UrlLocked != null && UrlLocked.Contains("steamcdn-a.akamaihd.net") && UrlLocked.Length < 75) || UrlLocked.IsNullOrEmpty() || !UrlUnlocked.IsNullOrEmpty() || UrlLocked == UrlUnlocked);

        /// <summary>
        /// Gets a value indicating whether the rarity indicator is enabled in settings.
        /// </summary>
        [DontSerialize]
        public bool EnableRaretyIndicator => PluginDatabase.PluginSettings.Settings.EnableRaretyIndicator;

        /// <summary>
        /// Gets a value indicating whether the rarity value should be displayed.
        /// </summary>
        [DontSerialize]
        public bool DisplayRaretyValue => !NoRarety && (!PluginDatabase.PluginSettings.Settings.EnableRaretyIndicator
                    ? PluginDatabase.PluginSettings.Settings.EnableRaretyIndicator
                    : PluginDatabase.PluginSettings.Settings.DisplayRarityValue);

        /// <summary>
        /// Gets or sets a value indicating whether the achievement has no rarity value.
        /// </summary>
        public bool NoRarety { get; set; } = false;

        /// <summary>
        /// Gets the achievement name with the unlock date appended, if available.
        /// </summary>
        [DontSerialize]
        public string NameWithDateUnlock
        {
            get
            {
                string nameWithDateUnlock = Name;
                if (DateWhenUnlocked != null)
                {
                    LocalDateTimeConverter converter = new LocalDateTimeConverter();
                    nameWithDateUnlock += " (" + converter.Convert(DateWhenUnlocked, null, null, CultureInfo.CurrentCulture) + ")";
                }
                return nameWithDateUnlock;
            }
        }

        /// <summary>
        /// Gets a compact tooltip UI element for the achievement (list view).
        /// </summary>
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

        /// <summary>
        /// Gets a compact tooltip UI element for the achievement (partial view).
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the achievement is unlocked.
        /// </summary>
        [DontSerialize]
        public bool IsUnlock => DateWhenUnlocked != null || DateUnlocked.ToString().Contains("1982");

        private bool isVisible = true;
        /// <summary>
        /// Gets or sets a value indicating whether the achievement is visible in the UI.
        /// </summary>
        [DontSerialize]
        public bool IsVisible { get => isVisible; set => SetValue(ref isVisible, value); }

        /// <summary>
        /// Gets or sets the local date and time when the achievement was unlocked, or null if not unlocked.
        /// </summary>
        [DontSerialize]
        public DateTime? DateWhenUnlocked
        {
            get => DateUnlocked == null || DateUnlocked == default || DateUnlocked.ToString().Contains("0001") || DateUnlocked.ToString().Contains("1982")
                    ? null
                    : (DateTime?)((DateTime)DateUnlocked).ToLocalTime();
            set => DateUnlocked = value;
        }

        /// <summary>
        /// Gets the unlock date as a localized string.
        /// </summary>
        [DontSerialize]
        public string DateWhenUnlockedString => (string)new LocalDateTimeConverter().Convert(DateWhenUnlocked, null, null, CultureInfo.CurrentCulture);

        /// <summary>
        /// Gets or sets the progression data for the achievement.
        /// </summary>
        public AchProgression Progression { get; set; }

        /// <summary>
        /// Gets the icon text for locked achievements from settings.
        /// </summary>
        [DontSerialize]
        public string IconText => PluginDatabase.PluginSettings.Settings.IconLocked;

        /// <summary>
        /// Gets the custom icon for locked achievements, depending on settings and state.
        /// </summary>
        [DontSerialize]
        public string IconCustom
        {
            get
            {
                if (PluginDatabase.PluginSettings.Settings.IconCustomOnlyMissing && IsGray)
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

    /// <summary>
    /// Represents the progression state of an achievement, such as partial completion.
    /// </summary>
    public class AchProgression
    {
        /// <summary>
        /// Gets or sets the minimum value for progression.
        /// </summary>
        public double Min { get; set; }

        /// <summary>
        /// Gets or sets the maximum value for progression.
        /// </summary>
        public double Max { get; set; }

        /// <summary>
        /// Gets or sets the current value for progression.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Gets a string representation of the progression (e.g., "3 / 10").
        /// </summary>
        [DontSerialize]
        public string Progression => Value + " / " + Max;
    }
}