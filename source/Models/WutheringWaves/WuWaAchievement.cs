using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.WutheringWaves
{
    public class WuWaAchievement
    {
        [SerializationPropertyName("Id")]
        public int Id { get; set; }

        [SerializationPropertyName("GroupId")]
        public int GroupId { get; set; }

        [SerializationPropertyName("Level")]
        public int Level { get; set; }

        [SerializationPropertyName("Name")]
        public string Name { get; set; }

        [SerializationPropertyName("Desc")]
        public string Desc { get; set; }

        [SerializationPropertyName("IconPath")]
        public string IconPath { get; set; }

        [SerializationPropertyName("OverrideDropId")]
        public int OverrideDropId { get; set; }

        [SerializationPropertyName("Hidden")]
        public bool Hidden { get; set; }

        [SerializationPropertyName("NextLink")]
        public int NextLink { get; set; }

        [SerializationPropertyName("ClientTrigger")]
        public bool ClientTrigger { get; set; }

        [SerializationPropertyName("ThirdPartyTrophyId")]
        public int ThirdPartyTrophyId { get; set; }
    }
}
