using System;

namespace SuccessStory.Models
{
    /// <summary>
    /// Represents achievements object.
    /// </summary>
    class Achievements
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string UrlUnlocked { get; set; }
        public string UrlLocked { get; set; }
        public DateTime? DateUnlocked { get; set; }
    }
}
