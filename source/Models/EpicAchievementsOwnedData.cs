using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    class EpicAchievementsOwnedData
    {
        public PlayerAchievement PlayerAchievement { get; set; }
    }
    
    public class PlayerAchievement3
    {
        public string sandboxId { get; set; }
        public string epicAccountId { get; set; }
        public bool unlocked { get; set; }
        public int progress { get; set; }
        public int XP { get; set; }
        public DateTime? unlockDate { get; set; }
        public string achievementName { get; set; }
    }

    public class PlayerAchievement2
    {
        public PlayerAchievement3 playerAchievement { get; set; }
    }

    public class Record
    {
        public int totalXP { get; set; }
        public int totalUnlocked { get; set; }
        public List<PlayerAchievement2> playerAchievements { get; set; }
    }

    public class PlayerAchievementGameRecordsBySandbox
    {
        public List<Record> records { get; set; }
    }

    public class PlayerAchievement
    {
        public PlayerAchievementGameRecordsBySandbox playerAchievementGameRecordsBySandbox { get; set; }
    }




}
