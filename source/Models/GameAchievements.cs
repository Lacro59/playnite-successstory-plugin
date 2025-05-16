using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Models;
using Playnite.SDK.Data;
using SuccessStory.Clients;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using YamlDotNet.Core.Tokens;

namespace SuccessStory.Models
{
    public class GameAchievements : PluginDataBaseGame<Achievement>
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;


        #region Cache fields
        private bool? _hasAchievements;

        private float? _totalGamerScore;
        private int? _total;
        private int? _unlocked;
        private int? _locked;

        private DateTime? _firstUnlock;
        private DateTime? _lastUnlock;
        private List<DateTime> _datesUnlock;

        private AchRaretyStats _commonStats;
        private AchRaretyStats _uncommonStats;
        private AchRaretyStats _rareStats;
        private AchRaretyStats _ultraRareStats;
        #endregion


        /// <summary>
        /// Indicate if the game has achievements.
        /// </summary>
        [DontSerialize]
        public bool HasAchievements => (_hasAchievements ?? (_hasAchievements = Items?.Count > 0)).Value;

        /// <summary>
        /// Indicate if the achievements have added manualy.
        /// </summary>
        public bool IsManual { get; set; }

        /// <summary>
        /// Indicate if the game is ignored.
        /// </summary>
        public bool IsIgnored { get; set; }

        [DontSerialize]
        public bool Is100Percent => Total == Unlocked;

        [DontSerialize]
        public string SourceIcon => TransformIcon.Get(PlayniteTools.GetSourceName(Id));

        public SourceLink SourcesLink { get; set; }


        private List<GameStats> itemsStats = new List<GameStats>();
        public List<GameStats> ItemsStats { get => itemsStats; set => SetValue(ref itemsStats, value); }

        public bool ShowStats { get; set; } = true;

        /// <summary>
        /// Indicate if the game has stats data.
        /// </summary>
        [DontSerialize]
        public virtual bool HasDataStats => ItemsStats?.Count > 0;


        [DontSerialize]
        public OrderAchievement OrderAchievement { get; set; }

        [DontSerialize]
        public ObservableCollection<Achievement> OrderItems
        {
            get
            {
                List<Achievement> OrderItems = Items;
                IOrderedEnumerable<Achievement> OrderedItems = null;

                if (OrderItems == null)
                {
                    return new ObservableCollection<Achievement>();
                }

                if (OrderAchievement != null)
                {
                    if (OrderAchievement.OrderGroupByUnlocked)
                    {
                        OrderedItems = OrderItems.OrderByDescending(x => x.IsUnlock);
                    }

                    switch (OrderAchievement.OrderAchievementTypeFirst)
                    {
                        case OrderAchievementType.AchievementName:
                            OrderedItems = OrderAchievement.OrderTypeFirst == OrderType.Ascending
                                ? OrderedItems?.ThenBy(x => x.Name) ?? OrderItems.OrderBy(x => x.Name)
                                : OrderedItems?.ThenByDescending(x => x.Name) ?? OrderItems.OrderByDescending(x => x.Name);
                            break;

                        case OrderAchievementType.AchievementDateUnlocked:
                            OrderedItems = OrderAchievement.OrderTypeFirst == OrderType.Ascending
                                ? OrderedItems?.ThenBy(x => x.DateWhenUnlocked) ?? OrderItems.OrderBy(x => x.DateWhenUnlocked)
                                : OrderedItems?.ThenByDescending(x => x.DateWhenUnlocked) ?? OrderItems.OrderByDescending(x => x.DateWhenUnlocked);
                            break;

                        case OrderAchievementType.AchievementRarety:
                            OrderedItems = OrderAchievement.OrderTypeFirst == OrderType.Ascending
                                ? OrderedItems?.ThenBy(x => x.Percent) ?? OrderItems.OrderBy(x => x.Percent)
                                : OrderedItems?.ThenByDescending(x => x.Percent) ?? OrderItems.OrderByDescending(x => x.Percent);
                            break;

                        default:
                            break;
                    }

                    switch (OrderAchievement.OrderAchievementTypeSecond)
                    {
                        case OrderAchievementType.AchievementName:
                            OrderedItems = OrderAchievement.OrderTypeSecond == OrderType.Ascending
                                ? OrderedItems.ThenBy(x => x.Name)
                                : OrderedItems.ThenByDescending(x => x.Name);
                            break;

                        case OrderAchievementType.AchievementDateUnlocked:
                            OrderedItems = OrderAchievement.OrderTypeSecond == OrderType.Ascending
                                ? OrderedItems.ThenBy(x => x.DateWhenUnlocked)
                                : OrderedItems.ThenByDescending(x => x.DateWhenUnlocked);
                            break;

                        case OrderAchievementType.AchievementRarety:
                            OrderedItems = OrderAchievement.OrderTypeSecond == OrderType.Ascending
                                ? OrderedItems.ThenBy(x => x.Percent)
                                : OrderedItems.ThenByDescending(x => x.Percent);
                            break;

                        default:
                            break;
                    }

                    switch (OrderAchievement.OrderAchievementTypeThird)
                    {
                        case OrderAchievementType.AchievementName:
                            OrderedItems = OrderAchievement.OrderTypeThird == OrderType.Ascending
                                ? OrderedItems.ThenBy(x => x.Name)
                                : OrderedItems.ThenByDescending(x => x.Name);
                            break;

                        case OrderAchievementType.AchievementDateUnlocked:
                            OrderedItems = OrderAchievement.OrderTypeThird == OrderType.Ascending
                                ? OrderedItems.ThenBy(x => x.DateWhenUnlocked)
                                : OrderedItems.ThenByDescending(x => x.DateWhenUnlocked);
                            break;

                        case OrderAchievementType.AchievementRarety:
                            OrderedItems = OrderAchievement.OrderTypeThird == OrderType.Ascending
                                ? OrderedItems.ThenBy(x => x.Percent)
                                : OrderedItems.ThenByDescending(x => x.Percent);
                            break;

                        default:
                            break;
                    }

                    OrderAchievement = null;

                    return OrderedItems.ToObservable();
                }
                else
                {
                    return OrderItems.ToObservable();
                }
            }
        }

