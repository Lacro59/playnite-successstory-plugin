using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.GuildWars2
{
    public class GW2OwnedAchievements
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }
        [SerializationPropertyName("current")]
        public int Current { get; set; }
        [SerializationPropertyName("tymaxpe")]
        public int Max { get; set; }
        [SerializationPropertyName("done")]
        public bool Done { get; set; }
        [SerializationPropertyName("bits")]
        public List<int> Bits { get; set; }
    }
}
