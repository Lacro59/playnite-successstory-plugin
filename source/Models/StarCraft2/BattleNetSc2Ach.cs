using Playnite.SDK.Data;
using System.Collections.Generic;

namespace SuccessStory.Models.StarCraft2
{
    public class BattleNetSc2Ach
    {
        [SerializationPropertyName("achievements")]
        public List<Achievement> Achievements { get; set; }
        [SerializationPropertyName("criteria")]
        public List<CriterionAch> Criteria { get; set; }
        [SerializationPropertyName("categories")]
        public List<Category> Categories { get; set; }
        [SerializationPropertyName("rewards")]
        public List<Reward> Rewards { get; set; }
    }

    public class Achievement
    {
        [SerializationPropertyName("categoryId")]
        public string CategoryId { get; set; }
        [SerializationPropertyName("chainAchievementIds")]
        public List<string> ChainAchievementIds { get; set; }
        [SerializationPropertyName("chainRewardSize")]
        public int ChainRewardSize { get; set; }
        [SerializationPropertyName("criteriaIds")]
        public List<string> CriteriaIds { get; set; }
        [SerializationPropertyName("description")]
        public string Description { get; set; }
        [SerializationPropertyName("flags")]
        public int Flags { get; set; }
        [SerializationPropertyName("id")]
        public string Id { get; set; }
        [SerializationPropertyName("imageUrl")]
        public string ImageUrl { get; set; }
        [SerializationPropertyName("isChained")]
        public bool IsChained { get; set; }
        [SerializationPropertyName("points")]
        public int Points { get; set; }
        [SerializationPropertyName("title")]
        public string Title { get; set; }
        [SerializationPropertyName("uiOrderHint")]
        public int UiOrderHint { get; set; }
    }

    public class CriterionAch
    {
        [SerializationPropertyName("achievementId")]
        public string AchievementId { get; set; }
        [SerializationPropertyName("description")]
        public string Description { get; set; }
        [SerializationPropertyName("evaluationClass")]
        public string EvaluationClass { get; set; }
        [SerializationPropertyName("flags")]
        public int Flags { get; set; }
        [SerializationPropertyName("id")]
        public string Id { get; set; }
        [SerializationPropertyName("necessaryQuantity")]
        public int NecessaryQuantity { get; set; }
        [SerializationPropertyName("uiOrderHint")]
        public int UiOrderHint { get; set; }
    }

    public class Category
    {
        [SerializationPropertyName("childrenCategoryIds")]
        public List<string> ChildrenCategoryIds { get; set; }
        [SerializationPropertyName("featuredAchievementId")]
        public string FeaturedAchievementId { get; set; }
        [SerializationPropertyName("id")]
        public string Id { get; set; }
        [SerializationPropertyName("name")]
        public string Name { get; set; }
        [SerializationPropertyName("parentCategoryId")]
        public string ParentCategoryId { get; set; }
        [SerializationPropertyName("points")]
        public int Points { get; set; }
        [SerializationPropertyName("uiOrderHint")]
        public int UiOrderHint { get; set; }
        [SerializationPropertyName("medalTiers")]
        public List<int> MedalTiers { get; set; }
    }

    public class Reward
    {
        [SerializationPropertyName("flags")]
        public int Flags { get; set; }
        [SerializationPropertyName("id")]
        public string Id { get; set; }
        [SerializationPropertyName("achievementId")]
        public string AchievementId { get; set; }
        [SerializationPropertyName("name")]
        public string Name { get; set; }
        [SerializationPropertyName("imageUrl")]
        public string ImageUrl { get; set; }
        [SerializationPropertyName("unlockableType")]
        public string UnlockableType { get; set; }
        [SerializationPropertyName("isSkin")]
        public bool IsSkin { get; set; }
        [SerializationPropertyName("uiOrderHint")]
        public int UiOrderHint { get; set; }
    }
}
