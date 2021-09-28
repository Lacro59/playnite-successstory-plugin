using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
