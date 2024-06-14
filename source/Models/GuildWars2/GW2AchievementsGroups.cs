using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.GuildWars2
{
    public class GW2AchievementsGroups
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }
        [SerializationPropertyName("name")]
        public string Name { get; set; }
        [SerializationPropertyName("description")]
        public string Description { get; set; }
        [SerializationPropertyName("order")]
        public int Order { get; set; }
        [SerializationPropertyName("icon")]
        public string Icon { get; set; }
        [SerializationPropertyName("achievements")]
        public List<int> Achievements { get; set; }
    }
}