        [DontSerialize]
        public ObservableCollection<Achievement> OrderItemsOnlyUnlocked => OrderItems.Where(x => x.IsUnlock).ToObservable();

        [DontSerialize]
        public ObservableCollection<Achievement> OrderItemsOnlyLocked => OrderItems.Where(x => !x.IsUnlock).ToObservable();





        /// <summary>
        /// Indicate if the game is a rom.
        /// </summary>
        // TODO Not assigned
        public bool IsEmulators { get; set; }


        #region Achievements global stats
        /// <summary>
        /// Total GamerScore accumulated from unlocked achievements.
        /// </summary>
        [DontSerialize]
        public float TotalGamerScore
        {
            get
            {
                if (_totalGamerScore == null)
                {
                    _totalGamerScore = Items.Where(x => x.IsUnlock).Sum(x => x.GamerScore);
                }
                return _totalGamerScore.Value;
            }
        }

        /// <summary>
        /// Total number of achievements.
        /// </summary>
        [DontSerialize]
        public int Total => (_total ?? (_total = Items.Count)).Value;

        /// <summary>
        /// Number of unlocked achievements.
        /// </summary>
        [DontSerialize]
        public int Unlocked => (_unlocked ?? (_unlocked = Items.Count(x => x.IsUnlock))).Value;

        /// <summary>
        /// Number of locked achievements.
        /// </summary>
        [DontSerialize]
        public int Locked => (_locked ?? (_locked = Items.Count(x => !x.IsUnlock))).Value;

        /// <summary>
        /// Overall progression in percentage (rounded up).
        /// </summary>
        [DontSerialize]
        public int Progression => Total != 0 ? (int)Math.Ceiling((double)(Unlocked * 100) / Total) : 0;

        /// <summary>
        /// Estimate time to unlock all achievements.
        /// </summary>
        public EstimateTimeToUnlock EstimateTime { get; set; }
        #endregion


        #region Achievements rarity stats
        /// <summary>
        /// Get statistics for Common achievements (with a rarity above the "Uncommon" threshold).
        /// </summary>
        [DontSerialize]
        public AchRaretyStats Common
        {
            get
            {
                if (_commonStats == null)
                {
                    double rarityUncommon = PluginDatabase.PluginSettings.Settings.RarityUncommon;

                    var commonAchievements = Items.Where(x => x.Percent > rarityUncommon).ToList();

                    _commonStats = new AchRaretyStats
                    {
                        Total = commonAchievements.Count,
                        UnLocked = commonAchievements.Count(x => x.IsUnlock),
                        Locked = commonAchievements.Count(x => !x.IsUnlock)
                    };
                }
                return _commonStats;
            }
        }

        /// <summary>
        /// Get statistics for Uncommon achievements (rarity between "Rare" and "Uncommon" thresholds).
        /// </summary>
        [DontSerialize]
        public AchRaretyStats UnCommon
        {
            get
            {
                if (_uncommonStats == null)
                {
                    double rarityUncommon = PluginDatabase.PluginSettings.Settings.RarityUncommon;
                    double rarityRare = PluginDatabase.PluginSettings.Settings.RarityRare;

                    var uncommonAchievements = Items
                        .Where(x => x.Percent <= rarityUncommon && x.Percent > rarityRare)
                        .ToList();

                    _uncommonStats = new AchRaretyStats
                    {
                        Total = uncommonAchievements.Count,
                        UnLocked = uncommonAchievements.Count(x => x.IsUnlock),
                        Locked = uncommonAchievements.Count(x => !x.IsUnlock)
                    };
                }
                return _uncommonStats;
            }
        }

