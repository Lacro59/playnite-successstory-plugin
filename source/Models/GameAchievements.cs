using CommonPluginsShared.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using SuccessStory.Clients;
using Playnite.SDK.Data;
using CommonPluginsShared;
using CommonPluginsShared.Models;
using System.IO;
using SuccessStory.Services;
using System.Collections.ObjectModel;

namespace SuccessStory.Models
{
    public class GameAchievements : PluginDataBaseGame<Achievements>
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;


        private List<Achievements> items = new List<Achievements>();
        public override List<Achievements> Items { get => items; set => SetValue(ref items, value); }

        private List<GameStats> itemsStats = new List<GameStats>();
        public List<GameStats> ItemsStats { get => itemsStats; set => SetValue(ref itemsStats, value); }

        public bool ShowStats { get; set; } = true;

        [DontSerialize]
        public float TotalGamerScore => Items?.Where(x => x.IsUnlock).Sum(x => x.GamerScore) ?? 0;


        [DontSerialize]
        public OrderAchievement orderAchievement;

        [DontSerialize]
        public ObservableCollection<Achievements> OrderItems
        {
            get
            {
                List<Achievements> OrderItems = Items;
                IOrderedEnumerable<Achievements> OrderedItems = null;

                if (OrderItems == null)
                {
                    return new ObservableCollection<Achievements>();
                }

                if (orderAchievement != null)
                {
                    if (orderAchievement.OrderGroupByUnlocked)
                    {
                        OrderedItems = OrderItems.OrderByDescending(x => x.IsUnlock);
                    }

                    switch (orderAchievement.OrderAchievementTypeFirst)
                    {
                        case OrderAchievementType.AchievementName:
                            OrderedItems = orderAchievement.OrderTypeFirst == OrderType.Ascending
                                ? OrderedItems?.ThenBy(x => x.Name) ?? OrderItems.OrderBy(x => x.Name)
                                : OrderedItems?.ThenByDescending(x => x.Name) ?? OrderItems.OrderByDescending(x => x.Name);
                            break;

                        case OrderAchievementType.AchievementDateUnlocked:
                            OrderedItems = orderAchievement.OrderTypeFirst == OrderType.Ascending
                                ? OrderedItems?.ThenBy(x => x.DateUnlocked) ?? OrderItems.OrderBy(x => x.DateUnlocked)
                                : OrderedItems?.ThenByDescending(x => x.DateUnlocked) ?? OrderItems.OrderByDescending(x => x.DateUnlocked);
                            break;

                        case OrderAchievementType.AchievementRarety:
                            OrderedItems = orderAchievement.OrderTypeFirst == OrderType.Ascending
                                ? OrderedItems?.ThenBy(x => x.Percent) ?? OrderItems.OrderBy(x => x.Percent)
                                : OrderedItems?.ThenByDescending(x => x.Percent) ?? OrderItems.OrderByDescending(x => x.Percent);
                            break;

                        default:
                            break;
                    }

                    switch (orderAchievement.OrderAchievementTypeSecond)
                    {
                        case OrderAchievementType.AchievementName:
                            OrderedItems = orderAchievement.OrderTypeSecond == OrderType.Ascending
                                ? OrderedItems.ThenBy(x => x.Name)
                                : OrderedItems.ThenByDescending(x => x.Name);
                            break;

                        case OrderAchievementType.AchievementDateUnlocked:
                            OrderedItems = orderAchievement.OrderTypeSecond == OrderType.Ascending
                                ? OrderedItems.ThenBy(x => x.DateUnlocked)
                                : OrderedItems.ThenByDescending(x => x.DateUnlocked);
                            break;

                        case OrderAchievementType.AchievementRarety:
                            OrderedItems = orderAchievement.OrderTypeSecond == OrderType.Ascending
                                ? OrderedItems.ThenBy(x => x.Percent)
                                : OrderedItems.ThenByDescending(x => x.Percent);
                            break;

                        default:
                            break;
                    }

                    switch (orderAchievement.OrderAchievementTypeThird)
                    {
                        case OrderAchievementType.AchievementName:
                            OrderedItems = orderAchievement.OrderTypeThird == OrderType.Ascending
                                ? OrderedItems.ThenBy(x => x.Name)
                                : OrderedItems.ThenByDescending(x => x.Name);
                            break;

                        case OrderAchievementType.AchievementDateUnlocked:
                            OrderedItems = orderAchievement.OrderTypeThird == OrderType.Ascending
                                ? OrderedItems.ThenBy(x => x.DateUnlocked)
                                : OrderedItems.ThenByDescending(x => x.DateUnlocked);
                            break;

                        case OrderAchievementType.AchievementRarety:
                            OrderedItems = orderAchievement.OrderTypeThird == OrderType.Ascending
                                ? OrderedItems.ThenBy(x => x.Percent)
                                : OrderedItems.ThenByDescending(x => x.Percent);
                            break;

                        default:
                            break;
                    }

                    orderAchievement = null;

                    return OrderedItems.ToObservable();
                }
                else
                {
                    return OrderItems.ToObservable();
                }
            }
        }

        [DontSerialize]
        public ObservableCollection<Achievements> OrderItemsOnlyUnlocked => OrderItems.Where(x => x.IsUnlock).ToObservable();

        [DontSerialize]
        public ObservableCollection<Achievements> OrderItemsOnlyLocked => OrderItems.Where(x => !x.IsUnlock).ToObservable();


        /// <summary>
        /// Indicate if the game has stats data.
        /// </summary>
        [DontSerialize]
        public virtual bool HasDataStats => ItemsStats?.Count > 0;

        /// <summary>
        /// Indicate if the game has achievements.
        /// </summary>
        [DontSerialize]
        public bool HasAchievements => Items?.Count > 0;

