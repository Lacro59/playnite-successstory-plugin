using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.GuildWars2
{
    public class GW2Achievements
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }
        [SerializationPropertyName("name")]
        public string Name { get; set; }
        [SerializationPropertyName("description")]
        public string Description { get; set; }
        [SerializationPropertyName("requirement")]
        public string Requirement { get; set; }
        [SerializationPropertyName("locked_text")]
        public string LockedText { get; set; }
        [SerializationPropertyName("type")]
        public string Type { get; set; }
        [SerializationPropertyName("flags")]
        public List<string> Flags { get; set; }
        [SerializationPropertyName("tiers")]
        public List<GW2Tier> Tiers { get; set; }
        [SerializationPropertyName("rewards")]
        public List<GW2Reward> Rewards { get; set; }
        [SerializationPropertyName("bits")]
        public List<Bit> Bits { get; set; }
    }

    public class GW2Tier
    {
        [SerializationPropertyName("count")]
        public int Count { get; set; }
        [SerializationPropertyName("points")]
        public int Points { get; set; }
    }

    public class GW2Reward
    {
        [SerializationPropertyName("type")]
        public string Type { get; set; }
        [SerializationPropertyName("id")]
        public int Id { get; set; }
    }

    public class Bit
    {
        [SerializationPropertyName("type")]
        public string Type { get; set; }
        [SerializationPropertyName("text")]
        public string Text { get; set; }
    }
}
