using System;

namespace SuccessStory.Models
{
    public class Achievements
    {
        public string Name { get; set; }
        public string ApiName { get; set; } = "";
        public string Description { get; set; }
        public string UrlUnlocked { get; set; }
        public string UrlLocked { get; set; }
        public DateTime? DateUnlocked { get; set; }
        public bool IsHidden { get; set; } = false;
        public float Percent { get; set; } = 100;
    }
}
