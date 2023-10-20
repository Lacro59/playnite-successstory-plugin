using System.Collections.Generic;
using System.Linq;

namespace SuccessStory.Models
{
    public class SearchResult
    {
        public string Url { get; set; }
        public string Name { get; set; }
        public string UrlImage { get; set; }
        public List<string> Platforms { get; set; }
        public int AchievementsCount { get; set; }

        public int AppId { get; set; }

        public string PlatformsFirst => Platforms?.FirstOrDefault();
    }
}
