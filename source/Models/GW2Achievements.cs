using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    public class GW2Achievements
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string requirement { get; set; }
        public string locked_text { get; set; }
        public string type { get; set; }
        public List<string> flags { get; set; }
        public List<gw2Tier> tiers { get; set; }
        public List<gw2Reward> rewards { get; set; }
        public List<Bit> bits { get; set; }
    }

    public class gw2Tier
    {
        public int count { get; set; }
        public int points { get; set; }
    }

    public class gw2Reward
    {
        public string type { get; set; }
        public int id { get; set; }
    }

    public class Bit
    {
        public string type { get; set; }
        public string text { get; set; }
    }
}
