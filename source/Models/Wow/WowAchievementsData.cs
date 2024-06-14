using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace SuccessStory.Models.Wow
{
    public class Achievement
    {
        [SerializationPropertyName("accountWide")]
        public bool AccountWide;

        [SerializationPropertyName("description")]
        public string Description;

        [SerializationPropertyName("icon")]
        public Icon Icon;

        [SerializationPropertyName("id")]
        public int Id;

        [SerializationPropertyName("name")]
        public string Name;

        [SerializationPropertyName("point")]
        public int Point;

        [SerializationPropertyName("progressSteps")]
        public List<ProgressStep> ProgressSteps;

        [SerializationPropertyName("steps")]
        public List<Step> Steps;

        [SerializationPropertyName("url")]
        public string Url;

        [SerializationPropertyName("time")]
        public DateTime Time;

        [SerializationPropertyName("reward")]
        public Reward Reward;
    }

    public class AchievementsList
    {
        [SerializationPropertyName("accountWide")]
        public bool AccountWide;

        [SerializationPropertyName("description")]
        public string Description;

        [SerializationPropertyName("icon")]
        public Icon Icon;

        [SerializationPropertyName("id")]
        public int Id;

        [SerializationPropertyName("name")]
        public string Name;

        [SerializationPropertyName("point")]
        public int Point;

        [SerializationPropertyName("progressSteps")]
        public List<ProgressStep> ProgressSteps;

        [SerializationPropertyName("steps")]
        public List<Step> Steps;

        [SerializationPropertyName("time")]
        public DateTime Time;

        [SerializationPropertyName("url")]
        public string Url;

        [SerializationPropertyName("reward")]
        public Reward Reward;
    }

    public class SubcategoriesItem
    {
        [SerializationPropertyName("achievements")]
        public List<Achievement> Achievements;

        [SerializationPropertyName("id")]
        public string Id;

        [SerializationPropertyName("name")]
        public string Name;
    }

    public class Icon
    {
        [SerializationPropertyName("url")]
        public string Url;
    }

    public class ProgressStep
    {
        [SerializationPropertyName("completed")]
        public bool Completed;

        [SerializationPropertyName("count")]
        public int Count;

        [SerializationPropertyName("isGold")]
        public bool IsGold;

        [SerializationPropertyName("total")]
        public int Total;

        [SerializationPropertyName("description")]
        public string Description;
    }

    public class Quality
    {
        [SerializationPropertyName("enum")]
        public string Enum;

        [SerializationPropertyName("id")]
        public int Id;

        [SerializationPropertyName("name")]
        public string Name;

        [SerializationPropertyName("slug")]
        public string Slug;
    }

    public class Reward
    {
        [SerializationPropertyName("icon")]
        public Icon Icon;

        [SerializationPropertyName("name")]
        public string Name;

        [SerializationPropertyName("quality")]
        public Quality Quality;

        [SerializationPropertyName("type")]
        public string Type;
    }

    public class WowAchievementsData
    {
        [SerializationPropertyName("name")]
        public string Name;

        [SerializationPropertyName("category")]
        public string Category;

        [SerializationPropertyName("achievementsList")]
        public List<AchievementsList> AchievementsList;

        [SerializationPropertyName("subcategories")]
        public object Subcategories;
    }

    public class Step
    {
        [SerializationPropertyName("completed")]
        public bool Completed;

        [SerializationPropertyName("description")]
        public string Description;

        [SerializationPropertyName("icon")]
        public Icon Icon;

        [SerializationPropertyName("isGold")]
        public bool IsGold;

        [SerializationPropertyName("url")]
        public string Url;
    }
}
