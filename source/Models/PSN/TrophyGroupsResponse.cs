using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace SuccessStory.Models.PSN
{
    public class TrophyGroupsResponse
    {
        [SerializationPropertyName("trophySetVersion")]
        public string TrophySetVersion { get; set; }

        [SerializationPropertyName("hasTrophyGroups")]
        public bool HasTrophyGroups { get; set; }

        [SerializationPropertyName("trophyTitleName")]
        public string TrophyTitleName { get; set; }

        [SerializationPropertyName("trophyTitleIconUrl")]
        public string TrophyTitleIconUrl { get; set; }

        [SerializationPropertyName("trophyTitlePlatform")]
        public string TrophyTitlePlatform { get; set; }

        [SerializationPropertyName("trophyGroups")]
        public List<TrophyGroup> TrophyGroups { get; set; } = new List<TrophyGroup>();
    }

    public class TrophyGroup
    {
        [SerializationPropertyName("trophyGroupId")]
        public string TrophyGroupId { get; set; }

        [SerializationPropertyName("trophyGroupName")]
        public string TrophyGroupName { get; set; }

        [SerializationPropertyName("trophyGroupDetail")]
        public string TrophyGroupDetail { get; set; }

        [SerializationPropertyName("trophyGroupIconUrl")]
        public string TrophyGroupIconUrl { get; set; }

        [SerializationPropertyName("definedTrophies")]
        public DefinedTrophies DefinedTrophies { get; set; }
    }
}
