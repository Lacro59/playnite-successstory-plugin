using System.Collections.Generic;

namespace SuccessStory.Models
{
    public class BattleNetSc2Ach
    {
        public List<Achievement> achievements { get; set; }
        public List<CriterionAch> criteria { get; set; }
        public List<Category> categories { get; set; }
        public List<Reward> rewards { get; set; }
    }

    public class Achievement
    {
        public string categoryId { get; set; }
        public List<string> chainAchievementIds { get; set; }
        public int chainRewardSize { get; set; }
        public List<string> criteriaIds { get; set; }
        public string description { get; set; }
        public int flags { get; set; }
        public string id { get; set; }
        public string imageUrl { get; set; }
        public bool isChained { get; set; }
        public int points { get; set; }
        public string title { get; set; }
        public int uiOrderHint { get; set; }
    }

    public class CriterionAch
    {
        public string achievementId { get; set; }
        public string description { get; set; }
        public string evaluationClass { get; set; }
        public int flags { get; set; }
        public string id { get; set; }
        public int necessaryQuantity { get; set; }
        public int uiOrderHint { get; set; }
    }

    public class Category
    {
        public List<string> childrenCategoryIds { get; set; }
        public string featuredAchievementId { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string parentCategoryId { get; set; }
        public int points { get; set; }
        public int uiOrderHint { get; set; }
        public List<int> medalTiers { get; set; }
    }

    public class Reward
    {
        public int flags { get; set; }
        public string id { get; set; }
        public string achievementId { get; set; }
        public string name { get; set; }
        public string imageUrl { get; set; }
        public string unlockableType { get; set; }
        public bool isSkin { get; set; }
        public int uiOrderHint { get; set; }
    }
}
