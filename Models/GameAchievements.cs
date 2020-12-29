using Newtonsoft.Json;
using CommonShared.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        [JsonIgnore]
        public virtual bool HasDataStats
        {
            get
            {
                return ItemsStats.Count > 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HaveAchivements { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsEmulators { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public bool Is100Percent
        {
            get
            {
                return Total == Unlocked;
            }
        }
        /// <summary>
        /// Total achievements for the game.
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Unlocked { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Locked { get; set; }
        /// <summary>
        /// Percentage
        /// </summary>
        public int Progression { get; set; }
    }
}
