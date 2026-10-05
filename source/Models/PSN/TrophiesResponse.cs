using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace SuccessStory.Models.PSN
{
    public class TrophiesResponse
    {
        [SerializationPropertyName("trophySetVersion")]
        public string TrophySetVersion { get; set; }

        [SerializationPropertyName("hasTrophyGroups")]
        public bool HasTrophyGroups { get; set; }

        [SerializationPropertyName("totalItemCount")]
        public int TotalItemCount { get; set; }

        [SerializationPropertyName("trophies")]
        public List<Trophy> Trophies { get; set; } = new List<Trophy>();
    }

    public class Trophy
    {
        [SerializationPropertyName("trophyId")]
        public int TrophyId { get; set; }

        [SerializationPropertyName("trophyHidden")]
        public bool TrophyHidden { get; set; }

        [SerializationPropertyName("trophyType")]
        public string TrophyType { get; set; }

        [SerializationPropertyName("trophyName")]
        public string TrophyName { get; set; }

        [SerializationPropertyName("trophyDetail")]
        public string TrophyDetail { get; set; }

        [SerializationPropertyName("trophyIconUrl")]
        public string TrophyIconUrl { get; set; }

        [SerializationPropertyName("trophyGroupId")]
        public string TrophyGroupId { get; set; }

        [SerializationPropertyName("earned")]
        public bool Earned { get; set; }

        [SerializationPropertyName("earnedDateTime")]
        public DateTime? EarnedDateTime { get; set; }

        [SerializationPropertyName("trophyEarnedRate")]
        public string TrophyEarnedRate { get; set; }

        [SerializationPropertyName("trophyProgressTargetValue")]
        public string TrophyProgressTargetValue { get; set; }

        [SerializationPropertyName("progress")]
        public string Progress { get; set; }

        [SerializationPropertyName("progressRate")]
        public string ProgressRate { get; set; }

        [SerializationPropertyName("progressedDateTime")]
        public DateTime? ProgressedDateTime { get; set; }
    }
}