        /// <summary>
        /// Get statistics for Rare achievements (rarity between "UltraRare" and "Rare" thresholds).
        /// </summary>
        [DontSerialize]
        public AchRaretyStats Rare
        {
            get
            {
                if (_rareStats == null)
                {
                    double rarityRare = PluginDatabase.PluginSettings.Settings.RarityRare;
                    double rarityUltraRare = PluginDatabase.PluginSettings.Settings.UseUltraRare
                        ? PluginDatabase.PluginSettings.Settings.RarityUltraRare
                        : 0;

                    var rareAchievements = Items
                        .Where(x => x.Percent <= rarityRare && x.Percent > rarityUltraRare)
                        .ToList();

                    _rareStats = new AchRaretyStats
                    {
                        Total = rareAchievements.Count,
                        UnLocked = rareAchievements.Count(x => x.IsUnlock),
                        Locked = rareAchievements.Count(x => !x.IsUnlock)
                    };
                }
                return _rareStats;
            }
        }

        /// <summary>
        /// Get statistics for Ultra Rare achievements (rarity equal to or below the "UltraRare" threshold).
        /// </summary>
        [DontSerialize]
        public AchRaretyStats UltraRare
        {
            get
            {
                if (_ultraRareStats == null)
                {
                    double rarityUltraRare = PluginDatabase.PluginSettings.Settings.RarityUltraRare;

                    var ultraRareAchievements = Items
                        .Where(x => x.Percent <= rarityUltraRare)
                        .ToList();

                    _ultraRareStats = new AchRaretyStats
                    {
                        Total = ultraRareAchievements.Count,
                        UnLocked = ultraRareAchievements.Count(x => x.IsUnlock),
                        Locked = ultraRareAchievements.Count(x => !x.IsUnlock)
                    };
                }
                return _ultraRareStats;
            }
        }
        #endregion


        #region For RetroAchievements
        public int RAgameID { get; set; }

        [DontSerialize]
        public bool IsRa => RAgameID > 0;
        #endregion


        # region For PSN
        public string CommunicationId { get; set; }
        #endregion


        [DontSerialize]
        public bool ImageIsCached
        {
            get
            {
                if (!HasAchievements)
                {
                    return true;
                }

                Achievement first = Items.FirstOrDefault();
                if (first?.UrlUnlocked?.IndexOf("GenshinImpact", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    first?.UrlUnlocked?.IndexOf("rpcs3", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                return Items.All(x => x.ImageUnlockedIsCached && x.ImageLockedIsCached);
            }
        }


        #region Achievements dates stats
        /// <summary>
        /// Gets the earliest unlock date among all achievements.
        /// Returns null if no achievements have been unlocked.
        /// </summary>
        [DontSerialize]
        public DateTime? FirstUnlock
        {
            get
            {
                if (_firstUnlock == null && Items?.Any() == true)
                {
                    _firstUnlock = Items
                        .Where(x => x.DateWhenUnlocked.HasValue)
                        .Select(x => x.DateWhenUnlocked.Value)
                        .OrderBy(d => d)
                        .FirstOrDefault();
                }
                return _firstUnlock;
            }
        }

        /// <summary>
        /// Gets the latest unlock date among all achievements.
        /// Returns null if no achievements have been unlocked.
        /// </summary>
        [DontSerialize]
        public DateTime? LastUnlock
        {
            get
            {
                if (_lastUnlock == null && Items?.Any() == true)
                {
                    _lastUnlock = Items
                        .Where(x => x.DateWhenUnlocked.HasValue)
                        .Select(x => x.DateWhenUnlocked.Value)
                        .OrderByDescending(d => d)
                        .FirstOrDefault();
                }
                return _lastUnlock;
            }
        }

        /// <summary>
        /// Gets a list of all unlock dates, sorted in ascending order.
        /// </summary>
        [DontSerialize]
        public List<DateTime> DatesUnlock
        {
            get
            {
                return _datesUnlock ?? (_datesUnlock = Items?
                    .Where(x => x.DateWhenUnlocked.HasValue)
                    .Select(x => x.DateWhenUnlocked.Value)
                    .OrderBy(date => date)
                    .ToList() ?? new List<DateTime>());
            }
        }

        #endregion


        /// <summary>
        /// Clears cached. Automatically called when Items is updated.
        /// </summary>
        protected override void RefreshCachedValues()
        {
            _hasAchievements = null;

            _total = null;
            _unlocked = null;
            _locked = null;
            _totalGamerScore = null;

            _firstUnlock = null;
            _lastUnlock = null;
            _datesUnlock = null;

            _commonStats = null;
            _uncommonStats = null;
            _rareStats = null;
            _ultraRareStats = null;
        }

        public void SetRaretyIndicator()
        {
            if (HasAchievements)
            {
                bool allNoRarety = Items.All(x => x.Percent == 100);
                if (allNoRarety)
                {
                    foreach (Achievement achievement in Items)
                    {
                        achievement.NoRarety = true;
                    }
                }
            }
        }
    }
}
