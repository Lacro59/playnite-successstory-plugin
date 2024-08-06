using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.Xbox
{
    public class XboxOneAchievement
    {
        public string id { get; set; }
        public string serviceConfigId { get; set; }
        public string name { get; set; }
        public List<TitleAssociations> titleAssociations { get; set; }
        public string progressState { get; set; }
        public Progression progression { get; set; }
        public List<MediaAssets> mediaAssets { get; set; }
        public List<string> platforms { get; set; }
        public bool isSecret { get; set; }
        public string description { get; set; }
        public string lockedDescription { get; set; }
        public string productId { get; set; }
        public string achievementType { get; set; }
        public string participationType { get; set; }
        public List<Reward> rewards { get; set; }
    }

    public class Reward
    {
        public string name { get; set; }
        public string description { get; set; }
        public string value { get; set; }
        public string type { get; set; }
        public object mediaAsset { get; set; }
        public string valueType { get; set; }
    }

    public class TitleAssociations
    {
        public string name { get; set; }
        public int id { get; set; }
    }

    public class Progression
    {
        public List<Requirements> requirements { get; set; }
        public DateTime timeUnlocked { get; set; }
    }

    public class Requirements
    {
        public string id { get; set; }
        public string current { get; set; }
        public string target { get; set; }
        public string operationType { get; set; }
        public string valueType { get; set; }
        public string ruleParticipationType { get; set; }
    }

    public class MediaAssets
    {
        public string name { get; set; }
        public string type { get; set; }
        public string url { get; set; }
    }

    public class XboxOneAchievementResponse
    {
        public List<XboxOneAchievement> achievements { get; set; }
        public PagingInfo pagingInfo { get; set; }
    }
}
