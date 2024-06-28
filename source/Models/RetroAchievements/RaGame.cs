using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.RetroAchievements
{
    public class RaGame
    {
        public string Title { get; set; }
        public int ID { get; set; }
        public int ConsoleID { get; set; }
        public string ConsoleName { get; set; }
        public string ImageIcon { get; set; }
        public int NumAchievements { get; set; }
        public int NumLeaderboards { get; set; }
        public int Points { get; set; }
        public string DateModified { get; set; }
        public int? ForumTopicID { get; set; }
        public List<string> Hashes { get; set; }
    }
}
