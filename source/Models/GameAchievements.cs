using CommonPluginsShared.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuccessStory.Clients;
using Playnite.SDK.Data;
using CommonPluginsShared;
using CommonPluginsShared.Models;

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


        [DontSerialize]
        public virtual bool HasDataStats
        {
            get
            {
                return ItemsStats.Count > 0;
            }
        }

        /// <summary>
        /// Indicate if the game has achievements.
        /// </summary>
        public bool HaveAchivements { get; set; }

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
        public int Total { get; set; }

        /// <summary>
        /// Total unlocked achievements for the game.
        /// </summary>
        public int Unlocked { get; set; }

        /// <summary>
        /// Total locked achievements for the game.
        /// </summary>
        public int Locked { get; set; }

        /// <summary>
        /// Estimate time to unlock all achievements.
        /// </summary>
        public EstimateTimeToUnlock EstimateTime { get; set; }

        /// <summary>
        /// Percentage
        /// </summary>
        public int Progression { get; set; }

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
    }
}
