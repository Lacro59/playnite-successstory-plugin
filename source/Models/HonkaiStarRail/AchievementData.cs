using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.HonkaiStarRail
{
    public class AchievementData
    {
        [SerializationPropertyName("AchievementID")]
        public int AchievementID { get; set; }

        [SerializationPropertyName("SeriesID")]
        public int SeriesID { get; set; }

        [SerializationPropertyName("QuestID")]
        public int QuestID { get; set; }

        [SerializationPropertyName("LinearQuestID")]
        public int LinearQuestID { get; set; }

        [SerializationPropertyName("AchievementTitle")]
        public AchievementTitle AchievementTitle { get; set; }

        [SerializationPropertyName("AchievementDesc")]
        public AchievementDesc AchievementDesc { get; set; }

        [SerializationPropertyName("AchievementDescPS")]
        public AchievementDescPS AchievementDescPS { get; set; }

        [SerializationPropertyName("ParamList")]
        public List<object> ParamList { get; set; }

        [SerializationPropertyName("Priority")]
        public int Priority { get; set; }

        [SerializationPropertyName("Rarity")]
        public string Rarity { get; set; }

        [SerializationPropertyName("ShowType")]
        public string ShowType { get; set; }

        [SerializationPropertyName("PSTrophyID")]
        public string PSTrophyID { get; set; }
    }

    public class AchievementDesc
    {
        [SerializationPropertyName("Hash")]
        public string Hash { get; set; }
    }

    public class AchievementDescPS
    {
        [SerializationPropertyName("Hash")]
        public string Hash { get; set; }
    }

    public class AchievementTitle
    {
        [SerializationPropertyName("Hash")]
        public string Hash { get; set; }
    }

    public class ParamList
    {
        [SerializationPropertyName("Value")]
        public int Value { get; set; }
    }
}
