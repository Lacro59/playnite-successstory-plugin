using Newtonsoft.Json;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Database
{
    /// <summary>
    /// Represents GameAchievements file.
    /// </summary>
    class GameAchievements
    {
        public string Name { get; set; }
        public bool HaveAchivements { get; set; }
        public int Total { get; set; }
        public int Unlocked { get; set; }
        public int Locked { get; set; }
        public int Progression { get; set; }
        public List<Achievements> Achievements  { get; set; }
    }
}
