using Playnite.SDK.Data;
using System;

namespace SuccessStory.Models.Stats
{
    /// <summary>
    /// Represents total achievement progression statistics.
    /// </summary>
    public class AchProgressionTotal
    {
        /// <summary>
        /// Total number of achievements.
        /// </summary>
        [DontSerialize]
        public int Total => Locked + Unlocked;

        /// <summary>
        /// Number of achievements that are still locked.
        /// </summary>
        public int Locked { get; set; }

        /// <summary>
        /// Number of achievements that have been unlocked.
        /// </summary>
        public int Unlocked { get; set; }

        /// <summary>
        /// Progression percentage (0-100).
        /// </summary>
        [DontSerialize]
        public int Progression => (Total != 0) ? (int)Math.Round((double)(Unlocked * 100 / Total)) : 0;

        /// <summary>
        /// Total gamerscore earned.
        /// </summary>
        public int GamerScore { get; set; }
    }
}