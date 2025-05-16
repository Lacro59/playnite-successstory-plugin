namespace SuccessStory.Models
{
    /// <summary>
    /// Represents statistics about achievement rarity, including the number of locked, unlocked, and total achievements.
    /// </summary>
    public class AchRaretyStats
    {
        /// <summary>
        /// Gets or sets the number of locked achievements.
        /// </summary>
        public int Locked { get; set; }

        /// <summary>
        /// Gets or sets the number of unlocked achievements.
        /// </summary>
        public int UnLocked { get; set; }

        /// <summary>
        /// Gets or sets the total number of achievements.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Gets a string representation of the unlocked and total achievements (e.g., "3 / 10").
        /// </summary>
        public string Stats => UnLocked + " / " + Total;
    }
}