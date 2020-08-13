using System;

namespace SuccessStory.Models
{
    /// <summary>
    /// Represents achievements object.
    /// </summary>
    public class Achievements
    {
        /// <summary>
        /// Achievement's name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ApiName { get; set; } = "";
        /// <summary>
        /// 
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string UrlUnlocked { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string UrlLocked { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? DateUnlocked { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float Percent { get; set; } = 100;
    }
}
