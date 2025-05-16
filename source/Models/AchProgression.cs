using Playnite.SDK.Data;

namespace SuccessStory.Models
{
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