using CommonPluginsShared.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using SuccessStory.Clients;
using Playnite.SDK.Data;
using CommonPluginsShared;
using CommonPluginsShared.Models;
using System.IO;

namespace SuccessStory.Models
{
    public class GameAchievements : PluginDataBaseGame<Achievements>
    {
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


        /// <summary>
        /// Indicate if the game has stats data.
        /// </summary>
        [DontSerialize]
        public virtual bool HasDataStats
        {
            get
            {
                return (bool)(ItemsStats?.Count > 0);
            }
        }

        /// <summary>
        /// Indicate if the game has achievements.
        /// </summary>
        [DontSerialize]
        public bool HasAchivements
        {
            get
            {
                return (bool)(Items?.Count > 0);
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
                string SourceName = PlayniteTools.GetSourceName(SuccessStory.PluginDatabase.PlayniteApi, Id);
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
        public AchRaretyStats Common {
            get
            {
                AchRaretyStats achRaretyStats = new AchRaretyStats
                {
                    Total = Items.Where(x => x.Percent > 30).Count(),
                    UnLocked = Items.Where(x => x.Percent > 30 && x.IsUnlock).Count()
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
                AchRaretyStats achRaretyStats = new AchRaretyStats
                {
                    Total = Items.Where(x => x.Percent <= 30 && x.Percent > 10).Count(),
                    UnLocked = Items.Where(x => x.Percent <= 30 && x.Percent > 10 && x.IsUnlock).Count()
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
                AchRaretyStats achRaretyStats = new AchRaretyStats
                {
                    Total = Items.Where(x => x.Percent <= 10).Count(),
                    UnLocked = Items.Where(x => x.Percent <= 10 && x.IsUnlock).Count()
                };
                achRaretyStats.Locked = achRaretyStats.Total - achRaretyStats.UnLocked;

                return achRaretyStats;
            }
        }


        // only for RA
        public int RAgameID { get; set; }


        public bool ImageIsCached
        {
            get
            {
                if (!HasAchivements)
                {
                    return true;
                }

                string pathImageUnlocked = PlayniteTools.GetCacheFile(Items?.FirstOrDefault()?.CacheUnlocked, "SuccessStory");
                return !pathImageUnlocked.IsNullOrEmpty() && File.Exists(pathImageUnlocked);
            }
        }
    }
}
