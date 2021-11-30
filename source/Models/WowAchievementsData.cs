using System;
using System.Collections.Generic;

namespace SuccessStory.Models
{
    public class WowAchievementsData
    {
        public string name { get; set; }
        public string category { get; set; }
        public List<AchievementsList> achievementsList { get; set; }
        public object subcategories { get; set; }
    }
    
    public class Icon
    {
        public string url { get; set; }
    }

    public class ProgressStep
    {
        public bool completed { get; set; }
        public int count { get; set; }
        public string description { get; set; }
        public bool isGold { get; set; }
        public int total { get; set; }
    }

    public class Step
    {
        public bool completed { get; set; }
        public string description { get; set; }
        public Icon icon { get; set; }
        public bool isGold { get; set; }
    }

    public class AchievementsList
    {
        public bool accountWide { get; set; }
        public string description { get; set; }
        public Icon icon { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public int point { get; set; }
        public List<ProgressStep> progressSteps { get; set; }
        public List<Step> steps { get; set; }
        public DateTime time { get; set; }
        public string url { get; set; }
    }

    public class WowAchievement
    {
        public bool accountWide { get; set; }
        public string description { get; set; }
        public Icon icon { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public int point { get; set; }
        public List<ProgressStep> progressSteps { get; set; }
        public List<Step> steps { get; set; }
        public DateTime? time { get; set; }
        public string url { get; set; }
    }

    public class SubcategoriesItem
    {
        public string name { get; set; }
        public string id { get; set; }
        public List<WowAchievement> achievements { get; set; }
    }

    public class Subcategories
    {
       //public Global global { get; set; }
    }
}
