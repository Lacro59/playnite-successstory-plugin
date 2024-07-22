using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Playnite.SDK.Data;

namespace SuccessStory.Models.GenshinImpact
{
    public class Data
    {
        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("achievements")]
        public List<object> Achievements { get; set; }

        [SerializationPropertyName("order")]
        public int Order { get; set; }
    }

    public class GenshinAchievement
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("desc")]
        public string Desc { get; set; }

        [SerializationPropertyName("reward")]
        public int Reward { get; set; }

        [SerializationPropertyName("ver")]
        public string Ver { get; set; }

        [SerializationPropertyName("commissions")]
        public string Commissions { get; set; }

        [SerializationPropertyName("quest")]
        public Quest Quest { get; set; }

        [SerializationPropertyName("checklist")]
        public List<string> Checklist { get; set; }
    }

    public class Quest
    {
        [SerializationPropertyName("id")]
        public List<string> Id { get; set; }

        [SerializationPropertyName("name")]
        public List<string> Name { get; set; }
    }
}
