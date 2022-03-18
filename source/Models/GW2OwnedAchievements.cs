using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    public class GW2OwnedAchievements
    {
        public int id { get; set; }
        public int current { get; set; }
        public int max { get; set; }
        public bool done { get; set; }
        public List<int> bits { get; set; }
    }
}
