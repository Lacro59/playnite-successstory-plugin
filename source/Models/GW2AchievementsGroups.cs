using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    public class GW2AchievementsGroups
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int order { get; set; }
        public string icon { get; set; }
        public List<int> achievements { get; set; }
    }
}