        /// <summary>
        /// Indicate if the game is a rom.
        /// </summary>
        public bool IsEmulators { get; set; }

        [DontSerialize]
        public bool Is100Percent => Total == Unlocked;

        [DontSerialize]
        public string SourceIcon => TransformIcon.Get(PlayniteTools.GetSourceName(Id));

        /// <summary>
        /// Total achievements for the game.
        /// </summary>
        [DontSerialize]
        public int Total => Items.Count();

        /// <summary>
        /// Total unlocked achievements for the game.
        /// </summary>
        [DontSerialize]
        public int Unlocked => Items.FindAll(x => x.IsUnlock).Count;

        /// <summary>
        /// Total locked achievements for the game.
        /// </summary>
        [DontSerialize]
        public int Locked => Items.FindAll(x => !x.IsUnlock).Count;

        /// <summary>
        /// Estimate time to unlock all achievements.
        /// </summary>
        public EstimateTimeToUnlock EstimateTime { get; set; }

        /// <summary>
        /// Percentage
        /// </summary>
        [DontSerialize]
        public int Progression => (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;

        /// <summary>
        /// Indicate if the achievements have added manualy.
        /// </summary>
        public bool IsManual { get; set; }

        /// <summary>
        /// Indicate if the game is ignored.
        /// </summary>
        public bool IsIgnored { get; set; }


        public SourceLink SourcesLink { get; set; }


        // Commun, Non Commun, Rare, Épique
        [DontSerialize]
        public AchRaretyStats Common
        {
            get
            {
                double RarityUncommon = PluginDatabase.PluginSettings.Settings.RarityUncommon;

                AchRaretyStats achRaretyStats = new AchRaretyStats
                {
                    Total = Items.Where(x => x.Percent > RarityUncommon).Count(),
                    UnLocked = Items.Where(x => x.Percent > RarityUncommon && x.IsUnlock).Count()
                };
                achRaretyStats.Locked = achRaretyStats.Total - achRaretyStats.UnLocked;

                return achRaretyStats;
            }
        }

        [DontSerialize]
        public AchRaretyStats NoCommon
        {
            get
            {
                double RarityUncommon = PluginDatabase.PluginSettings.Settings.RarityUncommon;
                double RarityRare = PluginDatabase.PluginSettings.Settings.RarityRare;

                AchRaretyStats achRaretyStats = new AchRaretyStats
                {
                    Total = Items.Where(x => x.Percent <= RarityUncommon && x.Percent > RarityRare).Count(),
                    UnLocked = Items.Where(x => x.Percent <= RarityUncommon && x.Percent > RarityRare && x.IsUnlock).Count()
                };
                achRaretyStats.Locked = achRaretyStats.Total - achRaretyStats.UnLocked;

                return achRaretyStats;
            }
        }

        [DontSerialize]
        public AchRaretyStats Rare
        {
            get
            {
                double RarityRare = PluginDatabase.PluginSettings.Settings.RarityRare;
                double RarityUltraRare = PluginDatabase.PluginSettings.Settings.UseUltraRare ? 
                                            PluginDatabase.PluginSettings.Settings.RarityUltraRare :
                                            0;

                AchRaretyStats achRaretyStats = new AchRaretyStats
                {
                    Total = Items.Where(x => x.Percent <= RarityRare && x.Percent > RarityUltraRare).Count(),
                    UnLocked = Items.Where(x => x.Percent <= RarityRare && x.Percent > RarityUltraRare && x.IsUnlock).Count()
                };
                achRaretyStats.Locked = achRaretyStats.Total - achRaretyStats.UnLocked;

                return achRaretyStats;
            }
        }

        [DontSerialize]
        public AchRaretyStats UltraRare
        {
            get
            {
                double RarityUltraRare = PluginDatabase.PluginSettings.Settings.RarityUltraRare;

                AchRaretyStats achRaretyStats = new AchRaretyStats
                {
                    Total = Items.Where(x => x.Percent <= RarityUltraRare).Count(),
                    UnLocked = Items.Where(x => x.Percent <= RarityUltraRare && x.IsUnlock).Count()
                };
                achRaretyStats.Locked = achRaretyStats.Total - achRaretyStats.UnLocked;

                return achRaretyStats;
            }
        }


        // only for RA
        public int RAgameID { get; set; }

        // only for PSN
        public string CommunicationId { get; set; }


        [DontSerialize]
        public bool ImageIsCached
        {
            get
            {
                if (!HasAchievements)
                {
                    return true;
                }

                if (Items?.First()?.UrlUnlocked?.Contains("GenshinImpact", StringComparison.InvariantCultureIgnoreCase) ?? false)
                {
                    return true;
                }
                if (Items?.First()?.UrlUnlocked?.Contains("rpcs3", StringComparison.InvariantCultureIgnoreCase) ?? false)
                {
                    return true;
                }

                return !(Items.Where(x => x.ImageUnlockedIsCached && x.ImageLockedIsCached).Count() == 0);
            }
        }


        [DontSerialize]
        public DateTime? FirstUnlock => Items.Select(x => x.DateWhenUnlocked).Min();

        [DontSerialize]
        public DateTime? LastUnlock => Items.Select(x => x.DateWhenUnlocked).Max();

        [DontSerialize]
        public List<DateTime> DatesUnlock => Items.Where(x => x.DateWhenUnlocked != null).Select(x => (DateTime)x.DateWhenUnlocked).ToList();


        public void SetRaretyIndicator()
        {
            if (HasAchievements)
            {
                bool NoRarety = Items?.Where(x => x.Percent != 100)?.Count() == 0;
                if (NoRarety)
                {
                    _ = Items.All(x => { x.NoRarety = true; return true; });
                }
            }
        }
    }
}
