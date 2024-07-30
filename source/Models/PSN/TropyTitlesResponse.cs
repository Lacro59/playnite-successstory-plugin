using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Playnite.SDK.Data;

namespace SuccessStory.Models.PSN
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class DefinedTrophies
    {
        [SerializationPropertyName("bronze")]
        public int Bronze { get; set; }

        [SerializationPropertyName("silver")]
        public int Silver { get; set; }

        [SerializationPropertyName("gold")]
        public int Gold { get; set; }

        [SerializationPropertyName("platinum")]
        public int Platinum { get; set; }
    }

    public class EarnedTrophies
    {
        [SerializationPropertyName("bronze")]
        public int Bronze { get; set; }

        [SerializationPropertyName("silver")]
        public int Silver { get; set; }

        [SerializationPropertyName("gold")]
        public int Gold { get; set; }

        [SerializationPropertyName("platinum")]
        public int Platinum { get; set; }
    }

    public class TropyTitlesResponse
    {
        [SerializationPropertyName("trophyTitles")]
        public List<TrophyTitle> TrophyTitles { get; set; }

        [SerializationPropertyName("totalItemCount")]
        public int TotalItemCount { get; set; }
    }

    public class TrophyTitle
    {
        [SerializationPropertyName("npServiceName")]
        public string NpServiceName { get; set; }

        [SerializationPropertyName("npCommunicationId")]
        public string NpCommunicationId { get; set; }

        [SerializationPropertyName("trophySetVersion")]
        public string TrophySetVersion { get; set; }

        [SerializationPropertyName("trophyTitleName")]
        public string TrophyTitleName { get; set; }

        [SerializationPropertyName("trophyTitleDetail")]
        public string TrophyTitleDetail { get; set; }

        [SerializationPropertyName("trophyTitleIconUrl")]
        public string TrophyTitleIconUrl { get; set; }

        [SerializationPropertyName("trophyTitlePlatform")]
        public string TrophyTitlePlatform { get; set; }

        [SerializationPropertyName("hasTrophyGroups")]
        public bool HasTrophyGroups { get; set; }

        [SerializationPropertyName("trophyGroupCount")]
        public int TrophyGroupCount { get; set; }

        [SerializationPropertyName("definedTrophies")]
        public DefinedTrophies DefinedTrophies { get; set; }

        [SerializationPropertyName("progress")]
        public int Progress { get; set; }

        [SerializationPropertyName("earnedTrophies")]
        public EarnedTrophies EarnedTrophies { get; set; }

        [SerializationPropertyName("hiddenFlag")]
        public bool HiddenFlag { get; set; }

        [SerializationPropertyName("lastUpdatedDateTime")]
        public DateTime LastUpdatedDateTime { get; set; }
    }


}
