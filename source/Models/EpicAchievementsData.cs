using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    public class EpicAchievementsData
    {
        public AchievementEpic Achievement { get; set; }
    }

    public class PlatinumRarity
    {
        public int percent { get; set; }
    }

    public class Tier
    {
        public string name { get; set; }
        public string hexColor { get; set; }
        public int min { get; set; }
        public int max { get; set; }
    }

    public class Rarity
    {
        public int percent { get; set; }
    }

    public class Achievement3
    {
        public string sandboxId { get; set; }
        public string deploymentId { get; set; }
        public string name { get; set; }
        public bool hidden { get; set; }
        public string unlockedDisplayName { get; set; }
        public string lockedDisplayName { get; set; }
        public string unlockedDescription { get; set; }
        public string lockedDescription { get; set; }
        public string unlockedIconId { get; set; }
        public string lockedIconId { get; set; }
        public int XP { get; set; }
        public string flavorText { get; set; }
        public string unlockedIconLink { get; set; }
        public string lockedIconLink { get; set; }
        public Tier tier { get; set; }
        public Rarity rarity { get; set; }
    }

    public class Achievement2
    {
        public Achievement3 achievement { get; set; }
    }

    public class ProductAchievementsRecordBySandbox
    {
        public string productId { get; set; }
        public string sandboxId { get; set; }
        public int totalAchievements { get; set; }
        public int totalProductXP { get; set; }
        public PlatinumRarity platinumRarity { get; set; }
        public List<Achievement2> achievements { get; set; }
    }

    public class AchievementEpic
    {
        public ProductAchievementsRecordBySandbox productAchievementsRecordBySandbox { get; set; }
    }
}
