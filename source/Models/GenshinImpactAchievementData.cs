using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    public class GenshinImpactAchievementData
    {
        public int GoalId { get; set; }
        public int OrderId { get; set; }
        public object TitleTextMapHash { get; set; }
        public object DescTextMapHash { get; set; }
        public object Ps5TitleTextMapHash { get; set; }
        public string Ttype { get; set; }
        public string PsTrophyId { get; set; }
        public string Ps4TrophyId { get; set; }
        public string Ps5TrophyId { get; set; }
        public string Icon { get; set; }
        public int FinishRewardId { get; set; }
        public bool IsDisuse { get; set; }
        public int Id { get; set; }
        public TriggerConfig TriggerConfig { get; set; }
        public int Progress { get; set; }
        public object IsDeleteWatcherAfterFinish { get; set; }
        public int? PreStageAchievementId { get; set; }
        public string IsShow { get; set; }
        public string ProgressShowType { get; set; }
    }

    public class TriggerConfig
    {
        public string TriggerType { get; set; }
        public List<string> ParamList { get; set; }
    }
}
