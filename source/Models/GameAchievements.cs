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
        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        private List<Achievements> _Items = new List<Achievements>();
        public override List<Achievements> Items
        {
            get
            {
                return _Items;
            }

            set
            {
                _Items = value;
                OnPropertyChanged();
            }
        }

        private List<GameStats> _ItemsStats = new List<GameStats>();
        public List<GameStats> ItemsStats
        {
            get
            {
                return _ItemsStats;
            }

            set
            {
                _ItemsStats = value;
                OnPropertyChanged();
            }
        }


        [DontSerialize]
        public OrderAchievement orderAchievement;

        public ObservableCollection<Achievements> OrderItems
        {
            get
            {
                List<Achievements> OrderItems = Serialization.GetClone(Items);
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
                        case (OrderAchievementType.AchievementName):
                            if (orderAchievement.OrderTypeFirst == OrderType.Ascending)
                            {
                                OrderedItems = OrderedItems?.ThenBy(x => x.Name) ?? OrderItems.OrderBy(x => x.Name);
                            }
                            else
                            {
                                OrderedItems = OrderedItems?.ThenByDescending(x => x.Name) ?? OrderItems.OrderByDescending(x => x.Name);
                            }
                            break;

                        case (OrderAchievementType.AchievementDateUnlocked):
                            if (orderAchievement.OrderTypeFirst == OrderType.Ascending)
                            {
                                OrderedItems = OrderedItems?.ThenBy(x => x.DateUnlocked) ?? OrderItems.OrderBy(x => x.DateUnlocked);
                            }
                            else
                            {
                                OrderedItems = OrderedItems?.ThenByDescending(x => x.DateUnlocked) ?? OrderItems.OrderByDescending(x => x.DateUnlocked);
                            }
                            break;

                        case (OrderAchievementType.AchievementRarety):
                            if (orderAchievement.OrderTypeFirst == OrderType.Ascending)
                            {
                                OrderedItems = OrderedItems?.ThenBy(x => x.Percent) ?? OrderItems.OrderBy(x => x.Percent);
                            }
                            else
                            {
                                OrderedItems = OrderedItems?.ThenByDescending(x => x.Percent) ?? OrderItems.OrderByDescending(x => x.Percent);
                            }
                            break;
                    }

                    switch (orderAchievement.OrderAchievementTypeSecond)
                    {
                        case (OrderAchievementType.AchievementName):
                            if (orderAchievement.OrderTypeSecond == OrderType.Ascending)
                            {
                                OrderedItems = OrderedItems.ThenBy(x => x.Name);
                            }
                            else
                            {
                                OrderedItems = OrderedItems.ThenByDescending(x => x.Name);
                            }
                            break;

                        case (OrderAchievementType.AchievementDateUnlocked):
                            if (orderAchievement.OrderTypeSecond == OrderType.Ascending)
                            {
                                OrderedItems = OrderedItems.ThenBy(x => x.DateUnlocked);
                            }
                            else
                            {
                                OrderedItems = OrderedItems.ThenByDescending(x => x.DateUnlocked);
                            }
                            break;

                        case (OrderAchievementType.AchievementRarety):
                            if (orderAchievement.OrderTypeSecond == OrderType.Ascending)
                            {
                                OrderedItems = OrderedItems.ThenBy(x => x.Percent);
                            }
                            else
                            {
                                OrderedItems = OrderedItems.ThenByDescending(x => x.Percent);
                            }
                            break;
                    }

                    switch (orderAchievement.OrderAchievementTypeThird)
                    {
                        case (OrderAchievementType.AchievementName):
                            if (orderAchievement.OrderTypeThird == OrderType.Ascending)
                            {
                                OrderedItems = OrderedItems.ThenBy(x => x.Name);
                            }
                            else
                            {
                                OrderedItems = OrderedItems.ThenByDescending(x => x.Name);
                            }
                            break;

                        case (OrderAchievementType.AchievementDateUnlocked):
                            if (orderAchievement.OrderTypeThird == OrderType.Ascending)
                            {
                                OrderedItems = OrderedItems.ThenBy(x => x.DateUnlocked);
                            }
                            else
                            {
                                OrderedItems = OrderedItems.ThenByDescending(x => x.DateUnlocked);
                            }
                            break;

                        case (OrderAchievementType.AchievementRarety):
                            if (orderAchievement.OrderTypeThird == OrderType.Ascending)
                            {
                                OrderedItems = OrderedItems.ThenBy(x => x.Percent);
                            }
                            else
                            {
                                OrderedItems = OrderedItems.ThenByDescending(x => x.Percent);
                            }
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

        public ObservableCollection<Achievements> OrderItemsOnlyUnlocked
        {
            get
            {
                return OrderItems.Where(x => x.IsUnlock).ToObservable();
            }
        }

        public ObservableCollection<Achievements> OrderItemsOnlyLocked
        {
            get
            {
                return OrderItems.Where(x => !x.IsUnlock).ToObservable();
            }
        }


        /// <summary>
        /// Indicate if the game has stats data.
        /// </summary>
        [DontSerialize]
        public virtual bool HasDataStats
        {
            get
            {
                return ItemsStats?.Count > 0;
            }
        }

        /// <summary>
        /// Indicate if the game has achievements.
        /// </summary>
        [DontSerialize]
        public bool HasAchievements
        {
            get
            {
                return Items?.Count > 0;
            }
        }

        /// <summary>
        /// Indicate if the game is a rom.
        /// </summary>
        public bool IsEmulators { get; set; }

        [DontSerialize]
        public bool Is100Percent
        {
            get
            {
                return Total == Unlocked;
            }
        }

        [DontSerialize]
        public string SourceIcon
        {
            get
            {
                string SourceName = PlayniteTools.GetSourceName(Id);
                return TransformIcon.Get(SourceName);
            }
        }

        /// <summary>
        /// Total achievements for the game.
        /// </summary>
        [DontSerialize]
        public int Total
        {
            get
            {
                return Items.Count();
            }
        }

        /// <summary>
        /// Total unlocked achievements for the game.
        /// </summary>
        [DontSerialize]
        public int Unlocked
        {
            get
            {
                return Items.FindAll(x => x.IsUnlock).Count;
            }
        }

        /// <summary>
        /// Total locked achievements for the game.
        /// </summary>
        [DontSerialize]
        public int Locked
        {
            get
            {
                return Items.FindAll(x => !x.IsUnlock).Count;
            }
        }

        /// <summary>
        /// Estimate time to unlock all achievements.
        /// </summary>
        public EstimateTimeToUnlock EstimateTime { get; set; }

        /// <summary>
        /// Percentage
        /// </summary>
        [DontSerialize]
        public int Progression
        {
            get
            {
                return (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;
            }
        }

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
                var RarityUncommon = PluginDatabase.PluginSettings.Settings.RarityUncommon;

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
                var RarityUncommon = PluginDatabase.PluginSettings.Settings.RarityUncommon;
                var RarityRare = PluginDatabase.PluginSettings.Settings.RarityRare;

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
                var RarityRare = PluginDatabase.PluginSettings.Settings.RarityRare;
                var RarityUltraRare = PluginDatabase.PluginSettings.Settings.UseUltraRare ? 
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
                var RarityUltraRare = PluginDatabase.PluginSettings.Settings.RarityUltraRare;

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


        public bool ImageIsCached
        {
            get
            {
                if (!HasAchievements)
                {
                    return true;
                }

                return Items.Where(x => PlayniteTools.GetCacheFile(x.CacheUnlocked, "SuccessStory").IsNullOrEmpty()).Count() == 0;
            }
        }
    }
}
